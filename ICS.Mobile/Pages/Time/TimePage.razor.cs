using System;
using System.Threading.Tasks;
using System.Globalization;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Enumerations;
using ICS.Portal.Data.Queries.Models;
using static ICS.Mobile.Components.DataPicker;
using System.Net.Mime;
using ICS.Mobile.DataModels.Local;
using ICS.Portal.Data.Commands.Models;

namespace ICS.Mobile.Pages.Time
{
    public partial class TimePage : PageBase
    {
        bool debugMode = false;  // REMOVE THIS!  OVERRIDES EmployeeId to be xxx instead.  
        int debugUserId = 0; // 248; // 335 // 349; // 248;//32;  // Helpful when debugging to get tech with lotsa data!

        private string MapsAPIKey => SettingsService.AzureMapsKey;

        #region Data Variables

        private List<TimeclockDetailsResult.TimeclockDispatches>? TimeEvDispatches { set; get; } = null;
        private List<TimeclockDetailsResult.Timeclock>? TimeEvList { set; get; } = null;
        private List<TimeclockDetailsResult.TimeclockApprovals>? TimeEvApprovals { set; get; } = null;
        public TimeclockDetailsResult.Timeclock? TimeEvItem { set; get; } = null;
        public Dictionary<string, string> ImageLibrary { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Page and Loose Variables

        //AzureMapsAddressSearchResults? azureMapsAddressSearchResults { set; get; }= null;   

        // TimeTabs
        private enum DisplayModes
        {
            Today = 0,
            ThisWeek = 1,
            LastWeek = 2,
            None = 100
        }

        public string ErrorMessage { get; set; } = "";

        private ICS.Mobile.Pages.Time.Components.Tabs TimeTabs;
        private int OffsetWeekNumHistory { set; get; } = 0;

        private DisplayModes DisplayMode { set; get; } = DisplayModes.ThisWeek;
        private TimeClockPageTabs TimeClockPageTabDefault { set; get; } = TimeClockPageTabs.ThisWeek;

        private int currentTabIs = 0;
        public bool showSpinner { get; set; } = false;

        public bool showGeoSummary { get; set; } = false;
        public bool showDispatchDetail { get; set; } = false;

        public bool seeAllIsPushed { get; set; } = false;

        // location finder variables
        private Double Latitude { get; set; } = 0;
        private Double Longitude { get; set; } = 0;

        private Double DestLatitude { get; set; } = 0;
        private Double DestLongitude { get; set; } = 0;

        public string FromAddress { get; set; } = "";
        public string ToAddress { get; set; } = "";


        private string RouteResultStr { set; get; } = "";

        //private DateTime? lastRefreshed { set; get; } = null;

        //tally ho variables
        private decimal ttm = 0, twm = 0, tm = 0;

        #endregion

        #region Page OnEvents & Hide/Show Dialogs 

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var l = await LocationService.GetGeoPositionAsync();
            if (l is not null)
            {
                Latitude = Double.Parse(l.Latitude.ToString("F5", CultureInfo.InvariantCulture));
                Longitude = Double.Parse(l.Longitude.ToString("F5", CultureInfo.InvariantCulture));

            }
            await LoadData(DisplayModes.ThisWeek, true);

            Initialized = true;
        }
      
        private async Task OnTabClick(TimeClockPageTabs tab)
        {
            TimeClockPageTabDefault = tab;
            showDispatchDetail = false;

            if (tab == TimeClockPageTabs.Today)
            {
                currentTabIs = 0;
                seeAllIsPushed = false;
                TimeEvItem = null;
                await LoadData(DisplayModes.Today, true);
            }

            if (tab == TimeClockPageTabs.ThisWeek)
            {
                currentTabIs = 1;
                await LoadData(DisplayModes.ThisWeek, true);
            }

            if (tab == TimeClockPageTabs.LastWeek)
            {
                currentTabIs = 2;
                await LoadData(DisplayModes.LastWeek, true);
            }
        }

        private void SeeAllDispatchesForWeek()
        {
            seeAllIsPushed = !seeAllIsPushed;
        }

