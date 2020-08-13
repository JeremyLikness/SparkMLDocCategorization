// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Linq;
using Common;

namespace DocRepoParser
{
    internal class Program
    {
        private static readonly MarkdownParser MarkdownParser = new MarkdownParser();

        private static FilesHelper filesHelper;

        private static void Main(string[] args)
        {
            Console.WriteLine("Doc Repo Parser");
            Console.WriteLine("Parses a repository with markdown into an input file for Spark.");
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage:");
                Console.Write($"dotnet run {typeof(Program).Assembly.Location.Split("\\")[^1]}");
                Console.Write(" <path-to-repo> [path-to-cache]");
                return;
            }

            Console.WriteLine($"Initializing with repo path: {args[0]}");
            if (args.Length == 1)
            {
                filesHelper = new FilesHelper(trainingRepo: args[0]);
            }
            else
            {
                filesHelper = new FilesHelper(trainingRepo: args[0], cache: args[1]);
            }

            Console.WriteLine($"Initialized cache to {filesHelper.PathToCache}");

            ProcessFiles();
        }

        private static void ProcessFiles()
        {
            Console.WriteLine("Warning: the existing input file, if it exists, will be overwritten.");
            Console.WriteLine("Hit ENTER to cancel or type 'go' (without quotes) to continue.");

            if (!Console.ReadLine().ToLowerInvariant().StartsWith("go"))
            {
                return;
            }

            var files = filesHelper.RecurseFiles(
                filesHelper.PathToTrainingRepo,
                file => file.EndsWith(".md")).ToList();

            var success = 0;
            var total = files.Count();

            Console.WriteLine($"Processing {total} markdown files. Press ESC to pause.");

            var progress = new ProgressHelper(total, Console.Write);

            foreach (var file in files)
            {
                if (progress.Index == 0)
                {
                    filesHelper.NewTempSession();
                }

                progress.Increment();

                var fileOnly = file.file.Split("\\")[^1];
                var fileName = $"{file.level}-{fileOnly}";
                var fileParse = MarkdownParser.Parse(fileName, filesHelper.ReadFile(file.file));
                if (!string.IsNullOrWhiteSpace(fileParse.File) &&
                    !string.IsNullOrWhiteSpace(fileParse.Title))
                {
                    success++;
                    filesHelper.WriteToTempFiles(fileParse);
                }

                if (CheckForBreak(progress.Index, total))
                {
                    break;
                }
            }

            Console.WriteLine("!");
            Console.WriteLine($"Successfully processed {success} of {total} files.");
            Console.WriteLine($"The input data for Spark is ready at {filesHelper.TempDataFile}");
        }

        /// <summary>
        /// Check for user break.
        /// </summary>
        /// <param name="processed">Processed rows.</param>
        /// <param name="total">Total rows.</param>
        /// <returns><c>true</c> if the user wants to break.</returns>
        private static bool CheckForBreak(int processed, int total)
        {
            while (Console.KeyAvailable)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Processed {processed} of {total} files.");
                    Console.WriteLine("Paused. Hit ENTER to continue or type 'done' (without quotes) to stop processing.");
                    if (Console.ReadLine().ToLowerInvariant().StartsWith("done"))
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }
    }
}
