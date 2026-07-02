namespace ICS.Portal.Data.Extensions
{
    public static class DateTimeExtensions
    {
        #region Convert To UTC
        public static string? ConvertToUtc(this TimeOnly? value)
        {
            if (value is null) return null;

            DateTime localDateTime = DateTime.Parse($"1/1/2000 {value}");
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime);

            return TimeOnly.FromDateTime(utcDateTime).ToString("HH:mm");
        }

        public static string? ConvertToUtc(this string? value)
        {
            if (value is null) return null;

            if (TimeOnly.TryParse(value, out TimeOnly timeOnly))
            {
                return ConvertToUtc(timeOnly);
            }

            return null;
        }


        #endregion Convert To UTC

        #region Convert From Utc

        public static TimeOnly? ConvertToLocal(this TimeOnly? value)
        {
            if (value is null) return value;

            DateTime utcDateTime = DateTime.Parse($"1/1/2000 {value}");
            DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);

            return TimeOnly.FromDateTime(localDateTime);
        }

        public static TimeOnly? ConvertToLocal(this string? value)
        {
            if (value is null) return null;

            if (TimeOnly.TryParse(value, out TimeOnly timeOnly))
            {
                return ConvertToLocal(timeOnly);
            }

            return null;
        }

        public static string? ConvertToLocalWithTimeZone(this TimeOnly? value)
        {
            if (value is null) return null;

            return $"{ConvertToLocal(value)!.Value.ToString("HH:mm")} {TimeZoneInfo.Local.StandardName}";
        }

        public static string? ConvertToLocalWithTimeZone(this string? value)
        {
            TimeOnly? temp = ConvertToLocal(value);

            if (temp is null)
            {
                return null;
            }

            return $"{temp.Value.ToString("HH:mm")} {TimeZoneInfo.Local.StandardName}";
        }

        #endregion Convert From UTC

        #region Convert To Specific

        public static string? ConvertToSpecificTimeWithTimeZone(string value, TimeZoneInfo timeZoneInfo)
        {
            if (value is null) return value;

            DateTime utcDateTime = DateTime.Parse($"1/1/2000 {value}");
            DateTime specificDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
            TimeOnly specificTime = TimeOnly.FromDateTime(specificDateTime);

            return $"{specificTime.ToString("HH:mm")} {timeZoneInfo.StandardName}";
        }

        #endregion Convert To Specific
    }
}
