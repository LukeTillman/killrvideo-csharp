Submitting jobs on open source Spark requires the connector to be present on the machine.

For open source:
Ensure you've set the $SPARK_CONNECTOR_JAR to the location of the connector.
Submit jobs using `./submit job-name`

DataStax Enterprise:
use dse-spark-submit job-name, no additional configuration required.
