using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Mobile.Helpers;

public static class DateFunctions
{

    #region Date Formatting Functions
    public static string DateLabelDisplay(DateTimeOffset? dt)
    {
        if (!dt.HasValue)
            return "";

        // Convert to local time
        DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();

        // Determine if minutes are '00' and set time format accordingly
        bool shouldOmitMinutes = d.Minute == 0;

        string timepart = shouldOmitMinutes ? d.ToString("h:mm").Split(":").FirstOrDefault() : d.ToString("h:mm");

        // Get AM/PM designator and reduce to 'a' or 'p'
        string amPmDesignator = d.ToString("tt").ToLower()[0].ToString(); // Converts 'AM' to 'a' and 'PM' to 'p'

        string monDate = d.ToString("M/dd");
        // Combine date and time with modified AM/PM designator
        return $"{monDate} {timepart}{amPmDesignator}";
    }

    public static string GetDueDateTime(DateTimeOffset? dt, string sepchar = "@", string dateFormatFilter="ddd M/dd")
    {
        if (!dt.HasValue) return string.Empty;
        DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();
        bool shouldOmitMinutes = d.Minute == 0;
        string? timepart = shouldOmitMinutes ? d.ToString("htt") : d.ToString("h:mm");
        string amPmDesignator = d.ToString("tt").ToLower()[0].ToString(); // Converts 'AM' to 'a' and 'PM' to 'p'
        return $"{d.ToString(dateFormatFilter)}{sepchar}{timepart}{amPmDesignator}";
    }
    public static string GetDueDate(DateTimeOffset? dt)
    {
        if (!dt.HasValue) return "";
        DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();
        return d.ToString("ddd M/dd");

    }
    public static string GetDueDateNoDay(DateTimeOffset? dt)
    {
        if (!dt.HasValue) return "";
        DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();
        return d.ToString("M/dd");

    }
    public static string GetDueTime(DateTimeOffset? dt)
    {
        if (!dt.HasValue) return "";
        DateTime d = dt.Value.LocalDateTime;
        bool shouldOmitMinutes = d.Minute == 0;
        string? timepart = shouldOmitMinutes ? d.ToString("htt").ToLower() : d.ToString("h:mmtt").ToLower();
        return timepart;
    }

    #endregion

    #region Bespoke Date Conversions
    public static DateTime JSONdateTime2dateTime(long JSONdatetimelong)
    {
        long DT_Tic = (JSONdatetimelong + 62135607600000) * 10000;
        return new DateTime(DT_Tic);
    }

    public static DateTimeOffset? ConvertUTCOffsetHrsToDateTimeOffset(DateTime? dt, int OffsetHours = 0)
    {

        if (dt is null) return null;

        DateTime CurrentUTCDate = dt.Value; // UTC DateTime

        // Step 1: Create a TimeSpan representing the offset
        TimeSpan timezoneOffset = TimeSpan.FromHours(OffsetHours); // Timezone offset in hours (e.g., -6 for CST)

        // Step 2: Create a new DateTimeOffset directly from the UTC DateTime and apply the offset
        DateTimeOffset NewDateTimeOffset = new DateTimeOffset(CurrentUTCDate).ToOffset(timezoneOffset);

        //Console.WriteLine("NewDateTimeOffset: " + NewDateTimeOffset);

        return NewDateTimeOffset;
    }


    public static DateTime? ConvertFromDateTimeOffset(DateTimeOffset? dateTime)
    {
        if (dateTime is null) return null;

        if (dateTime.Value.Offset.Equals(TimeSpan.Zero))
            return dateTime.Value.UtcDateTime;
        else if (dateTime.Value.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.Value.DateTime)))
            return DateTime.SpecifyKind(dateTime.Value.DateTime, DateTimeKind.Local);
        else
            return dateTime.Value.DateTime;
    }



    public static DateTime? DateParseJsonDate(string jsonDate, string defaultDate = "01/01/2023 01:00:00", string minimumDate = "01/01/1980 00:00:00")
    {

        if (string.IsNullOrEmpty(jsonDate)) return null;

        DateTime? ConvertedDate = null;

        if (jsonDate.Contains("Date") || jsonDate.Contains('('))
        {
            jsonDate = jsonDate.Replace("Date(", "").Replace(")", "").Replace("/", "");
            bool dtLongParsed = long.TryParse(jsonDate, out long dtLong);
            if (dtLongParsed) ConvertedDate = JSONdateTime2dateTime(dtLong);
            if (ConvertedDate < DateTime.Parse(minimumDate)) ConvertedDate = DateTime.Parse(defaultDate);

        }
        else
        {
            if (DateTime.TryParse(jsonDate, out DateTime dt)) ConvertedDate = dt;
        }

        return ConvertedDate;
    }

    #endregion

}
