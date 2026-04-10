using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Utils
{
    public static class MarkdownParser
    {
        public static FormattedString ParseToFormattedString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new FormattedString();

            var formattedString = new FormattedString();
            var pattern = @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)";

            var lastIndex = 0;
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    var normalText = text.Substring(lastIndex, match.Index - lastIndex);
                    formattedString.Spans.Add(new Span { Text = normalText });
                }

                string formattedText = "";
                FontAttributes attributes = FontAttributes.None;

                if (match.Groups[2].Success) // ***text***
                {
                    formattedText = match.Groups[2].Value;
                    attributes = FontAttributes.Bold | FontAttributes.Italic;
                }
                else if (match.Groups[3].Success) // **text**
                {
                    formattedText = match.Groups[3].Value;
                    attributes = FontAttributes.Bold;
                }
                else if (match.Groups[4].Success) // *text*
                {
                    formattedText = match.Groups[4].Value;
                    attributes = FontAttributes.Italic;
                }
                else if (match.Groups[5].Success) // _text_
                {
                    formattedText = match.Groups[5].Value;
                    attributes = FontAttributes.Italic;
                }

                formattedString.Spans.Add(new Span
                {
                    Text = formattedText,
                    FontAttributes = attributes
                });

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                formattedString.Spans.Add(new Span { Text = remainingText });
            }
            if (formattedString.Spans.Count == 0)
            {
                formattedString.Spans.Add(new Span { Text = text });
            }

            return formattedString;
        }

        public static bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Regex.IsMatch(text, @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)");
        }
    }
}
