// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Toolkit.Parsers.Markdown;
using Microsoft.Toolkit.Parsers.Markdown.Blocks;
using Microsoft.Toolkit.Parsers.Markdown.Inlines;

namespace Common
{
    /// <summary>
    /// Parses markdown documents.
    /// </summary>
    public class MarkdownParser
    {
        /// <summary>
        /// Parse the contents of a markdown document.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="contents">The file content.</param>
        /// <remarks>
        /// The file property is only set if the content could be parsed.
        /// </remarks>
        /// <returns>The parsed <see cref="FileDataParse"/>.</returns>
        public FileDataParse Parse(string filename, string contents)
        {
            Extensions.CheckNotNull(filename, nameof(filename));

            var result = default(FileDataParse);

            if (string.IsNullOrEmpty(contents))
            {
                return result;
            }

            var markdown = new MarkdownDocument();

            try
            {
                markdown.Parse(contents);
            }
            catch
            {
                return result;
            }

            result.File = filename;
            var titles = new List<(int level, string text)>();
            var words = new StringBuilder();
            markdown.Blocks.ForEach(b => result = RecurseBlock(b, result, words, titles));
            result.Words = words.ToString().NormalizeWhiteSpace();

            // de-dup
            var distinctTitles = titles.Distinct().Where(t => !titles.Any(t2 => t2.text == t.text && t2.level > t.level));

            distinctTitles.OrderBy(t => t.level).ThenByDescending(t => t.text.Length).Take(6)
                .ForEach(t => result.AddHeading(t.text));

            return result;
        }

        /// <summary>
        /// Recurses markdown blocks.
        /// </summary>
        /// <param name="block">The parent block.</param>
        /// <param name="candidate">The <see cref="FileDataParse"/> to parse to.</param>
        /// <param name="words">The <see cref="StringBuilder"/> for the words list.</param>
        /// <param name="titles">The title cache.</param>
        private FileDataParse RecurseBlock(
            MarkdownBlock block,
            FileDataParse candidate,
            StringBuilder words,
            List<(int level, string title)> titles)
        {
            switch (block)
            {
                case HeaderBlock header:
                    var title = header.Inlines.OfType<TextRunInline>().Select(t => t.Text).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        titles.Add((header.HeaderLevel, title.TitleTrim()));
                    }

                    header.Inlines.ForEach(i => candidate = RecurseInline(i, candidate, words, titles));
                    break;
                case LinkReferenceBlock link:
                    if (link.Tooltip != null)
                    {
                        words.Append(link.Tooltip.ExtractWords());
                    }

                    break;
                case ListBlock list:
                    list.Items.SelectMany(item => item.Blocks).ForEach(
                        listItemBlock =>
                        candidate = RecurseBlock(listItemBlock, candidate, words, titles));
                    break;
                case ParagraphBlock paragraph:
                    paragraph.Inlines.ForEach(i => candidate = RecurseInline(i, candidate, words, titles));
                    break;
                case QuoteBlock quote:
                    quote.Blocks.ForEach(b => candidate = RecurseBlock(b, candidate, words, titles));
                    break;
                case TableBlock table:
                    table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Inlines)
                        .ForEach(i => candidate = RecurseInline(i, candidate, words, titles));
                    break;
                case YamlHeaderBlock yaml:
                    var yTitle = yaml.Children.Where(c => c.Key == "title")
                        .Select(c => c.Value).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(yTitle))
                    {
                        titles.Add((0, yTitle.TitleTrim()));
                        words.Append(yTitle.ExtractWords());
                    }

                    break;
                default:
                    break;
            }

            return candidate;
        }

        /// <summary>
        /// Recurses inline runs.
        /// </summary>
        /// <param name="inline">The parent inline.</param>
        /// <param name="candidate">The <see cref="FileDataParse"/> to parse to.</param>
        /// <param name="words">The <see cref="StringBuilder"/> for the words list.</param>
        /// <param name="titles">The title cache.</param>
        /// <returns>The parsed <see cref="FileDataParse"/>.</returns>
        private FileDataParse RecurseInline(
            MarkdownInline inline,
            FileDataParse candidate,
            StringBuilder words,
            List<(int level, string title)> titles)
        {
            switch (inline)
            {
                case MarkdownLinkInline link:
                    if (!string.IsNullOrWhiteSpace(link.Tooltip))
                    {
                        words.Append(link.Tooltip.ExtractWords());
                    }

                    link.Inlines.ForEach(i => candidate = RecurseInline(i, candidate, words, titles));
                    break;
                case IInlineContainer container:
                    if (container is CodeInline)
                    {
                        break;
                    }

                    container.Inlines.ForEach(
                        cInline => candidate = RecurseInline(cInline, candidate, words, titles));
                    break;
                case HyperlinkInline hyper:
                    if (!string.IsNullOrWhiteSpace(hyper.Text))
                    {
                        words.Append(hyper.Text.ExtractWords());
                    }

                    break;
                case TextRunInline textRun:
                    if (!string.IsNullOrWhiteSpace(textRun.Text))
                    {
                        words.Append(textRun.Text.ExtractWords());
                    }

                    break;
                default:
                    break;
            }

            return candidate;
        }
    }
}
