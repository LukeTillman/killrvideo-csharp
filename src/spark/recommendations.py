from pyspark import SparkContext
from pyspark.mllib.recommendation import ALS, MatrixFactorizationModel, Rating
from pyspark.sql import SQLContext
from pyspark.sql import types
from uuid import UUID

class UUIDType(types.AtomicType):
    pass

import sys

print sys.path
print sys.executable

types.UUIDType = UUIDType
types._type_mappings[UUID] = UUIDType
types._atomic_types.append(UUIDType)
types._all_atomic_types = dict((t.typeName(), t) for t in types._atomic_types)


sc = SparkContext()
sql = SQLContext(sc)

ratings = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="killrvideo", table="video_ratings_by_user")

training_data = ratings.select(ratings.videoid.cast("string").alias("product"),
                               ratings.userid.cast("string").alias("user"),
                               ratings.rating)

users = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="killrvideo", table="users")

model = ALS.train(training_data, 10)

def get_recs(user):
    recs = model.recommendProducts(user.userid, 20)
    # persist to db
    print "Saving recommendations for " + user.userid

users.foreach(get_recs)
