// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Linq;
using Common;
using Microsoft.Spark.Sql;

namespace SparkWordsProcessor
{
    /// <summary>
    /// Main program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Average words per minute reading time.
        /// </summary>
        private const double WordsPerMinute = 225;

        /// <summary>
        /// Number of top words.
        /// </summary>
        private const int TopWordCount = 20;

        /// <summary>
        /// Stop words.
        /// </summary>
        private static readonly StopWords StopWords = new StopWords();

        /// <summary>
        /// The files helper.
        /// </summary>
        private static FilesHelper filesHelper;

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">List of arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("Spark Words Processor");
            Console.WriteLine("Parses a file with word counts for the training model.");
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Arguments are: <sessionTag> [path-to-cache] where sessionTag is an integer.");
                return;
            }

            if (!int.TryParse(args[0], out var tag))
            {
                Console.WriteLine($"Session tag must be an integer! Value {args[0]} is invalid!");
            }

            if (args.Length == 2)
            {
                filesHelper = new FilesHelper(tag, cache: args[1]);
            }
            else
            {
                filesHelper = new FilesHelper(tag);
            }

            Console.WriteLine($"Initialized cache to {filesHelper.PathToCache}");

            if (!filesHelper.FileExists(filesHelper.TempDataFile))
            {
                Console.WriteLine($"Could not find input file: {filesHelper.TempDataFile}.");
            }

            RunJob();

            Console.WriteLine($"Successfully generated model training file to {filesHelper.ModelTrainingFile}.");
        }

        /// <summary>
        /// Runs the Spark job.
        /// </summary>
        private static void RunJob()
        {
            const string wordList = nameof(wordList);
            const string word = nameof(word);
            const string count = nameof(count);
            const string docFrequency = nameof(docFrequency);
            const string total = nameof(total);
            const string inverseDocFrequency = nameof(inverseDocFrequency);
            const string termFreq_inverseDocFreq = nameof(termFreq_inverseDocFreq);

            Console.WriteLine("Starting Spark job to analyze words...");

            var spark = SparkSession.Builder()
                .AppName(nameof(SparkWordsProcessor))
                .GetOrCreate();

            spark.SparkContext.SetLogLevel("ERROR");

            filesHelper.NewModelSession();

            // everything
            var docs = spark.Read().HasHeader().Csv(filesHelper.TempDataFile);

            docs.CreateOrReplaceTempView(nameof(docs));

            // all docs in corpus
            var totalDocs = docs.Count();

            // easy reference
            var fileCol = nameof(FileDataParse.File).AsColumn();

            // split words and group by count
            var words = docs

                // transform words into an array of words
                .Select(
                    fileCol,
                    Functions.Split(
                        nameof(FileDataParse.Words).AsColumn(), " ")
                    .Alias(wordList))

                // flatten into one row per word
                .Select(
                    fileCol,
                    Functions.Explode(
                        wordList.AsColumn())
                    .Alias(word));

            // get frequency of word per document
            var termFrequency = words

                // group by attributes of file plus word
                .GroupBy(fileCol, Functions.Lower(word.AsColumn()).Alias(word))

                // generate count
                .Count()

                // order by word count per file descending
                .OrderBy(fileCol, count.AsColumn().Desc());

            // count by word
            termFrequency.CreateOrReplaceTempView(nameof(termFrequency));

            // now count frequency of word across all documents
            var documentFrequency = words
                .GroupBy(Functions.Lower(word.AsColumn()).Alias(word))
                .Agg(Functions.CountDistinct(fileCol).Alias(docFrequency));

            documentFrequency.CreateOrReplaceTempView(nameof(documentFrequency));

            static double CalculateIdf(int docFrequency, int totalDocuments) =>
                Math.Log(totalDocuments + 1) / (docFrequency + 1);
            spark.Udf().Register<int, int, double>(nameof(CalculateIdf), CalculateIdf);

            var idfPrep = documentFrequency.Select(
                word.AsColumn(),
                docFrequency.AsColumn())
                .WithColumn(total, Functions.Lit(totalDocs))
                .WithColumn(
                inverseDocFrequency,
                Functions.CallUDF(
                    nameof(CalculateIdf),
                    docFrequency.AsColumn(),
                    total.AsColumn()));
            idfPrep.CreateOrReplaceTempView(nameof(documentFrequency));

            var idfJoin = spark.Sql($"SELECT t.File, d.word, d.{docFrequency}, d.{inverseDocFrequency}, t.count, d.{inverseDocFrequency} * t.count as {termFreq_inverseDocFreq} " +
                $"from {nameof(documentFrequency)} d inner join {nameof(termFrequency)} t " +
                " on t.word = d.word");
            idfJoin.CreateOrReplaceTempView(nameof(idfJoin));

            // get total word count for document
            var rollup = spark.Sql($"SELECT File, sum(count) as WordCount from {nameof(termFrequency)} group by File");

            // rollup
            rollup.CreateOrReplaceTempView(nameof(rollup));

            // skip stop words
            static bool IsStopWord(string val) => val.Length < 4 || StopWords.List.Contains(val);
            spark.Udf().Register<string, bool>(nameof(IsStopWord), IsStopWord);

            var filteredWords = spark.Sql($"SELECT File, word, {termFreq_inverseDocFreq} FROM {nameof(idfJoin)} WHERE NOT IsStopWord(word)");
            filteredWords.CreateOrReplaceTempView(nameof(filteredWords));

            // top 20 words that aren't stop words
            var top20 = spark.Sql(
                $"SELECT File, word, {termFreq_inverseDocFreq}, " +
                "ROW_NUMBER() OVER " +
                $"   (PARTITION BY File ORDER BY {termFreq_inverseDocFreq} DESC) As RowNumber " +
                $"FROM {nameof(filteredWords)}");

            top20.CreateOrReplaceTempView(nameof(top20));

            // main query, max 20 words per file
            var join = spark.Sql(
                "SELECT distinct d.File, d.Title, d.Subtitle1, d.Subtitle2, d.Subtitle3, d.Subtitle4, d.Subtitle5, " +
                "w.word, w.RowNumber, " +
                "t.WordCount " +
                $"from {nameof(docs)} d inner join {nameof(top20)} w on d.File = w.File " +
                $"inner join {nameof(rollup)} t on d.File = t.File " +
                "where w.RowNumber <= 20");

            var cols = new[]
            {
                fileCol,
                nameof(FileDataParse.Title).AsColumn(),
                nameof(FileDataParse.Subtitle1).AsColumn(),
                nameof(FileDataParse.Subtitle2).AsColumn(),
                nameof(FileDataParse.Subtitle3).AsColumn(),
                nameof(FileDataParse.Subtitle4).AsColumn(),
                nameof(FileDataParse.Subtitle5).AsColumn(),
                nameof(FileDataParse.WordCount).AsColumn(),
            };

            // calculate reading time
            static string CalculateReadingTime(int count)
            {
                var totalTime = count / WordsPerMinute;
                return ParseTime(totalTime);
            }

            spark.Udf().Register<int, string>(nameof(CalculateReadingTime), CalculateReadingTime);

            // roll-up words into single "top 20" field
            var final = join.GroupBy(cols)
                .Agg(Functions.CollectList(word.AsColumn()).Alias(nameof(FileDataParse.Top20Words)))
                .Select(cols
                    .Append(Functions.ConcatWs(" ", nameof(FileDataParse.Top20Words).AsColumn())
                        .Alias(nameof(FileDataParse.Top20Words)))
                    .Append(
                    Functions.CallUDF(
                        nameof(CalculateReadingTime),
                        nameof(FileDataParse.WordCount).AsColumn())
                    .Alias(nameof(FileDataParse.ReadingTime))).ToArray())
                .OrderBy(fileCol);

            Console.WriteLine("Processing data...");

            final
                .Collect()
                .Where(row => !string.IsNullOrWhiteSpace(row.GetColumnValue(f => f.Title)))
                .Select(row => new FileDataParse
                {
                    File = row.GetColumnValue(f => f.File, false),
                    Title = row.GetColumnValue(f => f.Title),
                    Subtitle1 = row.GetColumnValue(f => f.Subtitle1),
                    Subtitle2 = row.GetColumnValue(f => f.Subtitle2),
                    Subtitle3 = row.GetColumnValue(f => f.Subtitle3),
                    Subtitle4 = row.GetColumnValue(f => f.Subtitle4),
                    Subtitle5 = row.GetColumnValue(f => f.Subtitle5),
                    Top20Words = row.GetColumnValue(f => f.Top20Words),
                    WordCount = row.GetColumnValue(f => f.WordCount),
                    ReadingTime = row.GetColumnValue(f => f.ReadingTime, false),
                }).ForEach(fp => filesHelper
                    .AppendToFile(filesHelper.ModelTrainingFile, fp.ModelData));

            Console.WriteLine("Processed.");
        }

        /// <summary>
        /// Turns number into readable minutes.
        /// </summary>
        /// <param name="time">The time to read in minutes.</param>
        /// <returns>The user-friendly text.</returns>
        private static string ParseTime(double time)
        {
            if (time <= 1)
            {
                return "< 1 minute";
            }

            if (time < 60)
            {
                return $"{Math.Floor(time)} minutes";
            }

            var hours = time / 60;
            var leftOver = time % 60;

            if (leftOver < 1)
            {
                return $"{Math.Floor(hours)} hours";
            }

            return $"{Math.Floor(hours)} hours and {Math.Floor(leftOver)} minutes";
        }
    }
}
