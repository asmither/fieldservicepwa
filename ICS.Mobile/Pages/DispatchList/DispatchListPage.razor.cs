//using ICS.Mobile.Components.ScratchPad;
using ICS.Portal.Data.Enumerations;
using ICS.Portal.Data.Queries.Models;

using System.Timers;

namespace ICS.Mobile.Pages.DispatchList
{
    public partial class DispatchListPage : PageBase, IDisposable
    {
        private enum DisplayModes
        {
            Current = 0,
            Recent = 1,
            Today = 2,
            None = 100
        }

        #region Page Variables
        
        
        

        private int CurrentDispatchTechId = 0;
        private List<DispatchListResult>? AllDispatches = new();
        private List<DispatchListResult>? DispatchesToDisplay = new List<DispatchListResult>();
        private DisplayModes DisplayMode = DisplayModes.Current;

        #endregion Page Variables

        #region Timer

        private System.Timers.Timer? _refreshTimer;
        private DateTime lastRefreshed = DateTime.UtcNow;
        private void RefreshTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _refreshTimer!.Stop();
            Console.WriteLine("Dispatch List Refresh Timer Elapsed - Refreshing Data");
            RefreshData();
            Console.WriteLine("Dispatch List Data Refreshed.  ");
            lastRefreshed = DateTime.UtcNow;


            _refreshTimer.Start();
        }

        #endregion Timer

        #region OnInit OnDispose
        protected override async Task OnInitializedAsync()
        {

            await base.OnInitializedAsync();

            CurrentDispatchTechId = DataService.AppState.DispatchTechId.GetValueOrDefault(0);

            _ = GetDispatches();

            _refreshTimer = new System.Timers.Timer(15000);
            _refreshTimer.Elapsed += RefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();

        }

        public void Dispose()
        {
            // This is used and invoked on page end
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Elapsed -= RefreshTimerElapsed;
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }
        #endregion

        #region Page OnEvents

        public async Task OnItemSelected(int id)
        {
            // they picked a dispatch to view, so start the spinner
            Initialized = false;

            var selectedDispatch = DispatchesToDisplay!.FirstOrDefault(d => d.Id == id);
            if (selectedDispatch is not null)
            {
                if (DataService.AppState.DispatchTechId != id || DataService.AppState.DispatchId != selectedDispatch.WorkOrderDispatchId)
                {
                    // only save if we are really changing appstate data
                    DataService.AppState.DispatchTechId = id;
                    DataService.AppState.DispatchId = selectedDispatch.WorkOrderDispatchId;
                    await DataService.SaveAppStateInstance();
                }
                NavigateToDispatchDetail();
            }
        }
        private void OnTabClick(DispatchListPageTabs tab)
        {
            if (tab == DispatchListPageTabs.Current)
            {
                SetMode(DisplayModes.Current);
            }
            else if (tab == DispatchListPageTabs.Recent)
            {
                SetMode(DisplayModes.Recent);
            }
            else if (tab == DispatchListPageTabs.Today)
            {
                SetMode(DisplayModes.Today);
            }
        }
        private void SetMode(DisplayModes displayMode)
        {
            DisplayMode = displayMode;
            LoadDataAsync(AllDispatches ?? new());
        }

        #endregion

        #region Get Dispatches from IndexDB and SQL

        public void RefreshData()
        {
            DataService.GetDispatchesForTechnicianAsync(async (e) => await GetDispatches(), CurrentUserId);
            StateHasChanged();
        }
        private async Task GetDispatches()
        {
            LoadDataAsync(await DataService.GetDispatchesForTechnicianAsync((e) =>
            {
                LoadDataAsync(e, true);
                //StateHasChanged();
            },
            CurrentUserId, 30));
        }
        private void LoadDataAsync(List<DispatchListResult> data, bool isApiCallback = false)
        {
            if (data is not null && data.Count > 0)
            {
                AllDispatches = data;

                switch (DisplayMode)
                {
                    case DisplayModes.Current:
                        DispatchesToDisplay = AllDispatches.Where(d => !d.Status.Equals("Complete", StringComparison.CurrentCultureIgnoreCase)).Take(SettingsService.MaxDispatchesToShow).ToList();
                        break;
                    case DisplayModes.Recent:
                        DispatchesToDisplay = AllDispatches.Where(d => d.Status.Equals("Complete", StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(d => d.DueDate).Take(SettingsService.MaxDispatchesToShow).ToList();
                        break;
                    case DisplayModes.Today:
                        DispatchesToDisplay = AllDispatches.Where(d => d.WorkStop.GetValueOrDefault(DateTime.UtcNow).ToLocalTime().Date == DateTime.Today ||
                                            d.WorkStart.GetValueOrDefault(DateTime.UtcNow).ToLocalTime().Date == DateTime.Today ||
                                            d.Dispatched.GetValueOrDefault(DateTime.UtcNow).ToLocalTime().Date == DateTime.Today).Take(SettingsService.MaxDispatchesToShow).ToList();
                        break;
                }

                // filter for locked dispatches
                if (DispatchesToDisplay is not null && DataService.AppState.LockedDispatchId.GetValueOrDefault(0) != 0)
                {
                    DispatchesToDisplay = [.. DispatchesToDisplay.Where(x => x.WorkOrderDispatchId.Equals(DataService.AppState.LockedDispatchId))];
                }

                //Console.WriteLine($"LoadDataAsync Complete {DateTime.UtcNow}");
                Initialized = true;
                
                _ = UpdateDispatchCount();
                StateHasChanged();
            }

            //if (!Initialized)
            //{
            //    Initialized = isApiCallback;
            //}
            
        }
        private async Task UpdateDispatchCount()
        {
            int currCnt = DataService.AppState.DispatchCount;

            if (AllDispatches is not null)
            {
                int newCnt = AllDispatches.Where(d => !d.Status.Equals("Complete", StringComparison.CurrentCultureIgnoreCase)).Count();
                if (currCnt != newCnt)
                {
                    DataService.AppState.DispatchCount = newCnt;
                    await DataService.SaveAppStateInstance();
                }
            }

        }

        #endregion

        #region Data Formatting Functions

        private string GetDispatchDueDate(DateTimeOffset? dt)
        {
            if (dt.HasValue)
            {
                string time = dt.Value.Minute == 0 ? dt.Value.ToString("htt") : dt.Value.ToString("h:mmtt");
                return $"{dt.Value.ToString("ddd M/dd")} @ {time}";
            }

            return string.Empty;
        }

        #endregion
    }
}