        public void ShowDispatchDetails(TimeclockDetailsResult.Timeclock? ev)
        {
            if (TimeEvDispatches is not null && TimeEvDispatches.Count > 0)
            {
                if (ev is not null)
                {
                    TimeEvItem = ev;
                    seeAllIsPushed = false;
                    showDispatchDetail = true;
                }
            }
        }
        public async Task ShowDayDetails(TimeclockDetailsResult.Timeclock? ev)
        {
            if (ev is null)
                return;

            string imgsrcData = string.Empty;

            TimeEvItem = ev;

            if (!string.IsNullOrEmpty(ev.AzugaFirstLatLng) && !ImageLibrary.ContainsKey(ev.AzugaFirstLatLng))
            {
                imgsrcData = await ProcessMapImageForLatLong(ev.AzugaFirstLatLng);
                ImageLibrary.Add(ev.AzugaFirstLatLng, imgsrcData);
            }


            if (!string.IsNullOrEmpty(ev.AzugaLastLatLng) && !ImageLibrary.ContainsKey(ev.AzugaLastLatLng))
            {
                imgsrcData = await ProcessMapImageForLatLong(ev.AzugaLastLatLng);
                ImageLibrary.Add(ev.AzugaLastLatLng, imgsrcData);
            }

            if (!string.IsNullOrEmpty(ev.StartOfDayLatLng) && !ImageLibrary.ContainsKey(ev.StartOfDayLatLng))
            {
                imgsrcData = await ProcessMapImageForLatLong(ev.StartOfDayLatLng);
                ImageLibrary.Add(ev.StartOfDayLatLng, imgsrcData);
            }

            if (!string.IsNullOrEmpty(ev.EndOfDayLatLng) && !ImageLibrary.ContainsKey(ev.EndOfDayLatLng))
            {
                imgsrcData = await ProcessMapImageForLatLong(ev.EndOfDayLatLng);
                ImageLibrary.Add(ev.EndOfDayLatLng, imgsrcData);
            }

            showGeoSummary = true;
        }


        private void OnCloseClick()
        {
            PageNavManager.NavigateTo("/");
        }

        public void CloseDispatchDetails()
        {
            TimeEvItem = null;
            showDispatchDetail = false;

        }
        public void CloseDayDetails()
        {
            showGeoSummary = false;
            TimeEvItem = null;
        }


        #endregion

        #region Data Formatting Functions

        public string GetColorFromStatus(int status)
        {
            string color = string.Empty;
            switch (status)
            {
                case 1:
                    color = "color: limegreen;";
                    break;
                case 2:
                    color = "color: red;";
                    break;
                case 3:
                    color = "color: deepskyblue;";
                    break;
                default:
                    color = "";
                    break;

            }
            return color;
        }
        public int GetWeekFromDate(DateTime? date)
        {
            DateTime today = date ?? DateTime.Today;
            return ISOWeek.GetWeekOfYear(today);
        }

        public string TimeFromDecimal(decimal? x)
        {
            if (x is null)
                return string.Empty;

            string ret = "";
            int hh = 0;
            int mm = (int)decimal.Round(x.GetValueOrDefault(0));

            // Adjust minutes and hours if necessary
            while (mm >= 60)
            {
                hh++;
                mm -= 60;
            }

            if (hh > 0)
            {
                ret = $"{hh}h {mm}m";
            }
            else if (mm > 0)
            {
                ret = $"{mm}m";
            }

            return ret;


        }
        public string TimeNice(double? x)
        {
            string ret = "";
            if (x is null)
                return string.Empty;

            // Get TimeSpan from hours
            var timeSpan = TimeSpan.FromHours(x.GetValueOrDefault(0));

            // Check if seconds are more than 30 and round up the minutes accordingly
            int hh = timeSpan.Hours;
            int mm = timeSpan.Minutes + (timeSpan.Seconds >= 30 ? 1 : 0); // Round up if seconds >= 30

            // Adjust minutes and hours if necessary
            while (mm >= 60)
            {
                hh++;
                mm -= 60;
            }

            if (hh > 0)
            {
                ret = $"{hh}h {mm}m";
            }
            else if (mm > 0)
            {
                ret = $"{mm}m";
            }

            return ret;
        }

        public string TimeNiceFromDecimal(decimal? x)
        {
            string ret = "";
            if (x is null || x == 0)
                return string.Empty;

            // Get TimeSpan from hours
            var timeSpan = TimeSpan.FromHours((double)x.GetValueOrDefault(0));

            // Check if seconds are more than 30 and round up the minutes accordingly
            int hh = timeSpan.Hours;
            int mm = timeSpan.Minutes + (timeSpan.Seconds >= 30 ? 1 : 0); // Round up if seconds >= 30

            // Adjust minutes and hours if necessary
            if (mm >= 60)
            {
                hh++;
                mm -= 60;
            }

            if (hh > 0)
            {
                ret = $"{hh}h {mm}m";
            }
            else if (mm > 0)
            {
                ret = $"{mm}m";
            }

            return ret;
        }
        #endregion

