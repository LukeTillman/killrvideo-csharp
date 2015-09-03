from pyspark.mllib.recommendation import ALS, MatrixFactorizationModel, Rating
from pyspark.sql import SQLContext
sql = SQLContext(sc)

ratings = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="lens", table="ratings_by_user")
training_data = ratings.select(ratings.user_id.alias("user"), ratings.movie_id.alias("product"), "rating")

users = sql.read.format("org.apache.spark.sql.cassandra").load(keyspace="lens", table="users")

model = ALS.train(training_data, 10)

def get_recs(user):
    recs = model.recommendProducts(user.user_id, 20)
    # persist to db

users.foreach(get_recs)
