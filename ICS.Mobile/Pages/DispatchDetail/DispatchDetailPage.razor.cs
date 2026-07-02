//using ICS.Mobile.Components.ScratchPad;
using ICS.Mobile.DataModels.Local;
using ICS.Mobile.Pages.DispatchDetail.Components;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Runtime.InteropServices;


namespace ICS.Mobile.Pages.DispatchDetail
{
    public partial class DispatchDetailPage : PageBase
    {

        #region Page Variables
        [Inject] protected IJSRuntime JSRuntime { set; get; } = default!;
        
        
        // BLUON INTEGRATION
        private string IsBluonEnabled = "auto";  // on/off/auto
        private bool bUseBluon = false;

        private string ChecklistHeaderTitle = "Checklists";
        private string ChecklistHeaderStatus = "Active";

        // visit history expand/collapse state
        private HashSet<string> expandedVisitAppwideList = new HashSet<string>();

        private bool ShowOnlyActiveChecklists { get; set; } = true;
        private bool ChecklistHeaderShowFilter { set; get; } = true;
        private bool ShowAddChecklists { get; set; } = false;
        private int AddOnCount { get; set; } = 0;
        private bool ShowChecklists { get; set; } = false;
        private bool IsPending { get; set; } = false;
        private int DispatchId { set; get; }
        private int DispatchTechId { set; get; }

        private void ViewModeChanged(ChecklistHeader.ViewMode mode)
        {
            // callback from ChecklistHeader Switch Control filter view
            switch (mode)
            {
                case ChecklistHeader.ViewMode.Active:
                    ChecklistHeaderStatus = "Active";
                    ShowOnlyActiveChecklists = true;
                    break;
                case ChecklistHeader.ViewMode.Completed:
                    ChecklistHeaderStatus = "Completed";
                    ShowOnlyActiveChecklists = false;
                    break;
                default:
                    break;
            }
        }

        // Workflows that are started but not complete
        private List<WorkflowResultListResult>? InProgressWorkflows = null;

        // Workflows that are completed
        private List<WorkflowResultListResult>? CompletedWorkflows = null;

        // List of available workflows (for adding)
        private List<WorkflowResultListResult>? AvailableWorkflows = null;

        //Loaded on demand (Show Prior Visits)
        private List<WorkflowResultListResult> PriorVisitWorkflows = new();

        private List<WorkflowResultListResult>? LoadedWorkflows = null;
        private List<WorkflowResultListResult>? LoadedComplete = null;


        private string AllLocationWorkflowsStatus = "";
        private List<DispatchesByLocationResult>? DispatchesByLocation = null;
        private DispatchTracker? DispTrackItem { set; get; }

        #endregion

        #region OnInit and LoadData

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (!(DataService.AppState.DispatchId.HasValue && DataService.AppState.DispatchTechId.HasValue))
            {
                _ = DataService.TryWriteError("Dispatch detail page called without values populated in app state.", nameof(OnInitialized));
                NavigateToDispatchList();
            }
            else
            {
                DispatchId = DataService.AppState.DispatchId.Value;
                DispatchTechId = DataService.AppState.DispatchTechId.Value;
            }

            // Check for Bluon API setup and infer use
            if (IsBluonEnabled.ToLower() == "auto" || IsBluonEnabled.ToLower() == "on")
                if (!string.IsNullOrEmpty(SettingsService.BluonApiKey)) bUseBluon = true; else bUseBluon = false;
            else
                bUseBluon = false;

            // Get Saved Expanded Visit List
            var savedVisitStateOpenedList = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "expandedVisitAppwideList");
            if (!string.IsNullOrEmpty(savedVisitStateOpenedList))
                expandedVisitAppwideList = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(savedVisitStateOpenedList) ?? new();
            else
                expandedVisitAppwideList = new();

            // auto-reduce list size if over 48 items
            if (expandedVisitAppwideList is not null && expandedVisitAppwideList.Any() && expandedVisitAppwideList.Count > 48)
            {
                HashSet<string> NewExpandedVisit = new HashSet<string>(
                    expandedVisitAppwideList.Skip(expandedVisitAppwideList.Count - 24).Take(24)
                );

                expandedVisitAppwideList?.Clear(); expandedVisitAppwideList = null;
                expandedVisitAppwideList = NewExpandedVisit;

            }


            // Let's get the details!
            await LoadDispatchAsync();

