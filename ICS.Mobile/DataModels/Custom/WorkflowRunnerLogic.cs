using ICS.Mobile.Services;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Enumerations;
using ICS.Portal.Data.Queries.Models;
using Microsoft.JSInterop;
using System.Data;
using System.Text.Json;
namespace ICS.Portal.Data.Custom
{
    public enum AttributeContexts
    {
        Tag = 1,
        Customer = 2,
        Location = 3,
        Equipment = 4,
        WorkOrderTask = 5,
        Dispatch = 6,
        ActiveTechs = 7
    }
    public enum WorkflowRunnerLogicContexts
    {
        Mobile,
        Preview,
        Portal
    }
    public class WorkflowRunnerLogic
    {
        #region Readonly Fields

        private readonly DateTimeService dateTimeService;
        private readonly IWorkflowData dataService;
        private readonly ITimestampAndPositionResolver timestampAndPositionResolver;
        private readonly WorkflowRunnerLogicContexts workflowRunnerLogicContext;
        private readonly int employeeId;
        #endregion Readonly Fields

        #region Private Fields

        public bool loaded = false;

        private WorkflowResultDetailOutput? workflowResultDetailOutput;
        private WorkflowResultDetailResult.Workflow? workflow
        {
            get
            {
                return workflowResultDetailOutput!.ResultData!.WorkflowResult;
            }
        }
        private List<WorkflowResultDetailResult.WorkflowStep>? allSteps
        {
            get
            {
                return workflowResultDetailOutput!.ResultData!.WorkflowStepResult;
            }
        }
        private List<WorkflowResultDetailResult.WorkflowStepOption>? allOptions
        {
            get
            {
                return workflowResultDetailOutput.ResultData!.WorkflowStepOptionResult;
            }
        }
        private List<WorkflowResultDetailResult.WorkflowStepValue>? allValues
        {
            get
            {
                return workflowResultDetailOutput.ResultData!.WorkflowStepValueResult;
            }
        }

        private DispatchDetailResult? dispatch;
        private LookupsResult? lookups;

        private bool workflowIsDirty = false;
        private bool dispatchIsDirty = false;
        private bool resolvingOrphans = false;

        #endregion

        #region Public Properties

        public string? InvalidWorkflowErrorMessage { private set; get; }
        public int WorkflowVersion { private set; get; } = 0;
        public WorkflowResultDetailResult.Workflow? Workflow => workflow;
        public List<WorkflowResultDetailResult.WorkflowStep>? AllSteps => allSteps;
        public List<WorkflowResultDetailResult.WorkflowStepOption>? AllOptions => allOptions;
        public List<WorkflowResultDetailResult.WorkflowStepValue>? AllValues => allValues;
        public DispatchDetailResult? Dispatch => dispatch;
        public WorkflowResultDetailResult.WorkflowStep? CurrentStep { private set; get; }
        public WorkflowStepResultValueSaveInput? CurrentInput { private set; get; }
        public WorkflowResultDetailResult.WorkflowStepValue? CurrentValue { private set; get; }
        public LookupsResult? Lookups => lookups;
        public List<WorkflowResultDetailResult.WorkflowStepOption>? CurrentStepOptions
        {
            get
            {
                if (CurrentStep == null)
                {
                    throw new ArgumentNullException(nameof(CurrentStep), "Cannot call property when current step is null");
                }
                return allOptions?.Where(o => o.WorkflowStepId == CurrentStep.WorkflowStepId).ToList();
            }
        }
        public WorkflowDataTypeDetail? CurrentStepDetails
        {
            get
            {
                if (CurrentStep == null)
                {
                    throw new ArgumentNullException(nameof(CurrentStep), "Cannot call property when current step is null");
                }
                return new WorkflowDataTypeDetail(CurrentStep.WorkflowDataTypeDetail);
            }
        }
        public List<SelectableConsumable> CurrentSelectableConsumables { private set; get; }
        public List<WorkflowResultDetailResult.WorkflowStepValue>? OrphanedValues;
        public List<WorkflowResultDetailResult.WorkflowStepValue>? ActualValues;

        #endregion Public Properties

        #region Contructors

        private bool ReadOnly = false;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public WorkflowRunnerLogic()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            ReadOnly = true;
        }

        public WorkflowRunnerLogic(DateTimeService dateTimeService, IJSRuntime JS, IWorkflowData dataService, ITimestampAndPositionResolver timestampAndPositionResolver, int employeeId, WorkflowRunnerLogicContexts workflowRunnerLogicContext)
        {
            this.dateTimeService = dateTimeService;
            this.dataService = dataService;
            this.timestampAndPositionResolver = timestampAndPositionResolver;
            this.employeeId = employeeId;
            this.workflowRunnerLogicContext = workflowRunnerLogicContext;
        }

        #endregion Contructors

        #region Public Methods

