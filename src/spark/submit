#!/bin/bash

spark-submit --jars  $SPARK_CONNECTOR_JAR --driver-class-path $SPARK_CONNECTOR_JAR  \
        --conf spark.cassandra.connection.host=127.0.0.1 \
        --executor-memory 12G \
        --master local[2] $1
