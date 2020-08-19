// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Matrix of words for categories.
    /// </summary>
    /// <remarks>
    /// Used as a getter that automatically creates the new instance.
    /// </remarks>
    public class CategoryMatrix
    {
        private readonly IDictionary<uint, WordMatrix> categoryList =
            new Dictionary<uint, WordMatrix>();

        /// <summary>
        /// Accesses the word matrix for the category.
        /// </summary>
        /// <param name="category">The catgory.</param>
        /// <returns>The related <see cref="WordMatrix"/>.</returns>
        public WordMatrix this[uint category]
        {
            get
            {
                if (!categoryList.ContainsKey(category))
                {
                    categoryList.Add(category, new WordMatrix());
                }

                return categoryList[category];
            }
        }

        /// <summary>
        /// Gets the category "title" based on word frequencies.
        /// </summary>
        /// <param name="category">The category to consider.</param>
        /// <returns>The title based on top 5 words by frequency.</returns>
        public string GetCategoryTitle(uint category) =>
            string.Join(
                ",",
                this[category]
                    .WordsHighToLow
                    .Take(10)
                    .OrderByDescending(w => w.word.Length)
                    .Select(w => w.word)
                    .ToArray());
    }
}
