namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Split
    {

        #region Fields and Properties

        private List<string>? Options { set; get; }

        #endregion Fields and Properties

        private void SetValue(string value)
        {
            if (Input!.Value == value)
            {
                Input.Value = null;
                NextDisabled = !IsValid();
            }
            else
            {
                if (Input!.Value != value)
                {
                    Input.Value = value;
                    NextDisabled = !IsValid();
                }
            }
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

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            Options = StepDetails!.Options;

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
