using ICS.Mobile.Components;
using ICS.Mobile.DataModels.Local;
using ICS.Mobile.Helpers;
using ICS.Mobile.Services;
using ICS.Portal.Data.Custom;
//using ICS.Mobile.Components.ScratchPad;
using Microsoft.AspNetCore.Components;

using System.Text.RegularExpressions;

namespace ICS.Mobile.Pages
{
    public class PageBase : ComponentBase
    {
        #region Inject

        [Inject]
        protected IWorkflowData DataService { set; get; } = default!;

        [Inject]
        protected NavigationManager PageNavManager { set; get; } = default!;

        [Inject]
        protected LocationService LocationService { set; get; } = default!;

        [Inject]
        protected SettingsService SettingsService { set; get; } = default!;

        [Inject]
        protected HTTPService HTTPService { set; get; } = default!;

        [Inject]
        protected JSUI ui { set; get; } = default!;


        [Inject]
        public ScratchPadService ScratchPadSvc { get; set; } = default!;

        //public ScratchPadOverlay? scratchPadRef { set; get; } = default!;

        [SupplyParameterFromQuery]
        public string? Token { set; get; }

        #endregion Inject

        #region Fields

        protected bool Initialized { set; get; } = false;
        protected bool LoadingWorkflow { set; get; } = false;
        protected string? LoadError = null;

        protected ICSDialogBox DialogBox = new();

        protected int CurrentUserId = 0;
        protected string UserName = string.Empty;
        protected void NavigateToHome()
        {
            PageNavManager.NavigateTo("/");
        }

        protected void NavigateToLogin(string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                PageNavManager.NavigateTo($"/login?token={token}");
            }
            else
            {
                PageNavManager.NavigateTo("/login");
            }
            return;
        }
        protected void NavigateToDispatchList()
        {
            PageNavManager.NavigateTo("/dispatch-list");
        }
        protected void NavigateToDispatchDetail()
        {
            PageNavManager.NavigateTo($"/dispatch-detail");
        }
        protected void NavigateToSettings()
        {
            PageNavManager.NavigateTo("/settings");
        }

        #endregion

        /// <summary>
        /// This method ensures the availability of app state and sets the current user info.
        /// If the app state is not present it will redirect to the login page.
        /// Once it is confirmed that the app state is loaded the CurrentUserId and UserName is set
        /// taking into account the optional settings for  user override id.
        /// It also starts the location services.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            Initialized = false;

            bool IdxDBInitOk = await DataService.LoadAppStateAsync();

            if (IdxDBInitOk)
            {
                CurrentUserId = DataService?.AppState?.EmployeeId ?? 0;
            }
            else
            {
                if (!string.IsNullOrEmpty(Token))
                    NavigateToLogin(Token);
                else
                    NavigateToLogin();
                return;
            }

            // found external token on a page
            if (!string.IsNullOrEmpty(Token))
            {
                NavigateToLogin(Token);
                return;
            }

