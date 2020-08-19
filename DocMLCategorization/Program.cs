// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace DocMLCategorization
{
    internal class Program
    {
        /// <summary>
        /// The files helper.
        /// </summary>
        private static FilesHelper filesHelper;

        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">Arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("Doc ML Categorization.");
            Console.WriteLine("Predicts categories documents should belong to.");
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage:");
                Console.Write($"dotnet run {typeof(Program).Assembly.Location.Split("\\")[^1]}");
                Console.Write(" <sessionTag> [path-to-cache]");
                return;
            }

            if (!int.TryParse(args[0], out var tag))
            {
                Console.WriteLine($"Session tag must be an integer! Value {args[0]} is invalid!");
            }

            if (args.Length == 1)
            {
                filesHelper = new FilesHelper(tag);
            }
            else
            {
                filesHelper = new FilesHelper(tag, cache: args[1]);
            }

            Console.WriteLine($"Initialized cache to {filesHelper.PathToCache}");

            Train();
        }

        /// <summary>
        /// Train and save the model.
        /// </summary>
        private static void Train()
        {
            string features = nameof(features);
            const int lowCategory = 2;
            const int highCategory = 20;

            var context = new MLContext(seed: 0);

            // load the data
            var dataToTrain = context.Data.LoadFromTextFile<FileData>(
                path: filesHelper.ModelTrainingFile,
                hasHeader: true,
                allowQuoting: true,
                separatorChar: ',');

            // turn the data in vectors that represent the feature
            var pipeline = context.Transforms
                .Text.FeaturizeText(nameof(FileData.Title).Featurized(), nameof(FileData.Title))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Subtitle1).Featurized(), nameof(FileData.Subtitle1)))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Subtitle2).Featurized(), nameof(FileData.Subtitle2)))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Subtitle3).Featurized(), nameof(FileData.Subtitle3)))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Subtitle4).Featurized(), nameof(FileData.Subtitle4)))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Subtitle5).Featurized(), nameof(FileData.Subtitle5)))
                .Append(context.Transforms.Text.FeaturizeText(nameof(FileData.Top20Words).Featurized(), nameof(FileData.Top20Words)))
                .Append(context.Transforms.Concatenate(
                    features,
                    nameof(FileData.Title).Featurized(),
                    nameof(FileData.Subtitle1).Featurized(),
                    nameof(FileData.Subtitle2).Featurized(),
                    nameof(FileData.Subtitle3).Featurized(),
                    nameof(FileData.Subtitle4).Featurized(),
                    nameof(FileData.Subtitle5).Featurized(),
                    nameof(FileData.Top20Words).Featurized()));

            var distances = new Dictionary<int, double>();

            for (var categories = lowCategory; categories <= highCategory; categories += 1)
            {
                Console.WriteLine($"Testing for {categories} categories...");

                var options = new KMeansTrainer.Options
                {
                    FeatureColumnName = features,
                    NumberOfClusters = categories,
                };

                var clusterPipeline = pipeline.Append(context.Clustering.Trainers.KMeans(options));

                Console.WriteLine("Training the model...");
                var model = clusterPipeline.Fit(dataToTrain);
                Console.WriteLine("Trained!");

                Console.WriteLine("Testing predictions...");
                var predictions = model.Transform(dataToTrain);
                var metrics = context.Clustering.Evaluate(predictions);
                Console.WriteLine($"Average distance for {categories} is {metrics.AverageDistance}");
                distances.Add(categories, metrics.AverageDistance);
            }

            var categoriesToUse = distances.OrderBy(d => d.Value).First().Key;

            Console.WriteLine($"Optimal categories is {categoriesToUse}");

            var finalOptions = new KMeansTrainer.Options
            {
                FeatureColumnName = features,
                NumberOfClusters = categoriesToUse,
            };

            var optimalClusterPipeline = pipeline.Append(context.Clustering.Trainers.KMeans(finalOptions));

            Console.WriteLine("Training the optimal model...");
            var optimalModel = optimalClusterPipeline.Fit(dataToTrain);

            Console.WriteLine("Running predictions...");
            var optimalPredictions = optimalModel.Transform(dataToTrain);
            Console.WriteLine("Iterating predictions...");

            var finalRows = context.Data.CreateEnumerable<FileDataLabel>(optimalPredictions, reuseRowObject: false);

            var categoryMatrix = new CategoryMatrix();

            filesHelper.NewPredictionSession();

            var totalInputs = filesHelper.GetModelInputCount();

            var progress = new ProgressHelper(totalInputs, Console.Write);

            var summary = new Dictionary<uint, List<string>>();

            foreach (var row in finalRows)
            {
                progress.Increment();
                if (!summary.ContainsKey(row.PredictedLabel))
                {
                    summary.Add(row.PredictedLabel, new List<string>());
                }

                summary[row.PredictedLabel].Add(row.Title);

                var wordMatrix = categoryMatrix[row.PredictedLabel];

                wordMatrix.ParseWords(row.Top20Words);
                wordMatrix.ParseWords(row.Title, true);

                filesHelper.AppendToFile(filesHelper.CategorizedList, row.Data);
            }

            var summaryText = new List<string>();

            foreach (var category in summary.Keys.OrderBy(k => k))
            {
                summaryText.Add($"Category {category}: {categoryMatrix.GetCategoryTitle(category)}");
                foreach (var title in summary[category].OrderBy(t => t))
                {
                    summaryText.Add($"\t{title}");
                }
            }

            filesHelper.WriteCategorySummary(summaryText);

            Console.WriteLine();
            Console.WriteLine($"Done. Wrote predicted categories to {filesHelper.CategorizedList}");
            Console.WriteLine($"Wrote summary to {filesHelper.SummaryText}");
        }
    }
}
