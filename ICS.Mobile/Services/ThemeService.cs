using Microsoft.JSInterop;

namespace ICS.Mobile.Services
{
    /// <summary>
    /// Runtime theme preference (light / dark / system). The actual switch is a
    /// data-theme attribute on the document element driven by window.icsTheme
    /// (defined in index.html so it runs before first paint); this service is
    /// the .NET wrapper around it. Preference persists in localStorage.
    /// </summary>
    public class ThemeService
    {
        public const string Light = "light";
        public const string Dark = "dark";
        public const string System = "system";

        private readonly IJSRuntime _js;

        public ThemeService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> GetPreferenceAsync()
        {
            try
            {
                return await _js.InvokeAsync<string>("icsTheme.get");
            }
            catch
            {
                return System;
            }
        }

        public async Task SetPreferenceAsync(string preference)
        {
            try
            {
                await _js.InvokeVoidAsync("icsTheme.set", preference);
            }
            catch
            {
                // Theme is cosmetic — never let interop failure break a page.
            }
        }
    }
}
