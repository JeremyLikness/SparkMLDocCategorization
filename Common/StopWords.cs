// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System.Collections.Generic;
using System.IO;

namespace Common
{
    /// <summary>
    /// Provides the list of stopwords for parsing.
    /// </summary>
    public class StopWords
    {
        /// <summary>
        /// Internal list.
        /// </summary>
        private readonly List<string> stopwords = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StopWords"/> class.
        /// Loads the stop words.
        /// </summary>
        public StopWords()
        {
            var resourceName = $"{typeof(StopWords).Namespace}.stopwords.txt";
            using (var stream = typeof(StopWords).Assembly.GetManifestResourceStream(resourceName))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var stopWords = streamReader.ReadToEnd();
                    stopwords.AddRange(stopWords.Split(','));
                }
            }
        }

        /// <summary>
        /// Gets the stop word list.
        /// </summary>
        public IReadOnlyCollection<string> List => stopwords.AsReadOnly();
    }
}
