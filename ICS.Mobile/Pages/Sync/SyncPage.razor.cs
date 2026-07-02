using ICS.Mobile.Components;
using ICS.Mobile.Helpers;
using ICS.Mobile.Services;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Sync
{
    public partial class SyncPage
    {
        #region Basic Page Stuff

        private string? Error = null;

        [Inject]
        public NavigationManager NavigationManager { set; get; } = default!;

        [Inject]
        protected IWorkflowData DataManager { set; get; } = default!;

        #endregion Basic Page Stuff

        private bool InternetConnected = false;
        private int SyncCount = 0;
        private Dictionary<string, List<IDXDBSyncRecord>> SyncRecords = new();
        private bool Initialized = false;
        private string? SyncError = null;
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            DataManager.SyncEnabled = false;

            await LoadDataAsync();
        }

        private string InternetConnectedDisplay()
        {
            if (DataManager.InternetConnected)
            {
                return "Connection Status: Connected";
            }
            return "Connection Status: None";
        }

        public bool AllowCompleteEvent
        {
            get
            {
                if (SyncRecords.ContainsKey(nameof(WorkflowStepResultValueSaveInput)) || SyncRecords.ContainsKey(nameof(ImageInsertInput)))
                {
                    return false;
                }
                return true;
            }
        }
        private async Task LoadDataAsync()
        {
            Initialized = false;

            InternetConnected = await DataManager.Ping();
            
            await DataManager.CheckStatuses();
            SyncCount = DataManager.SyncCount;
            if (SyncCount > 0)
            {
                SyncRecords = await DataManager.GetSyncRecordsAsync();
            }

            Initialized = true;
        }

        private async Task RetrySync(string storeName, IDXDBSyncRecord syncRecord)
        {
            Initialized = false;

            try
            {
                if (await DataManager.PushChangeAsync(storeName, syncRecord))
                {
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                SyncError = ex.ToString();
            }
            finally
            {
                Initialized = true;
            }
        }

        private void OnCloseClick()
        {
            DataManager.SyncEnabled = true;

            NavigationManager.NavigateTo("/settings");
        }
    }
}