// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Linq;
using Common;
using Microsoft.ML;

namespace DocMLCategorization
{
    internal class Program
    {
        /// <summary>
        /// The files helper.
        /// </summary>
        private static FilesHelper filesHelper;

        /// <summary>
        /// Current mode.
        /// </summary>
        private static Modes mode;

        /// <summary>
        /// Modes to run with.
        /// </summary>
        private enum Modes
        {
            Train,
            Predict,
            TrainAndPredict,
        }

        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">Arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("Doc ML Categorization.");
            Console.WriteLine("Predicts categories documents should belong to.");
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Usage:");
                Console.Write($"dotnet run {typeof(Program).Assembly.Location.Split("\\")[^1]}");
                Console.Write(" <sessionTag> <train|predict|trainandpredict> [path-to-cache]");
                return;
            }

            if (!int.TryParse(args[0], out var tag))
            {
                Console.WriteLine($"Session tag must be an integer! Value {args[0]} is invalid!");
            }

            if (Enum.TryParse(args[1].Trim(), ignoreCase: true, out Modes result))
            {
                mode = result;
            }
            else
            {
                Console.WriteLine($"Invalid mode passed as second argument: {args[1].Trim()}");
                Console.WriteLine("Valid modes are: train, predict, or trainandpredict.");
                return;
            }

            if (args.Length == 2)
            {
                filesHelper = new FilesHelper(tag);
            }
            else
            {
                filesHelper = new FilesHelper(tag, cache: args[2]);
            }

            Console.WriteLine($"Initialized cache to {filesHelper.PathToCache}");

            if (mode == Modes.Train || mode == Modes.TrainAndPredict)
            {
                Train();
            }

            if (mode == Modes.Predict || mode == Modes.TrainAndPredict)
            {
                Predict();
            }
        }

        /// <summary>
        /// Train and save the model.
        /// </summary>
        private static void Train()
        {
            string features = nameof(features);

            var context = new MLContext(seed: 0);

            // load the data
            var dataToTrain = context.Data.LoadFromTextFile<FileData>(
                path: filesHelper.ModelTrainingFile,
                hasHeader: true,
                allowQuoting: true,
                separatorChar: ',');

            // setup options
            var options = new Microsoft.ML.Trainers.KMeansTrainer.Options
            {
                FeatureColumnName = features,
                NumberOfClusters = 20,
            };

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
                    nameof(FileData.Top20Words).Featurized()))
                .Append(context.Clustering.Trainers.KMeans(options));

            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(dataToTrain);
            Console.WriteLine("Trained!");

            filesHelper.StreamModelToDisk(stream => context.Model.Save(model, dataToTrain.Schema, stream));
            Console.WriteLine($"Model saved to {filesHelper.TrainedModel}");
        }

        /// <summary>
        /// Run predictions.
        /// </summary>
        private static void Predict()
        {
            var context = new MLContext(seed: 0);

            ITransformer trainedModel = null;

            filesHelper.StreamModelFromDisk(stream =>
                trainedModel = context.Model.Load(stream, out var modelSchema));
            Console.WriteLine($"Model loaded from {filesHelper.TrainedModel}");

            var predictionEngine = context.Model.CreatePredictionEngine<FileData, ClusterPrediction>(trainedModel);

            Console.WriteLine("Loading documents for prediction...");

            IDataView data = context.Data.LoadFromTextFile<FileData>(
                filesHelper.ModelTrainingFile,
                separatorChar: ',',
                hasHeader: true);

            Console.WriteLine($"Loaded.");

            Console.WriteLine("Running predictions...");
            var predictions = trainedModel.Transform(data);
            Console.WriteLine("Iterating predictions...");

            var rows = context.Data.CreateEnumerable<FileDataLabel>(predictions, reuseRowObject: false);

            filesHelper.NewPredictionSession();

            var progress = new ProgressHelper(TimeSpan.FromSeconds(5), Console.Write);

            foreach (var row in rows)
            {
                progress.Increment();
                filesHelper.AppendToFile(filesHelper.CategorizedList, row.Data);
            }

            Console.WriteLine();
            Console.WriteLine($"Done. Wrote predicted categories to {filesHelper.CategorizedList}");
        }
    }
}
