using System.Text.RegularExpressions;

namespace ICS.Mobile.Extensions
{
    public static class Formatters
    {
        /// <summary>
        /// Clears out CR/LF
        /// </summary>
        /// <param name="value">Value string to cleanse.</param>
        /// <param name="search">Optional search value. Must also supply replace.</param>
        /// <param name="replace">Optional replace value. Must also supply search.</param>
        /// <returns>String with CR/LF values removed and search value replace with replace value</returns>
        public static string SanitizeString(this string? value, string? search = null, string? replace = null)
        {
            // if this note is empty, null, or just filled with spaces and CR/LF, return empty
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string result = value.Replace("\r\n", "");
            result = result.Replace("\r", "");
            result = result.Replace("\n", "");
            result = result.Trim();
            if(search != null && replace != null)
            {
                result = result.Replace(search, replace);
            }
            return result;
        }

        public static string TextToHtml(string? text)
        {
            if (string.IsNullOrEmpty(SanitizeString(text))) return string.Empty;

            // Emails - use Regex.Replace to handle each match once
            var emailParser = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            text = emailParser.Replace(text, m => $"<a href=\"mailto:{m.Value}\">{m.Value}</a>");

            // Links
            if (text.Contains("://"))
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                text = linkParser.Replace(text, m => $"<a href=\"{m.Value}\" target='_blank'>{m.Value.Replace("https://", "")}</a>");
            }

            // Phone Numbers
            var phoneParser = new Regex(@"\b(?:\d{3}[-.\s]??\d{3}[-.\s]??\d{4})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            text = phoneParser.Replace(text, m => $"<a href=\"tel:{m.Value}\">{m.Value}</a>");

            // CRLF
            text = text.Replace("\r\n", "\r");
            text = text.Replace("\n", "\r");
            text = text.Replace("\r", "<br>\r\n");
            text = text.Replace("  ", " &nbsp;");
            text = text.Replace("<br>\r\n<br>\r\n", "<br>\r\n");

            return text;
        }

        public static string DateFormatAs(this DateTime dts, string formatAs)
        {
            if (string.IsNullOrEmpty(formatAs))
                return string.Empty;
            return dts.ToString(formatAs, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
