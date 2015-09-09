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
ratings = ratings.select(ratings.userid.cast("string").alias("userid"),
                   ratings.videoid.cast("string").alias("videoid"),
                   ratings.rating).cache()

# the ML libraries require integers, so we need to create keys for the users & videos temporarily
user_ids = ratings.rdd.zipWithUniqueId()
user_map = user_ids.map(lambda (x, y): Row(u=x.userid, iu=y)).toDF().cache()

video_ids = ratings.distinct().rdd.zipWithUniqueId().cache()
video_map = video_ids.map(lambda (x, y): Row(v=x.videoid, iv=y)).toDF().cache()

training_data = ratings.join(video_map, ratings.videoid == video_map.v).\
                join(user_map, ratings.userid == user_map.u).\
                select(video_map.iv.alias('item'),
                       user_map.iu.alias('user'),
                       "rating")

model = ALS.train(training_data, 10)

# recommendations = ALS(rank=10, userCol="u", itemCol="v")
# model = recommendations.fit(training_data)
# model = ALS.train(training_data, 10)

# model.userFactors

# ratings.join(video_ids, "video", "v", "videoid")
# print user_map

# def get_recs(user):
#     # recs = model.recommendProducts(user.userid, 20)
#     # persist to db
#     print "Saving recommendations for " + user.userid
#
# users.foreach(get_recs)