        #region Load and Save Data

        async Task Thumbs(int thumbsUpOrDown, int hashKey, TimeclockDetailsResult.Timeclock? evDay, TimeclockDetailsResult.TimeclockDispatches? dispatch = null)
        {
            if ((evDay is null && dispatch is null) || hashKey == 0 || thumbsUpOrDown == 0)
                return;

            // dialog box return vals
            int inputBoxResult = 0; // 1 = OK, 2 = Cancel, 3 = OK+COMMENT
            string comment = string.Empty; // tech comments 

            // msg for errors
            string msg = string.Empty; // msg for errors 


            // Build Input Object
            TimeclockActivityInput timeclockActivityInput = new TimeclockActivityInput()
            {
                Hash = hashKey,
                Status = thumbsUpOrDown,
                By = CurrentUserId,// UserManager?.AuthorizedUser?.EntityId ?? 0,
                Role = 2,
                Notes = null,
                Dispatch = null
            };

            // if approving a dispatch, manually and laboriously add the clock details to the input
            if (dispatch is not null)
            {
                timeclockActivityInput.Dispatch = dispatch.Dispatch;
                timeclockActivityInput.EventDate = dispatch.DispatchDate;
                timeclockActivityInput.EmployeeId = dispatch.EmployeeId;
                timeclockActivityInput.WeekNum = dispatch.Week;
                timeclockActivityInput.Payload = GeneralFunctions.ObjectToJSON(dispatch, false);
            }


            // Build Review Data for Dialog
            Dictionary<string, string> ReviewData = new Dictionary<string, string>();

            if (dispatch is not null)
            {
                ReviewData.Add($"  ", $"{dispatch.EmployeeName}");
                ReviewData.Add("Dispatch#", $"{dispatch.Dispatch}");
                ReviewData.Add("Work Date", $"{GetDueDate(dispatch.DispatchDate)}");
                ReviewData.Add(" ", $" ");
                ReviewData.Add("Trvl Hrs", $"{TimeNice(dispatch.TravelHrs)}");
                ReviewData.Add("Work Hrs", $"{TimeNice(dispatch.WorkHrs)}");
                ReviewData.Add(" *TOTAL*", $"{TimeNice(dispatch.TotalHrs)}");
            }
            else
            {
                ReviewData.Add($"   ", $"{evDay.EmployeeName}");
                ReviewData.Add("Work Date", $"{GetDueDate(evDay.EventDate)}");
                ReviewData.Add(" ", $" ");
                ReviewData.Add("Srt Day", $"{GetDueTime(evDay.StartOfDay)}");
                ReviewData.Add("End Day", $"{GetDueTime(evDay.EndOfDay)}");
                ReviewData.Add("Trvl Hrs", $"{TimeFromDecimal(evDay.ESCTravelMins)}");
                ReviewData.Add("Work Hrs", $"{TimeFromDecimal(evDay.ESCWorkMins)}");
                ReviewData.Add(" *TOTAL*", $"{TimeFromDecimal(evDay.ESCTotalMins)}");
            }


            // Make sure review data has data, else null it
            if (ReviewData is not null && ReviewData.Count.Equals(0))
                ReviewData = null;

            // Get the user's answer
            if (thumbsUpOrDown == 1)
            {
                (inputBoxResult, comment) = await DialogBox.WaitForDialogResultAndCommentAsync(header: "Approve Time",
                                       prompt: "Do you approve of this timecard?" as string, tableData: ReviewData,
                                       commentBoxPlaceholderText: "Note any additional thoughts...", commentDefaultValue: "",
                                       okLabel: "APPROVE", cancelLabel: "Cancel", optLabel: "Approve+Note", defaultButton: 1);

            }
            if (thumbsUpOrDown == 2)
            {
                (inputBoxResult, comment) = await DialogBox.WaitForDialogResultAndCommentAsync(header: "Decline Time",
                                       prompt: "Do you want to decline this timecard?" as string, tableData: ReviewData,
                                       commentBoxPlaceholderText: "Please note why you are disputing...", commentDefaultValue: "",
                                       okLabel: "DECLINE", cancelLabel: "Cancel", optLabel: "Decline+Note", defaultButton: 3);


            }
            if (thumbsUpOrDown == 3)
            {
                (inputBoxResult, comment) = await DialogBox.WaitForDialogResultAndCommentAsync(header: "Hold Time",
                                       prompt: "Do you want to mark this time as HOLD for later review?" as string, tableData: ReviewData,
                                       commentBoxPlaceholderText: "Notes/Thoughts/Concerns here...", commentDefaultValue: string.Empty,
                                       okLabel: "HOLD", cancelLabel: "Cancel", optLabel: "Hold+Note", defaultButton: 3);
            }

            if (inputBoxResult.Equals(2))
            {
                // So, they cancelled....
                return;
            }

            // Set the employee's comment into the notes
            if (!string.IsNullOrEmpty(comment))
                timeclockActivityInput.Notes = comment;

            // Update The Activity
            var response = await HTTPService.GetHttpCommandsClient(DataService.AppState.AuthorizedUser!).PostAsync<TimeclockActivityOutput>(timeclockActivityInput);
            if (response.IsSuccess)
            {
                var data = response.Data!;

                if (data.ReturnValue == TimeclockActivityOutput.Returns.Locked)
                {
                    msg = "This timecard is locked, which means the payroll team has processed already and it's too late to change here.   Please call your supervisor or the HR team.";
                }
                else if (data.ReturnValue == TimeclockActivityOutput.Returns.Ok)
                {
                    await LoadData(DisplayMode, true);
                }
                else
                {

                    if (data.ReturnValue == TimeclockActivityOutput.Returns.InvalidDispatchDetails)
                        msg = "All Dispatch details weren't provided.  We cannot update this timecard.  Please try again later.";
                    if (data.ReturnValue == TimeclockActivityOutput.Returns.InvalidKey)
                        msg = "Looks like this timecard changed! Please exit and reload timecards and try again.";
                    if (data.ReturnValue == TimeclockActivityOutput.Returns.InvalidApproverParams)
                        msg = "You don't seem to have the proper rights to approve this timecard.";
                    if (data.ReturnValue == TimeclockActivityOutput.Returns.Error)
                        msg = "An error occurred while trying to approve this timecard.  We're super sorry.  Please try later or call your supervisor.";

                }


            }
            else
            {
                msg = "There was an internet connectivity issue trying to update this timecard.  Please try this later when you're on wifi or a stronger cell signal.  Sorry!";
            }

            if (!string.IsNullOrEmpty(msg))
            {
                await DialogBox.WaitForDialogResultAsync(header: "Error Updating Time Data",
                                          prompt: msg
                                          as string, okLabel: "OK", cancelLabel: string.Empty, defaultButton: 1);
            }

            return;
        }

