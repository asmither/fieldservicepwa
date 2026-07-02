//using ICS.Mobile.Components.ScratchPad;
using ICS.Mobile.Services;
using ICS.Portal.Data.Queries.Models;

using System.Data;

namespace ICS.Mobile.Pages.Home
{
    /// <summary>
    /// Step 1 - If the app is not running in standalone force the install. The best we can do is provide instructions.
    /// Step 2 - If the app requires inform the user that the application is out of date and provide instructions on how to uninstall, reinstall.
    /// Step 3 - Check the permissions and allow opt ins for location, notifications, camera, files?
    /// Step 4 - Load page and kick off the sync
    /// </summary>
    public partial class HomePage : PageBase
    {

        
        

        // Reference to the overlay component
        //public ScratchPadOverlay? scratchPadRef;

        private enum HomePageStates
        {
            Loading,
            Install,
            Reinstall,
            Login,
            AppVersionCheck,
            Permissions,
            Active
        }

        #region Page Variables

        private HomePageStates PageState = HomePageStates.Loading;

        private List<TechKPIListResult>? KPIList = null;
        private int KPICount;

        private List<TechNewsListResult>? NewsList = null;
        private int NewsCount;

        private List<DispatchListResult>? Dispatches = new List<DispatchListResult>();
        private int DispatchesCount = 0;


        private int ChecklistCount { get; set; } = 0;

        int subscriptionRetries = 0;

        #endregion

        private List<WorkflowResultDetailResult.Workflow> LocalActiveWorkflows = new();
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (CurrentUserId > 0)
            {
                await GetVersionAsync();
                await GetNewsAsync();
                await GetTechKPIListAsync();

                PageState = HomePageStates.Active;

                DispatchesCount = DataService.AppState!.DispatchCount;
                //ChecklistCount = DataService.AppState.WorkflowCount;
                
            }
            Initialized = true;
        }


        #region Update Data

        //public async Task UpdateAll()
        //{
        //    int u = UserManager.AuthorizedUser?.EntityId ?? 0;
        //    if (u > 0)
        //    {
        //        await DataService.GetDispatchesForTechnicianAsync(u);
        //        await DataService.GetLookupsAsync(true);
        //    }
        //}



        #endregion

        #region News

        private CancellationTokenSource newsCancellationToken = new CancellationTokenSource();
        public async Task GetNewsAsync()
        {

            LoadNewsAsync(await DataService.GetNewsAsync((e) =>
            {
                LoadNewsAsync(e);
                StateHasChanged();
            },
            CurrentUserId));
        }

        private void LoadNewsAsync(List<TechNewsListResult> data)
        {
            NewsList = null;

            if (data is not null && data.Count != 0)
            {
                NewsList = data
                            .Where(news => !news.Archived
                                        && news.DisplayOn <= DateTime.UtcNow
                                        && (news.DisplayUntil is null || news.DisplayUntil >= DateTime.UtcNow)
                                        && (news.EmployeeId == 0 || news.EmployeeId == CurrentUserId))
                            .OrderByDescending(news => news.DisplayOn)
                            .ToList();
            }

            NewsCount = NewsList?.Count ?? 0;
        }

        #endregion News

        #region KPI

        public async Task GetTechKPIListAsync()
        {
            LoadKPIData(await DataService.GetTechKPIListAsync((e) =>
            {
                LoadKPIData(e);
                StateHasChanged();
            },
            CurrentUserId));
        }
        private void LoadKPIData(List<TechKPIListResult> data)
        {
            KPIList = null;

            if (data is not null && data.Count != 0)
            {
                KPIList = data
                .Where(kpi => (!kpi.Archived && kpi.DisplayOn <= DateTime.UtcNow && (kpi.DisplayUntil is null || kpi.DisplayUntil >= DateTime.UtcNow)) && kpi.EmployeeId == 0 || kpi.EmployeeId == CurrentUserId)
                .OrderByDescending(kpi => kpi.DisplayOn)
                .ToList();
            }
            
            KPICount = KPIList?.Count ?? 0;
        }

        #endregion KPI

