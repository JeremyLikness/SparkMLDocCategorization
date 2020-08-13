// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Common
{
    /// <summary>
    /// Helper extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Regular expression to strip non-alpha content.
        /// </summary>
        public static readonly Regex StripNonAlpha = new Regex(@"[^\w @-]", RegexOptions.Compiled);

        /// <summary>
        /// Strips non-alpha and returns "clean" words list.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <returns>The cleansed text.</returns>
        public static string ExtractWords(this string source)
            => string.IsNullOrWhiteSpace(source) ? string.Empty :
            $" {StripNonAlpha.Replace(source, string.Empty)}";

        /// <summary>
        /// Turns foo into fooFeaturized.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <returns>The featurized text.</returns>
        public static string Featurized(this string source)
            => $"{source}Featurized";

        /// <summary>
        /// Removes extraneous spaces/whitespace.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The input stripped of redundant whitespace.</returns>
        public static string NormalizeWhiteSpace(this string input)
        {
            int len = input.Length,
                index = 0,
                i = 0;
            var src = input.ToCharArray();
            bool skip = false;
            char ch;
            for (; i < len; i++)
            {
                ch = src[i];
                switch (ch)
                {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        if (skip)
                        {
                            continue;
                        }

                        src[index++] = ch;
                        skip = true;
                        continue;
                    default:
                        skip = false;
                        src[index++] = ch;
                        continue;
                }
            }

            return new string(src, 0, index).Trim();
        }

        /// <summary>
        /// Checks to see if the value is not null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="argName">The name of the parameter/argument.</param>
        public static void CheckNotNull<T>(T value, string argName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        /// <summary>
        /// Foreach extension.
        /// </summary>
        /// <typeparam name="T">The type to iterate.</typeparam>
        /// <param name="list">The list to iterate.</param>
        /// <param name="action">The action to perform on each item.</param>
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            CheckNotNull(list, nameof(list));
            CheckNotNull(action, nameof(action));
            foreach (T item in list)
            {
                action(item);
            }
        }
    }
}
