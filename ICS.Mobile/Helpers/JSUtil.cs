using ICS.Mobile.DataModels.Local;
using Microsoft.JSInterop;

namespace ICS.Mobile.Helpers;

public class JSUtil(IJSRuntime js)
{
    private readonly IJSRuntime js = js;

    public async Task<LatLong> GetLocation()
    {
        // Use New Static Class:  return await GeolocationUtil.GetCoordsAsync();

        var coords = await js.InvokeAsync<LatLong>("util.getCoords");
        //var coords = await GeolocationUtil.GetCoordsAsync();
        if (coords is null)
        {
            coords = new LatLong() { Latitude = 0, Longitude = 0 };
        }
        return coords;

        

    }

    public async Task<bool> CheckOnlineStatus()
    {
        bool x = false;
        x = await js.InvokeAsync<bool>("util.checkInternetStatus");
        return x;
    }

    public async Task SetAppBadge(int nmbr)
    {
        if (nmbr > 0)
        {
            await js.InvokeVoidAsync("setAppBadge", nmbr); 
        }
        else
        {
            await js.InvokeVoidAsync("clearAppBadge"); // clear badge
        }
        
    }

    public async Task ClearAppBadge()
    {
        await js.InvokeVoidAsync("clearAppBadge"); // clear badge
    }

    
    

    public async Task TryLogInfoAsync(string message)
    {
        try
        {
            await this.js.InvokeVoidAsync("console.info", message);
        }
        catch { }
    }

    public async Task TryLogErrorAsync(Error error)
    {
        try
        {
            await this.js.InvokeVoidAsync("console.error", error);
        }
        catch { }

    }

    public async Task TryLogWarningAsync(string message)
    {
        try
        {
            await this.js.InvokeVoidAsync("console.warn", message);
        }
        catch { }
    }

    public async Task<decimal> Calculate(string expression)
    {
        try
        {
            return await this.js.InvokeAsync<decimal>("eval", expression);
        }
        catch { return 0; }
    }

}
