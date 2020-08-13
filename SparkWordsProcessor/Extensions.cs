// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Common;
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
        /// <param name="reader">The <see cref="DataFrameReader"/>.</param>
        /// <returns>The <see cref="DataFrameReader"/> with configured option.</returns>
        public static DataFrameReader HasHeader(this DataFrameReader reader)
        {
            return reader.Option("header", true);
        }

        /// <summary>
        /// Shortcut to get column value using an expression.
        /// </summary>
        /// <typeparam name="T">The type of the column.</typeparam>
        /// <param name="row">The <see cref="Row"/> to fetch from.</param>
        /// <param name="expr">An expression that points to the property and type.</param>
        /// <param name="extractAndTrim">Set to <c>true</c> to cleanse data before returning.</param>
        /// <returns>The value.</returns>
        public static T GetColumnValue<T>(
            this Row row,
            Expression<Func<FileDataParse, T>> expr,
            bool extractAndTrim = true)
        {
            var value = row.GetAs<T>((expr.Body as MemberExpression).Member.Name);
            if (extractAndTrim && value is string valueStr)
            {
                return (T)Convert.ChangeType(valueStr.ExtractWords().Trim(), typeof(T));
            }

            return value;
        }

        /// <summary>
        /// Casts a string to a column type.
        /// </summary>
        /// <param name="str">The string to transform.</param>
        /// <returns>The <see cref="Column"/>.</returns>
            public static Column AsColumn(this string str) => Functions.Col(str);
    }
}
