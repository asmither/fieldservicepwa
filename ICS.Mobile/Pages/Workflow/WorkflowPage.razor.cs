namespace ICS.Mobile.Pages.Workflow
{
    //using ICS.Mobile.Components.ScratchPad;
    using ICS.Mobile.Helpers;
    using ICS.Mobile.Services;
    using ICS.Portal.Data.Commands.Models;
    using ICS.Portal.Data.Custom;
    using ICS.Portal.Data.Enumerations;
    using ICS.Portal.Data.Images;
    using ICS.Portal.Data.Queries.Models;

    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System.Text.Json;
    public partial class WorkflowPage
    {
        #region Basic Page Stuff

        private WorkflowRunnerLogicContexts thisWorkflowRunnerLogicContexts = WorkflowRunnerLogicContexts.Mobile;
        private string? Error = null;

        [Inject]
        public NavigationManager NavigationManager { set; get; } = default!;

        [Inject]
        protected LocationService LocationService { set; get; } = default!;

        [Inject]
        private JSUI ui { set; get; } = default!;

        [Inject]
        public IWorkflowData DataService { set; get; } = default!;

        [Inject]
        public SettingsService SettingsService { set; get; } = default!;

        [Inject]
        private IJSRuntime JS { set; get; } = default!;

        [Inject]
        private DateTimeService DateTimeService { set; get; } = default!;


        //[Inject]
        //public ScratchPadService ScratchPadSvc { get; set; } = default!;

        // Reference to the overlay component
        //public ScratchPadOverlay? scratchPadRef;


        #endregion Basic Page Stuff

        #region Parameters and Fields

        [Parameter]
        public int? WorkflowResultId { set; get; }

        [Parameter]
        public bool? InReview { set; get; }

        [Parameter]
        public EventCallback OnNavigateBack { set; get; }

        private Mobile.Components.PageHeaderTabbed? wfHeader;
        private Mobile.Components.ICSDialogBox? DialogBox;
        private WorkflowRunnerLogic? runner;
        private WorkflowStepResultValueSaveInput? Input;
        private WorkflowResultDetailResult.Workflow? Workflow;
        private WorkflowResultDetailResult.WorkflowStep? WorkflowStep;
        private List<WorkflowResultDetailResult.WorkflowStepOption>? StepOptions;
        private WorkflowDataTypeDetail? StepDetails;
        private List<WorkflowResultDetailResult.WorkflowStep>? AllSteps;
        private List<WorkflowReviewItem>? WorkflowReviewItemList;
        private WorkflowRunnerModes WorkflowRunnerMode = WorkflowRunnerModes.None;
        private bool Initialized = false;
        private bool WorkflowExistsLocally = false;
        private string CustomerNumber = "";
        private string CustomerTitle = "Checklist";
        private bool IsBusy = true;
        private List<SelectableConsumable>? Consumables;
        private List<LookupsResult.Attribute>? TruckStockItems = null;
        private string CSSControlDisplay = "none";
        private string CSSSpinnerDisplay = "block";
        private List<PresentationMedia> Media = new();
        private bool ShowDownloadSaveButton = true;
        private ITimestampAndPositionResolver? timestampAndPositionResolver;
        private int CurrentUserId = 0;

        #endregion Parameters and Fields

        protected override async Task OnInitializedAsync()
        {
            Initialized = false;

            if (await DataService.LoadAppStateAsync())
            {
                CurrentUserId = DataService.AppState.AuthorizedUser!.EntityId;
                //await ScratchPadSvc.LoadAsync(CurrentUserId);
                DataService.SyncEnabled = true;
                await LocationService.StartOrResetWatchAsync();
                timestampAndPositionResolver = new TimestampAndPositionResolver(LocationService);
            }
            else
            {
                DataService.AppState.LoginRedirect = NavigationManager.Uri;
                await DataService.SaveAppStateInstance();
                NavigationManager.NavigateTo("/login");
            }

            await LoadData();
        }
        private async Task LoadData()
        {
            if (WorkflowResultId is null)
            {
                await DialogBox.WaitForDialogResultAsync(header: "Error", prompt: "Parameter workflow result id was not supplied.", okLabel: "OK", defaultButton: 1);
                NavigationManager.NavigateTo("/");
                return;

            }

            runner = new WorkflowRunnerLogic(DateTimeService, JS, DataService, timestampAndPositionResolver!, DataService.AppState.AuthorizedUser!.EntityId, thisWorkflowRunnerLogicContexts);

            try
            {
                if (await runner.LoadWorkflow(WorkflowResultId.Value))
                {
                    CustomerNumber = string.Empty;
                    CustomerTitle = "Checklist";

                    if (runner.Dispatch is not null)
                    {
                        if (runner.Dispatch.DispatchResult is not null && runner.Dispatch.DispatchResult.ClientWorkOrder is not null)
                        {
                            CustomerNumber = runner.Dispatch.DispatchResult.ClientWorkOrder;
                        }
                        if (runner.Dispatch.CustomerLocationResult is not null && runner.Dispatch.CustomerLocationResult.LocName is not null)
                        {
                            CustomerTitle = runner.Dispatch.CustomerLocationResult.LocName;
                        }
                    }
                   
                    if (InReview == true)
                    {
                        await runner.GetFirstStep();
                        await SetValuesFromRunner();
                        await OnReview();
                    }
                    else
                    {
                        await runner.MoveToLastCompletedOrFirst();
                        await SetValuesFromRunner();
                        WorkflowRunnerMode = WorkflowRunnerModes.InProgress;
                    }
                }
                else
                {
                    await DialogBox.WaitForDialogResultAsync(header: "Error", prompt: runner.InvalidWorkflowErrorMessage, okLabel: "OK", defaultButton: 1);
                    NavigationManager.NavigateTo("/");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DialogBox.WaitForDialogResultAsync(header: "Error", prompt: ex.ToString(), okLabel: "OK", defaultButton: 1);
                return;
            }

            Initialized = true;
        }
        private async Task OnEdit()
        {
            try
            {
                await runner!.GetFirstStep();
                await SetValuesFromRunner();
                WorkflowRunnerMode = WorkflowRunnerModes.InProgress;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                WorkflowRunnerMode = WorkflowRunnerModes.Error;
            }
        }
        private async Task OnReview()
        {
            runner!.ResolveActualAndOrphanedValues();

            WorkflowReviewItemList = await WorkflowReviewBuilder.BuildReviewItems(SettingsService, DataService, runner!.ActualValues!, runner.AllSteps!, runner.AllOptions!, runner.Dispatch, runner.Lookups, false);
            WorkflowRunnerMode = WorkflowRunnerModes.InReview;

            SetStepVisibility(true);
        }
        private async Task OnDeleteLocalCopy()
        {
            //if (Workflow is not null)
            //    await DataService.DeleteLocalWorkflowAsync(Workflow.WorkflowResultId);
            //NavigationManager.NavigateTo("/dispatch-list");
        }

        private void SetStepVisibility(bool visible)
        {
            if (visible)
            {
                CSSControlDisplay = "block";
                CSSSpinnerDisplay = "none";
            }
            else
            {
                CSSControlDisplay = "none";
                CSSSpinnerDisplay = "block";
            }

        }
        private async Task MoveNext()
        {
            if (WorkflowStep is null || Input is null) return;

            try
            {
                SetStepVisibility(false);

                await runner!.SaveWorkflowStepAsync(Input, true);

                if (await runner.MoveNext())
                {
                    await SetValuesFromRunner();
                }
                else
                {
                    WorkflowRunnerMode = WorkflowRunnerModes.CompleteOrReview;
                    SetStepVisibility(false);
                }
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                WorkflowRunnerMode = WorkflowRunnerModes.Error;
            }

            await ui.ScrollToTop();
        }
        private async Task MovePrevious()
        {
            try
            {
                SetStepVisibility(false);

                if (runner!.MovePrevious())
                {
                    await SetValuesFromRunner();
                }
                else
                {
                    SetStepVisibility(true);
                }
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                WorkflowRunnerMode = WorkflowRunnerModes.Error;
            }
        }
        
        private async Task SetValuesFromRunner()
        {
            try
            {
                Workflow = runner!.Workflow;
                WorkflowStep = runner.CurrentStep;
                Input = runner.CurrentInput;
                StepDetails = runner.CurrentStepDetails;
                StepOptions = runner.CurrentStepOptions;
                if (WorkflowStep!.WorkflowDataTypeId == (int)WorkflowDataTypes.DataQuery)
                {
                    if (AllSteps is null)
                    {
                        AllSteps = runner.AllSteps;
                    }
                }

                Media = new();

                if (!string.IsNullOrEmpty(WorkflowStep.PresentationMedia))
                {
                    List<ImageFileArrayItem>? list = JsonSerializer.Deserialize<List<ImageFileArrayItem>>(WorkflowStep.PresentationMedia);
                    if (list is not null)
                    {
                        foreach (var item in list)
                        {
                            PresentationMedia presentationMedia = new PresentationMedia(item.GeneratedFileName, item.Title, item.Caption, item.ImageLastModifiedUTC, null);
                            if (await DataService.TryHydrateFromCache(presentationMedia))
                            {
                                Media.Add(presentationMedia);
                            }
                        }
                    }
                }

                Consumables = new List<SelectableConsumable>();

                if (WorkflowStep.WorkflowDataTypeId == (int)WorkflowDataTypes.Consumable)
                {
                    if (WorkflowStep.AttributeId != 0)
                    {
                        Consumables = runner.GetSelectableConsumables(WorkflowStep.AttributeId.Value);
                    }
                    else
                    {
                        Consumables = null;
                    }
                }

                if (WorkflowStep.WorkflowDataTypeId == (int)WorkflowDataTypes.TruckStockItems)
                {
                    if (TruckStockItems is null && runner?.Lookups?.AttributeResult is not null)
                    {
                        TruckStockItems = [.. runner.Lookups.AttributeResult.FindAll(a => a.IsItem == true)];
                    }
                }

                SetStepVisibility(true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private async Task OnComplete()
        {
            var input = new WorkflowResultCompleteInput(Workflow!.WorkflowResultId, DateTime.UtcNow, DataService.AppState.AuthorizedUser!.Id);
            await DataService.SaveAndPostWorkflowResultCompleteAsync(input);

            WorkflowStep = null;
            Input = null;
            WorkflowRunnerMode = WorkflowRunnerModes.CompleteSync;
        }

        public void ReturnToSender()
        {
            if (Workflow is not null)
            {
                if (Workflow.RequireDispatch)
                {
                    NavigationManager.NavigateTo($"/dispatch-detail");
                }
                else
                {
                    NavigationManager.NavigateTo($"/");
                }
            }
        }
    }
}