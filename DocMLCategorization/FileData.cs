// Licensed under the MIT License. See LICENSE in the repository root for license information.

using Microsoft.ML.Data;

namespace DocMLCategorization
{
    /// <summary>
    /// Class to hold model data.
    /// </summary>
    public class FileData
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        [LoadColumn(0)]
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [LoadColumn(1)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the 1st subtitle.
        /// </summary>
        [LoadColumn(2)]
        public string Subtitle1 { get; set; }

        /// <summary>
        /// Gets or sets the 2nd subtitle.
        /// </summary>
        [LoadColumn(3)]
        public string Subtitle2 { get; set; }

        /// <summary>
        /// Gets or sets the 3rd subtitle.
        /// </summary>
        [LoadColumn(4)]
        public string Subtitle3 { get; set; }

        /// <summary>
        /// Gets or sets the 4th subtitle.
        /// </summary>
        [LoadColumn(5)]
        public string Subtitle4 { get; set; }

        /// <summary>
        /// Gets or sets the 5th subtitle.
        /// </summary>
        [LoadColumn(6)]
        public string Subtitle5 { get; set; }

        /// <summary>
        /// Gets or sets the top 20 words.
        /// </summary>
        [LoadColumn(7)]
        public string Top20Words { get; set; }

        /// <summary>
        /// Gets or sets the word count.
        /// </summary>
        [LoadColumn(8)]
        public int WordCount { get; set; }

        /// <summary>
        /// Gets or sets the reading time.
        /// </summary>
        [LoadColumn(9)]
        public string ReadingTime { get; set; }
    }
}
