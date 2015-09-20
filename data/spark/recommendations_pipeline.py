from pyspark import SparkContext
from pyspark.ml.recommendation import ALS
from pyspark.sql import SQLContext, Row
from pyspark.sql.functions import lit, col

from pyspark.sql import types
from uuid import UUID

# patch to fix pyspark UUID support
class UUIDType(types.AtomicType):
    pass

types.UUIDType = UUIDType
types._type_mappings[UUID] = UUIDType
types._atomic_types.append(UUIDType)
types._all_atomic_types = dict((t.typeName(), t) for t in types._atomic_types)


sc = SparkContext()
sql = SQLContext(sc)

ratings = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="killrvideo", table="video_ratings_by_user")
ratings = ratings.select(ratings.userid.cast("string").alias("userid"), 
                         ratings.videoid.cast("string").alias("videoid"),
                         "rating")
                         
# the ML libraries require integers, so we need to create keys for the users & videos temporarily
user_ids = ratings.select("userid").distinct().rdd.zipWithUniqueId()
user_map = user_ids.map(lambda (x, y): Row(userid=x.userid, userid_int=y)).toDF().cache()

# same as above - this is a UUID/int mapping
video_ids = ratings.select("videoid").distinct().rdd.zipWithUniqueId().cache()
video_map = video_ids.map(lambda (x, y): Row(videoid=x.videoid, videoid_int=y)).toDF().cache()

training_data = ratings.join(user_map, ratings.userid == user_map.userid).\
                    join(video_map, ratings.videoid == video_map.videoid).\
                    select(user_map.userid, user_map.userid_int, video_map.videoid, video_map.videoid_int, "rating")

# Create ALS transformer and train with the ratings from our C* table
als = ALS(rank=10, maxIter=10).setUserCol("userid_int").setItemCol("videoid_int").setRatingCol("rating")
model = als.fit(training_data)

users = user_map.collect()
user_map.unpersist()
count = 0
length = len(users)
for user in users:
    videos_and_user = video_map.withColumn("userid", lit(user.userid)).\
                            withColumn("userid_int", lit(user.userid_int))

    model.transform(videos_and_user).\
        sort("prediction", ascending=False).limit(30).\
        select("videoid", "userid", col("prediction").alias("rating")).\
        write.format("org.apache.spark.sql.cassandra").\
        options(keyspace="killrvideo", table="video_recommendations_by_video").\
        save(mode="append")
        
    count += 1
    print "{} ({}/{})".format(user.userid, count, length)

video_map.unpersist()

# We need to include video catalog information with the recommendations (video name, added date, etc)
# so let's find videos in the table we just wrote to without that catalog information
recs_by_video = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="killrvideo", table="video_recommendations_by_video")

# Ideally we wouldn't need to include userid since we're just saving static column values below, 
# but save doesn't work unless a userid column is present
missing_catalog_info = recs_by_video.select(recs_by_video.videoid.cast("string").alias("videoid"), recs_by_video.name, 
                                            recs_by_video.userid.cast("string").alias("userid")).\
                        dropDuplicates(["videoid"]).\
                        where("name IS NULL").cache()
                        
# Write the catalog information that's missing
video_catalog = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="killrvideo", table="videos")
video_catalog = video_catalog.select(video_catalog.videoid.cast("string").alias("videoid"),
                                     video_catalog.userid.cast("string").alias("authorid"),
                                     video_catalog.added_date, video_catalog.name, video_catalog.preview_image_location)

missing_catalog_info.join(video_catalog, video_catalog.videoid == missing_catalog_info.videoid).\
    select(missing_catalog_info.videoid, missing_catalog_info.userid, video_catalog.authorid, video_catalog.added_date, 
           video_catalog.name, video_catalog.preview_image_location).\
    write.format("org.apache.spark.sql.cassandra").\
    options(keyspace="killrvideo", table="video_recommendations_by_video").\
    save(mode="append")
    
# Now take the recommendations by video and copy them to a table keyed by user so we can look recs up by user
recs_by_video.write.format("org.apache.spark.sql.cassandra").\
    options(keyspace="killrvideo", table="video_recommendations").\
    save(mode="append")

