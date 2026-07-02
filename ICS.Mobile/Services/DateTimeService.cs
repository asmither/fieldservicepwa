using Microsoft.JSInterop;

namespace ICS.Mobile.Services
{
    public class DateTimeService : JSServiceBase
    {
        //private const string js = "dateTimeService.js"; // Using this during development then replace the js text when ready for deployment.
        private const string js = @"
export function init(settings) {
    // Grab any values needed from settings here.
    return true;
}
export function getTimeZone() {
    const options = Intl.DateTimeFormat().resolvedOptions();
    return options.timeZone;
}";

        private DateTime expires;
        private TimeZoneInfo? timeZoneInfo;
        
        public DateTimeService(IJSRuntime jsRuntime, SettingsService settings)
            : base(jsRuntime, settings, js)
        {
            expires = DateTime.UtcNow.AddDays(-1);
        }
        private async Task GetTimeZone()
        {
            if(expires < DateTime.UtcNow)
            {
                await WaitForReferenceAsync();
                string timeZone = await jsRef.Value.InvokeAsync<string>("getTimeZone");
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                expires = DateTime.UtcNow.AddMinutes(1);
            }
        }
        public async Task<DateTime> GetLocalDateTime()
        {
            await GetTimeZone();
            if(timeZoneInfo is not null)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
            }
            return DateTime.Now;
        }
    }
}