        #region AppVersion
        public async Task GetVersionAsync()
        {
            await CheckVersion(await DataService.GetVersionTrackerAsync(async (e) =>
            {
                await CheckVersion(e);
            }));

        }
        private async Task CheckVersion(VersionTrackerResult data)
        {
            if (double.TryParse(data.MobileVersion, out var serverVersion) &&
                double.TryParse(SettingsService.AppVersion, out var appVersion))
            {
                if (serverVersion > appVersion)
                {
                    if (DataService.AppState.LastAppVersionWarning is null || DataService.AppState.LastAppVersionWarning.Value.AddMinutes(10) < DateTime.UtcNow)
                    {
                        await Alert($"Software update required. Go to the settings page and click the refresh button on app version. The current version should be {SettingsService.AppVersion}");
                        DataService.AppState.LastAppVersionWarning = DateTime.UtcNow;
                        await DataService.SaveAppStateInstance();
                    }
                }
            }
            else
            {
                // Fallback to string compare if parsing fails
                if (data.MobileVersion != SettingsService.AppVersion)
                {
                    if (DataService.AppState.LastAppVersionWarning is null || DataService.AppState.LastAppVersionWarning.Value.AddMinutes(10) < DateTime.UtcNow)
                    {
                        await Alert($"Software update required. Go to the settings page and click the refresh button on app version. The current version should be {SettingsService.AppVersion}");
                        DataService.AppState.LastAppVersionWarning = DateTime.UtcNow;
                        await DataService.SaveAppStateInstance();
                    }
                }
            }
        }
        #endregion AppVersion

        #region Colors

        public string SetColorBasedOnValueToGoal(Portal.Data.Queries.Models.TechKPIListResult k)
        {
            // select colors for Goals
            string cv = "";

            if (k.KPIValue >= k.KPIValueGoal)
                cv = "--ColorGreen";
            else if (k.KPIValue >= k.KPIValueGoal * 0.75)
                cv = "--ColorLightGreen";
            else if (k.KPIValue >= k.KPIValueGoal * 0.5)
                cv = "--ColorYellowGreen";
            else if (k.KPIValue >= k.KPIValueGoal * 0.25)
                cv = "--ColorGold";
            else if (k.KPIValue >= k.KPIValueGoal * 0.01)
                cv = "--ColorPink";
            else if (k.KPIValue >= k.KPIValueGoal * 0.001)
                cv = "--ColorRed";
            else
                cv = "--ColorGray";

            return cv;
        }

        public string RotateColors(int lineValue)
        {
            // select colors for Goals
            string cv = "";

            int colorValue = lineValue;
            if (colorValue > 4)
            {
                Random Random = new Random();
                colorValue = Random.Next(1, 4);
            }

            switch (colorValue)
            {
                case 2:
                    cv = "--ColorGreen";
                    break;
                case 3:
                    cv = "--ColorLightGreen";
                    break;
                case 1:
                    cv = "--ColorGold";
                    break;
                case 4:
                    cv = "--ColorPink";
                    break;

                default:
                    return "--PrimaryColor";
            }

            return cv;
        }

        #endregion

        #region New Stuff not live
        public async Task GetUserActivityToday()
        {
            DateTime d = DateTime.Now.AddDays(-0);
            WorkflowCheckActivityInput wfActivity = new WorkflowCheckActivityInput()
            {
                AsOfDate = d,
                IsUTC = false,
                AnyActionOnDate = true,
                EmployeeId = (CurrentUserId),
                IncludeAllSince = false,
                RequireDispatch = false,
                TimezoneDefaultAbbr = GetTimeZoneAbbr(),
                WorkflowIdList = null,
                WorkflowResultIdList = null,
                WorkflowId = null,
                WorkflowResultId = null,
                LocalTZ = null,

            };
            if (!wfActivity.IsValid())
            {
                Console.WriteLine(string.Join(" ", wfActivity.ValidationResults.Select(item => item)));
                return;
            }

            var response = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<WorkflowCheckActivityOutput>(wfActivity);
            if (response.IsSuccess)
            {
                var data = response.Data!;

                if (data.ReturnValue == WorkflowCheckActivityOutput.Returns.Ok)
                {
                    // save to local storage
                    if (data.ResultData.Count > 0)
                    {
                        foreach (var x in data.ResultData)
                        {
                            Console.WriteLine($"WorkflowCheckActivityOutput: {x.WorkflowResultId} {x.Prompt} {x.CompletedLkl.ToString()}");
                            if (x.WorkflowId.Equals(63))
                            {
                                HideStartOfDay = true;
                            }
                            if (x.WorkflowId.Equals(39))
                            {
                                HideEndOfDay = true;
                            }
                            if (x.WorkflowId.Equals(44))
                            {
                                HideTake5 = true;
                                HideResume = false;
                            }

                        }
                        //await storage.Set("WorkflowCount", ChecklistCount);
                    }
                }
                if (data.ReturnValue == WorkflowCheckActivityOutput.Returns.NotFound)
                {
                    HideStartOfDay = false;
                    HideEndOfDay = false;
                    HideTake5 = false;
                    HideResume = true;

                    if (RemindAboutStartOfDay == 0)
                    {
                        RemindAboutStartOfDay = 1;
                    }
                }
            }
        }