        //public bool IsDayEventApprovalAllowedByTech(TimeclockDetailsResult.Timeclock e)
        //{
        //    bool wellisit = false;

        //    if (e is not null && TimeEvApprovals is not null)
        //    {

        //        TimeclockDetailsResult.TimeclockApprovals? thisEventApproval = TimeEvApprovals.Find(a => a.Hash == e.Hash);
        //        if (thisEventApproval is not null)
        //        {
        //            // is this already locked by the payroll team?
        //            if (thisEventApproval.LockStatus > 0)
        //                wellisit = false;

        //            // did Manager already release this and it's approved for tech to approve?
        //            if (thisEventApproval.ReleaseStatus == 1)
        //                wellisit = true;

        //            // did tech already approve this?
        //            //if (thisEventApproval.ApprovalStatus > 0)
        //            //    wellisit = false;

        //        }

        //    }

        //    return wellisit;
        //}

        async Task ShiftWeekNum(int offsetWeekNum)
        {
            if (offsetWeekNum != 0)
            {
                if (!seeAllIsPushed)
                {
                    seeAllIsPushed = true;
                }
            }
            OffsetWeekNumHistory = offsetWeekNum;
            showGeoSummary = false;
            await LoadData(DisplayMode);
        }

        private async Task LoadData(DisplayModes displayMode, bool forceRefresh = false)
        {
            DisplayMode = displayMode;
            ErrorMessage = "";
                
            showSpinner = true;

            // GET DISPATCH DETAIL TIMES

            TimeclockDetailsInput timeclockDetailsInput = new TimeclockDetailsInput()
            {
                EmployeeId = CurrentUserId,// HTTPService?.AuthorizedUser?.EntityId ?? 0,
                EventDate = null,
                WeekNum = null,
                Zone = null

            };

            // employee override
            if (debugMode)
            {
                timeclockDetailsInput.EmployeeId = debugUserId;
            }

            // data scope
            if (displayMode == DisplayModes.Today)
            {
                timeclockDetailsInput.EventDate = DateTime.Today;
            }


            if (displayMode == DisplayModes.ThisWeek)
            {
                timeclockDetailsInput.WeekNum = GetWeekFromDate(null);
            }

            if (displayMode == DisplayModes.LastWeek)
            {
                int weekd = GetWeekFromDate(null);
                int lweek = weekd - 1;

                if (OffsetWeekNumHistory != 0)
                {
                    lweek -= OffsetWeekNumHistory;
                }

                // Wrap week numbers to valid range (1-52)
                while (lweek < 1)
                {
                    lweek += 52;
                }

                timeclockDetailsInput.WeekNum = lweek;
            }

            var response = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Queries.Models.TimeclockDetailsOutput>(timeclockDetailsInput);
            if (response.IsSuccess)
            {
                var data = response.Data!;


                if (data?.ResultData is not null && data.ResultData.TimeclockDispatchesResult is not null)
                {
                    TimeEvDispatches?.Clear();
                    TimeEvDispatches = data.ResultData.TimeclockDispatchesResult;
                }


                if (data?.ResultData is not null && data.ResultData.TimeclockResult is not null)
                {
                    int lastId = 0;

                    if (TimeEvItem is not null)
                        lastId = TimeEvItem.Id;

                    TimeEvList?.Clear();
                    TimeEvList = data.ResultData.TimeclockResult;

                    if (TimeEvList is not null)
                    {
                        if (lastId > 0)
                        {
                            if (TimeEvList.Exists(a => a.Id == lastId))
                            {
                                TimeEvItem = TimeEvList.Find(a => a.Id == lastId);
                            }
                            if (TimeEvItem is null)
                            {
                                if (displayMode == DisplayModes.Today)
                                {
                                    TimeEvItem = TimeEvList.Find(x => x.EventDate.Equals(DateTime.Today));
                                    if (TimeEvItem is null)
                                    {
                                        TimeEvItem = TimeEvList.FirstOrDefault();
                                    }
                                }
                                else
                                {

                                    TimeEvItem = TimeEvList.FirstOrDefault();
                                }
                            }
                        }
                        else
                            TimeEvItem = TimeEvList.FirstOrDefault();
                    }
                    else
                    {
                        TimeEvItem = null;
                    }
                }

                TimeEvApprovals?.Clear();
                if (data?.ResultData is not null && data.ResultData.TimeclockApprovalsResult is not null)
                    TimeEvApprovals = data.ResultData.TimeclockApprovalsResult;

            }
            else
            {
                await DialogBox.WaitForDialogResultAsync(header: "Error Retrieving Time Data",
                                       prompt: "Please try this again when you have better signal strength or WIFI.  Sorry.   You should exit this panel now and try again later."
                                       as string, okLabel: "OK", cancelLabel: string.Empty, defaultButton: 1);

            }
            showSpinner = false;


        }

