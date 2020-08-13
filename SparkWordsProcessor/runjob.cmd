:: submits and runs the job to Spark

%SPARK_HOME%\bin\spark-submit --class org.apache.spark.deploy.dotnet.DotnetRunner^
 --master local microsoft-spark-2.4.x-0.12.1.jar^
 dotnet SparkWordsProcessor.dll %1 %2