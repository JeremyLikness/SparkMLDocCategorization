// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System.Linq;

namespace Common
{
    /// <summary>
    /// Class with label and score.
    /// </summary>
    public class FileDataLabel
    {
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
        /// Gets or sets the score.
        /// </summary>
        public float[] Score { get; set; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        public string Headers =>
            Row(string.Join(",", new[]
            {
                nameof(File),
                nameof(Title),
                nameof(Subtitle1),
                nameof(Subtitle2),
                nameof(Subtitle3),
                nameof(Subtitle4),
                nameof(Subtitle5),
                nameof(WordCount),
                nameof(ReadingTime),
                nameof(Top20Words),
                nameof(PredictedLabel),
            }.Select(h => $"\"{h}\"")
                .ToArray()));

        /// <summary>
        /// Gets the row data.
        /// </summary>
        public string Data =>
            Row(string.Join(
                ",",
                new[]
                {
                    File,
                    Title,
                    Subtitle1,
                    Subtitle2,
                    Subtitle3,
                    Subtitle4,
                    Subtitle5,
                }.Select(val => val.StartsWith("\"") ? val : $"\"{val}\"")
                .Append(WordCount.ToString())
                .Append(ReadingTime.StartsWith("\"") ? ReadingTime : $"\"{ReadingTime}\"")
                .Append(Top20Words.StartsWith("\"") ? Top20Words : $"\"{Top20Words}\"")
                .Append(PredictedLabel.ToString())
                .ToArray()));

        /// <summary>
        /// Wraps the row with a newline.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <returns>The source with a new line.</returns>
        private string Row(string src) => $"{src}\r\n";
    }
}
