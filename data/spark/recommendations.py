from pyspark import SparkContext
# from pyspark.ml.recommendation import ALS
from pyspark.mllib.recommendation import ALS
from pyspark.sql import SQLContext, Row
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

# we have to cast our UUIDs to strings because spark doesn't know what to do with them in UUID form
ratings = ratings.select(ratings.userid.cast("string").alias("userid"),
                   ratings.videoid.cast("string").alias("videoid"),
                   ratings.rating).cache()

# the ML libraries require integers, so we need to create keys for the users & videos temporarily
user_ids = ratings.select("userid").distinct().rdd.zipWithUniqueId()
user_map = user_ids.map(lambda (x, y): Row(userid=x.userid, userid_int=y)).toDF().cache()

# same as above - this is a UUID/int mapping
video_ids = ratings.select("videoid").distinct().rdd.zipWithUniqueId().cache()
video_map = video_ids.map(lambda (x, y): Row(videoid=x.videoid, videoid_int=y)).toDF().cache()

# lets join the mapping tables to the original data, and swap our UUIDS for the int ids
training_data = ratings.join(video_map, ratings.videoid == video_map.videoid).\
                join(user_map, ratings.userid == user_map.userid).\
                select(user_map.userid_int.alias("user"),
                       video_map.videoid_int.alias("product"),
                       "rating")

model = ALS.train(training_data, 10)

# later we're going to want to save the model and the 2 mappings
# but not just yet
# model.save(sc, "/tmp/recommendations")

# TODO only get recommendations for users who have less than 20
# sadly, we have to do this in the driver.  the model can't be distributed
for user in user_map.collect():
    products = model.recommendProducts(user.userid_int, 30)
    products = sql.createDataFrame(products)

    recs = products.join(video_map, video_map.videoid_int == products.product).\
            join(user_map, user_map.userid_int == products.user).\
            select(user_map.userid, video_map.videoid, "rating")

    recs.write.format("org.apache.spark.sql.cassandra").\
        options(keyspace="killrvideo", table="video_recommendations_by_video").\
        save(mode="append")

    print products
    userid = user.userid
    print userid

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