        public async Task<bool> LoadWorkflow(int workflowResultId)
        {
            try
            {
                var output = await dataService.GetWorkflowAsync(workflowResultId);
                if (output.ReturnValue == WorkflowResultDetailOutput.Returns.NotFound)
                {
                    InvalidWorkflowErrorMessage = "Sorry, the requested checklist was not not loaded.  Your internet connection may be offline.  You can 'Pre-load' checklists by opening and exiting to make them available when you're offline.  Please try again later.";
                    return false;
                }

                if (output.ResultData!.WorkflowResult is null)
                {
                    InvalidWorkflowErrorMessage = "WorkflowResult is corrupted - Serious error, please call IT Support.";
                    return false;
                }

                if (output.ResultData!.WorkflowStepResult is null)
                {
                    InvalidWorkflowErrorMessage = "WorkflowStepResult is corrupted - Serious error, please call IT Support.";
                    return false;
                }

                if (output.ResultData!.WorkflowStepOptionResult is null)
                {
                    output.ResultData!.WorkflowStepOptionResult = new();
                }

                if (output!.ResultData.WorkflowStepValueResult is null)
                {
                    output.ResultData.WorkflowStepValueResult = new();
                }

                if (output.ResultData!.WorkflowResult!.WorkflowId == 10)
                {
                    switch (workflowRunnerLogicContext)
                    {
                        //TODO: get values from preview and portal runner
                        case WorkflowRunnerLogicContexts.Preview:
                            output.ResultData.WorkflowResult!.WorkOrderDispatchId = 28149;
                            output.ResultData.WorkflowResult!.WorkOrderDispatchTechId = 54217;
                            output.ResultData.WorkflowResult!.RequireDispatch = true;
                            break;
                        case WorkflowRunnerLogicContexts.Mobile:
                            output.ResultData.WorkflowResult!.WorkOrderDispatchId = 28149;
                            output.ResultData.WorkflowResult!.WorkOrderDispatchTechId = 54217;
                            output.ResultData.WorkflowResult!.RequireDispatch = true;
                            break;
                        case WorkflowRunnerLogicContexts.Portal:
                            output.ResultData.WorkflowResult!.WorkOrderDispatchId = 28149;
                            output.ResultData.WorkflowResult!.WorkOrderDispatchTechId = 54217;
                            output.ResultData.WorkflowResult!.RequireDispatch = true;
                            break;
                    }
                }

                if (output.ResultData.WorkflowResult.RequireDispatch)
                {
                    var dispatchOutput = await dataService.GetIDXDBRecord<DispatchDetailOutput>(output.ResultData.WorkflowResult.WorkOrderDispatchTechId.ToString());
                    if (dispatchOutput is not null)
                    {
                        dispatch = dispatchOutput.ResultData;
                    }
                    else
                    {
                        dispatch = await dataService.GetDispatchDataAsync(output.ResultData.WorkflowResult.WorkOrderDispatchId, output.ResultData.WorkflowResult.WorkOrderDispatchTechId, false);
                    }

                    if (dispatch is null)
                    {
                        //InvalidWorkflowErrorMessage = "The required dispatch for this checklist cannot be retrieved";
                        //return false;
                        // KIRK CHANGED TO BE ABLE TO LOAD WORKFLOWS THAT NO LONGER HAVE A DISPATCH
                        dispatch = new();
                    }
                    dispatch!.DispatchTechsResult ??= new();
                    dispatch.CustomerLocationContactResult ??= new();
                    dispatch.EquipmentResult ??= new();
                    dispatch.EquipmentAttributeValueResult ??= new();
                    dispatch.CustomerAttributeResult ??= new();
                    dispatch.CustomerLocationAttributeResult ??= new();
                    dispatch.WorkOrderTaskCodeAttributeResult ??= new();
                    dispatch.AllAttributeValueResult ??= new();
                }

                lookups = await dataService.GetLookupsAsync();
                workflowResultDetailOutput = output;

                CheckWorkflowVersion();

                loaded = true;
            }
            catch (Exception ex)
            {
                InvalidWorkflowErrorMessage = ex.Message;
                loaded = false;
            }
            return loaded;
        }
        public bool LoadWorkflow(WorkflowResultDetailResult workflowData, DispatchDetailResult? dispatch, LookupsResult? lookups)
        {
            if (workflowData!.WorkflowResult is null)
            {
                InvalidWorkflowErrorMessage = "WorkflowResult is null";
                return false;
            }

            if (workflowData!.WorkflowStepResult is null)
            {
                InvalidWorkflowErrorMessage = "WorkflowStepResult is null";
                return false;
            }

            if (workflowData!.WorkflowStepOptionResult is null)
            {
                workflowData!.WorkflowStepOptionResult = new();
            }

            if (workflowData!.WorkflowStepValueResult is null)
            {
                workflowData.WorkflowStepValueResult = new();
            }

            this.dispatch = dispatch;
            this.lookups = lookups;

            workflowResultDetailOutput = new() { ResultData = workflowData, ReturnValue = WorkflowResultDetailOutput.Returns.Ok };

            CheckWorkflowVersion();

            loaded = true;
            return loaded;
        }
        public void CheckWorkflowVersion()
        {
            if (workflow.Created < new DateTime(2025, 5, 12, 23, 59, 59))
            {
                WorkflowVersion = 1;
            }
            else
            {
                WorkflowVersion = 2;
            }
        }
        public async Task GetFirstStep()
        {
            ThrowExceptionIfInvalidStateForOperation(nameof(GetFirstStep));

            CurrentStep = FirstStep();

            if (allValues!.Count == 0)
            {
                LoopPath loopPath = new();
                int loopIndex = 0;
                if (IsLoopTarget(CurrentStep))
                {
                    loopPath.AttachOrIncrementLoop(CurrentStep.WorkflowStepResultId);
                    loopIndex++;
                }
                CurrentInput = await CreateInputFromStep(CurrentStep, loopIndex, loopPath.ToString(), null, 0, null);
                if (CurrentStep.WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch)
                {
                    CurrentInput.Value = ResolveAutoSwitchSwitchOption(CurrentStep);
                    await SaveWorkflowStepAsync(CurrentInput, true);
                    await MoveNext();
                }
            }
            else
            {
                CurrentInput = CreateInputFromResultValue(allValues[0]);
                CurrentValue = allValues[0];
            }
        }
        public async Task MoveToLastCompletedOrFirst()
        {
            ThrowExceptionIfInvalidStateForOperation(nameof(MoveToLastCompletedOrFirst));

            if (allValues!.Count == 0)
            {
                await GetFirstStep();
            }
            else
            {
                CurrentValue = allValues.OrderByDescending(x => x.CompletedTimestamp).FirstOrDefault()!;
                CurrentStep = allSteps!.FirstOrDefault(s => s.WorkflowStepResultId == CurrentValue.WorkflowStepResultId)!;
                CurrentInput = CreateInputFromResultValue(CurrentValue);
            }

            if (CurrentInput.WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch)
            {
                await MoveNext();
            }
        }

        private Dictionary<int, bool> LoopTargetSteps = null;

        private void LoadLoopTargets()
        {
            LoopTargetSteps = new();

            foreach (var step in allSteps.Where(s => (int)WorkflowDataTypes.Loop == s.WorkflowDataTypeId))
            {
                WorkflowDataTypeDetail details = new(step.WorkflowDataTypeDetail);
                if (details.LoopToStepId.HasValue)
                {
                    if (!LoopTargetSteps.ContainsKey(details.LoopToStepId.Value))
                    {
                        LoopTargetSteps.Add(details.LoopToStepId.Value, details.IsMasterLoop.GetValueOrDefault(false));
                    }
                }
            }
        }

        private bool IsLoopTarget(int stepId)
        {
            if (LoopTargetSteps is null)
            {
                LoadLoopTargets();
            }
            return LoopTargetSteps!.ContainsKey(stepId);
        }

        private bool IsMasterLoop(int stepId)
        {
            if (LoopTargetSteps is null)
            {
                LoadLoopTargets();
            }

            if (LoopTargetSteps!.ContainsKey(stepId))
            {
                return LoopTargetSteps[stepId];
            }

            return false;
        }

        private Dictionary<int, List<int>> parallelSplitStepIds = new();
        private List<int> GetParallelSplitStepIds(WorkflowResultDetailResult.WorkflowStep parallelSplitStep)
        {
            if (!parallelSplitStepIds.ContainsKey(parallelSplitStep.WorkflowStepResultId))
            {
                WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(parallelSplitStep!.WorkflowDataTypeDetail);
                ParallelSplitOptions parallelSplitOptions = new(details.Values["Options"]);
                List<int> result = new();

                foreach (string option in parallelSplitOptions.GetOptionNames())
                {
                    var step = allSteps.FirstOrDefault(s => s.SplitStepId == parallelSplitStep.WorkflowStepId && s.SplitOption == option && s.Position == 1);
                    if (step is not null)
                    {
                        result.Add(step.WorkflowStepResultId);
                    }
                }

                var terminatingStep = allSteps.FirstOrDefault(s => s.SplitStepId == parallelSplitStep.SplitStepId && s.Position == parallelSplitStep.Position + 1);
                // If this step is null here, it has to be a nested parallel split.

                var parent = allSteps!.FirstOrDefault(s => s.WorkflowStepId == parallelSplitStep.SplitStepId);
                while (parent is not null)
                {
                    if (parent.WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit)
                    {
                        terminatingStep = parent;
                        break;
                    }
                    parent = allSteps.FirstOrDefault(s => s.WorkflowStepId == parent.SplitStepId);
                }

                if (terminatingStep is null)
                {
                    result.Add(0);
                }
                else
                {
                    result.Add(terminatingStep.WorkflowStepResultId);
                }

                parallelSplitStepIds.Add(parallelSplitStep.WorkflowStepResultId, result);
            }

            return parallelSplitStepIds[parallelSplitStep.WorkflowStepResultId];

        }

