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
            if (args.Length > 1)
            {
                Console.WriteLine("This application only accepts one optional argument: the path to the cache.");
                return;
            }

            if (args.Length == 1)
            {
                filesHelper = new FilesHelper(cache: args[0]);
            }
            else
            {
                filesHelper = new FilesHelper();
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

            Console.WriteLine("Starting Spark job to analyze words...");

            var spark = SparkSession.Builder()
                .AppName(nameof(SparkWordsProcessor)).GetOrCreate();

            var dataFrame = spark.Read().HasHeader().Csv(filesHelper.TempDataFile);
            var columns = default(FileDataParse)
                .GroupBy()
                .Select(Functions.Col)
                .ToArray();
            var words = dataFrame

                // transform words into an array of words
                .Select(columns.Append(
                    Functions.Split(
                        nameof(FileDataParse.Words).AsColumn(), " ")
                    .Alias(wordList)).ToArray())

                // flatten into one row per word
                .Select(columns.Append(
                    Functions.Explode(
                        wordList.AsColumn())
                    .Alias(word)).ToArray())

                // group by attributes of file plus word
                .GroupBy(columns.Append(word.AsColumn()).ToArray())

                // generate count
                .Count()

                // order by word count per file descending
                .OrderBy(nameof(FileDataParse.File).AsColumn(), count.AsColumn().Desc());

            var results = words.Collect();

            Console.WriteLine($"Processing data...");

            var fileData = default(FileDataParse);
            var progress = new ProgressHelper(TimeSpan.FromSeconds(10), Console.Write);

            filesHelper.NewModelSession();

            int topWordCount = TopWordCount;

            var first = true;

            foreach (var result in results)
            {
                if (first)
                {
                    first = false;
                    Console.Write("Spark processing complete. Parsing results");
                }

                progress.Increment();

                var file = result.GetAs<string>(nameof(FileDataParse.File));
                if (fileData.File != file)
                {
                    if (!string.IsNullOrWhiteSpace(fileData.File))
                    {
                        fileData.ReadingTime = ParseTime(fileData.WordCount / WordsPerMinute);
                        filesHelper.AppendToFile(filesHelper.ModelTrainingFile, fileData.ModelData);
                    }

                    fileData = new FileDataParse
                    {
                        File = file,
                        Title = result.GetAs<string>(nameof(FileDataParse.Title)).ExtractWords().Trim(),
                        Subtitle1 = result.GetAs<string>(nameof(FileDataParse.Subtitle1)).ExtractWords().Trim(),
                        Subtitle2 = result.GetAs<string>(nameof(FileDataParse.Subtitle2)).ExtractWords().Trim(),
                        Subtitle3 = result.GetAs<string>(nameof(FileDataParse.Subtitle3)).ExtractWords().Trim(),
                        Subtitle4 = result.GetAs<string>(nameof(FileDataParse.Subtitle4)).ExtractWords().Trim(),
                        Subtitle5 = result.GetAs<string>(nameof(FileDataParse.Subtitle5)).ExtractWords().Trim(),
                        Top20Words = string.Empty,
                        WordCount = result.GetAs<int>(count),
                    };

                    topWordCount = TopWordCount;
                }
                else
                {
                    var wordCount = result.GetAs<int>(count);
                    fileData.WordCount += wordCount;
                }

                if (topWordCount > 0)
                {
                    var currentWord = result.GetAs<string>(word).Trim();
                    if (currentWord.Length > 4 && !StopWords.List.Contains(currentWord.ToLowerInvariant()))
                    {
                        fileData.Top20Words = string.IsNullOrWhiteSpace(fileData.Top20Words) ?
                            currentWord : $"{fileData.Top20Words} {currentWord}";
                        topWordCount--;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(fileData.Title))
            {
                fileData.ReadingTime = ParseTime(fileData.WordCount / WordsPerMinute);
                filesHelper.AppendToFile(filesHelper.ModelTrainingFile, fileData.ModelData);
            }

            Console.WriteLine();
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
