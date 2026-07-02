namespace ICS.Portal.Data.Custom
{
    public class TimestampAndPosition
    {
        public TimestampAndPosition(DateTime dateTime, decimal latitude, decimal longitude)
        {
            DateTime = dateTime;
            Latitude = latitude;
            Longitude = longitude;
        }

        public DateTime DateTime { get; }
        public decimal Latitude { get; }
        public decimal Longitude { get; }
    }
}
