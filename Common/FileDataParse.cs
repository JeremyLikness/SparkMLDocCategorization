// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System.Linq;

namespace Common
{
    /// <summary>
    /// For parsing file data.
    /// </summary>
    public struct FileDataParse
    {
        private int idx;

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the 1st subtitle.
        /// </summary>
        public string Subtitle1 { get; set; }

        /// <summary>
        /// Gets or sets the 2nd subtitle.
        /// </summary>
        public string Subtitle2 { get; set; }

        /// <summary>
        /// Gets or sets the 3rd subtitle.
        /// </summary>
        public string Subtitle3 { get; set; }

        /// <summary>
        /// Gets or sets the 4th subtitle.
        /// </summary>
        public string Subtitle4 { get; set; }

        /// <summary>
        /// Gets or sets the 5th subtitle.
        /// </summary>
        public string Subtitle5 { get; set; }

        /// <summary>
        /// Gets or sets the word count.
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// Gets or sets the reading time.
        /// </summary>
        public string ReadingTime { get; set; }

        /// <summary>
        /// Gets or sets the top 20 words.
        /// </summary>
        public string Top20Words { get; set; }

        /// <summary>
        /// Gets or sets the predicted label.
        /// </summary>
        public uint PredictedLabel { get; set; }

        /// <summary>
        /// Gets or sets te score.
        /// </summary>
        public float[] Score { get; set; }

        /// <summary>
        /// Gets or sets all words in the document.
        /// </summary>
        public string Words { get; set; }

        /// <summary>
        /// Gets the comma-delimited header for for the temporary document processed by Spark.
        /// </summary>
        public string TempHeader => Row(TempHeaders());

        /// <summary>
        /// Gets the comma-delimited row of data for the temporary document
        /// processed by Spark.
        /// </summary>
        public string TempData => Row(TempDatas());

        /// <summary>
        /// Gets the comma-delimited header for the generated model training file.
        /// </summary>
        public string ModelHeader => Row(ModelHeaders());

        /// <summary>
        /// Gets the command-delimited row for the generated model training file.
        /// </summary>
        public string ModelData => Row(ModelDatas());

        /// <summary>
        /// Group by columns for Spark processing.
        /// </summary>
        /// <returns>The list of column names.</returns>
        public string[] GroupBy() =>
            new[]
            {
                nameof(File),
                nameof(Title),
                nameof(Subtitle1),
                nameof(Subtitle2),
                nameof(Subtitle3),
                nameof(Subtitle4),
                nameof(Subtitle5),
            };

        /// <summary>
        /// Addes a heading. Fills down from <see cref="Title"/> through <see cref="Subtitle5"/>.
        /// </summary>
        /// <param name="heading">The heading to add.</param>
        public void AddHeading(string heading)
        {
            if (idx > 5)
            {
                return;
            }

            heading = heading.Trim().Replace("\"", string.Empty);
            if (heading.Length < 5)
            {
                return;
            }

            switch (idx++)
            {
                case 0:
                    Title = heading;
                    break;
                case 1:
                    Subtitle1 = heading;
                    break;
                case 2:
                    Subtitle2 = heading;
                    break;
                case 3:
                    Subtitle3 = heading;
                    break;
                case 4:
                    Subtitle4 = heading;
                    break;
                case 5:
                    Subtitle5 = heading;
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Quotes text.
        /// </summary>
        /// <param name="text">The text to quote.</param>
        /// <returns>The quoted text.</returns>
        private string Quote(string text) => $"\"{text}\"";

        /// <summary>
        /// Returns a row based on columns.
        /// </summary>
        /// <param name="columns">The colums to emit.</param>
        /// <returns>The comma-delimited row.</returns>
        private string Row(string[] columns) => $"{string.Join(",", columns)}\r\n";

        /// <summary>
        /// The temporary headers for Spark processing.
        /// </summary>
        /// <returns>The list of headers.</returns>
        private string[] TempHeaders() =>
            GroupBy().Append(
                nameof(Words)).Select(h => $"\"{h}\"").ToArray();

        /// <summary>
        /// The row of data for Spark processing.
        /// </summary>
        /// <returns>The list of data.</returns>
        private string[] TempDatas() =>
            new[]
            {
                Quote(File),
                Quote(Title),
                Quote(Subtitle1),
                Quote(Subtitle2),
                Quote(Subtitle3),
                Quote(Subtitle4),
                Quote(Subtitle5),
                Quote(Words),
            };

        /// <summary>
        /// The row of headers for model training.
        /// </summary>
        /// <returns>The list of headers.</returns>
        private string[] ModelHeaders() =>
            GroupBy().Concat(new[]
            {
                nameof(Top20Words),
                nameof(WordCount),
                nameof(ReadingTime),
                nameof(Words),
            }).Select(h => $"\"{h}\"").ToArray();

        /// <summary>
        /// The row of data for input into the model training.
        /// </summary>
        /// <returns>The data row.</returns>
        private string[] ModelDatas() =>
            new[]
            {
                Quote(File),
                Quote(Title),
                Quote(Subtitle1),
                Quote(Subtitle2),
                Quote(Subtitle3),
                Quote(Subtitle4),
                Quote(Subtitle5),
                Quote(Top20Words),
                WordCount.ToString(),
                Quote(ReadingTime),
                Quote(Words),
            };
    }
}
