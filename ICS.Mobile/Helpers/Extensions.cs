using System.Runtime.CompilerServices;

namespace ICS.Mobile.Helpers
{
    public static class Extensions
    {
        public static string ToLocalDateTime(this DateTime date)
        {;
            if(date.Kind == DateTimeKind.Utc)
            {
               return date.ToLocalTime().ToString();
            }
            else
            {
                DateTime utcDateTime = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                return utcDateTime.ToLocalTime().ToString();
            }
        }
    }
}
