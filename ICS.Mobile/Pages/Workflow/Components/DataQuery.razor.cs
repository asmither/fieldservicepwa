using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;
using System.Transactions;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class DataQuery
    {
        #region Fields and Properties

        private Dictionary<string, string>? options = null;
        private bool AllowAdditions = false;
        private string listId => $"wf-{Workflow!.WorkflowResultId}-{WorkflowStep!.WorkflowStepId}";

        private List<string> prompts = null;

        private string? prompt = null;
        public string? Prompt
        {
            set
            {
                if (value != prompt)
                {
                    prompt = value;

                    if (string.IsNullOrEmpty(prompt))
                    {
                        Input!.Value = null;
                    }
                    else
                    {
                        if (options!.TryGetValue(prompt, out string? optionValue))
                        {
                            Input!.Value = optionValue;
                        }
                        else
                        {
                            Input!.Value = prompt;
                        }
                    }

                    NextDisabled = !IsValid();
                }
            }
            get
            {
                return prompt;
            }
        }

        #endregion Fields and Properties

        #region Callbacks

        protected override void NoteChanged()
        {
            NextDisabled = !IsValid();
        }
        protected override void ImageChanged()
        {
            NextDisabled = !IsValid();
        }

        #endregion Callbacks

        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (WorkflowStep!.IsRequired)
            {
                if (string.IsNullOrEmpty(Input!.Value))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(Input!.Value))
            {
                if (!AllowAdditions)
                {
                    if (!options!.ContainsValue(Input.Value))
                    {
                        return false;
                    }
                }
            }

            return base.IsValid();

        }

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();

            if (string.IsNullOrEmpty(Input!.Value))
            {
                if (!string.IsNullOrEmpty(WorkflowStep!.DefaultValue))
                {
                    Input.Value = WorkflowStep.DefaultValue;
                }
            }

            if (StepDetails!.DataQueryId is null)
            {
                throw new ArgumentException("Missing required parameter Data Query Id");
            }

            if (StepDetails.AllowAdditions is null)
            {
                throw new ArgumentException("Missing required parameter AllowAdditions");
            }

            AllowAdditions = StepDetails.AllowAdditions.Value;

            string? parameter = null;

            if (StepDetails.WorkflowDataStepId.HasValue)
            {
                // TODO: Remove this and get value from runner
                parameter = await DataService.QueryWorkflowStepResultValueAsync(Workflow!.WorkflowResultId, StepDetails.WorkflowDataStepId.Value, Input.LoopIndex.Value);
            }
            else if (!string.IsNullOrEmpty(StepDetails.WorkflowDataText))
            {
                parameter = StepDetails.WorkflowDataText;
            }

            var optionsResult = await DataService.GetWorkflowDataQueryOptionsAsync(new WorkflowDataQueryOptionsInput
            {
                CustomerId = Workflow!.CustomerId,
                WorkOrderDispatchId = Workflow.WorkOrderDispatchId,
                WorkOrderDispatchTechId = Workflow.WorkOrderDispatchTechId,
                CustomerLocationId = Workflow.CustomerLocationId,
                QueryId = StepDetails!.DataQueryId,
                EmployeeId = Workflow.EmployeeId,
                WorkflowId = Workflow.WorkflowId,
                BinderId = Workflow.BinderId,
                Parameter = parameter
            });

            options = new Dictionary<string, string>();
            prompts = new List<string>();

            if (optionsResult is not null && optionsResult.Count != 0)
            {
                foreach (var option in optionsResult)
                {
                    options.Add(option.Prompt, option.Value);
                    prompts.Add(option.Prompt);
                    if (option.Value == Input.Value)
                    {
                        prompt = option.Prompt;
                    }
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
