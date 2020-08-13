// Licensed under the MIT License. See LICENSE in the repository root for license information.

using Microsoft.Spark.Sql;

namespace SparkWordsProcessor
{
    /// <summary>
    /// Helper extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Set up a reader with the options for a header.
        /// </summary>
        /// <param name="reader">The <see cref="DataFrameReader"/></param>
        /// <returns>The <see cref="DataFrameReader"/> with configured option.</returns>
        public static DataFrameReader HasHeader(this DataFrameReader reader)
        {
            return reader.Option("header", true);
        }

        /// <summary>
        /// Casts a string to a column type.
        /// </summary>
        /// <param name="str">The string to transform.</param>
        /// <returns>The <see cref="Column"/>.</returns>
        public static Column AsColumn(this string str) => Functions.Col(str);
    }
}
