using ICS.Portal.Data.Custom;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Loop
    {
        #region Fields

        private int? loopExitType;
        private string? loopContinueText;
        private string? loopExitText;
        private int? fixedLoopCount;
        private int? loopToStepId;
        bool ignoreChange = false;
        #endregion Fields

        private void SetValue(string value)
        {
            ignoreChange = true;
            Input!.Value = value;
            NextDisabled = !IsValid();
            ignoreChange = false;
        }
        protected override bool IsValid()
        {
            if (string.IsNullOrEmpty(Input!.Value))
            {
                return false;
            }
            return base.IsValid();
        }

        
        protected override void OnParametersSet()
        {
            if (ignoreChange) return;

            base.OnInitialized();

            if (StepDetails is not null)
            {
                if (StepDetails.LoopExitType is not null)
                {
                    loopExitType = StepDetails.LoopExitType;
                }
                if (StepDetails.LoopContinueText is not null)
                {
                    loopContinueText = StepDetails.LoopContinueText;
                }
                if (StepDetails.LoopExitText is not null)
                {
                    loopExitText = StepDetails.LoopExitText;
                }
                if (StepDetails.FixedLoopCount is not null)
                {
                    fixedLoopCount = StepDetails.FixedLoopCount;
                }
                if (StepDetails.LoopToStepId is not null)
                {
                    loopToStepId = StepDetails.LoopToStepId;
                }
            }


            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
