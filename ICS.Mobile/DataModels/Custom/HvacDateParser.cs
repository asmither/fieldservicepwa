using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ICS.Portal.Data.Custom
{
    public static class HvacDateParser
    {
        /// <summary>
        /// Parses various HVAC manufacturing date formats into a DateTime (Month/Year)
        /// Returns the first day of the month/year combination
        /// </summary>
        public static DateTime? ParseDateText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Clean the input - trim whitespace AND strip leading/trailing punctuation first
            input = input.Trim();
            input = CleanLeadingTrailingPunctuation(input);
            
            if (string.IsNullOrWhiteSpace(input))
                return null;

            
            return TryMonthShortYearParse(input)       
                ?? TrySpaceSeparatedFormats(input)
                ?? TryFullDateFormats(input)
                ?? TryMonthYearParse(input)
                ?? TryYearMonthParse(input)
                ?? TryTextMonthParse(input)
                ?? TryYearWeekParse(input)
                ?? TryStandardDateParse(input)         
                ?? TryAmbiguousNumericParse(input);
        }

        /// <summary>
        /// Parse MM-YY or M-YY formats (e.g., 12-24 = December 2024)
        /// </summary>
        private static DateTime? TryMonthShortYearParse(string input)
        {
            // Match patterns like 12-24, 1-24, 12/24, 1.24 (where second part is 2 digits)
            var pattern = @"^(\d{1,2})[/\-.](\d{2})$";
            var match = Regex.Match(input, pattern);

            if (!match.Success)
                return null;

            int first = int.Parse(match.Groups[1].Value);
            int second = int.Parse(match.Groups[2].Value);

            // First part must be a valid month (1-12)
            if (first < 1 || first > 12)
                return null;

            // Second part is a 2-digit year
            // Convert to 4-digit year: 00-29 = 2000-2029, 30-99 = 1930-1999
            int year = second <= 29 ? 2000 + second : 1900 + second;

            return new DateTime(year, first, 1);
        }

        /// <summary>
        /// Try to parse standard date formats using DateTime.TryParse
        /// This is now a FALLBACK - explicit patterns are tried first
        /// </summary>
        private static DateTime? TryStandardDateParse(string input)
        {
            // Skip MM-YY patterns - these are handled by TryMonthShortYearParse
            // This prevents DateTime.TryParse from misinterpreting them
            if (Regex.IsMatch(input, @"^\d{1,2}[/\-\.]\d{2}$"))
                return null;

            if (DateTime.TryParse(input, out DateTime result))
            {
                return new DateTime(result.Year, result.Month, result.Day);
            }
            return null;
        }

        /// <summary>
        /// Parse space-separated date formats intelligently based on part count and lengths
        /// </summary>
        private static DateTime? TrySpaceSeparatedFormats(string input)
        {
            if (!input.Contains(' '))
                return null;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || parts.Length > 3)
                return null;

            // Check if all parts are numeric
            foreach (var part in parts)
            {
                if (!part.All(char.IsDigit))
                    return null;
            }

            if (parts.Length == 2)
            {
                int first = int.Parse(parts[0]);
                int second = int.Parse(parts[1]);

                // If first is 4 digits, it's YYYY MM
                if (parts[0].Length == 4)
                {
                    int year = first;
                    int monthOrWeek = second;

                    if (monthOrWeek >= 1 && monthOrWeek <= 12)
                        return new DateTime(year, monthOrWeek, 1);
                    else if (monthOrWeek > 12 && monthOrWeek <= 53)
                        return GetDateFromWeek(year, monthOrWeek);
                }
                // If second is 4 digits, it's MM YYYY
                else if (parts[1].Length == 4)
                {
                    int monthOrWeek = first;
                    int year = second;

                    if (monthOrWeek >= 1 && monthOrWeek <= 12)
                        return new DateTime(year, monthOrWeek, 1);
                    else if (monthOrWeek > 12 && monthOrWeek <= 53)
                        return GetDateFromWeek(year, monthOrWeek);
                }
                // Both 2 digits - assume MM YY format
                else if (parts[0].Length <= 2 && parts[1].Length == 2)
                {
                    int month = first;
                    int shortYear = second;

                    if (month >= 1 && month <= 12)
                    {
                        int year = shortYear <= 29 ? 2000 + shortYear : 1900 + shortYear;
                        return new DateTime(year, month, 1);
                    }
                }
            }
            else if (parts.Length == 3)
            {
                var lengths = parts.Select(p => p.Length).ToArray();

                int part1 = int.Parse(parts[0]);
                int part2 = int.Parse(parts[1]);
                int part3 = int.Parse(parts[2]);

                // Pattern: 4 2 2 → YYYY MM DD
                if (lengths[0] == 4)
                {
                    int year = part1;
                    int month = part2;
                    int day = part3;

                    if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        try
                        {
                            var date = new DateTime(year, month, day);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                }
                // Pattern: 2 2 4 → MM DD YYYY or DD MM YYYY
                else if (lengths[2] == 4)
                {
                    int year = part3;

                    if (part1 > 12 && part1 <= 31 && part2 <= 12)
                    {
                        try
                        {
                            var date = new DateTime(year, part2, part1);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                    else if (part2 > 12 && part2 <= 31 && part1 <= 12)
                    {
                        try
                        {
                            var date = new DateTime(year, part1, part2);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                    else if (part1 <= 12 && part2 <= 31)
                    {
                        try
                        {
                            var date = new DateTime(year, part1, part2);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch
                        {
                            try
                            {
                                var date = new DateTime(year, part2, part1);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch { }
                        }
                    }
                }
                // Pattern: 2 2 2 → MM DD YY
                else if (lengths[2] == 2)
                {
                    int shortYear = part3;
                    int year = shortYear <= 29 ? 2000 + shortYear : 1900 + shortYear;

                    if (part1 > 12 && part1 <= 31 && part2 <= 12)
                    {
                        try
                        {
                            var date = new DateTime(year, part2, part1);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                    else if (part2 > 12 && part2 <= 31 && part1 <= 12)
                    {
                        try
                        {
                            var date = new DateTime(year, part1, part2);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                    else if (part1 <= 12 && part2 <= 31)
                    {
                        try
                        {
                            var date = new DateTime(year, part1, part2);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch
                        {
                            try
                            {
                                var date = new DateTime(year, part2, part1);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch { }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parse full date formats with explicit patterns
        /// </summary>
        private static DateTime? TryFullDateFormats(string input)
        {
            // Patterns for full dates with 4-digit years
            var fourDigitYearPatterns = new[]
            {
                @"^(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{4})$",  // MM/DD/YYYY or DD-MM-YYYY
                @"^(\d{4})[/\-.](\d{1,2})[/\-.](\d{1,2})$",  // YYYY-MM-DD
            };

            foreach (var pattern in fourDigitYearPatterns)
            {
                var match = Regex.Match(input, pattern);
                if (match.Success)
                {
                    if (pattern.StartsWith(@"^(\d{4})"))
                    {
                        // YYYY-MM-DD format
                        int year = int.Parse(match.Groups[1].Value);
                        int month = int.Parse(match.Groups[2].Value);
                        int day = int.Parse(match.Groups[3].Value);

                        if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                        {
                            try
                            {
                                var date = new DateTime(year, month, day);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        // MM/DD/YYYY or DD/MM/YYYY
                        int first = int.Parse(match.Groups[1].Value);
                        int second = int.Parse(match.Groups[2].Value);
                        int year = int.Parse(match.Groups[3].Value);

                        if (first > 12 && first <= 31 && second <= 12)
                        {
                            try
                            {
                                var date = new DateTime(year, second, first);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch { }
                        }
                        else if (second > 12 && second <= 31 && first <= 12)
                        {
                            try
                            {
                                var date = new DateTime(year, first, second);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch { }
                        }
                        else if (first <= 12 && second <= 31)
                        {
                            try
                            {
                                var date = new DateTime(year, first, second);
                                return new DateTime(date.Year, date.Month, date.Day);
                            }
                            catch
                            {
                                try
                                {
                                    var date = new DateTime(year, second, first);
                                    return new DateTime(date.Year, date.Month, date.Day);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            // 2-digit year patterns (3-part dates like MM-DD-YY)
            var twoDigitYearPattern = @"^(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2})$";
            var match2 = Regex.Match(input, twoDigitYearPattern);

            if (match2.Success)
            {
                int first = int.Parse(match2.Groups[1].Value);
                int second = int.Parse(match2.Groups[2].Value);
                int shortYear = int.Parse(match2.Groups[3].Value);
                int year = shortYear <= 29 ? 2000 + shortYear : 1900 + shortYear;

                if (first > 12 && first <= 31 && second <= 12)
                {
                    try
                    {
                        var date = new DateTime(year, second, first);
                        return new DateTime(date.Year, date.Month, 1);
                    }
                    catch { }
                }
                else if (second > 12 && second <= 31 && first <= 12)
                {
                    try
                    {
                        var date = new DateTime(year, first, second);
                        return new DateTime(date.Year, date.Month, date.Day);
                    }
                    catch { }
                }
                else if (first <= 12 && second <= 31)
                {
                    try
                    {
                        var date = new DateTime(year, first, second);
                        return new DateTime(date.Year, date.Month, date.Day);
                    }
                    catch
                    {
                        try
                        {
                            var date = new DateTime(year, second, first);
                            return new DateTime(date.Year, date.Month, date.Day);
                        }
                        catch { }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parse Month/Year formats (MM/YYYY)
        /// </summary>
        private static DateTime? TryMonthYearParse(string input)
        {
            var pattern = @"^(\d{1,2})[/\-.](\d{4})$";
            var match = Regex.Match(input, pattern);

            if (match.Success)
            {
                int month = int.Parse(match.Groups[1].Value);
                int year = int.Parse(match.Groups[2].Value);

                if (month >= 1 && month <= 12)
                {
                    return new DateTime(year, month, 1);
                }
            }
            return null;
        }

        /// <summary>
        /// Parse Year/Month formats (YYYY/MM)
        /// </summary>
        private static DateTime? TryYearMonthParse(string input)
        {
            var pattern = @"^(\d{4})[/\-.](\d{1,2})$";
            var match = Regex.Match(input, pattern);

            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);

                if (month >= 1 && month <= 12)
                {
                    return new DateTime(year, month, 1);
                }
            }
            return null;
        }

        /// <summary>
        /// Parse text month formats
        /// </summary>
        private static DateTime? TryTextMonthParse(string input)
        {
            // Remove commas and extra spaces
            input = Regex.Replace(input, @"[,]+", " ");
            input = Regex.Replace(input, @"\s+", " ").Trim();

            var patterns = new[]
            {
                @"^([A-Za-z]+)\s+(\d{1,2})\s+(\d{4})$",  // November 11 2004
                @"^(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})$",  // 11 November 2004
                @"^([A-Za-z]+)\s+(\d{4})$",               // November 2004
                @"^(\d{4})\s+([A-Za-z]+)$",               // 2004 November
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern);
                if (match.Success)
                {
                    if (pattern == patterns[0])
                    {

                        string monthName = match.Groups[1].Value;
                        int year = int.Parse(match.Groups[3].Value);
                        string dayName = match.Groups[2].Value;
                        int day = 1;
                        int.TryParse(dayName, out day);

                        if (TryParseMonth(monthName, out int month))
                        {
                            return new DateTime(year, month, day);
                        }
                    }
                    else if (pattern == patterns[1])
                    {

                        string monthName = match.Groups[2].Value;
                        int year = int.Parse(match.Groups[3].Value);
                        string dayName = match.Groups[1].Value;
                        int day = 1;
                        int.TryParse(dayName, out day);

                        if (TryParseMonth(monthName, out int month))
                        {
                            return new DateTime(year, month, day);
                        }
                    }
                    else if (pattern == patterns[2])
                    {
                        string monthName = match.Groups[1].Value;
                        int year = int.Parse(match.Groups[2].Value);

                        if (TryParseMonth(monthName, out int month))
                        {
                            return new DateTime(year, month, 1);
                        }
                    }
                    else if (pattern == patterns[3])
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        string monthName = match.Groups[2].Value;

                        if (TryParseMonth(monthName, out int month))
                        {
                            return new DateTime(year, month, 1);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Parse Year-Week formats ONLY when number is > 12
        /// </summary>
        private static DateTime? TryYearWeekParse(string input)
        {
            var patterns = new[]
            {
                @"^(\d{4})[\-\s.](\d{1,2})$",
                @"^(\d{1,2})[/\-.](\d{4})$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern);
                if (match.Success)
                {
                    int year, weekNumber;

                    if (pattern.StartsWith(@"^(\d{4})"))
                    {
                        year = int.Parse(match.Groups[1].Value);
                        weekNumber = int.Parse(match.Groups[2].Value);
                    }
                    else
                    {
                        weekNumber = int.Parse(match.Groups[1].Value);
                        year = int.Parse(match.Groups[2].Value);
                    }

                    // ONLY treat as week if > 12
                    if (weekNumber > 12 && weekNumber <= 53)
                    {
                        return GetDateFromWeek(year, weekNumber);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Handle remaining ambiguous numeric formats
        /// </summary>
        private static DateTime? TryAmbiguousNumericParse(string input)
        {
            // Most cases are handled by other parsers

            string lastChance = CleanDateInput(input);

            return TryMonthShortYearParse(lastChance)  // Include new parser in fallback chain
                ?? TrySpaceSeparatedFormats(lastChance)
                ?? TryFullDateFormats(lastChance)
                ?? TryMonthYearParse(lastChance)
                ?? TryYearMonthParse(lastChance)
                ?? TryTextMonthParse(lastChance)
                ?? TryYearWeekParse(lastChance);
        }

        /// <summary>
        /// Parse month name or abbreviation
        /// </summary>
        private static bool TryParseMonth(string monthName, out int month)
        {
            month = 0;

            if (DateTime.TryParseExact(monthName, "MMMM",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fullMonth))
            {
                month = fullMonth.Month;
                return true;
            }

            if (DateTime.TryParseExact(monthName, "MMM",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime abbrevMonth))
            {
                month = abbrevMonth.Month;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Convert week number to the first day of that week's month
        /// </summary>
        private static DateTime GetDateFromWeek(int year, int weekNumber)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekNumber;
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            var result = firstThursday.AddDays(weekNum * 7);
            return new DateTime(result.Year, result.Month, 1);
        }

        /// <summary>
        /// Parses to DateOnly (no time component)
        /// </summary>
        public static DateOnly? ParseManufacturingDateOnly(string? input)
        {
            var dateTime = ParseDateText(input);
            if (dateTime.HasValue)
            {
                return DateOnly.FromDateTime(dateTime.Value);
            }
            return null;
        }

        /// <summary>
        /// Extension method for easier use
        /// </summary>
        public static DateTime? ToManufacturingDate(this string? input)
        {
            return ParseDateText(input);
        }

        /// <summary>
        /// Extension method for DateOnly
        /// </summary>
        public static DateOnly? ToManufacturingDateOnly(this string? input)
        {
            return ParseManufacturingDateOnly(input);
        }

        /// <summary>
        /// Removes leading/trailing punctuation (/, \, -, .) but preserves internal structure
        /// </summary>
        private static string CleanLeadingTrailingPunctuation(string input)
        {
            // Remove starting/ending /, \, -, or . characters
            return Regex.Replace(input, @"^[/\\\.\-]+|[/\\\.\-]+$", "").Trim();
        }

        /// <summary>
        /// Removes all a-zA-Z characters, trims starting/ending /, \, or . characters
        /// </summary>
        private static string CleanDateInput(string input)
        {
            // Remove all a-zA-Z characters
            string cleaned = Regex.Replace(input, "[a-zA-Z]", "");

            // Remove starting/ending /, \, or . characters
            cleaned = Regex.Replace(cleaned, @"^[/\\\.]+|[/\\\.]+$", "");

            return cleaned.Trim();
        }
    }
}
