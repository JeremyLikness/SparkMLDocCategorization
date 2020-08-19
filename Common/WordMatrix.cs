// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Word matrix: keeps track of words and their frequency.
    /// </summary>
    public class WordMatrix
    {
        private readonly IDictionary<string, int> wordCounts =
            new Dictionary<string, int>();

        /// <summary>
        /// Gets the sorted list of words, high to low.
        /// </summary>
        public IEnumerable<(string word, int count)> WordsHighToLow =>
            wordCounts.Select(w => (w.Key, w.Value)).OrderByDescending(w => w.Value)
            .ThenByDescending(w => Weight(w.Key, w.Value));

        /// <summary>
        /// Indexer to access word count.
        /// </summary>
        /// <remarks>
        /// Automatically creates a new index and increments when set with any value.
        /// </remarks>
        /// <param name="word">The word to consider.</param>
        /// <returns>The word count.</returns>
        public int this[string word]
        {
            get => wordCounts.ContainsKey(word) ?
                wordCounts[word] : 0;

            set
            {
                word = (word ?? string.Empty).Replace("\"", string.Empty);
                if (string.IsNullOrWhiteSpace(word))
                {
                    return;
                }

                if (!wordCounts.ContainsKey(word))
                {
                    wordCounts.Add(word, 0);
                }

                wordCounts[word]++;
            }
        }

        /// <summary>
        /// Parses the word list into the dictionary.
        /// </summary>
        /// <param name="wordList">The list of whitespace-delimited words.</param>
        /// <param name="isTitle">True for title. Will parse title phrases.</param>
        public void ParseWords(string wordList, bool isTitle = false)
        {
            if (isTitle)
            {
                var parts = wordList.Replace("\"", string.Empty).Split(' ');
                for (var idx = 0; idx < parts.Length; idx++)
                {
                    var partial = new string[idx + 1];
                    Array.Copy(parts, partial, partial.Length);

                    // skip hyphens, etc.
                    var wordCheck = Extensions.StripNonAlpha.Replace(partial[idx], string.Empty);
                    if (string.IsNullOrWhiteSpace(wordCheck) || wordCheck == "-")
                    {
                        continue;
                    }

                    this[string.Join(" ", partial)] = 1;
                }

                return;
            }

            foreach (var word in wordList.Replace("\"", string.Empty).Split(' '))
            {
                this[word] = 1;
            }
        }

        /// <summary>
        /// Weight phrases higher than word counts.
        /// </summary>
        /// <param name="word">The word to consider.</param>
        /// <param name="count">The word or phrase count.</param>
        /// <returns>The relative weight.</returns>
        private int Weight(string word, int count)
        {
            var length = word.Split(' ').Length;
            return length ^ 2 * count;
        }
    }
}
