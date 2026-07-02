using ICS.Mobile.DataModels.Local;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace ICS.Mobile.Helpers;

public class JSUI(IJSRuntime js)
{
    private readonly IJSRuntime js = js;


    public async Task<bool> IsIOSPWA()
    {
        return await js.InvokeAsync<bool>("ui.isIOSPWA");
    }

    public async Task<int> GetBodyWidth()
    {
        return (int) await js.InvokeAsync<float>("ui.getBodyWidth");
    }

    public async Task SetBodyModalState(string onOff)
    {
        await js.InvokeVoidAsync("ui.setBodyModalState", onOff);
    }

    public async Task InitializeSwipeEvents(object id)
    {
        await js.InvokeVoidAsync("initializeSwipeEvents", id);
    }

    public async Task SetBodyDarkTheme(string onOff)
    {
        await js.InvokeVoidAsync("ui.setBodyDarkTheme", onOff);
    }

    public async Task<bool> Confirm(string message)
    {
        return await js.InvokeAsync<bool>("confirm", message);
    }

    public async Task Alert(string message)
    {
        await js.InvokeVoidAsync("alert", message);
    }

    public async Task Focus(string id)
    {
        await js.InvokeAsync<object>("statemanager.setFocus", id);
    }
    public async Task ScrollToTop()
    {
        await js.InvokeVoidAsync("scrollToTop");
    }
    public async Task ScrollToSearchInput()
    {
        await js.InvokeVoidAsync("setSearchBoxFocus");
    }
    
    public async Task SetInputBoxFocus(string inputControlName)
    {
        await js.InvokeVoidAsync("setInputBoxFocus", inputControlName);
    }


    public async Task Back(int levels)
    {
        await js.InvokeAsync<object>("history.go", levels);
    }

    public async Task<string> GetSignaturePadData(ElementReference canvas)
    {
        return await js.InvokeAsync<string>("ui.getSignaturePadData", canvas);
    }

    public async Task<string> GetHighlightedImageData(ElementReference canvas, int x, int y, int width, int height)
    {
        return await js.InvokeAsync<string>("ui.getHighlightedImageData", canvas, x, y, width, height);
    }

    public async Task SetSignaturePadElement(string? backgroundBase64 = null)
    {
        if (string.IsNullOrEmpty(backgroundBase64))
        {
            await js.InvokeVoidAsync("ui.setSignaturePadElement");
            return;
        }
        await js.InvokeVoidAsync("ui.setSignaturePadElement", backgroundBase64);
    }

    public async Task OpenInMaps(Decimal Lat, Decimal Long, string FullAddress)
    {
        await js.InvokeVoidAsync("ui.openInMaps", Lat, Long, FullAddress);
    }

    public async Task NavigateByLatLong(Decimal Lat,Decimal Long)
    {
        await js.InvokeVoidAsync("ui.navigateByLatLong", Lat, Long);
    }

    public async Task CopyToClipboard(string clpboardtxt)
    {
        await js.InvokeVoidAsync("ui.copyTextSilent", clpboardtxt);
    }

  
    

    public async Task<string> GetClipboardText(string clpboardTxtBuffer)
    {
        string aa = await js.InvokeAsync<string>("ui.readClipboardText", clpboardTxtBuffer);
        
        return clpboardTxtBuffer;
    }

    //public string Ellipsis(string text, int length)
    //{
    //    if (string.IsNullOrEmpty(text)) return string.Empty;
    //    if (text.Length <= length) return text;
    //    int pos = text.IndexOf(" ", length);
    //    if (pos >= 0) return text.Substring(0, pos) + "...";
    //    return text.Substring(0, length) + "...";
    //}

    public async Task ClearServiceWorkerCache()
    {
        await js.InvokeVoidAsync("ui.clearServiceWorkerCache");
        //await js.InvokeVoidAsync("unloadApplication");
    }

    
}