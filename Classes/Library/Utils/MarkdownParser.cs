using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Classes.Library.Utils
{
    /// <summary>
    /// Utilitar pentru parsarea simplă a Markdown-ului în FormattedString
    /// Suportă: **bold**, *italic*, ***bold+italic***
    /// </summary>
    public static class MarkdownParser
    {
        /// <summary>
        /// Convertește text cu Markdown simplu în FormattedString pentru Label
        /// </summary>
        public static FormattedString ParseToFormattedString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new FormattedString();

            var formattedString = new FormattedString();

            // Pattern pentru detectarea Markdown:
            // ***text*** -> bold + italic
            // **text** -> bold
            // *text* sau _text_ -> italic
            var pattern = @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)";

            var lastIndex = 0;
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                // Adaugă textul normal dinaintea match-ului
                if (match.Index > lastIndex)
                {
                    var normalText = text.Substring(lastIndex, match.Index - lastIndex);
                    formattedString.Spans.Add(new Span { Text = normalText });
                }

                // Determină tipul de formatare și extrage textul
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

                // Adaugă span-ul formatat
                formattedString.Spans.Add(new Span
                {
                    Text = formattedText,
                    FontAttributes = attributes
                });

                lastIndex = match.Index + match.Length;
            }

            // Adaugă textul rămas după ultimul match
            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                formattedString.Spans.Add(new Span { Text = remainingText });
            }

            // Dacă nu există niciun match, returnează textul normal
            if (formattedString.Spans.Count == 0)
            {
                formattedString.Spans.Add(new Span { Text = text });
            }

            return formattedString;
        }

        /// <summary>
        /// Verifică dacă textul conține Markdown
        /// </summary>
        public static bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Regex.IsMatch(text, @"(\*\*\*(.+?)\*\*\*|\*\*(.+?)\*\*|\*(.+?)\*|_(.+?)_)");
        }
    }
}