        private bool IsChildOf(WorkflowResultDetailResult.WorkflowStep childStep, WorkflowResultDetailResult.WorkflowStep possibleParentStep)
        {
            if (childStep.SplitStepId is null)
            {
                return false;
            }

            var parent = allSteps!.FirstOrDefault(s => s.WorkflowStepId == childStep.SplitStepId);
            while (parent is not null)
            {
                if (parent.WorkflowStepId == possibleParentStep.WorkflowStepId)
                {
                    return true;
                }

                //parent = allSteps.FirstOrDefault(s => s.WorkflowStepId == parent.WorkflowStepId); OOPS
                parent = allSteps.FirstOrDefault(s => s.WorkflowStepId == parent.SplitStepId);

            }

            return false;
        }
        private List<int> ValuesAsListInt(string? list)
        {
            if (!string.IsNullOrEmpty(list))
            {
                return list.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => int.Parse(s))
                              .ToList();
            }
            return new();
        }
        private string? ResolveValueSwitch(WorkflowResultDetailResult.WorkflowStep step, string sourceLoopPath)
        {
            ValueSwitcher? valueSwitcher = GetValueSwitcher(step);

            if (valueSwitcher is not null)
            {
                var valueSwitchStep = AllSteps!.FirstOrDefault(s => s.WorkflowStepId == valueSwitcher.StepId);
                if (valueSwitchStep is not null)
                {
                    var valueSwitchSourceValue = allValues!.FirstOrDefault(v => v.WorkflowStepResultId == valueSwitchStep.WorkflowStepResultId && v.LoopPath == sourceLoopPath);
                    if (valueSwitchSourceValue is not null)
                    {
                        return ResolveAutoSwitchSwitchOption(step, valueSwitchSourceValue.Value);
                    }
                }
            }
            return null;
        }
        public async Task<bool> MoveNext()
        {
            ThrowExceptionIfInvalidStateForOperation(nameof(MoveNext));

            var sourceStep = CurrentStep!;
            var sourceInput = CurrentInput!;
            var sourceOptions = CurrentStepOptions!;
            var sourceValue = CurrentValue!;

            var nextStep = TryGetNextStep(sourceStep, sourceOptions, sourceInput);
            if (nextStep is null)
            {
                return false;
            }

            int sourceWorkflowStepResultId = sourceStep.WorkflowStepResultId;
            LoopPath sourceLoopPath = new(sourceInput.LoopPath);
            int sourceLoopIndex = sourceInput.LoopIndex.GetValueOrDefault(0);

            LoopPath nextLoopPath = new(sourceInput.LoopPath);
            int nextLoopIndex = sourceLoopIndex;

            // Set up the next loop path
            if (IsLoopTarget(nextStep.WorkflowStepId))
            {
                nextLoopPath.AttachOrIncrementLoop(nextStep.WorkflowStepResultId);
                if (IsMasterLoop(nextStep.WorkflowStepId))
                {
                    nextLoopIndex++;
                }
            }
            else if (sourceInput.WorkflowDataTypeId == (int)WorkflowDataTypes.Loop)
            {
                WorkflowDataTypeDetail details = new(sourceStep.WorkflowDataTypeDetail);
                if (details.LoopToStepId != nextStep.WorkflowStepId)
                {
                    nextLoopPath.Detach();
                }
            }

            var val = AllValues.FirstOrDefault(v => v.SourceWorkflowStepResultId == sourceWorkflowStepResultId && v.WorkflowStepResultId == nextStep.WorkflowStepResultId && v.LoopPath == nextLoopPath.ToString());

            if (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit)
            {
                var currentValue = AllValues!.FirstOrDefault(v => v.WorkflowStepResultId == nextStep.WorkflowStepResultId && v.LoopPath == nextLoopPath.ToString());

                if (currentValue is not null)
                {
                    CurrentInput = CreateInputFromResultValue(currentValue);

                    // Is the source step (incoming step) a child step of the next step?
                    // If so the json payload of the parallel step is holding the option name.
                    // This indicates that the parallel option is completed!
                    if (IsChildOf(sourceStep, nextStep))
                    {
                        // These are the existing Values of the parallelSplit.
                        List<int> existingStepIds = ValuesAsListInt(CurrentInput.Value);

                        // This is the first step in the option path. If this is null we are terminating.
                        var firstStepInParallelOption = allSteps!.FirstOrDefault(s => s.SplitStepId == nextStep.WorkflowStepId && s.SplitOption == currentValue.JsonPayload && s.Position == 1);
                        if (firstStepInParallelOption is not null)
                        {
                            existingStepIds.Add(firstStepInParallelOption.WorkflowStepResultId);
                        }

                        List<int> allParallelStepIds = GetParallelSplitStepIds(nextStep);
                        List<int> sortedCompletedSteps = new();
                        // This is to sort the steps according to the order of the options and remove any duplicates.

                        foreach (var stepId in allParallelStepIds)
                        {
                            if (existingStepIds.Contains(stepId))
                            {
                                sortedCompletedSteps.Add(stepId);
                            }
                        }
                        CurrentInput.Value = string.Join(',', sortedCompletedSteps);

                        // The new values will be saved on the next move forward.
                    }

                    CurrentInput.JsonPayload = null;
                }
                else
                {
                    CurrentInput = await CreateInputFromStep(nextStep, nextLoopIndex, nextLoopPath.ToString(), sourceWorkflowStepResultId, sourceLoopIndex, sourceLoopPath.ToString());
                }

                CurrentStep = nextStep;
                return true;
            }

            if (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch)
            {
                while (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch)
                {
                    var resolvedValue = ResolveAutoSwitchSwitchOption(nextStep);
                    var autoSwitchValue = AllValues.FirstOrDefault(v =>
                        v.WorkflowStepResultId == nextStep.WorkflowStepResultId &&
                        v.SourceWorkflowStepResultId == sourceWorkflowStepResultId &&
                        v.SourceLoopPath == sourceLoopPath.ToString());
                    if (autoSwitchValue is not null)
                    {
                        sourceInput = CreateInputFromResultValue(autoSwitchValue);
                        if (sourceInput.Value != resolvedValue)
                        {
                            sourceInput.Value = resolvedValue;
                            await SaveWorkflowStepAsync(sourceInput, true);
                        }
                    }
                    else
                    {
                        sourceInput = await CreateInputFromStep(nextStep, nextLoopIndex, nextLoopPath.ToString(), sourceWorkflowStepResultId, sourceLoopIndex, sourceLoopPath.ToString());
                        sourceInput.Value = resolvedValue;
                        await SaveWorkflowStepAsync(sourceInput, true);
                    }

                    sourceWorkflowStepResultId = sourceInput.WorkflowStepResultId!.Value;
                    sourceLoopIndex = sourceInput.LoopIndex.GetValueOrDefault(0);
                    sourceLoopPath = new(sourceInput.LoopPath);
                    sourceOptions = AllOptions!.FindAll(o => o.WorkflowStepId == nextStep.WorkflowStepId);
                    sourceStep = nextStep;

                    nextLoopIndex = sourceLoopIndex;
                    nextLoopPath = new(sourceInput.LoopPath);

                    nextStep = TryGetNextStep(sourceStep, sourceOptions, sourceInput);

                    if (nextStep is null) return false;

                    if (IsLoopTarget(nextStep))
                    {
                        nextLoopPath.AttachOrIncrementLoop(nextStep.WorkflowStepResultId);
                        if (IsMasterLoop(nextStep.WorkflowStepId))
                        {
                            nextLoopIndex++;
                        }
                    }
                }
            }

            if (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.ValueSwitch)
            {
                var resolvedValue = ResolveValueSwitch(nextStep, sourceLoopPath.ToString());

                var valueSwitchValue = GetValue(nextStep.WorkflowStepResultId, sourceInput.WorkflowStepResultId, sourceInput.LoopPath, sourceInput.LoopIndex.GetValueOrDefault(0));
                if (valueSwitchValue is not null)
                {
                    sourceInput = CreateInputFromResultValue(valueSwitchValue);
                    if (sourceInput.Value != resolvedValue)
                    {
                        sourceInput.Value = resolvedValue;
                        await SaveWorkflowStepAsync(sourceInput, true);
                    }
                }
                else
                {
                    sourceInput = await CreateInputFromStep(nextStep, nextLoopIndex, nextLoopPath.ToString(), sourceWorkflowStepResultId, sourceLoopIndex, sourceLoopPath.ToString());
                    sourceInput.Value = resolvedValue;
                    await SaveWorkflowStepAsync(sourceInput, true);
                }

                sourceWorkflowStepResultId = sourceInput.WorkflowStepResultId!.Value;
                sourceLoopIndex = sourceInput.LoopIndex.GetValueOrDefault(0);
                sourceLoopPath = new(sourceInput.LoopPath);
                sourceOptions = AllOptions!.FindAll(o => o.WorkflowStepId == nextStep.WorkflowStepId);

                nextLoopIndex = sourceLoopIndex;
                nextLoopPath = new(sourceInput.LoopPath);

                nextStep = TryGetNextStep(sourceStep, sourceOptions, sourceInput);

                if (nextStep is null) return false;

                if (IsLoopTarget(nextStep))
                {
                    nextLoopPath.AttachOrIncrementLoop(nextStep.WorkflowStepResultId);
                    if (IsMasterLoop(nextStep.WorkflowStepId))
                    {
                        nextLoopIndex++;
                    }
                }

            }

            // Handle variable assignments in calculator steps
            if (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.Calculator)
            {
                WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(nextStep.WorkflowDataTypeDetail);
                CalculationItems calculationItems = new CalculationItems(details.Values[nameof(CalculationItems)]);

                Dictionary<string, decimal> variableReplacements = new();
                foreach (var variableMap in calculationItems.VariableMaps)
                {
                    var valueStep = allSteps.FirstOrDefault(s => s.WorkflowStepId == variableMap.StepId);
                    if (valueStep is not null)
                    {
                        var valueValue = allValues.FirstOrDefault(v => v.WorkflowStepResultId == valueStep.WorkflowStepResultId && v.LoopPath == sourceInput.LoopPath);
                        if (valueValue is not null)
                        {
                            decimal value = decimal.Parse(valueValue.Value);
                            string variableReplacement = "[" + variableMap.VariableName + "]";
                            variableReplacements.Add(variableReplacement, value);
                        }
                    }
                }
                var script = calculationItems.Script;
                string? presentation = calculationItems.Presentation;

                foreach (var variableReplacement in variableReplacements)
                {
                    script = script.Replace(variableReplacement.Key, variableReplacement.Value.ToString());
                    presentation = presentation.Replace(variableReplacement.Key, variableReplacement.Value.ToString());
                }

                calculationItems.SetReplacedScript(script);
                calculationItems.SetReplacedPresentation(presentation);

                details.Values[nameof(CalculationItems)] = calculationItems.ToString();
                nextStep.WorkflowDataTypeDetail = details.Serialize();

            }

            WorkflowResultDetailResult.WorkflowStepValue? nextValue = null;

            if (nextStep.WorkflowDataTypeId == (int)WorkflowDataTypes.Loop || IsLoopTarget(nextStep.WorkflowStepId))
            {
                nextValue = AllValues.FirstOrDefault(v =>
                    v.WorkflowStepResultId == nextStep.WorkflowStepResultId &&
                    v.SourceWorkflowStepResultId == sourceWorkflowStepResultId &&
                    v.LoopPath == nextLoopPath.ToString());
            }
            else
            {
                nextValue = GetValue(nextStep.WorkflowStepResultId, sourceWorkflowStepResultId, sourceLoopPath.ToString(), sourceLoopIndex);
            }

            if (nextValue is not null)
            {
                CurrentInput = CreateInputFromResultValue(nextValue);
                CurrentValue = nextValue;
            }
            else
            {
                CurrentInput = await CreateInputFromStep(nextStep, nextLoopIndex, nextLoopPath.ToString(), sourceWorkflowStepResultId, sourceLoopIndex, sourceLoopPath.ToString());
            }

            CurrentStep = nextStep;

            return true;
        }

        private ValueSwitcher? GetValueSwitcher(WorkflowResultDetailResult.WorkflowStep nextStep)
        {
            WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(nextStep.WorkflowDataTypeDetail);
            string json = details.Values[nameof(ValueSwitcher)];
            if (!string.IsNullOrEmpty(json))
            {
                return json.ToObject<ValueSwitcher>();
            }

            return null;
        }

        public bool MovePrevious()
        {
            ThrowExceptionIfInvalidStateForOperation(nameof(MoveToLastCompletedOrFirst));

            if (CurrentInput!.SourceWorkflowStepResultId is null)
            {
                return false;
            }

            var sourceStep = CurrentStep!;
            var sourceInput = CurrentInput!;
            var sourceOptions = CurrentStepOptions!;
            var sourceValue = CurrentValue!;

            LoopPath sourceLoopPath = new(sourceInput.LoopPath);
            LoopPath previousLoopPath = new(sourceInput.SourceLoopPath);

            var previousValue = allValues.FirstOrDefault(v => v.WorkflowStepResultId == sourceInput.SourceWorkflowStepResultId && v.LoopPath == sourceInput.SourceLoopPath);
            var previousStep = allSteps!.FirstOrDefault(s => s.WorkflowStepResultId == previousValue.WorkflowStepResultId)!;

            CurrentValue = previousValue;
            CurrentStep = previousStep;
            CurrentInput = CreateInputFromResultValue(previousValue);

            if (previousStep.WorkflowDataTypeId == (int)WorkflowDataTypes.Calculator)
            {
                WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(CurrentStep.WorkflowDataTypeDetail);
                CalculationItems calculationItems = new CalculationItems(details.Values[nameof(CalculationItems)]);
                calculationItems.SetReplacedScript(null);
                calculationItems.SetReplacedPresentation(null);

                Dictionary<string, decimal> variableReplacements = new();
                foreach (var variableMap in calculationItems.VariableMaps)
                {
                    var valueStep = allSteps.FirstOrDefault(s => s.WorkflowStepId == variableMap.StepId);
                    if (valueStep is not null)
                    {
                        var valueValue = allValues.FirstOrDefault(v => v.WorkflowStepResultId == valueStep.WorkflowStepResultId && v.LoopPath == CurrentInput.LoopPath);
                        if (valueValue is not null)
                        {
                            decimal value = decimal.Parse(valueValue.Value);
                            string variableReplacement = "[" + variableMap.VariableName + "]";
                            variableReplacements.Add(variableReplacement, value);
                        }
                    }
                }

                string? presentation = calculationItems.Presentation;
                if (presentation is not null)
                {
                    foreach (var variableReplacement in variableReplacements)
                    {
                        presentation = presentation.Replace(variableReplacement.Key, variableReplacement.Value.ToString());
                    }
                    presentation = presentation.Replace("[CALCULATION]", CurrentInput.Value);
                }
                else
                {
                    presentation = CurrentInput.Value;
                }

                calculationItems.SetReplacedPresentation(presentation);

                details.Values[nameof(CalculationItems)] = calculationItems.ToString();
                CurrentStep.WorkflowDataTypeDetail = details.Serialize();
            }

            if (previousStep.WorkflowDataTypeId == (int)WorkflowDataTypes.AutoSwitch || previousStep.WorkflowDataTypeId == (int)WorkflowDataTypes.ValueSwitch)
            {
                return MovePrevious();
            }

            return true;
        }
        public async Task SaveWorkflowStepAsync(WorkflowStepResultValueSaveInput sourceInput, bool boolPublish)
        {
            var inputClone = sourceInput.JsonClone<WorkflowStepResultValueSaveInput>()!;
            WorkflowResultDetailResult.WorkflowStepValue? stepValue = allValues!.FirstOrDefault(v => v.WorkflowStepResultId == inputClone.WorkflowStepResultId && v.LoopPath == inputClone.LoopPath); ;

            bool stepIsDirty = false;

            if (stepValue is not null)
            {
                if (stepValue.Value != inputClone.Value || stepValue.Note != inputClone.Note || stepValue.ImageFileName != inputClone.ImageFileName || stepValue.JsonPayload != inputClone.JsonPayload || stepValue.SourceWorkflowStepResultId != stepValue.SourceWorkflowStepResultId)
                {
                    var timeStampAndPosition = await timestampAndPositionResolver.GetTimestampAndPositionAsync();
                    DateTime localDateTime = await dateTimeService.GetLocalDateTime();

                    stepValue.Value = inputClone.Value;
                    stepValue.Note = inputClone.Note;
                    stepValue.ImageFileName = inputClone.ImageFileName;
                    stepValue.JsonPayload = inputClone.JsonPayload;
                    stepValue.SourceWorkflowStepResultId = inputClone.SourceWorkflowStepResultId;
                    stepValue.CompletedTimestamp = timeStampAndPosition.DateTime;
                    stepValue.CompletedLatitude = timeStampAndPosition.Latitude;
                    stepValue.CompletedLongitude = timeStampAndPosition.Longitude;
                    stepValue.RowUserId = employeeId;
                    
                    inputClone.CompletedTimestamp = timeStampAndPosition.DateTime;
                    inputClone.CompletedLocal = localDateTime;
                    inputClone.CompletedLatitude = timeStampAndPosition.Latitude;
                    inputClone.CompletedLongitude = timeStampAndPosition.Longitude;

                    workflowIsDirty = true;
                    stepIsDirty = true;
                }
            }
            else
            {
                var timeStampAndPosition = await timestampAndPositionResolver.GetTimestampAndPositionAsync();
                DateTime localDateTime = await dateTimeService.GetLocalDateTime();

                stepValue = new WorkflowResultDetailResult.WorkflowStepValue(
                    WorkflowStepResultValueId: 0,
                    WorkflowStepResultId: inputClone.WorkflowStepResultId!.Value,
                    WorkflowDataTypeId: inputClone.WorkflowDataTypeId!.Value,
                    LoopIndex: inputClone.LoopIndex!.Value,
                    LoopPath: inputClone.LoopPath,
                    WorkflowResultId: inputClone.WorkflowResultId!.Value,
                    StartedTimestamp: inputClone.StartedTimestamp!.Value,
                    StartedLocal: inputClone.StartedLocal.Value,
                    StartedLatitude: inputClone.StartedLatitude!.Value,
                    StartedLongitude: inputClone.StartedLongitude!.Value,
                    CompletedTimestamp: timeStampAndPosition.DateTime,
                    CompletedLocal: localDateTime,
                    CompletedLatitude: 0,
                    CompletedLongitude: 0,
                    SourceWorkflowStepResultId: inputClone.SourceWorkflowStepResultId,
                    SourceLoopIndex: inputClone.SourceLoopIndex,
                    SourceLoopPath: inputClone.SourceLoopPath,
                    Value: inputClone.Value,
                    Note: inputClone.Note,
                    ImageFileName: inputClone.ImageFileName,
                    JsonPayload: inputClone.JsonPayload,
                    RowUserId: employeeId,
                    WorkflowVersion: 0);

                
                inputClone.CompletedTimestamp = timeStampAndPosition.DateTime;
                inputClone.CompletedLocal = localDateTime;
                inputClone.CompletedLatitude = timeStampAndPosition.Latitude;
                inputClone.CompletedLongitude = timeStampAndPosition.Longitude;

                allValues!.Add(stepValue);

                workflowIsDirty = true;
                stepIsDirty = true;
            }

            if (stepIsDirty)
            {
                if (CurrentStep.WorkflowDataTypeId != (int)WorkflowDataTypes.Consumable)
                {
                    if (CurrentStep!.AttributeId.GetValueOrDefault(0) != 0)
                    {
                        dispatchIsDirty = TrySaveAttributeValue(CurrentStep.AttributeId.Value, inputClone.Value, inputClone.SourceWorkflowStepResultId, inputClone.SourceLoopPath);
                    }

                    if (dispatchIsDirty)
                    {
                        await dataService.SaveDispatchAsync(dispatch);
                        dispatchIsDirty = false;
                    }
                }

                if (workflowIsDirty)
                {
                    await dataService.SaveWorkflowAsync(workflowResultDetailOutput!);
                    workflowIsDirty = false;
                }

                if (boolPublish)
                {
                    await dataService.SaveAndPostWorkflowStepAsync(inputClone);
                }
            }

            CurrentInput = inputClone;
        }
        public async Task SaveDispatchAsync()
        {
            if (dispatch != null)
            {
                await dataService.SaveDispatchAsync(dispatch!);
            }
        }
        public void ResolveActualAndOrphanedValues()
        {
            WorkflowStepSortHelper sorter = new(allSteps, allValues);
            ActualValues = sorter.SortSteps();
            OrphanedValues = new();
        }
        public List<SelectableConsumable> GetSelectableConsumables(int attributeId)
        {
            var attribute = this.Lookups.AttributeResult.FirstOrDefault(a => a.Id == attributeId);
            if (attribute is not null)
            {
                if (attribute.Context == (int)AttributeContexts.Equipment)
                {
                    var equipStepValue = GetPriorEquipmentStepValue(CurrentInput.SourceWorkflowStepResultId, CurrentInput.SourceLoopPath);
                    if (equipStepValue is not null && !string.IsNullOrEmpty(equipStepValue.Value))
                    {
                        var equipment = GetEquipment(equipStepValue.Value);
                        if (equipment is not null)
                        {
                            var values = GetEquipmentAttributeValues(equipStepValue.Value, attributeId);
                            if (values is not null)
                            {
                                List<SelectableConsumable> result = new();

                                foreach (var cons in values)
                                {
                                    // Todo: capture source context
                                    result.Add(new(cons.Id, cons.Uid, equipment.Id, equipment.TemporaryId, cons.Quantity, cons.Value, cons.Notes, attributeId, attribute.Context, 0));
                                }
                                return result;
                            }
                        }
                    }
                }
                else
                {
                    //TODO: Get other selectable consumables.
                }
            }

            return null;
        }

        #endregion Public Methods

        #region Private Methods
        private WorkflowResultDetailResult.WorkflowStep? TryGetNextStep(WorkflowResultDetailResult.WorkflowStep sourceStep, List<WorkflowResultDetailResult.WorkflowStepOption> sourceOptions, WorkflowStepResultValueSaveInput sourceInput)
        {
            WorkflowResultDetailResult.WorkflowStep? NextSequential()
            {
                // Next step in line hits its simply moving through the chain
                var result = allSteps!.FirstOrDefault(s => s.SplitStepId == sourceStep.SplitStepId && s.SplitOption == sourceStep.SplitOption && s.Position == sourceStep.Position + 1);
                if (result is null)
                {
                    if (result is null)
                    {
                        // Try to walk up the splits looking for trailing steps after split path conclusions
                        var parentStep = sourceStep;
                        while (parentStep is not null)
                        {
                            if (parentStep.WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit)
                            {
                                result = parentStep;
                                break;
                            }
                            result = AllSteps!.FirstOrDefault(s => s.SplitStepId == parentStep.SplitStepId && s.SplitOption == parentStep.SplitOption && s.Position > parentStep.Position);
                            if (result is not null)
                            {
                                break;
                            }
                            parentStep = AllSteps!.FirstOrDefault(s => s.WorkflowStepId == parentStep.SplitStepId);
                        }
                    }
                }

                return result;
            }

            var dataType = (WorkflowDataTypes)sourceStep.WorkflowDataTypeId;

            #region Looking for jump steps

            // Boolean steps can jump on a true or false condition when there is a true or false jump condition.
            // Choice steps can jump on any option where there is a jump condition.
            // All other steps can jump on jump conditions.

            int? jumpStepId = null;

            switch (sourceStep.WorkflowDataTypeId)
            {
                case (int)WorkflowDataTypes.Boolean:
                    WorkflowDataTypeDetail dataTypeDetails = new WorkflowDataTypeDetail(sourceStep.WorkflowDataTypeDetail);
                    if (dataTypeDetails.TrueJumpStepId is not null && dataTypeDetails.TrueJumpConditionTypeId is not null && dataTypeDetails.TrueJumpConditionValue is not null)
                    {
                        if (JumpConditions.IsMatch(sourceStep.WorkflowDataTypeId, dataTypeDetails.TrueJumpConditionTypeId.Value, dataTypeDetails.TrueJumpConditionValue, sourceInput.Value))
                        {
                            jumpStepId = dataTypeDetails.TrueJumpStepId;
                        }
                    }
                    if (dataTypeDetails.FalseJumpStepId is not null && dataTypeDetails.FalseJumpConditionTypeId is not null && dataTypeDetails.FalseJumpConditionValue is not null)
                    {
                        if (JumpConditions.IsMatch(sourceStep.WorkflowDataTypeId, dataTypeDetails.FalseJumpConditionTypeId.Value, dataTypeDetails.FalseJumpConditionValue, sourceInput.Value))
                        {
                            jumpStepId = dataTypeDetails.FalseJumpStepId;
                        }
                    }
                    break;
                case (int)WorkflowDataTypes.Choice:
                    var option = sourceOptions.FirstOrDefault(o => o.Value == sourceInput.Value);
                    if (option is not null)
                    {
                        jumpStepId = option.JumpStepId;
                    }
                    break;
                case (int)WorkflowDataTypes.Information:
                    jumpStepId = sourceStep.JumpStepId;
                    break;
                default:
                    if (sourceStep.JumpStepId is not null && sourceStep.JumpConditionTypeId is not null)
                    {
                        if (JumpConditions.IsMatch(sourceStep.WorkflowDataTypeId, sourceStep.JumpConditionTypeId.Value, sourceStep.JumpConditionValue, sourceInput.Value))
                        {
                            jumpStepId = sourceStep.JumpStepId;
                        }
                    }
                    break;
            }

            if (jumpStepId is not null)
            {
                return allSteps!.FirstOrDefault(s => s.WorkflowStepId == sourceStep.JumpStepId);
            }

            #endregion Looking for jump steps

            #region Handling Loops and Splits

            if (sourceStep.WorkflowDataTypeId == (int)WorkflowDataTypes.Loop)
            {
                WorkflowDataTypeDetail dataTypeDetails = new WorkflowDataTypeDetail(sourceStep.WorkflowDataTypeDetail);
                if (dataTypeDetails.LoopExitType == 1)
                {
                    LoopPath loopPath = new(sourceInput.LoopPath);
                    var current = loopPath.Current();
                    if (current is not null)
                    {
                        var count = current.Index + 1;
                        if (count < dataTypeDetails.FixedLoopCount)
                        {
                            return allSteps!.FirstOrDefault(s => s.WorkflowStepId == dataTypeDetails.LoopToStepId);
                        }
                    }
                }
                if (dataTypeDetails.LoopExitType == 2 && dataTypeDetails.LoopContinueText == sourceInput.Value)
                {
                    return allSteps!.FirstOrDefault(s => s.WorkflowStepId == dataTypeDetails.LoopToStepId);
                }
                return NextSequential();
            }

            if (dataType == WorkflowDataTypes.Split)
            {
                return allSteps!.FirstOrDefault(s => s.SplitStepId == sourceStep.WorkflowStepId && s.SplitOption == sourceInput.Value) ?? NextSequential();
            }

            if (dataType == WorkflowDataTypes.ParallelSplit)
            {
                WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(sourceStep!.WorkflowDataTypeDetail);
                ParallelSplitOptions parallelSplitOptions = new(details.Values["Options"]);

                // Terminating
                if (sourceInput.JsonPayload is null)
                {
                    var trailingStep = allSteps.FirstOrDefault(s => s.SplitStepId == sourceStep.SplitStepId && s.SplitOption == sourceStep.SplitOption && s.Position == sourceStep.Position + 1);
                    if (trailingStep is not null)
                    {
                        return trailingStep;
                    }
                    var parallelSplitStepParent = allSteps!.FirstOrDefault(s => s.WorkflowStepId == sourceStep.SplitStepId);
                    while (parallelSplitStepParent is not null)
                    {
                        if (parallelSplitStepParent.WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit)
                        {
                            return parallelSplitStepParent;
                        }

                        var nextStep = allSteps.FirstOrDefault(s => s.SplitStepId == parallelSplitStepParent.SplitStepId && s.SplitOption == parallelSplitStepParent.SplitOption && s.Position == parallelSplitStepParent.Position + 1);
                        if (nextStep is not null)
                        {
                            return nextStep;
                        }
                        parallelSplitStepParent = allSteps.FirstOrDefault(s => s.WorkflowStepId == parallelSplitStepParent.SplitStepId);
                    }
                }
                else
                {
                    var option = parallelSplitOptions.Options.FirstOrDefault(o => o.Name == sourceInput.JsonPayload);
                    return allSteps.FirstOrDefault(s => s.SplitStepId == sourceStep.WorkflowStepId && s.SplitOption == sourceInput.JsonPayload);
                }

                return null;

            }

            if (dataType == WorkflowDataTypes.AutoSwitch)
            {
                return allSteps!.FirstOrDefault(s => s.SplitStepId == sourceStep.WorkflowStepId && s.SplitOption == sourceInput.Value) ?? NextSequential();
            }

            if (dataType == WorkflowDataTypes.ValueSwitch)
            {
                return allSteps!.FirstOrDefault(s => s.SplitStepId == sourceStep.WorkflowStepId && s.SplitOption == sourceInput.Value) ?? NextSequential();
            }

            #endregion Handling Loops and Splits

            return NextSequential();
        }
        private bool IsLoopTarget(WorkflowResultDetailResult.WorkflowStep testStep)
        {
            foreach (var step in allSteps!.Where(s => s.SplitStepId == testStep.SplitStepId && s.SplitOption == testStep.SplitOption && s.Position > testStep.Position))
            {
                if (step.WorkflowDataTypeDetail is not null && step.WorkflowDataTypeDetail.Contains("LoopToStepId"))
                {
                    WorkflowDataTypeDetail loopDetails = new WorkflowDataTypeDetail(step.WorkflowDataTypeDetail);
                    if (loopDetails.LoopToStepId == testStep.WorkflowStepId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private WorkflowResultDetailResult.WorkflowStepValue? GetValue(int nextWorkflowStepResultId, int? sourceWorkflowStepResultId, string? sourceLoopPath, int sourceLoopIndex)
        {
            var result = allValues.FirstOrDefault(v => v.WorkflowStepResultId == nextWorkflowStepResultId && v.SourceWorkflowStepResultId == sourceWorkflowStepResultId && v.LoopPath == sourceLoopPath && v.LoopIndex == sourceLoopIndex);
            if (result is null)
            {
                result = allValues.FirstOrDefault(v => v.WorkflowStepResultId == nextWorkflowStepResultId && v.LoopPath == sourceLoopPath && v.LoopIndex == sourceLoopIndex);
            }
            return result;
        }
        private WorkflowResultDetailResult.WorkflowStep FirstStep()
        {
            return allSteps!.Where(s => s.SplitStepId is null).OrderBy(s => s.Position).FirstOrDefault()!;
        }
        private DispatchDetailResult.AllAttributeValue? GetAttributeValueAsync(int attributeId)
        {
            return dispatch?.AllAttributeValueResult?.FirstOrDefault(a => a.AttributeId == attributeId);
        }
        private string? GetStepDefault(WorkflowResultDetailResult.WorkflowStep step, int? sourceWorkflowStepResultId, string? sourceLoopIndex)
        {
            string? result = null;

            if (step.AttributeId != null)
            {
                AttributeValue attributeResult = null;

                var attribute = lookups?.AttributeResult?.FirstOrDefault(a => a.Id == step.AttributeId);

                if (attribute != null)
                {
                    switch ((AttributeContexts)attribute.Context)
                    {
                        case AttributeContexts.Equipment:

                            var equipmentStepValue = GetPriorEquipmentStepValue(sourceWorkflowStepResultId, sourceLoopIndex);
                            if (equipmentStepValue != null)
                            {

                                if (attribute.IsConsumable)
                                {
                                    //Consumables will not have a default.
                                    return null;
                                }
                                else if (attribute.IsItem)
                                {
                                    // TODO: What a mess this is.
                                    return null;
                                }
                                else if (attribute.IsChecklist)
                                {
                                    // TODO: What a mess this is.
                                    return null;
                                }
                                else
                                {
                                    var equipmentAttribute = GetEquipmentAttribute(equipmentStepValue.Value!, step.AttributeId.Value);
                                    if (equipmentAttribute != null)
                                    {
                                        return equipmentAttribute.Value;
                                    }
                                }
                            }
                            break;
                        default:
                            var attributeStepValue = GetAttributeValueAsync(step.AttributeId.Value);
                            if (attributeStepValue != null)
                            {
                                return attributeStepValue.Value;
                            }
                            break;
                    }
                }

                if (attributeResult is not null)
                {
                    result = JsonSerializer.Serialize<AttributeValue>(attributeResult);
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                result = step.DefaultValue;
            }

            return result;
        }
        private DispatchDetailResult.Equipment? GetEquipment(string idOrGuid)
        {
            if (Guid.TryParse(idOrGuid, out Guid guid))
            {
                var equipment = dispatch?.EquipmentResult?.FirstOrDefault(e => e.TemporaryId == guid);
                if (equipment != null)
                {
                    return equipment;
                }
            }
            else if (int.TryParse(idOrGuid, out int id))
            {
                if (id != 0)
                {
                    var equipment = dispatch?.EquipmentResult?.FirstOrDefault(e => e.Id == id);
                    if (equipment != null)
                    {
                        return equipment;
                    }
                }
            }

            return null;
        }
        private DispatchDetailResult.EquipmentAttributeValue? GetEquipmentAttribute(string idOrGuid, int attributeId)
        {
            if (Guid.TryParse(idOrGuid, out Guid guid))
            {
                var equipmentAttribute = dispatch?.EquipmentAttributeValueResult?.FirstOrDefault(e => e.EquipmentUid == guid && e.AttributeId == attributeId);
                if (equipmentAttribute != null)
                {
                    return equipmentAttribute;
                }
            }
            else if (int.TryParse(idOrGuid, out int id))
            {
                if (id != 0)
                {
                    var equipmentAttribute = dispatch?.EquipmentAttributeValueResult?.FirstOrDefault(e => e.EquipmentId == id && e.AttributeId == attributeId);
                    if (equipmentAttribute != null)
                    {
                        return equipmentAttribute;
                    }
                }
            }
            return null;
        }
        private bool TrySaveAttributeValue(int attributeId, string? value, int? sourceWorkflowStepResultId, string? sourceLoopPath)
        {
            if (dispatch != null)
            {
                var attribute = lookups?.AttributeResult?.FirstOrDefault(a => a.Id == attributeId);

                if (attribute != null)
                {
                    switch ((AttributeContexts)attribute.Context)
                    {
                        case AttributeContexts.Equipment:

                            var equipmentStepValue = GetPriorEquipmentStepValue(sourceWorkflowStepResultId, sourceLoopPath);
                            if (equipmentStepValue is not null && equipmentStepValue.Value is not null)
                            {
                                var equipment = GetEquipment(equipmentStepValue.Value);

                                if (equipment is not null)
                                {
                                    var equipmentAttributeValue = this.GetEquipmentAttribute(equipmentStepValue.Value, attributeId);

                                    if (equipmentAttributeValue != null)
                                    {
                                        if (string.IsNullOrEmpty(value))
                                        {
                                            dispatch.EquipmentAttributeValueResult!.Remove(equipmentAttributeValue);
                                        }
                                        else
                                        {
                                            if (attribute.IsConsumable)
                                            {
                                                // TODO: If this is an array, it is coming from the consumable picker
                                                // Need a better way to delineate what consumables update the equipment and which do not.
                                                if (value.StartsWith("[")) return false;

                                                AttributeValue attributeValue = JsonSerializer.Deserialize<AttributeValue>(value)!;
                                                equipmentAttributeValue.Value = attributeValue.Value!;
                                                equipmentAttributeValue.Quantity = attributeValue.Quantity;
                                                equipmentAttributeValue.LastModifiedBy = employeeId;
                                                equipmentAttributeValue.LastModifiedDate = DateTime.UtcNow;
                                                equipment.IsDirty = true;
                                                return true;
                                            }

                                            if (equipmentAttributeValue.Value != value)
                                            {
                                                equipmentAttributeValue.Value = value;
                                                equipmentAttributeValue.LastModifiedBy = employeeId;
                                                equipmentAttributeValue.LastModifiedDate = DateTime.UtcNow;
                                                equipment.IsDirty = true;
                                                return true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            if (attribute.IsConsumable)
                                            {
                                                AttributeValue attributeValue = JsonSerializer.Deserialize<AttributeValue>(value)!;
                                                dispatch.EquipmentAttributeValueResult!.Add(new(0, Guid.NewGuid(), attribute.Id, equipment.Id, equipment.TemporaryId, attributeValue.Quantity, attributeValue.Value!, attributeValue.Note, employeeId, DateTime.UtcNow));
                                                return true;
                                            }
                                            else
                                            {
                                                dispatch.EquipmentAttributeValueResult!.Add(new(0, Guid.NewGuid(), attribute.Id, equipment.Id, equipment.TemporaryId, 0, value, null, employeeId, DateTime.UtcNow));
                                                equipment.IsDirty = true;
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }

                            break;

                        default:

                            var existingValue = dispatch.AllAttributeValueResult?.FirstOrDefault(a => a.AttributeId == attributeId);

                            if (existingValue is not null)
                            {
                                if (string.IsNullOrEmpty(value))
                                {
                                    dispatch.AllAttributeValueResult!.Remove(existingValue);
                                    return true;
                                }
                                else
                                {
                                    if (existingValue.Value != value)
                                    {
                                        existingValue.Value = value;
                                        existingValue.LastModifiedDate = DateTime.UtcNow;
                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(value))
                                {
                                    dispatch.AllAttributeValueResult!.Add(new(attributeId, attributeId, 0, 0, 0, value, DateTime.UtcNow));
                                    return true;
                                }
                            }

                            break;
                    }
                }
            }
            return false;
        }
        private WorkflowResultDetailResult.WorkflowStepValue? PreviousStepValue(WorkflowResultDetailResult.WorkflowStepValue stepValue)
        {
            return allValues!.FirstOrDefault(v => v.WorkflowStepResultId == stepValue.SourceWorkflowStepResultId && v.LoopPath == stepValue.SourceLoopPath)!;
        }
        public WorkflowResultDetailResult.WorkflowStepValue? GetPriorEquipmentStepValue(int? sourceWorkflowStepResultId, string? sourceLoopPath)
        {
            var previousValue = allValues!.FirstOrDefault(v => v.WorkflowStepResultId == sourceWorkflowStepResultId && v.LoopPath == sourceLoopPath);
            while (previousValue != null)
            {
                var step = allSteps!.FirstOrDefault(s => s.WorkflowStepResultId == previousValue.WorkflowStepResultId)!;
                if (step != null)
                {
                    if (step.WorkflowDataTypeId == (int)WorkflowDataTypes.Equipment)
                    {
                        return previousValue;
                    }
                }

                previousValue = PreviousStepValue(previousValue);
            }
            return null;
        }
        private WorkflowStepResultValueSaveInput CreateInputFromResultValue(WorkflowResultDetailResult.WorkflowStepValue value)
        {
            return new WorkflowStepResultValueSaveInput
            {
                WorkflowStepResultId = value.WorkflowStepResultId,
                LoopIndex = value.LoopIndex,
                WorkflowResultId = value.WorkflowResultId,
                WorkflowDataTypeId = value.WorkflowDataTypeId,
                LoopPath = value.LoopPath,
                StartedTimestamp = value.StartedTimestamp,
                StartedLocal = value.StartedLocal,
                StartedLatitude = value.StartedLatitude,
                StartedLongitude = value.StartedLongitude,
                CompletedTimestamp = value.CompletedTimestamp,
                CompletedLocal = value.CompletedLocal,
                CompletedLatitude = value.CompletedLatitude,
                CompletedLongitude = value.CompletedLongitude,
                SourceWorkflowStepResultId = value.SourceWorkflowStepResultId,
                SourceLoopIndex = value.SourceLoopIndex,
                SourceLoopPath = value.SourceLoopPath,
                Value = value.Value,
                Note = value.Note,
                ImageFileName = value.ImageFileName,
                JsonPayload = value.JsonPayload,
                RowUserId = value.RowUserId
            };
        }
        private async Task<WorkflowStepResultValueSaveInput> CreateInputFromStep(WorkflowResultDetailResult.WorkflowStep step, int loopIndex, string? loopPath, int? sourceWorkflowStepResultId, int sourceLoopIndex, string? sourceLoopPath)
        {
            TimestampAndPosition timestampAndPosition;
            if (timestampAndPositionResolver is null)
            {
                timestampAndPosition = new TimestampAndPosition(DateTime.UtcNow, 0, 0);
            }
            else
            {
                timestampAndPosition = await timestampAndPositionResolver.GetTimestampAndPositionAsync();
            }

            return new WorkflowStepResultValueSaveInput
            {
                LoopIndex = loopIndex,
                LoopPath = loopPath,
                WorkflowResultId = step.WorkflowResultId,
                WorkflowStepResultId = step.WorkflowStepResultId,
                WorkflowDataTypeId = step.WorkflowDataTypeId,
                StartedLatitude = timestampAndPosition.Latitude,
                StartedLongitude = timestampAndPosition.Longitude,
                StartedTimestamp = timestampAndPosition.DateTime,
                StartedLocal = DateTime.Now,
                SourceLoopIndex = sourceLoopIndex,
                SourceWorkflowStepResultId = sourceWorkflowStepResultId,
                SourceLoopPath = sourceLoopPath,
                Value = GetStepDefault(step, sourceWorkflowStepResultId, sourceLoopPath),
                RowUserId = employeeId
            };
        }
        private void ThrowExceptionIfInvalidStateForOperation(string methodName)
        {
            if (!loaded)
            {
                throw new InvalidOperationException($"The call to LoadData must be called prior to calling {methodName}!");
            }

            switch (methodName)
            {
                case nameof(MoveNext):
                case nameof(MovePrevious):
                    if (CurrentInput is null)
                    {
                        throw new ArgumentNullException(nameof(CurrentInput), $"Cannot call {methodName} step when CurrentInput has not been established - use {nameof(GetFirstStep)} or {nameof(MoveToLastCompletedOrFirst)} to establish CurrentInput");
                    }

                    if (CurrentStep is null)
                    {
                        throw new ArgumentNullException(nameof(CurrentInput), $"Cannot call {methodName} step when CurrentStep has not been established - use {nameof(GetFirstStep)} or {nameof(MoveToLastCompletedOrFirst)} to establish CurrentInput");
                    }
                    break;
            }
        }
        public void UpdateEquipment(WorkflowEquipment sourceEquipment)
        {
            if (sourceEquipment.Id != 0)
            {
                dispatch!.EquipmentResult!.RemoveAll(e => e.Id == sourceEquipment.Id);
                dispatch!.EquipmentAttributeValueResult!.RemoveAll(e => e.EquipmentId == sourceEquipment.Id);
            }
            else
            {
                dispatch!.EquipmentResult!.RemoveAll(e => e.TemporaryId == sourceEquipment.TemporaryId);
                dispatch!.EquipmentAttributeValueResult!.RemoveAll(e => e.EquipmentUid == sourceEquipment.TemporaryId);
            }

            dispatch!.EquipmentAttributeValueResult.AddRange(sourceEquipment.GetEquipmentAttributeAndConsumableValues());
            dispatch!.EquipmentResult.Add((DispatchDetailResult.Equipment)sourceEquipment);
        }
        private List<DispatchDetailResult.EquipmentAttributeValue>? GetEquipmentAttributeValues(string idOrGuid, int attributeId)
        {
            if (!string.IsNullOrEmpty(idOrGuid))
            {
                if (Guid.TryParse(idOrGuid, out Guid uid))
                {
                    return dispatch.EquipmentAttributeValueResult.FindAll(e => e.EquipmentUid == uid && e.AttributeId == attributeId);
                }
                if (int.TryParse(idOrGuid, out int id))
                {
                    if (id != 0)
                    {
                        return dispatch.EquipmentAttributeValueResult.FindAll(e => e.EquipmentId == id && e.AttributeId == attributeId);
                    }
                }
            }

            return null;
        }
        private string? ResolveAutoSwitchSwitchOption(WorkflowResultDetailResult.WorkflowStep step, string? valueSelectorValue = null)
        {
            string? switchOption = null;
            WorkflowDataTypeDetail detail = new WorkflowDataTypeDetail(step.WorkflowDataTypeDetail);

            if (step.WorkflowDataTypeId == (int)WorkflowDataTypes.ValueSwitch)
            {
                if (detail.Values.ContainsKey(nameof(ValueSwitcher)))
                {
                    var valueSwitcher = detail.Values[nameof(ValueSwitcher)].ToObject<ValueSwitcher>();
                    if (valueSwitcher != null)
                    {
                        if (decimal.TryParse(valueSelectorValue, out decimal value))
                        {
                            foreach (var selector in valueSwitcher.ValueSelectors.Where(s => s.IsDefault == false))
                            {
                                if (value >= selector.MinValue && value < selector.MaxValue)
                                {
                                    return selector.Name;
                                }
                            }
                        }
                    }
                    var defaultSelector = valueSwitcher.ValueSelectors.FirstOrDefault(s => s.IsDefault == true);
                    return defaultSelector != null ? defaultSelector.Name : string.Empty;
                }
            }
            else
            {
                if (detail.Values.ContainsKey(nameof(AutoSwitcher)))
                {
                    var autoSwitcher = detail.Values[nameof(AutoSwitcher)].ToObject<AutoSwitcher>();
                    if (autoSwitcher is not null)
                    {
                        var defaultSelector = autoSwitcher.SwitchSelectors.FirstOrDefault(s => s.IsDefault);
                        if (defaultSelector is not null)
                        {
                            switchOption = defaultSelector.Name;
                        }
                        int[] entityIds = GetEntityId(autoSwitcher.DataSourceId);
                        if (entityIds[0] != 0)
                        {
                            foreach (var entityId in entityIds)
                            {
                                foreach (var switchSelector in autoSwitcher.SwitchSelectors)
                                {
                                    if (switchSelector.Ids.Contains(entityId))
                                    {
                                        switchOption = switchSelector.Name;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return switchOption;
        }
        private int[] GetEntityId(int entityType)
        {
            int[]? result = new int[] { 0 };

            switch (entityType)
            {
                case 1:
                    break;

                case 2:
                    result = new int[] { Workflow?.CustomerId ?? 0 };
                    break;

                case 3:
                    result = new int[] { Workflow?.CustomerLocationId ?? 0 };
                    break;

                case 4:
                    if (Lookups?.TaskCodesResult is not null && Dispatch?.DispatchResult is not null && !string.IsNullOrEmpty(Dispatch.DispatchResult.Tasks))
                    {
                        var OurTaskCodes = Dispatch.DispatchResult.Tasks.Split(",").ToList();
                        result = Lookups.TaskCodesResult
                            .Where(tc => OurTaskCodes.Contains(tc.TaskCode ?? string.Empty))
                            .Select(tc => tc.Id)
                            .ToArray();
                    }
                    break;

                case 5:
                    var equipStep = GetPriorEquipmentStepValue(CurrentInput.WorkflowStepResultId, CurrentInput.LoopPath);
                    if (equipStep is not null)
                    {
                        var equip = GetEquipment(equipStep.Value);
                        if (equip is not null)
                        {
                            result = new int[] { equip.EquipmentTypeId };
                        }
                    }
                    break;

                case 10: // Employee
                    result = new int[] { Workflow?.EmployeeId ?? 0 };
                    break;

                case 11: // Notification Group
                    result = new int[] { Workflow?.NotificationGroupId ?? 0 };
                    break;

                case 12: // Attribute
                    result = new int[] { CurrentStep?.AttributeId ?? 0 };
                    break;

                case 101:
                    result = new int[] { (int)DateTime.Now.DayOfWeek };
                    break;

                case 102:
                    DateTime date = DateTime.Now;
                    var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                    var firstDayWeek = (int)firstDayOfMonth.DayOfWeek;
                    var adjustedDay = date.Day + firstDayWeek;
                    result = new int[] { (int)Math.Ceiling(adjustedDay / 7.0) };
                    break;


            }

            return result;
        }

        #endregion
    }
}

//public IEnumerable<LookupsResult.Attribute> GetItemAttributes()
//{
//    if (lookups?.AttributeResult is null)
//    {
//        yield break;
//    }
//    foreach (var attribute in lookups.AttributeResult.Where(a => a.IsItem == true))
//    {
//        yield return attribute;
//    }
//}


//public async Task CompleteWorkflow()
//{
//    workflowResultDetailOutput!.ResultData!.WorkflowResult!.Completed = DateTime.UtcNow;
//    workflowResultDetailOutput!.ResultData!.WorkflowResult!.Status = 4;
//    AllValues.RemoveAll(s => s.CompletedTimestamp == SQL_MIN_DATE);
//    await dataService.SaveWorkflowAsync(workflowResultDetailOutput!);
//    workflowIsDirty = false;
//}

//private WorkflowResultDetailResult.WorkflowStepValue Clone(WorkflowResultDetailResult.WorkflowStepValue source)
//{
//    return new WorkflowResultDetailResult.WorkflowStepValue(
//       WorkflowStepResultValueId: source.WorkflowStepResultValueId,
//       WorkflowStepResultId: source.WorkflowStepResultId,
//       WorkflowDataTypeId: source.WorkflowDataTypeId,
//       LoopIndex: source.LoopIndex,
//       LoopPath: source.LoopPath,
//       WorkflowResultId: source.WorkflowResultId,
//       StartedTimestamp: source.StartedTimestamp,
//       StartedLocal: source.StartedLocal,
//       StartedLatitude: source.StartedLatitude,
//       StartedLongitude: source.StartedLongitude,
//       CompletedTimestamp: source.CompletedTimestamp,
//       CompletedLocal: source.CompletedLocal,
//       CompletedLatitude: source.CompletedLatitude,
//       CompletedLongitude: source.CompletedLongitude,
//       SourceWorkflowStepResultId: source.SourceWorkflowStepResultId,
//       SourceLoopIndex: source.SourceLoopIndex,
//       SourceLoopPath: source.SourceLoopPath,
//       Value: source.Value,
//       Note: source.Note,
//       ImageFileName: source.ImageFileName,
//       JsonPayload: source.JsonPayload,
//       RowUserId: source.RowUserId,
//       WorkflowVersion: source.WorkflowVersion
//   );
//}