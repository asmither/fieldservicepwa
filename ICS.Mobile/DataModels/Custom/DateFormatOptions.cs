namespace ICS.Portal.Data.Custom
{
    public class DateFormatOptions
    {

        public static string TimeAgo(DateTimeOffset? eventDate)
        {

            if (!eventDate.HasValue)
                return "";


            DateTime currDate = eventDate.Value.DateTime.ToUniversalTime().ToLocalTime();

            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;


            var ts = new TimeSpan(DateTime.UtcNow.Ticks - currDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (ts.TotalSeconds < 0)
            {
                //return "In the future";
                return String.Empty;
            }

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "Just Now" : ts.Seconds + " secs ago";

            else if (delta < 2 * MINUTE)
                return "Now";

            else if (delta < 45 * MINUTE)
                return ts.Minutes + " mins ago";

            else if (delta < 90 * MINUTE)
                return "an hr ago";

            else if (delta < 24 * HOUR)
                return ts.Hours + " hrs ago";

            else if (delta < 48 * HOUR)
                return "Yesterday";

            else if (delta < 30 * DAY)
                return ts.Days + " days ago";

            else if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "1 month ago" : months + " months ago";
            }
            else
            {
                {
                    int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                    return years <= 1 ? "~ A yr ago" : years + " years ago";
                }
            }


        }

        public static DateFormatOptions Default()
        {
            DateFormatOptions result =  new("en-US", "gregory", "latn", "America/New_York", "numeric", "numeric", "numeric");
            result.IsDefault = true;
            return result;
        }

        public DateFormatOptions(string locale, string calendar, string numberingSystem, string timeZone, string year, string month, string day)
        {
            Locale = locale;
            Calendar = calendar;
            NumberingSystem = numberingSystem;
            TimeZone = timeZone;
            Year = year;
            Month = month;
            Day = day;
            IsDefault = false;
            LocalDateTime = DateTime.Now;
            UTCDateTime = DateTime.UtcNow;
        }

        public string Locale { get; }

        public string Calendar { get; }

        public string NumberingSystem { get; }

        public string TimeZone { get; }

        public string Year { get; }

        public string Month { get; }

        public string Day { get; }

        public DateTime LocalDateTime { get; }

        public DateTime UTCDateTime { get; }

        public bool IsDefault { private set;  get; } = false;

        //{"locale":"en-US","calendar":"gregory","numberingSystem":"latn","timeZone":"America/New_York","year":"numeric","month":"numeric","day":"numeric"}
    }
}
