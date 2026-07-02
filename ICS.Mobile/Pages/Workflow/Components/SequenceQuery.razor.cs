using ICS.Portal.Data.Queries.Models;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public  partial class SequenceQuery
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (StepDetails!.SequenceQueryId is null)
            {
                throw new ArgumentException("Missing required parameter Sequence Query Id");
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }

        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {
                if (string.IsNullOrEmpty(Input!.Value))
                {
                    return false;
                }
            }

            return base.IsValid();
        }

        public async Task GetSequence()
        {
            ErrorMessage = null;

            var valueResult = await DataService.WorkflowSequenceQueryValueAsync(new WorkflowSequenceQueryValueInput
            {
                CustomerId = Workflow!.CustomerId,
                WorkOrderDispatchId = Workflow.WorkOrderDispatchId,
                WorkOrderDispatchTechId = Workflow.WorkOrderDispatchTechId,
                CustomerLocationId = Workflow.CustomerLocationId,
                QueryId = StepDetails!.SequenceQueryId,
                EmployeeId = Workflow.EmployeeId,
                WorkflowId = Workflow.WorkflowId,
                BinderId = Workflow.BinderId
            });


            if (valueResult is null || valueResult.Value is null)
            {
                ErrorMessage = "Unexepected error trying to retrieve new sequence, please try again.";
            }
            else
            {
                Input!.Value = valueResult.Value;
            }

            Initialized = true;

            NextDisabled = !IsValid();
        }
    }
}
