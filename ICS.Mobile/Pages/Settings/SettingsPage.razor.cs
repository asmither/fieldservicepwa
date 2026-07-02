using ICS.Mobile.Components;
using ICS.Mobile.DataModels.Local;
using ICS.Mobile.Helpers;
using ICS.Mobile.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICS.Mobile.Pages.Settings
{
    public partial class SettingsPage : PageBase
    {
        #region Inject

        [Inject]
        JSUI jsui { set; get; } = default!;

        [Inject]
        IDXDBService idxdb { set; get; } = default!;

        [Inject]
        CacheService cache { set; get; } = default!;

        [Inject]
        ThemeService Theme { set; get; } = default!;

        [Inject]
        Microsoft.JSInterop.IJSRuntime JS { set; get; } = default!;

        #endregion Inject

        #region Fields

        ICSDialogBox? LogoutDialog;
        decimal Latitude { get; set; } = 0;
        decimal Longitude { get; set; } = 0;
        string ThemePreference { get; set; } = ThemeService.System;

        #endregion Fields

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            ThemePreference = await Theme.GetPreferenceAsync();
            try
            {
                TabBarAutoHide = await JS.InvokeAsync<bool>("icsTabBar.getAutoHide");
                WaveBg = await JS.InvokeAsync<bool>("icsTheme.getWave");
            }
            catch
            {
                // cosmetic preferences — never block Settings on interop
            }
            Initialized = true;
        }

        bool TabBarAutoHide { get; set; } = true;

        private async Task ToggleTabBarAutoHide()
        {
            TabBarAutoHide = !TabBarAutoHide;
            try
            {
                await JS.InvokeVoidAsync("icsTabBar.setAutoHide", TabBarAutoHide);
            }
            catch
            {
            }
        }

        bool WaveBg { get; set; }

        private async Task ToggleWaveBg()
        {
            WaveBg = !WaveBg;
            try
            {
                await JS.InvokeVoidAsync("icsTheme.setWave", WaveBg);
            }
            catch
            {
            }
        }

        private async Task SetThemeAsync(string preference)
        {
            ThemePreference = preference;
            await Theme.SetPreferenceAsync(preference);
        }

        private string SelectedCss(string preference)
            => ThemePreference == preference ? "Selected" : string.Empty;

        private string AriaChecked(string preference)
            => ThemePreference == preference ? "true" : "false";

#if DEBUG
        // Dev-only: impersonate any technician's data view. Queries pass
        // CurrentUserId, which PageBase swaps for SettingsService.UserOverrideId
        // when non-zero. Requires a real (Stage) login for API auth.
        private static bool ShowDevTools => true;
#else
        private static bool ShowDevTools => false;
#endif

        int? DevTechIdInput { get; set; }
        List<ICS.Portal.Data.Queries.Models.EmployeeQueryResult>? DevTechList { get; set; }
        bool DevTechLoading { get; set; }
        string? DevTechLoadError { get; set; }

        string DevOverrideStatus => Settings.UserOverrideId != 0
            ? $"Viewing as technician #{Settings.UserOverrideId} (clears on full app reload)"
            : "Viewing as yourself";

        private async Task LoadTechListAsync()
        {
            DevTechLoading = true;
            DevTechLoadError = null;

            var response = await HTTPService
                .GetHttpQueriesClient(DataService.AppState.AuthorizedUser!)
                .PostAsync<ICS.Portal.Data.Queries.Models.EmployeeQueryOutput>(
                    new ICS.Portal.Data.Queries.Models.EmployeeQueryInput());

            if (response.IsSuccess && response.Data?.ResultData is { Count: > 0 } employees)
            {
                DevTechList = employees.OrderBy(t => t.EmpName).ToList();
            }
            else
            {
                DevTechLoadError = "Couldn't load the technician list (needs a real Stage login). Enter an employee id manually:";
            }

            DevTechLoading = false;
        }

        private void ApplyTechOverride()
        {
            if (DevTechIdInput is > 0)
            {
                Settings.UserOverrideId = DevTechIdInput.Value;
                NavigateToHome(); // re-init pages under the new identity
            }
        }

        private void ClearTechOverride()
        {
            Settings.UserOverrideId = 0;
            DevTechIdInput = null;
        }
        private void OnCloseClick()
        {
            NavigateToHome();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LocationService.StartOrResetWatchAsync();
            }
        }
        private async Task GetPositions()
        {
            LatLong l = await LocationService.GetGeoPositionAsync();
            Latitude = l.Latitude;
            Longitude = l.Longitude;
        }
        public async Task ClearLocalData()
        {
            if (1 == await DialogBox!.WaitForDialogResultAsync(
        header: "Clear Local Data", prompt: "This erases all local data in the app, to make it as if you just logged in.  This is useful only in a bug or emergency situation to try to get running again." +
        " <br/><br/> Are you sure?", cancelLabel: "Cancel", okLabel: "CLEAR LOCAL", defaultButton: 2))
            {
                await ExecuteClear();
                await Alert("Local Data Cleared.");
            }
        }
        private async Task ExecuteClear()
        {
            await cache.Purge();
            await idxdb.Clear();
            DataService.AppState.LastCachePurge = DateTime.UtcNow;
            DataService.AppState.WorkflowCount = 0;
            DataService.AppState.DispatchCount = 0;
            await DataService.SaveAppStateInstance();
        }
        private async Task ClearServiceWorkerCache()
        {
            await LocationService.ForceQuit();
            await jsui.ClearServiceWorkerCache();
            PageNavManager.NavigateTo("/", forceLoad: true);
        }
        private async Task Logout()
        {
            int retCode = 2;
            
            if (DataService.SyncCount > 0) // || 1==1
                retCode = await LogoutDialog!.WaitForDialogResultAsync(defaultButton: 2);
            else
                retCode = 1;

            if (retCode == 1)
            {
                int result = await DialogBox!.WaitForDialogResultAsync(
                    header: "App LogOut", prompt: "Should we also clear out accumulated data when we log off?",
                    okLabel: "Logout", optLabel: "Also Clear", cancelLabel: "Cancel", defaultButton: 1);


                if (result == 2)
                    return;

                if (result == 3)
                {
                    await ExecuteClear();
                }

                base.DataService.AppState.AuthorizedUser = null;
                await base.DataService.SaveAppStateInstance();
                NavigateToLogin();

            }
        }
    }
}