            if (CurrentUserId > 0)
            {
                if (SettingsService.UserOverrideId != 0)
                {
                    CurrentUserId = SettingsService.UserOverrideId;
                    UserName = $"TST{CurrentUserId}";
                }
                else
                {
                    // CurrentUserId = DataService.AppState!.EmployeeId!.Value;
                    UserName = DataService.AppState!.UserName!;
                }
                DataService.SyncEnabled = true;

                //await ScratchPadSvc.LoadAsync(CurrentUserId);
                

                await LocationService.StartOrResetWatchAsync();
            }
            else
            {
                DataService.AppState.LoginRedirect = PageNavManager.Uri;
                await DataService.SaveAppStateInstance();
                NavigateToLogin();
            }
        }

        public static string TextToHtml(string? text)
        {
            if (string.IsNullOrEmpty(SanitizeString(text))) return string.Empty;

            // Emails - use Regex.Replace to handle each match once
            var emailParser = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            text = emailParser.Replace(text, m => $"<a href=\"mailto:{m.Value}\">{m.Value}</a>");

            // Links
            if (text.Contains("://"))
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                text = linkParser.Replace(text, m => $"<a href=\"{m.Value}\" target='_blank'>{m.Value.Replace("https://", "")}</a>");
            }

            // Phone Numbers
            var phoneParser = new Regex(@"\b(?:\d{3}[-.\s]??\d{3}[-.\s]??\d{4})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            text = phoneParser.Replace(text, m => $"<a href=\"tel:{m.Value}\">{m.Value}</a>");

            // CRLF
            text = text.Replace("\r\n", "\r");
            text = text.Replace("\n", "\r");
            text = text.Replace("\r", "<br>\r\n");
            text = text.Replace("  ", " &nbsp;");
            text = text.Replace("<br>\r\n<br>\r\n", "<br>\r\n");

            return text;
        }

        public static string SanitizeString(string? Notes, string? replacewhat = null, string? replacewith = null)
        {
            // if this note is empty, null, or just filled with spaces and CR/LF, return empty
            if (string.IsNullOrEmpty(Notes)) return string.Empty;
            Notes = Notes.Replace("\r\n", ""); Notes = Notes.Replace("\r", ""); Notes = Notes.Replace("\n", ""); Notes = Notes.Trim();
            if (string.IsNullOrEmpty(Notes)) return string.Empty;

            if (replacewhat is not null && replacewith is not null)
            {
                Notes = Notes.Replace(replacewhat, replacewith);
            }

            return Notes;
        }

        public async Task LoadAndLaunchDispatch(int employeeId, int workordDispatchTechId, string tagToLaunchIfFound = "", int wfResultIdToLaunchIfFound = 0, int wfToLaunchIfFound = 0)
        {
            int? defaultWorkflowResultId = 0; // optional default workflow to launch
            int rowUserId = DataService!.AppState!.RowUserId;

            if (DataService.InternetConnected == false)
            {
                ClearLoadAndLaunchFlags();
                return;
            }

            if (await LocationService.WatcherState() == GeoStates.Error)
            {
                await DialogBox.WaitForDialogResultAsync(header: "GeoLocation Issue",
                        prompt: "There is a problem with your Geolocation settings.  Please terminate the app and restart it.  <br/><br/>When prompted for Location permissions, please ACCEPT.<br/><br/>When app starts, wait about 5 seconds before trying to start a workflow."
                        as string, okLabel: "Understood",
                        defaultButton: 1
                        );
                ClearLoadAndLaunchFlags();
                StateHasChanged();
                return;
            }

            // Are ya sure?
            bool ok = await Confirm("Ready to begin this dispatch?", "Start the Call");
            if (!ok)
            {
                ClearLoadAndLaunchFlags();
                StateHasChanged();
                return;
            }

            Portal.Data.Commands.Models.BinderManagerInput binderManagerInput = new Portal.Data.Commands.Models.BinderManagerInput()
            {
                BinderId = 0, // new binder
                WorkflowId = 0, // no workflow - build automatically
                WorkOrderDispatchTechId = workordDispatchTechId, // this tech/dispatch
                EmployeeId = 0,
                RowUserId = DataService.AppState.RowUserId
            };

            // Ok Let's launch this rocket!
            var response = await HTTPService.GetHttpCommandsClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Commands.Models.BinderManagerOutput>(binderManagerInput);
            if (response.IsSuccess)
            {
                var data = response.Data!;
                if (data.ReturnValue == Portal.Data.Commands.Models.BinderManagerOutput.Returns.Ok)
                {
                    // Get the workflows for the binder we just created!
                    Portal.Data.Queries.Models.WorkflowResultListInput input = new Portal.Data.Queries.Models.WorkflowResultListInput() { BinderId = data.BinderId ?? 0 };
                    var myBinderWFList = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Queries.Models.WorkflowResultListOutput>(input);
                    if (myBinderWFList.IsSuccess)
                    {
                        defaultWorkflowResultId = 0;

                        var myBinderWFListOutput = myBinderWFList.Data!;
                        if (myBinderWFListOutput.ReturnValue == Portal.Data.Queries.Models.WorkflowResultListOutput.Returns.Ok)
                        {
                            // Load all workflows assigned to this tech and this dispatch
                            List<Portal.Data.Queries.Models.WorkflowResultListResult> myAssignedWorkflows = myBinderWFListOutput.ResultData!.FindAll(w => w.WorkOrderDispatchTechId.Equals(workordDispatchTechId) && w.Completed is null && w.Active.Equals(true));

                            // Find WF Marked as AutoStart (must also be marked AutoLoad, duh!)
                            var foundAutoStart = myAssignedWorkflows.Find(w => w.AutoStart == true && w.AutoLoad == true);
                            if (foundAutoStart != null) defaultWorkflowResultId = foundAutoStart.WorkflowResultId;

                            // Try to find a specific workflow to launch if passed in to function as OVERRIDE tag to load!
                            //  if all vars passed, process tag first, then workflowresultid, then workflowid.
                            // Will Auto-Launch AUTOSTART flagged workflow automatically, no matter what.

                            if (!string.IsNullOrEmpty(tagToLaunchIfFound))
                            {
                                myAssignedWorkflows.ForEach(w => w.WorkflowTag = w.WorkflowTag.ToUpper() ?? "");
                                var found = myAssignedWorkflows.Find(w => w.WorkflowTag == tagToLaunchIfFound.ToUpper()); //&& w.Started == null
                                if (found != null)
                                {
                                    defaultWorkflowResultId = found.WorkflowResultId;
                                }
                            }
                            else
                            {
                                if (wfResultIdToLaunchIfFound > 0)
                                {
                                    var found = myAssignedWorkflows.Find(w => w.WorkflowResultId == wfResultIdToLaunchIfFound); //&& w.Started == null
                                    if (found != null)
                                    {
                                        defaultWorkflowResultId = found.WorkflowResultId;
                                    }
                                }
                                else
                                {
                                    if (wfToLaunchIfFound > 0)
                                    {
                                        var found = myAssignedWorkflows.Find(w => w.WorkflowId == wfToLaunchIfFound); //&& w.Started == null
                                        if (found != null)
                                        {
                                            defaultWorkflowResultId = found.WorkflowResultId;
                                        }
                                    }
                                    ;
                                }
                            }

                            // if after all that, we still don't have a default workflow, just use the first one.
                            if (defaultWorkflowResultId == 0) defaultWorkflowResultId = myAssignedWorkflows[0].WorkflowResultId;

                            // Launch the workflow!
                            if (defaultWorkflowResultId > 0)
                            {

                                foreach (var item in myAssignedWorkflows.Where(x => x.AutoLoad == true))
                                {
                                    // Console.WriteLine("AutoLoad to IndexDB WorkflowResultId: " + item.WorkflowResultId.ToString());
                                    await DataService.GetWorkflowAsync(item.WorkflowResultId);
                                }

                                await Task.Delay(350);
                                // Launch the default workflow first!
                                PageNavManager.NavigateTo($"/workflow-runner/{defaultWorkflowResultId}");
                                ClearLoadAndLaunchFlags();
                                return;
                            }
                        }
                    }
                }
            }
            ClearLoadAndLaunchFlags(); // stop spinner

        }

        public async Task LoadAndLaunchWorkflow(int employeeId, int workflowId = 0, string tagToLaunchIfFound = "", string summaryText = "", int workordDispatchTechId = 0)
        {
            // Note: if summaryText param is passed with a value - you can have ONE-TAP start workflows!  Consider it!

            if (LoadingWorkflow || DataService is null)
                return;

            // set in-progress Flags();
            Initialized = false; // start spinner
            LoadingWorkflow = true; // prevents double-tap (starting multiple of same workflow)

            //try
            {
                int? defaultWorkflowResultId = 0; // optional default workflow to launch - that'd be strange

                int rowUserId = DataService?.AppState?.RowUserId ?? 0; // CurrentUserId  is global with base page
                int foundMode = 0; // 0=none found, 1=found existing local workflow to resume

                if (workflowId == 0 && string.IsNullOrEmpty(tagToLaunchIfFound))
                {
                    await Alert("No checklist specified to launch.");
                    ClearLoadAndLaunchFlags();
                    return;
                }

                if (await LocationService.WatcherState() == GeoStates.Error)
                {
                    await DialogBox.WaitForDialogResultAsync(header: "GeoLocation Issue",
                            prompt: "There is a problem with your Geolocation settings.  Please terminate the app and restart it.  <br/><br/>When prompted for Location permissions, please ACCEPT.<br/><br/>When app starts, wait about 5 seconds before trying to start a workflow."
                            as string, okLabel: "Understood",
                            defaultButton: 1
                            );
                    ClearLoadAndLaunchFlags();
                    return;
                }

                // First - VERIFY WE DON'T HAVE THIS LOADED OR ALLOCATED ALREADY, and if so, warn
                if (workordDispatchTechId.Equals(0))
                {
                    // This is a non-tech dispatch.  Start of Day?  End of Day? Something light and fun
                    // go digging thru all raw-dog workflowresults data to see if this workflow is already started for this user

                    List<ICS.Portal.Data.Queries.Models.WorkflowResultDetailResult.Workflow>? LoadedWorkflows;
                    LoadedWorkflows = await DataService.GetLocalWorkflows(null, workflowId, employeeId);
                    defaultWorkflowResultId = LoadedWorkflows?.Find(x =>
                        x.Completed is null
                        && x.EmployeeId.Equals(CurrentUserId)
                        && x.WorkflowId.Equals(workflowId)
                      )?.WorkflowResultId ?? 0;
                }
                else
                {
                    // So you're adding another WF to a binder while connected to a real dispatch
                    //   Check WorkflowResultList store for dispatchtechid/workflowid where the job is NOT Complete
                    List<ICS.Portal.Data.Queries.Models.WorkflowResultListResult>? LoadedWorkflows;
                    LoadedWorkflows = await DataService.GetWorkflowResultList(0, workordDispatchTechId);
                    defaultWorkflowResultId = LoadedWorkflows?.Find(x =>
                        x.Completed is null
                        && x.WorkOrderDispatchTechId.Equals(workordDispatchTechId)
                        && x.WorkflowId.Equals(workflowId)
                      )?.WorkflowResultId ?? 0;
                }

                if (defaultWorkflowResultId > 0)
                    foundMode = 1;

                // await DeleteLocalWorkflowAsync(workflowResultId);

                if (foundMode == 0)
                {
                    if (await DataService.CheckInternet() == false)
                    {
                        await Alert("Cannot begin a new checklist without an internet connection.  Please try again when re-connected.");
                        ClearLoadAndLaunchFlags();
                        return;
                    }
                    // Get the workflows for JUST this tech and this specific dispatch from SERVER (if connected)
                    var MyAllocatedWorkflows = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Queries.Models.WorkflowResultListOutput>(
                        new Portal.Data.Queries.Models.WorkflowResultListInput()
                        {
                            WorkOrderDispatchTechId = workordDispatchTechId,
                            EmployeeId = employeeId
                        });

                    if (MyAllocatedWorkflows is not null && MyAllocatedWorkflows.IsSuccess)
                    {
                        var output = MyAllocatedWorkflows.Data!;
                        if (output.ReturnValue == Portal.Data.Queries.Models.WorkflowResultListOutput.Returns.Ok)
                        {
                            // Load all workflows assigned to this tech and this dispatch
                            List<Portal.Data.Queries.Models.WorkflowResultListResult> myAssignedWorkflows = output.ResultData!.FindAll(
                                w => w.Completed is null && w.Active == true && w.WorkflowId.Equals(workflowId));
                            if (myAssignedWorkflows != null && myAssignedWorkflows.Count > 0)
                            {
                                myAssignedWorkflows.Sort((a, b) => a.Created.CompareTo(b.Created));
                                // Default to run is oldest one!
                                defaultWorkflowResultId = myAssignedWorkflows[0].WorkflowResultId;
                            }
                        }
                    }
                }

                // Found one?  Ask if they want to start another one
                if (defaultWorkflowResultId > 0)
                {
                    int behavior = 0;
                    if (foundMode == 1)
                    {
                        string existsPrompt = string.Empty;
                        if (!workordDispatchTechId.Equals(0))
                            existsPrompt = "Looks like you already have this checklist in progress. " +
                                           "To avoid mix-ups, finish or delete that one before starting another." +
                                           "<br/><br/>Resume current or begin another?";
                        else
                            existsPrompt = "Looks like you've already started this.  " +
                                "<br/><br/>Let's resume where you left off and complete it!";

                        behavior = await DialogBox.WaitForDialogResultAsync(header: "Quick Heads-Up",
                                prompt: existsPrompt as string,
                                okLabel: "Start New", optLabel: "RESUME", cancelLabel: "Cancel",
                                defaultButton: 3
                                );

                        if (behavior == 3)
                        {
                            PageNavManager.NavigateTo($"/workflow-runner/{defaultWorkflowResultId}");
                            ClearLoadAndLaunchFlags();
                            return;
                        }

                        if (behavior == 2)
                        {
                            ClearLoadAndLaunchFlags();
                            return;
                        }
                    }
                }

                // Ok Let's launch this workflow rocket
                if (await DataService.CheckInternet() == false)
                {
                    await Alert($"Sorry, it looks like you don't have internet connectivity.<br/><br/>Please toggle Airplane Mode on your device to attempt a better cell connection. <br/><br/>If that doesn't work, contact the ICS Help Desk to let them know you cannot create workflows.", "Unknown Error");
                    ClearLoadAndLaunchFlags();
                    return;
                }

                if (defaultWorkflowResultId.Equals(0))
                {
                    if (!string.IsNullOrEmpty(summaryText))
                    {
                        // Are ya sure?  Only ask if summaryText is passed with a value, that way you can have ONE-TAP start workflows.
                        int behavior = await DialogBox.WaitForDialogResultAsync(header: "Begin Checklist",
                            prompt: $"Ready to begin {summaryText} Checklist?"
                            as string, okLabel: "START", cancelLabel: "Cancel",
                            defaultButton: 1
                            );

                        if (behavior == 2)
                        {
                            ClearLoadAndLaunchFlags();
                            StateHasChanged();
                            return;
                        }

                    }
                }

                var response = await HTTPService.GetHttpCommandsClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Commands.Models.BinderManagerOutput>(new Portal.Data.Commands.Models.BinderManagerInput()
                {
                    BinderId = 0, // new binder
                    WorkflowId = workflowId,
                    WorkflowTag = tagToLaunchIfFound,
                    WorkOrderDispatchTechId = workordDispatchTechId,
                    EmployeeId = employeeId,
                    RowUserId = rowUserId // required for security!
                });

                if (response.IsSuccess)
                {
                    var data = response.Data!;
                    if (data.ReturnValue == Portal.Data.Commands.Models.BinderManagerOutput.Returns.Ok)
                    {
                        // Get the workflows for JUST this tech and this specific dispatch
                        Portal.Data.Queries.Models.WorkflowResultListInput input = new Portal.Data.Queries.Models.WorkflowResultListInput()
                        {
                            WorkflowId = workflowId,
                            EmployeeId = employeeId
                        };

                        var myWFList = await HTTPService.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<Portal.Data.Queries.Models.WorkflowResultListOutput>(input);
                        if (myWFList.IsSuccess)
                        {
                            var output = myWFList.Data!;
                            if (output.ReturnValue == Portal.Data.Queries.Models.WorkflowResultListOutput.Returns.Ok)
                            {
                                // Load all workflows assigned to this tech and this dispatch
                                List<Portal.Data.Queries.Models.WorkflowResultListResult> myAssignedWorkflows = output.ResultData!.FindAll(w => w.Completed == null && w.Active == true);

                                if (myAssignedWorkflows is not null && myAssignedWorkflows.Count > 0)
                                {
                                    myAssignedWorkflows.Sort((a, b) => b.Created.CompareTo(a.Created));  // sort DESCENDING so we get the most recent one

                                    // Default to run is first one!
                                    defaultWorkflowResultId = myAssignedWorkflows[0].WorkflowResultId;

                                    // Find the "default workflow" to launch.
                                    // If tag passed, process tag  and find workflow in resultlist with that tag
                                    if (!string.IsNullOrEmpty(tagToLaunchIfFound))
                                    {
                                        myAssignedWorkflows.ForEach(w => w.WorkflowTag = w.WorkflowTag.ToUpper() ?? "");
                                        var found = myAssignedWorkflows.Find(w => w.WorkflowTag == tagToLaunchIfFound.ToUpper());
                                        if (found != null)
                                            defaultWorkflowResultId = found.WorkflowResultId;
                                    }

                                    // Launch the workflow. Yes, not kidding.  Here we are - the final moment!
                                    if (defaultWorkflowResultId > 0)
                                    {
                                        PageNavManager.NavigateTo($"/workflow-runner/{defaultWorkflowResultId}");
                                        ClearLoadAndLaunchFlags();
                                        return;
                                    }
                                }
                                else
                                {
                                    // Error Loading new workflow - go back to dispatch list
                                    NavigateToDispatchDetail();
                                    ClearLoadAndLaunchFlags();
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    await Alert($"Sorry, it looks like you don't have internet connectivity.<br/><br/>Please toggle Airplane Mode on your device to attempt a better cell connection. <br/><br/>If that doesn't work, contact the ICS Help Desk to let them know you cannot create workflow {workflowId} due to {response.StatusCode} returning {response?.Data?.ReturnValue.ToString() ?? "Unknown Error"}.");
                    ClearLoadAndLaunchFlags(); // stop spinner and clear wf loading flag
                }

                //}
                //catch (Exception ex)
                //{
                //    _ = DataService.TryWriteError(ex, nameof(LoadAndLaunchWorkflow));
                //    await Alert($"An unexpected error occurred while trying to launch the checklist: {ex.Message}", "Call Tech Support");
                //}
                //finally
                //{
                ClearLoadAndLaunchFlags();
                //}
            }
        }

        public void ClearLoadAndLaunchFlags()
        {
            Initialized = true;
            LoadingWorkflow = false;
        }

        public async Task Alert(string message, string heading = "")
        {
            if (string.IsNullOrEmpty(heading)) heading = "ALERT";
            await DialogBox.WaitForDialogResultAsync(header: heading, prompt: message, okLabel: "OK", defaultButton: 1);
        }
        public async Task<bool> Confirm(string message, string heading = "Please Confirm", string okbtn = " Yes ", string cancelbtn = " No ")
        {
            if (string.IsNullOrEmpty(heading)) heading = "Question for You";

            int ret = await DialogBox.WaitForDialogResultAsync(header: heading, prompt: message, okLabel: okbtn, cancelLabel: cancelbtn, defaultButton: 2);

            if (ret == 1)
                return true;

            return false;
        }

        #region Date Formatting Functions

        public string DateFormatAs(DateTime dts, string formatAs)
        {
            if (string.IsNullOrEmpty(formatAs))
                return string.Empty;
            return dts.ToString(formatAs, System.Globalization.CultureInfo.InvariantCulture);
        }
        public string DateLabelDisplay(DateTimeOffset? dt)
        {
            if (!dt.HasValue)
                return "";

            // Convert to local time
            DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();

            // Determine if minutes are '00' and set time format accordingly
            bool shouldOmitMinutes = d.Minute == 0;

            string timepart = shouldOmitMinutes ? d.ToString("h:mm").Split(":").FirstOrDefault() : d.ToString("h:mm");

            // Get AM/PM designator and reduce to 'a' or 'p'
            string amPmDesignator = d.ToString("tt").ToLower()[0].ToString(); // Converts 'AM' to 'a' and 'PM' to 'p'

            string monDate = d.ToString("M/dd/yy");
            // Combine date and time with modified AM/PM designator
            return $"{monDate} {timepart}{amPmDesignator}";
        }
        public string GetDueDate(DateTimeOffset? dt)
        {
            if (!dt.HasValue) return "";
            DateTime d = dt.Value.DateTime.ToUniversalTime().ToLocalTime();
            return d.ToString("ddd M/dd");

        }
        public string GetDueTime(string? d)
        {
            if (!string.IsNullOrEmpty(d) && DateTimeOffset.TryParse(d, out DateTimeOffset dt))
            {
                return GetDueTime(dt);
            }
            return string.Empty;
        }
        public string GetDueTime(DateTimeOffset? dt)
        {
            if (!dt.HasValue) return "";
            DateTime d = dt.Value.LocalDateTime;
            bool shouldOmitMinutes = d.Minute == 0;
            string? timepart = shouldOmitMinutes ? d.ToString("htt").ToLower() : d.ToString("h:mmtt").ToLower();
            if (!string.IsNullOrEmpty(timepart))
            {
                timepart = timepart.Left(timepart.Length - 1);
            }
            return timepart;
        }

        #endregion




    }
}