            // all done, son!
            Initialized = true;

        }

        public async Task LoadDispatchAsync()
        {
            try
            {
                LookupsOutput? lookupsOutput = await DataService.GetIDXDBRecord<LookupsOutput>("1");
                DispatchDetailOutput? dispatchDetailOutput = await DataService.GetIDXDBRecord<DispatchDetailOutput>(DispatchId.ToString());
                WorkflowListAvailableOutput? workflowListAvailableOutput = await DataService.GetIDXDBRecord<WorkflowListAvailableOutput>(DispatchTechId.ToString());
                WorkflowResultListOutput? workflowResultListOutput = await DataService.GetIDXDBRecord<WorkflowResultListOutput>(DispatchTechId.ToString());

                var lookups = lookupsOutput?.ResultData ?? null;
                var dispatch = dispatchDetailOutput?.ResultData ?? null;
                var availableWorkflows = workflowListAvailableOutput?.ResultData ?? null;
                var workflowResults = workflowResultListOutput?.ResultData ?? null;

                if (1 == 1) // if (await DataService.CheckInternet())
                {
                    var result = await DataService.GetLookupsAsync();
                    if (result is not null)
                    {
                        lookups = result;
                    }
                }

                if (1 == 1) // if (await DataService.CheckInternet())
                {
                    var result = await DataService.GetDispatchDataAsync(DispatchId, DispatchTechId,true);
                    if (result is not null)
                    {
                        dispatch = result;
                    }
                }

                if (1 == 1) // if (await DataService.CheckInternet())
                {
                    var result = await DataService.GetWorkflowListAvailable(DispatchId, DispatchTechId);
                    if (result is not null && result.Count != 0)
                    {
                        availableWorkflows = result;
                    }
                }

                if (1 == 1) // if (await DataService.CheckInternet())
                {
                    var result = await DataService.GetWorkflowResultList(DispatchId, DispatchTechId);
                    if (result is not null && result.Count != 0)
                    {
                        workflowResults = result;
                    }
                }


                // This will hold WorkflowResultList + active with duplicates removed
                List<WorkflowResultListResult> allActiveWorkflows = new List<WorkflowResultListResult>();

                if (dispatch is not null && availableWorkflows is not null)
                {
                    // These are workflows that have been loaded and actively reside in idxdb
                    List<WorkflowResultDetailResult.Workflow> localActiveWorkflows = await DataService.GetLocalWorkflows(DispatchTechId);

                    if (workflowResults is not null && workflowResults.Count != 0)
                    {
                        allActiveWorkflows.AddRange(workflowResults);
                    }

                    // Add the locals
                    if (localActiveWorkflows is not null)
                    {
                        foreach (var localActiveWorkflow in localActiveWorkflows)
                        {
                            if (!allActiveWorkflows.Any(aw => aw.WorkflowResultId == localActiveWorkflow.WorkflowResultId))
                            {
                                var availableWorkflow = availableWorkflows?.FirstOrDefault(w => w.Id == localActiveWorkflow.WorkflowId);
                                if (availableWorkflow != null)
                                {
                                    allActiveWorkflows.Add(ConvertWorkflow(localActiveWorkflow, availableWorkflow));
                                }
                            }
                        }
                    }

                    allActiveWorkflows.Sort((a, b) => a.Prompt.CompareTo(b.Prompt));
                    InProgressWorkflows = allActiveWorkflows!.FindAll(w => w.Completed is null);
                    CompletedWorkflows = allActiveWorkflows!.FindAll(w => w.Completed.HasValue);
                    DispTrackItem = BuildTracker(dispatch, lookups, availableWorkflows);

                    StateHasChanged();
                }

                if (DispTrackItem is null)
                {
                    if (DataService.InternetConnected == false)
                    {
                        await Alert("No internet, cannot load dispatch.", "No Internet");

                    }
                    else
                    {
                        await Alert("Sorry, this dispatch has not been downloaded.  Please try again when you have internet connectivity.", "Dispatch Error");
                    }

                    NavigateToDispatchList();
                }
            }
            catch (Exception ex)
            {
                _ = DataService.TryWriteError(ex, nameof(DispTrackItem));
                await Alert(ex.Message, "Unhandled exception");
                NavigateToDispatchList();
            }
        }
        private DispatchTracker BuildTracker(DispatchDetailResult dispatchDetail, LookupsResult? lookups, List<WorkflowListAvailableResult> availableWorkflows)
        {
            var result = new DispatchTracker()
            {
                Dispatch = dispatchDetail.DispatchResult,
                DispatchTechs = dispatchDetail.DispatchTechsResult,
                Customer = dispatchDetail.CustomerResult,
                CustomerLocation = dispatchDetail.CustomerLocationResult,
                CustomerLocationContact = dispatchDetail.CustomerLocationContactResult,
                CustomerLocationContactComms = dispatchDetail.CustomerLocationContactCommunicationResult,
                WorkOrderDispatchId = DispatchId,
                WorkOrderDispatchTechId = DispatchTechId,
                EquipmentList = WorkflowEquipmentBuilder.GenerateEquipmentList(dispatchDetail, lookups),
                AddOnWorkflows = availableWorkflows
            };

            AddOnCount = result.AddOnWorkflows.Count;

            // SHOW OR HIDE CHECKLISTS FOR THIS DISPATCH

            // Get my current status
            string stat = result.DispatchTechs?.Find(x => x.Id.Equals(result.WorkOrderDispatchTechId))?.TechStatus ?? "";

            if (stat.Contains("ending", StringComparison.CurrentCultureIgnoreCase))
            {
                ShowChecklists = false;
                IsPending = true;
            }
            else if (stat.Contains("Complete", StringComparison.CurrentCultureIgnoreCase))
            {
                ShowChecklists = false;
                IsPending = false;
            }
            else
            {
                ShowChecklists = true;
                IsPending = false;
            }

            if (string.IsNullOrEmpty(stat)) ShowChecklists = false;

            // Last-Chance Check for overrides
            if (!ShowChecklists)
            {
                // if there are some in here already, perhaps app was reloaded, or portal pushed down some checklists, show it man.
                if (LoadedWorkflows is not null && (LoadedWorkflows.Count(x => x.WorkOrderDispatchTechId.Equals(DispatchTechId)) > 0))
                {
                    ShowChecklists = true;
                }
                else if (LoadedComplete is not null && (LoadedComplete.Count(x => x.Completed is not null && x.WorkOrderDispatchTechId.Equals(DispatchTechId)) > 0))
                {
                    ShowChecklists = true;
                }
                else if (CompletedWorkflows is not null && (CompletedWorkflows.Count(x => x.Completed is not null && x.WorkOrderDispatchTechId.Equals(DispatchTechId)) > 0))
                {
                    ShowChecklists = true;
                }
                else
                {
                    IsPending = true;
                }
            }

            return result;
        }

        #endregion

        #region Page On Event Functions

        private void OnHideCompleted()
        {
            ViewModeChanged(ChecklistHeader.ViewMode.Active);
        }

        private void OnShowCompleted()
        {
            ViewModeChanged(ChecklistHeader.ViewMode.Completed);
        }

        private async Task OnRemoveWorkflowClick(int wfResultsId)
        {
            await DeleteWorkflow(wfResultsId);
        }

        private async Task OnGotoWorkflowAddOn(int wf)
        {
            if (DispTrackItem is not null)
            {
                if (DispTrackItem.AddOnWorkflows is not null && DispTrackItem is not null && DispTrackItem.AddOnWorkflows.Count > 0)
                {
                    var workflow = DispTrackItem.AddOnWorkflows.Find(x => x.Id.Equals(wf));
                    if (workflow is not null)
                    {
                        await LoadAndLaunchWorkflow(CurrentUserId, workflow.Id, "", workflow.Prompt, DispTrackItem.WorkOrderDispatchTechId);
                    }
                }
            }
        }

        public WorkflowResultListResult ConvertWorkflow(WorkflowResultDetailResult.Workflow result, WorkflowListAvailableResult workflow)
        {
            return new WorkflowResultListResult(
                WorkflowResultId: result.WorkflowResultId,
                WorkflowId: result.WorkflowId,
                BinderId: result.BinderId,
                Active: result.Active,
                ShareExternally: result.ShareExternally,
                AutoLoad: workflow.AutoLoad,
                AutoStart: workflow.AutoStart,
                RecapBehavior: null,
                Name: result.Name,
                Description: result.Description,
                Prompt: result.Prompt,
                WorkflowTagId: result.WorkflowTagId,
                WorkflowTag: result.WorkflowTag,
                NotificationGroupId: result.NotificationGroupId,
                NotificationGroup: result.NotificationGroup,
                HelpText: result.HelpText,
                EmployeeId: result.EmployeeId,
                CustomerId: result.CustomerId,
                CustomerLocationId: result.CustomerLocationId,
                WorkOrderDispatchId: result.WorkOrderDispatchId,
                WorkOrderDispatchTechId: result.WorkOrderDispatchTechId,
                RunOnceId: result.RunOnceId,
                Created: result.Created,
                Started: result.Started,
                Completed: result.Completed,
                Status: result.Status,
                ExternalKey: result.ExternalKey,
                EntityId: result.EntityId,
                Hash: result.Hash,
                RowUserId: result.RowUserId,
                CustomerFullName: "TODO",
                LocName: "TODO",
                DivisionId: 0,
                EmpName: null,
                Employee: null,
                EmpNo: null,
                Tasks: null,
                ESCDispatchNum: null,
                ClientPONum: null,
                RequireDispatch: true,
                LastUpdated: DateTime.Now,
                TimezoneId: null,
                TimezoneOffset: 0,
                FormattedAddress: null,
                Latitude: 0m,
                Longitude: 0m,
                Zone: null,
                DispatchTechKey: null,
                Summary: null
                );
        }

        private async Task LoadPastWorkflows()
        {
            try
            {
                AllLocationWorkflowsStatus = "";

                if (DispTrackItem is not null && DispTrackItem.Dispatch is not null && DispTrackItem.CustomerLocation is not null)
                {
                    var result = await HTTPService!.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<WorkflowResultListOutput>(new WorkflowResultListInput() { CustomerLocationId = DispTrackItem.Dispatch.CustomerLocationID });
                    if (result.IsSuccess)
                    {
                        var output = result.Data!;
                        if (output.IsSuccess())
                        {
                            PriorVisitWorkflows = [.. output.ResultData!.OrderByDescending(x => x.LastUpdated)];
                        }
                        else
                        {
                            PriorVisitWorkflows = new();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = DataService.TryWriteError(ex, nameof(LoadPastWorkflows));
            }
            finally
            {
                if (PriorVisitWorkflows is null || PriorVisitWorkflows.Count == 0)
                {
                    AllLocationWorkflowsStatus = "No Past Data Found";
                }
            }
        }

        private void OnGoToWorkflowLoadedClick(int wfResultsId)
        {
            if (LoadedWorkflows is not null)
            {
                var workflow = LoadedWorkflows.Find(x => x.WorkflowResultId.Equals(wfResultsId));
                if (workflow is not null)
                {
                    string DestLink = MakeWFLink(workflow.WorkflowResultId, CurrentUserId, workflow.EmployeeId, workflow.Started.HasValue, workflow.Completed.HasValue);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
                else
                {
                    OnGoToWorkflowAllocatedClick(wfResultsId);
                }
            }
            else
            {
                OnGoToWorkflowAllocatedClick(wfResultsId);
            }
        }
        private void OnGoToWorkflowAllocatedClick(int wfResultsId)
        {
            if (InProgressWorkflows is not null)
            {
                var workflow = InProgressWorkflows.Find(x => x.WorkflowResultId.Equals(wfResultsId));
                if (workflow is not null)
                {
                    string DestLink = MakeWFLink(workflow.WorkflowResultId, CurrentUserId, workflow.EmployeeId, workflow.Started.HasValue, workflow.Completed.HasValue);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
            }
        }
        private void OnGoToWorkflowLoadedCompleteClick(int wfResultsId)
        {
            if (LoadedComplete is not null)
            {
                var workflow = LoadedComplete.Find(x => x.WorkflowResultId.Equals(wfResultsId));
                if (workflow is not null)
                {
                    string DestLink = MakeWFLink(workflow.WorkflowResultId, CurrentUserId, workflow.EmployeeId, workflow.Started.HasValue, workflow.Completed.HasValue);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
            }
        }
        private void OnGoToWorkflowReviewClick(int wfResultsId)
        {
            if (wfResultsId == 0)
                return;

            OnGoToWorkflowLoadedClick(wfResultsId); // try loaded 1st
            OnGoToWorkflowAllocatedClick(wfResultsId); // try allocated 2nd
            OnGoToWorkflowLoadedCompleteClick(wfResultsId); // try loaded completed 3rd
            // get wf info from completed list
            if (PriorVisitWorkflows is not null && PriorVisitWorkflows.Count > 0)
            {
                var workflow = PriorVisitWorkflows.Find(x => x.WorkflowResultId.Equals(wfResultsId));
                if (workflow is not null)
                {
                    string DestLink = MakeWFLink(workflow.WorkflowResultId, CurrentUserId, workflow.EmployeeId, workflow.Started.HasValue, workflow.Completed.HasValue);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
            }
            else if (CompletedWorkflows is not null && CompletedWorkflows.Count > 0)
            {
                var workflow = CompletedWorkflows.Find(x => x.WorkflowResultId.Equals(wfResultsId));
                if (workflow is not null)
                {
                    string DestLink = MakeWFLink(workflow.WorkflowResultId, CurrentUserId, workflow.EmployeeId, workflow.Started.HasValue, workflow.Completed.HasValue);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
                else
                {
                    string DestLink = MakeWFLink(wfResultsId, CurrentUserId, CurrentUserId, true, true);
                    PageNavManager.NavigateTo($"{DestLink}");
                }
            }
            else
            {
                // Load totally any old workflow
                string DestLink = MakeWFLink(wfResultsId, CurrentUserId, CurrentUserId, true, true);
                PageNavManager.NavigateTo($"{DestLink}");
            }
        }
        private void OnAddChecklistClick()
        {
            // Add Checklist Clicked.  Optionally Toggle ChecklistHeader Filter Menu, Set Show Add Checklist to switch view
            //   Close Function is: OnAddChecklistCloseClick
            ChecklistHeaderTitle = "Add New";
            ChecklistHeaderShowFilter = false;
            ShowAddChecklists = true;

        }
        private void OnAddChecklistCloseClick()
        {
            // Add Checklist Close Clicked.  Show Filter Menu, Hide Add Checklist
            ShowAddChecklists = false;
            ChecklistHeaderTitle = "Checklists";
            ChecklistHeaderShowFilter = true;
            StateHasChanged();
        }

        private bool LoadAndLaunchDispatchInProgress = false;
        private void OnStartDispatch(int techId = 0)
        {
            // don't await this!
            Initialized = false;
            _ = LoadAndLaunchDispatch(CurrentUserId, techId, "");
        }
        private void OnMapButtonClick()
        {
            if (DispTrackItem!.CustomerLocation != null)
            {
                _ = ui.NavigateByLatLong(DispTrackItem!.CustomerLocation!.Latitude, DispTrackItem!.CustomerLocation!.Longitude);
            }
        }
        private async Task OnAddNote(int x = 0)
        {
            await LoadAndLaunchWorkflow(CurrentUserId, 67, "", "", x);
        }

        private async Task OnMacroWorkflowClick(int wfResultsId)
        {
            //Console.WriteLine(nameof(OnRemoveWorkflowClick));
            if (await Confirm($"Send Checklist {wfResultsId} to whom?"))
            {
                await Alert("Sent!");
            }
        }

        #endregion

        #region Page String Support Functions

        private string DispatchStatus
        {
            get
            {
                string result = "Unknown";

                if (DispTrackItem is not null && DispTrackItem.DispatchTechs is not null && DataService.AppState.DispatchTechId.HasValue)
                {
                    var dispatchTech = DispTrackItem.DispatchTechs.FirstOrDefault(dt => dt.Id == DataService.AppState.DispatchTechId);
                    if (dispatchTech is not null && !string.IsNullOrEmpty(dispatchTech.TechStatus))
                    {
                        result = dispatchTech.TechStatus;
                    }
                }
                return result;
            }
        }
        private string DispatchPriority
        {
            get
            {
                string result = "Unknown";
                if (DispTrackItem is not null && DispTrackItem.Dispatch is not null && !string.IsNullOrEmpty(DispTrackItem.Dispatch.Priority))
                {
                    result = DispTrackItem.Dispatch.Priority;
                }
                return result;
            }
        }
        private string MakeWFLink(int wrid, int usr, int empId, bool isStarted, bool isCompleted)
        {
            string retLink = string.Empty;
            if (empId == usr && !isCompleted)
            {
                retLink = $"/workflow-runner/{wrid}";
            }
            else
            {
                retLink = $"/workflow-runner/{wrid}/true";
            }

            return retLink;
        }
        
        private string EnableChecklistFilters(int a)
        {
            if (a > 0)
                ChecklistHeaderShowFilter = true;
            else
                ChecklistHeaderShowFilter = false;

            return string.Empty;
        }

        #endregion region

        #region Workflow Functions

        private async Task DeleteWorkflow(int workflowResultId)
        {
            if (await DataService.CheckInternet())
            {
                if (await Confirm("You're about to <strong>DELETE</strong> this checklist from your binder.<br><b>Are you sure?</br>", "DELETE WORKFLOW"))
                {
                    await DataService.DeleteWorkflowAsync(workflowResultId, DispatchId, DispatchTechId);


                    InProgressWorkflows?.RemoveAll(w => w.WorkflowResultId.Equals(workflowResultId));
                    LoadedWorkflows?.RemoveAll(w => w.WorkflowResultId.Equals(workflowResultId));
                }
            }
            else
            {
                await Alert("Cannot remove workflow while internet is not connected");
            }
        }

        #endregion

        #region Dispatch Functions

        public async Task LoadPastVisits()
        {
            int custLocId = DispTrackItem?.CustomerLocation?.Id ?? 0;

            if (custLocId == 0)
                return;

            if (DispatchesByLocation is not null)
                return;

            try
            {
                // Go get all dispatches
                var response = await HTTPService!.GetHttpQueriesClient(DataService.AppState.AuthorizedUser!).PostAsync<DispatchesByLocationOutput>(new DispatchesByLocationInput()
                {
                    CustomerLocationId = custLocId
                });
                if (response.IsSuccess)
                {
                    var output = response.Data!;


                    if (output.ReturnValue == DispatchesByLocationOutput.Returns.Ok)
                    {
                        DispatchesByLocation = output.ResultData;
                        await LoadPastWorkflows();
                    }
                    else
                        DispatchesByLocation = null;
                }
            }
            catch { }


            //StateHasChanged();
        }

        #endregion

        #region Visit Expand/Collapse
        private string GetVisitId(int id)
        {
            // Create a unique identifier for each equipment item
            return $"{id}";
        }

        private bool IsVisitExpanded(int id)
        {
            return expandedVisitAppwideList.Contains(GetVisitId(id));
        }

        private async Task ToggleVisitExpanded(int id)
        {
            if (id > 0)
            {
                string visitId = GetVisitId(id);
                if (expandedVisitAppwideList.Contains(visitId))
                    expandedVisitAppwideList.Remove(visitId);
                else
                    expandedVisitAppwideList.Add(visitId);

                // Save to localStorage
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(expandedVisitAppwideList);
                    await JSRuntime.InvokeVoidAsync("localStorage.setItem", "expandedVisitAppwideList", json);
                }
                catch { }

                StateHasChanged();
            }

        }
        #endregion


    }

    public class DispatchTracker
    {
        public int WorkOrderDispatchId { get; set; } = 0;  // THIS IS THE PAGE-LEVEL "CURRENT DISPATCH"  TODO: THis should come from PageBase Global
        public int WorkOrderDispatchTechId { get; set; } = 0; // THIS IS THE PAGE-LEVEL "CURRENT DISPATCH"  TODO: THis should come from PageBase Global
        public DispatchDetailResult.Dispatch? Dispatch = null;
        public DispatchDetailResult.Customer? Customer = null;
        public DispatchDetailResult.CustomerLocation? CustomerLocation = null;
        public List<DispatchDetailResult.DispatchTechs>? DispatchTechs = null;
        public List<DispatchDetailResult.CustomerLocationContact>? CustomerLocationContact = null;
        public List<DispatchDetailResult.CustomerLocationContactCommunication>? CustomerLocationContactComms = null;
        public List<WorkflowEquipment>? EquipmentList = null;
        public List<WorkflowListAvailableResult>? AddOnWorkflows = null;

    }
}

//private bool IsNullOrExpired(IDXDBRecordBase? record)
//{
//    return record is null || record.IsExpired();
//}

//LookupsOutput? lookupsOutput = null;
//DispatchDetailOutput? dispatchDetailOutput = null;
//WorkflowListAvailableOutput? workflowListAvailableOutput = null;
//WorkflowResultListOutput? workflowResultListOutput = null;

//public async Task LoadLocalAsync()
//{
//    return;

//    // These are api query results that have been saved in idxdb
//    lookupsOutput = await DataService.GetIDXDBRecord<LookupsOutput>("1");
//    dispatchDetailOutput = await DataService.GetIDXDBRecord<DispatchDetailOutput>(DispatchTechId.ToString());
//    workflowListAvailableOutput = await DataService.GetIDXDBRecord<WorkflowListAvailableOutput>(DispatchTechId.ToString());
//    workflowResultListOutput = await DataService.GetIDXDBRecord<WorkflowResultListOutput>(DispatchTechId.ToString());

//    // These are workflows that have been loaded and actively reside in idxdb
//    List<WorkflowResultDetailResult.Workflow> localActiveWorkflows = await DataService.GetActiveWorkflows(DispatchTechId);

//    // This will hold WorkflowResultList + active with duplicates removed
//    List<WorkflowResultListResult> allActiveWorkflows = new List<WorkflowResultListResult>();

//    if (workflowResultListOutput is not null && workflowResultListOutput.ResultData is not null)
//    {
//        allActiveWorkflows.AddRange(workflowResultListOutput.ResultData);
//    }

//    // Add the locals
//    if (localActiveWorkflows is not null)
//    {
//        foreach (var localActiveWorkflow in localActiveWorkflows)
//        {
//            if (!allActiveWorkflows.Any(aw => aw.WorkflowResultId == localActiveWorkflow.WorkflowResultId))
//            {
//                var availableWorkflow = workflowListAvailableOutput?.ResultData?.FirstOrDefault(w => w.Id == localActiveWorkflow.WorkflowId);
//                if (availableWorkflow != null)
//                {
//                    allActiveWorkflows.Add(ConvertWorkflow(localActiveWorkflow, availableWorkflow));
//                }
//            }
//        }
//    }

//    allActiveWorkflows.Sort((a, b) => a.Prompt.CompareTo(b.Prompt));

//    LookupsResult? lookups = lookupsOutput?.ResultData ?? null;
//    DispatchDetailResult? dispatch = dispatchDetailOutput?.ResultData ?? null;
//    List<WorkflowListAvailableResult>? workflows = workflowListAvailableOutput?.ResultData ?? null;

//    if (lookups is not null && dispatch is not null && workflows is not null)
//    {
//        InProgressWorkflows = allActiveWorkflows!.FindAll(w => w.Completed is null);
//        CompletedWorkflows = allActiveWorkflows!.FindAll(w => w.Completed.HasValue);
//        DispTrackItem = BuildTracker(dispatch, lookups, workflows);
//    }
//}
//private async Task CancelWorkflow(int workflowResultId)
//{
//    // Cancel Workflow sends a ServiceBus Job to cancel the workflow, not just delete totally.
//    // TODO: Make Cancel proc, and set to inactive, and cancel in db vs delete.
//    //       THIS DELETES NOW

//    string cancelMessage = "You're about to <strong>cancel</strong> this checklist?<br><b>Are you sure?</br>";

//    if (await DataService.CheckInternet() && await Confirm(cancelMessage, "CANCEL WORKFLOW"))
//    {

//        // kill local copy
//        await DataService.CancelWorkflowAsync(workflowResultId);

//        // delete from server (TODO: should cancel, and set to inactive)
//        var response = await HTTPService.GetHttpCommandsClient(DataService.AppState.AuthorizedUser!).PostAsync<WorkflowResultDeleteOutput>(new WorkflowResultDeleteInput() { WorkflowResultId = workflowResultId });
//        if (response.IsSuccess)
//        {
//            var output = response.Data!;
//            if (output.ReturnValue == WorkflowResultDeleteOutput.Returns.Ok)
//            {
//                //Console.WriteLine($"Canceled WorkflowResults ID: {workflowResultId} on Server.");
//            }
//        }
//        else
//        {
//            Console.WriteLine($"ERROR Canceling WorkflowResults ID: {workflowResultId} from Server.");
//        }

//        // blow away Checklist cache safely (if there is no internet)

//        //await Task.Delay(2 * 1000);
//        //await GetWorkflows(true);
//        InProgressWorkflows?.RemoveAll(w => w.WorkflowResultId.Equals(workflowResultId));
//        LoadedWorkflows?.RemoveAll(w => w.WorkflowResultId.Equals(workflowResultId));
//        StateHasChanged();
//    }

//}