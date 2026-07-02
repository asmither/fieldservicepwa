using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Enumerations;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class ParallelSplit
    {
        #region Fields and Properties

        private bool ignoreChange = false;
        private List<ParallelSplitOption> Options = new();
        private List<string> PreviousSelectedOptions = new();
        private string TerminatingOption = string.Empty;

        private bool AllowExit;
        private int TerminatingStepId = 0;
        private List<int> Values;

        #endregion Fields and Properties

        [Parameter]
        public WorkflowRunnerLogic? Runner { set; get; }

        private string? _Value = null;
        public string? Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
            }
        }

        private void SetValue(string value)
        {
            ignoreChange = true;
            Input!.JsonPayload = null;
            List<int> temp = new(Values);
            if (value == TerminatingOption)
            {
                if (!temp.Contains(TerminatingStepId))
                {
                    temp.Add(TerminatingStepId);
                }
                Input.Value = string.Join(",", temp);
            }
            else
            {
                Input!.JsonPayload = value;
            }
            Value = value;
            NextDisabled = !IsValid();
            ignoreChange = false;
        }
        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {
                if (string.IsNullOrEmpty(Value))
                {
                    return false;
                }
            }
            return base.IsValid();
        }
        private bool PreviouslySelected(string option)
        {
            return PreviousSelectedOptions!.Contains(option);
        }

        protected override void OnParametersSet()
        {
            if (ignoreChange) return;

            base.OnParametersSet();

            if (Input is null)
            {
                return;
            }

            WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(WorkflowStep!.WorkflowDataTypeDetail);
            ParallelSplitOptions parallelSplitOptions = new(details.Values["Options"]);
            Options = parallelSplitOptions.Options.Where(o => o.IsTerminator == false).ToList();
            TerminatingOption = parallelSplitOptions.Options.FirstOrDefault(o => o.IsTerminator == true)!.Name;

            string? defaultSelectionAsString = WorkflowStep.DefaultValue;
            string? inputValueAsString = Input.Value;

            if(defaultSelectionAsString is not null && Input.Value is not null && defaultSelectionAsString == Input.Value)
            {
                Input.Value = null;
            }

            Values = string.IsNullOrEmpty(Input.Value) ? new List<int>() : Input.Value.Split(",").Select(s => int.Parse(s)).ToList();

            AllowExit = true;
            PreviousSelectedOptions = new();

            
            if (Runner is not null && Runner.AllSteps is not null)
            {
                foreach (var option in Options)
                {
                    var optionStep = Runner.AllSteps.FirstOrDefault(s => s.SplitStepId == WorkflowStep!.WorkflowStepId && s.SplitOption == option.Name && s.Position == 1);

                    if(optionStep is not null)
                    {
                        if (Values.Contains(optionStep.WorkflowStepResultId))
                        {
                            PreviousSelectedOptions.Add(option.Name);
                        }
                        else
                        {
                            if (option.Required == true)
                            {
                                AllowExit = false;
                            }
                        }
                    }

                    TerminatingStepId = 0;

                    var terminatingStep = Runner.AllSteps.FirstOrDefault(s => s.SplitStepId == WorkflowStep!.SplitStepId && s.SplitOption == WorkflowStep.SplitOption && s.Position == WorkflowStep.Position + 1);
                    if (terminatingStep is null)
                    {
                        terminatingStep = Runner.AllSteps.FirstOrDefault(s => s.WorkflowStepId == WorkflowStep!.SplitStepId && s.WorkflowDataTypeId == (int)WorkflowDataTypes.ParallelSplit);
                    }
                    if (terminatingStep is not null)
                    {
                        TerminatingStepId = terminatingStep.WorkflowStepResultId;
                    }
                    if (Values.Contains(TerminatingStepId))
                    {
                        PreviousSelectedOptions.Add(TerminatingOption);
                    }
                }

                Input.JsonPayload = null;
                Value = null;

                if(defaultSelectionAsString is not null && PreviousSelectedOptions.Count == 0)
                {
                    var option = Options.FirstOrDefault(s => s.Name == defaultSelectionAsString);
                    if(option is not null)
                    {
                        SetValue(option.Name);
                    }
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
    