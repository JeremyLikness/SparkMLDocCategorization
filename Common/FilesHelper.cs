// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Common
{
    /// <summary>
    /// Helper class for I/O.
    /// </summary>
    public class FilesHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilesHelper"/> class.
        /// </summary>
        /// <param name="sessionTag">Unique identifier for a workflow. Makes a unique folder.</param>
        /// <param name="trainingRepo">The path to the repo to train from.</param>
        /// <param name="cache">The path to the cache (defaults to user local data).</param>
        public FilesHelper(int sessionTag, string trainingRepo = null, string cache = null)
        {
            PathToTrainingRepo = trainingRepo;

            SessionTag = sessionTag;

            if (!string.IsNullOrWhiteSpace(PathToTrainingRepo))
            {
                if (!Directory.Exists(PathToTrainingRepo))
                {
                    throw new ArgumentException($"Invalid path: {PathToTrainingRepo}");
                }
            }

            if (string.IsNullOrWhiteSpace(cache))
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                PathToCache = $"{path}\\SparkMLDocCategorization\\session{sessionTag}";
            }
            else
            {
                PathToCache = cache;
            }

            EnsureExists(PathToCache);

            FeaturesFile = Path.Combine(PathToCache, "spark-features.json");
            TempDataFile = Path.Combine(PathToCache, "spark-docs-input.csv");
            ModelTrainingFile = Path.Combine(PathToCache, "spark-model-input.csv");
            TrainedModel = Path.Combine(PathToCache, "ml-clustering-model.zip");
            CategorizedList = Path.Combine(PathToCache, "categorized.csv");
            SummaryText = Path.Combine(PathToCache, "summary.txt");
        }

        /// <summary>
        /// Gets the session tag the helper is scoped to.
        /// </summary>
        public int SessionTag { get; private set; }

        /// <summary>
        /// Gets the name of the temporary data file for spark processing.
        /// </summary>
        public string TempDataFile { get; private set; }

        /// <summary>
        /// Gets the features file for spark processing.
        /// </summary>
        public string FeaturesFile { get; private set; }

        /// <summary>
        /// Gets the name of the file for training the ML.NET model.
        /// </summary>
        public string ModelTrainingFile { get; private set; }

        /// <summary>
        /// Gets the name of the file for saving the trained ML model.
        /// </summary>
        public string TrainedModel { get; private set; }

        /// <summary>
        /// Gets the path to the training repo.
        /// </summary>
        public string PathToTrainingRepo { get; private set; }

        /// <summary>
        /// Gets the path to the categorized list.
        /// </summary>
        public string CategorizedList { get; private set; }

        /// <summary>
        /// Gets the path to the summary text.
        /// </summary>
        public string SummaryText { get; private set; }

        /// <summary>
        /// Gets the path to the cache.
        /// </summary>
        public string PathToCache { get; private set; }

        /// <summary>
        /// Determine whether a file exists.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A value that indicates whether it exists.</returns>
        public bool FileExists(string path) => File.Exists(path);

        /// <summary>
        /// Stream the training model to disk.
        /// </summary>
        /// <param name="action">The action to pass the stream to.</param>
        public void StreamModelToDisk(Action<Stream> action)
        {
            using (var fileStream = new FileStream(TrainedModel, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                action(fileStream);
            }
        }

        /// <summary>
        /// Stream the training model from disk.
        /// </summary>
        /// <param name="action">The action to pass the stream to.</param>
        public void StreamModelFromDisk(Action<Stream> action)
        {
            using (var stream = File.OpenRead(TrainedModel))
            {
                action(stream);
            }
        }

        /// <summary>
        /// Ensure that the directory exists. Will create it if not.
        /// </summary>
        /// <param name="dir">The path to the directory.</param>
        public void EnsureExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Appends the passed strings to the file.
        /// </summary>
        /// <param name="pathToFile">The path to the file.</param>
        /// <param name="lines">The lines of text to pass.</param>
        public void AppendToFile(string pathToFile, params string[] lines)
        {
            if (lines.Length == 1)
            {
                File.AppendAllText(pathToFile, lines[0]);
            }
            else
            {
                File.AppendAllLines(pathToFile, lines);
            }
        }

        /// <summary>
        /// Gets the count of items in the model training file.
        /// </summary>
        /// <returns>The count of items.</returns>
        public int GetModelInputCount() =>
            File.ReadAllLines(ModelTrainingFile).Count();

        /// <summary>
        /// Writes a processed document to the cache.
        /// </summary>
        /// <param name="parse">The <see cref="FileDataParse"/> to persist.</param>
        public void WriteToTempFiles(FileDataParse parse) =>
            File.AppendAllText(TempDataFile, parse.TempData);

        /// <summary>
        /// Starts a new session and overwrites the existing temp file.
        /// </summary>
        public void NewTempSession() =>
            File.WriteAllText(TempDataFile, default(FileDataParse).TempHeader);

        /// <summary>
        /// Starts a new session and overwrites the existing model file.
        /// </summary>
        public void NewModelSession() =>
            File.WriteAllText(ModelTrainingFile, default(FileDataParse).ModelHeader);

        /// <summary>
        /// Starts a new session and overwrites the existing model file.
        /// </summary>
        public void NewPredictionSession() =>
            File.WriteAllText(CategorizedList, new FileDataLabel().Headers);

        /// <summary>
        /// Writes out the summary of auto-categorized documents.
        /// </summary>
        /// <param name="summaryText">The text to write.</param>
        public void WriteCategorySummary(IEnumerable<string> summaryText) =>
            File.WriteAllLines(SummaryText, summaryText);

        /// <summary>
        /// Recurses the files with a conditional filter. Returns the
        /// level and the filename.
        /// </summary>
        /// <param name="root">The root directory to begin traversal.</param>
        /// <param name="filter">The filter for files.</param>
        /// <param name="level">The level.</param>
        /// <param name="top">The top directory.</param>
        /// <returns>The list of recursed files with level.</returns>
        public IEnumerable<(int level, string file, string fileName)> RecurseFiles(
            string root,
            Func<string, bool> filter = null,
            int level = 1,
            string top = null)
        {
            Extensions.CheckNotNull(root, nameof(root));
            filter = filter ?? (str => true);

            string FileName(string file)
            {
                var fileOnly = file.Contains("\\") ? file.Split('\\')
                    : file.Split('/');
                var baseFileName = fileOnly[fileOnly.Length - 1];
                if (top == null)
                {
                    return baseFileName;
                }

                var start = root.IndexOf(top) + top.Length + 1;
                var relative = root.Substring(start);
                return $"{relative.Replace("\\", "-").Replace("/", "-")}-{baseFileName}";
            }

            if (!Directory.Exists(root))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(root))
            {
                if (filter(file))
                {
                    yield return (level, file, FileName(file));
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var info = new DirectoryInfo(dir);
                if (info.Name.StartsWith("."))
                {
                    continue;
                }

                foreach (var subFile in RecurseFiles(dir, filter, level + 1, top ?? root))
                {
                    yield return subFile;
                }
            }
        }

        /// <summary>
        /// Reads a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The file contents, or <c>null</c> if the file doesn't exist.</returns>
        public string ReadFile(string path)
        {
            Extensions.CheckNotNull(path, nameof(path));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
