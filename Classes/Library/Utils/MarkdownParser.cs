using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Utils
{
    public static class MarkdownParser
    {
        public static FormattedString ParseToFormattedString(string text, Color textColor = null)
        {
            if (string.IsNullOrEmpty(text))
                return new FormattedString();

            textColor ??= Colors.White;

            var formattedString = new FormattedString();

            // Split by lines first to handle list items properly
            var lines = text.Split('\n');

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx];

                // Add newline before each line (except first)
                if (lineIdx > 0)
                {
                    formattedString.Spans.Add(new Span { Text = "\n", TextColor = textColor });
                }

                // Check if line is a numbered list item (e.g., "1. ", "2. ")
                var listMatch = Regex.Match(line, @"^(\s*\d+\.\s+)(.*)$");
                if (listMatch.Success)
                {
                    // Add the number with bold
                    formattedString.Spans.Add(new Span 
                    { 
                        Text = listMatch.Groups[1].Value, 
                        FontAttributes = FontAttributes.Bold,
                        TextColor = textColor 
                    });
                    line = listMatch.Groups[2].Value;
                }

                // Check if line is a bullet list item
                var bulletMatch = Regex.Match(line, @"^(\s*[-•]\s+)(.*)$");
                if (bulletMatch.Success)
                {
                    formattedString.Spans.Add(new Span 
                    { 
                        Text = "  • ", 
                        FontAttributes = FontAttributes.Bold,
                        TextColor = textColor 
                    });
                    line = bulletMatch.Groups[2].Value;
                }

                // Parse markdown within the line
                ParseMarkdownLine(formattedString, line, textColor);
            }

            if (formattedString.Spans.Count == 0)
            {
                formattedString.Spans.Add(new Span { Text = text, TextColor = textColor });
            }

            return formattedString;
        }

        private static void ParseMarkdownLine(FormattedString formattedString, string line, Color textColor)
        {
            var pattern = @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)";
            var lastIndex = 0;
            var matches = Regex.Matches(line, pattern);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    var normalText = line.Substring(lastIndex, match.Index - lastIndex);
                    formattedString.Spans.Add(new Span { Text = normalText, TextColor = textColor });
                }

                string formattedText = "";
                FontAttributes attributes = FontAttributes.None;

                if (match.Groups[2].Success)
                {
                    formattedText = match.Groups[2].Value;
                    attributes = FontAttributes.Bold | FontAttributes.Italic;
                }
                else if (match.Groups[3].Success)
                {
                    formattedText = match.Groups[3].Value;
                    attributes = FontAttributes.Bold;
                }
                else if (match.Groups[4].Success)
                {
                    formattedText = match.Groups[4].Value;
                    attributes = FontAttributes.Italic;
                }
                else if (match.Groups[5].Success)
                {
                    formattedText = match.Groups[5].Value;
                    attributes = FontAttributes.Italic;
                }

                formattedString.Spans.Add(new Span
                {
                    Text = formattedText,
                    FontAttributes = attributes,
                    TextColor = textColor
                });

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < line.Length)
            {
                formattedString.Spans.Add(new Span { Text = line.Substring(lastIndex), TextColor = textColor });
            }
        }

        public static bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Regex.IsMatch(text, @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)");
        }
    }
}
