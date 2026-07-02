namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Workflow
    {
        private bool InterruptExisting = false;

        protected override void NoteChanged()
        {
            NextDisabled = !IsValid();
        }
        protected override void ImageChanged()
        {
            NextDisabled = !IsValid();
        }

        protected override bool IsValid()
        {
            if (WorkflowStep!.IsRequired)
            {

                if (Input!.Value is null)
                {
                    return false;
                }
                return base.IsValid();
            }
            else
            {
                return base.IsValid();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (StepDetails is not null)
            {
                Input!.Value = StepDetails.WorkflowId.ToString();
                if (StepDetails.InterruptExisting.HasValue)
                {
                    InterruptExisting = StepDetails.InterruptExisting.Value;
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
