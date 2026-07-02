using ICS.Mobile.DataModels.Local;
using ICS.Mobile.Services;

namespace ICS.Portal.Data.Custom
{
    public class TimestampAndPositionResolver : ITimestampAndPositionResolver
    {
        private LatLong lastVersion;
        private DateTime lastVersionExpires;
        private DateFormatOptions dateFormatOptionsLastVersion;
        private DateTime dateFormatOptionsLastVersionExpires;
        private readonly LocationService locationService;

        public TimestampAndPositionResolver(LocationService locationService)
        {
            this.locationService = locationService;
            lastVersionExpires = DateTime.UtcNow;
            lastVersion = new(0M, 0M);
            dateFormatOptionsLastVersion = DateFormatOptions.Default();
            dateFormatOptionsLastVersionExpires = DateTime.UtcNow;
        }

        //public async Task<DateFormatOptions> GetDateFormatOptions()
        //{
        //    if (DateTime.UtcNow >= lastVersionExpires)
        //    {
        //        dateFormatOptionsLastVersion = await locationService.GetDateTimeFormatOptions();
        //        dateFormatOptionsLastVersionExpires = DateTime.UtcNow.AddSeconds(10);
        //    }

        //    return dateFormatOptionsLastVersion; 
        //}

        public async Task<TimestampAndPosition> GetTimestampAndPositionAsync()
        {
            if (DateTime.UtcNow >= lastVersionExpires)
            {
                LatLong latLong = await locationService.GetGeoPositionAsync();
                if (latLong is not null)
                {
                    lastVersion = latLong;
                    lastVersionExpires = DateTime.UtcNow.AddSeconds(10);
                }
                else
                {
                    lastVersionExpires = DateTime.UtcNow;
                }
            }



            return new(DateTime.UtcNow, lastVersion.Latitude, lastVersion.Longitude);
        }
    }
}
