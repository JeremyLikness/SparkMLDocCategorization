# SparkMLDocCategorization

Example of automatic categorization using .NET for Spark and ML.NET.

This project will parse a set of markdown documents, produce a file with titles and words, then
process the file using .NET for Spark to summarize word counts. It then passes the data to
ML.NET to auto-categorize similar documents.

## Prerequisites

For the .NET for Spark portion, follow [this tutorial](https://docs.microsoft.com/dotnet/spark/tutorials/get-started).

You should also have [.NET Core 3.1 installed](https://dotnet.microsoft.com/download/dotnet-core).

## Getting Started

Each flow through is identified with a unique session tag. For example, `1` might point to a
set of documents while `2` points to a different repo. You can specify a file location, but it
will default to your user local app data directory. The jobs will show the path to the files.

The `runall.cmd` in the root will step through all phases:

`runall 1 c:\source\repo`

### Build the Spark Data Source

Navigate to the `DocRepoParser` project first.

Type `dotnet run 1 "c:\source\repo"` (replace the last path with the path to your repo).

You'll see a notice that the file has been processed. There is no need to remember the full path.

### Process the Word Counts

Next, navigate to the `SparkWordsProcessor` directory. Build the project:

`dotnet build`

Navigate to the output directory (`bin/Debug/netcoreapp3.1`). You have two options:

1. Debug: run the `debugspark.cmd`, right-click project properties and put "1" in "arguments" under debug and press F5.
2. Alternative: submit the job directly by running `runjob 1` (`1` is the session tag).

### Train and Apply the Machine Learning Model

Navigate to the `DocMLCategorization` project. 

To train _and_use the model, type:

`dotnet run 1`

Open the generated file and see how well the tool categorized your documents!