        private string GetTimeZoneAbbr()
        {
            // Get the current timezone
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

            // Get the current time
            DateTime currentTime = DateTime.Now;

            // Check if Daylight Saving Time is in effect
            bool isDaylightSaving = localTimeZone.IsDaylightSavingTime(currentTime);

            // Select the appropriate name based on DST
            string timeZoneName;

            timeZoneName = (isDaylightSaving && localTimeZone.SupportsDaylightSavingTime) ? localTimeZone.DaylightName : localTimeZone.StandardName;

            return timeZoneName;

        }
        #endregion

        #region Page UI Events and Helpers
        public void OnSettingsClick()
        {
            NavigateToSettings();
        }
        public void OnMyDispatchesClick()
        {
            NavigateToDispatchList();
        }
        public void CalcClick()
        {
            PageNavManager.NavigateTo("/calcmenu");
        }
        public void MadlibClick()
        {
            PageNavManager.NavigateTo("/madlibs"); 
        }
        public void OnBluonBrowseClick()
        {
            PageNavManager.NavigateTo("/bluonbrowse");
        }

        //public async Task ChecklistsInProgressClick()
        //{
        //    if (DataService.AppState.DispatchTechId.HasValue)
        //    {
        //        if (LocalActiveWorkflows.Any(w => w.WorkOrderDispatchTechId == DataService.AppState.DispatchTechId))
        //        {
        //            NavigateToDispatchDetail();
        //            return;
        //        }
        //    }

        //    var firstWorkflow = LocalActiveWorkflows.FirstOrDefault();
        //    if (firstWorkflow is not null)
        //    {
        //        DataService.AppState.DispatchId = firstWorkflow.WorkOrderDispatchId;
        //        DataService.AppState.DispatchTechId = firstWorkflow.WorkOrderDispatchTechId;
        //        await DataService.SaveAppStateInstance();
        //        NavigateToDispatchDetail();
        //        return;
        //    }

        //    NavigateToDispatchList();
        //}
        public async Task OnStartDayClick()
        {
            await LoadAndLaunchWorkflow(CurrentUserId, 63, "", "Start of Day");
        }
        public async Task OnTakeFiveClick()
        {
            await LoadAndLaunchWorkflow(CurrentUserId, 44, "", "Take a Break");
        }
        public async Task OnEndDayClick()
        {
            await LoadAndLaunchWorkflow(CurrentUserId, 39, "", "End of Day");
        }
        public async Task OnInjuryClick()
        {
            await LoadAndLaunchWorkflow(CurrentUserId, 45, "");
        }
        public void OnCompanyDirectoryClick()
        {
            PageNavManager.NavigateTo("/directory");
        }
        public void OnTimeTrackingClick()
        {
            PageNavManager.NavigateTo("/time");
        }

        #endregion

        #region New Stuff Not Implemented

        private bool HideStartOfDay = false;
        private bool HideEndOfDay = false;
        private bool HideTake5 = false;
        private bool HideResume = true;

        private int RemindAboutStartOfDay = 0;
        private int MaxRemindAboutStartOfDay = 3;

        #endregion
    }
}
