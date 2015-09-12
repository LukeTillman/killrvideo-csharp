[CmdletBinding()]
Param(
   [Parameter(Mandatory=$True, Position=1)]
   [string]$job,
   
   [Parameter()]
   [string]$master = 'local[2]',
	
   [Parameter()]
   [string]$cassandraHost = '127.0.0.1',
   
   [Parameter()]
   [string]$sparkConnectorJar = $env:SPARK_CONNECTOR_JAR
)

$hadoopHome = $env:HADOOP_HOME

# Temporary hack just for Windows until https://issues.apache.org/jira/browse/SPARK-2356 is addressed
if (!$hadoopHome) {
    $binDir = ".\bin"
    md -Force $binDir
    
    $winutils = Join-Path $binDir 'winutils.exe'
    if (!(Test-Path $winutils)) {
        # Grab winutils from HW repo, even though this job shouldn't touch anything Hadoop
        wget -outf $winutils http://public-repo-1.hortonworks.com/hdp-win-alpha/winutils.exe
    }
    
    $env:HADOOP_HOME = Resolve-Path .\
}

# Submit the spark job
spark-submit --jars  $sparkConnectorJar --driver-class-path $sparkConnectorJar  `
        --conf spark.cassandra.connection.host=$cassandraHost `
        --executor-memory 6G --master $master $job

# Restore Hadoop Home
$env:HADOOP_HOME = $hadoopHome