        #endregion

        #region Sum/Tally and Overtime calc functions


        string ResetTally()
        {
            ttm = 0; tm = 0; twm = 0;
            return string.Empty;
        }
        string TallyEv(TimeclockDetailsResult.Timeclock e)
        {
            tm += e.ESCTotalMins.GetValueOrDefault(0);
            twm += e.ESCWorkMins.GetValueOrDefault(0);
            ttm += e.ESCTravelMins.GetValueOrDefault(0);

            return string.Empty;
        }

        public decimal CalcOT(int mode, decimal val)
        {

            if (val < 1)
                return val;

            if (mode.Equals(0))
            {
                if (val < 2400)
                    return val;
                else
                    return 2400;
            }
            if (mode.Equals(1))
            {
                if (val < 2400)
                    return 0;
                else
                {
                    return val - 2400;
                }
            }
            return 0;

        }

        #endregion

        #region Map Images Functions 

        public string GetImage(string latlong)
        {
            if (ImageLibrary is not null && !string.IsNullOrEmpty(latlong))
            {
                if (ImageLibrary.ContainsKey(latlong))
                    return ImageLibrary[latlong];
                return "/images/icon-150.png";
            }
            return "/images/icon-150.png";
        }

        public async Task<string> ProcessMapImageForLatLong(string? latlong)
        {
            string placeHolder = "/images/icon-150.png";
            AzureMaps azureMaps = new AzureMaps();
            azureMaps.SetMapsKey(MapsAPIKey);

            if (!string.IsNullOrEmpty(latlong))
            {
                var ll = latlong.Split(",");
                if (ll.Length == 2)
                {
                    Latitude = Double.Parse(ll[0]);
                    Longitude = Double.Parse(ll[1]);

                    byte[] Data = await azureMaps.GetStaticMapImage(Latitude, Longitude, 13, 280, 280);

                    if (azureMaps.IsInitialized)
                    {
                        if (Data != null)
                            return $"data:image/jpg;base64,{Convert.ToBase64String(Data)}";

                    }
                }
            }
            return placeHolder;
        }

        #endregion

        #region Location / GPS / Address Functions

        string GetGeoCoords(decimal? lat, decimal? lon)
        {
            if (lat == null || lon == null) return " ";
            return $"{lat}, {lon}";
        }

        public (double, double) GetLatLongFromString(string latlong)
        {
            if (string.IsNullOrEmpty(latlong)) return (0, 0);

            string[] ll = latlong.Split(",");
            if (ll.Length == 2)
            {
                return (Double.Parse(ll[0]), Double.Parse(ll[1]));
            }
            return (0, 0);
        }


        public async Task GetLocationAsync()
        {
            AzureMaps azureMaps = new AzureMaps();
            azureMaps.SetMapsKey(MapsAPIKey);
            if (azureMaps.IsInitialized)
            {

                var l = await LocationService.GetGeoPositionAsync();
                if (l is not null)
                {
                    Latitude = Double.Parse(l.Latitude.ToString("F5", CultureInfo.InvariantCulture));
                    Longitude = Double.Parse(l.Longitude.ToString("F5", CultureInfo.InvariantCulture));

                    if (string.IsNullOrEmpty(ToAddress))
                    {
                        EmployeeQueryInput employeeQueryInput = new EmployeeQueryInput()
                        {
                            EmployeeId = CurrentUserId// UserManager?.AuthorizedUser?.EntityId ?? 0
                        };

                        var response = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<EmployeeQueryOutput>(employeeQueryInput);
                        if (response.IsSuccess)
                        {
                            var data = response.Data!;

                            if (data.ReturnValue == EmployeeQueryOutput.Returns.Ok)
                            {
                                DestLatitude = Double.Parse((data.ResultData?.FirstOrDefault()?.Latitude.ToString("F5") ?? "0")); // 40.0283938;
                                DestLongitude = Double.Parse((data.ResultData?.FirstOrDefault()?.Longitude.ToString("F5") ?? "0")); // 40.0283938;
                            }
                            else
                                RouteResultStr = "No Employee Found.  Error: " + data.ReturnValue.ToString();
                        }

                    }
                    else
                    {
                        DestLatitude = 0; DestLongitude = 0;
                        var x = await azureMaps.SearchAddressesAndGeocode(ToAddress);
                        if (x is not null && x.Summary?.NumResults.GetValueOrDefault(0) > 0 && (x.Results?.Length ?? 0) > 0)
                        {

                            DestLatitude = x.Results.FirstOrDefault().Position.Lat.GetValueOrDefault(0);
                            DestLongitude = x.Results.FirstOrDefault().Position.Lon.GetValueOrDefault(0);
                            ToAddress = x.Results.FirstOrDefault()?.Address.FreeformAddress ?? ToAddress;
                        }

                    }

                    AzureRouteResults.Summary? routeSummary = await azureMaps.GetDrivingDistanceAndTimeSummary(Latitude, Longitude, DestLatitude, DestLongitude);
                    if (routeSummary is not null)
                    {
                        RouteResultStr = $"Leave: {routeSummary.DepartureTime}  Arrive: {routeSummary.ArrivalTime} -- Distance: {routeSummary.LengthInMeters} meters.   Travel Time: {routeSummary.TravelTimeInSeconds}  Delay:  {routeSummary.TrafficDelayInSeconds} ";
                    }


                }
            }

        }



        #endregion
    }

}
