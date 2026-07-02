namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class SmallText : WorkflowComponentsBasePage
    {
        #region Fields

        private int MinLength = 1;
        private int MaxLength = 200;
        private string? RegEx = null;
        private string? PlaceHolder = null;

        #endregion Fields

        #region Callbacks

        protected override void ImageChanged()
        {
            if (IsValid())
            {
                NextDisabled = false;
            }
            else
            {
                NextDisabled = true;
            }
        }

        #endregion Callbacks

        #region Properties

        private string? Value
        {
            set
            {
                if (value != Input!.Value)
                {
                    Input.Value = value;
                    NextDisabled = !IsValid();
                }
            }
            get
            {
                return Input!.Value;
            }
        }

        #endregion Properties

        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(Input!.Value))
            {
                if (WorkflowStep!.IsRequired)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (Input!.Value.Length < MinLength || Input!.Value.Length > MaxLength)
                {
                    ErrorMessage = $"Input length must be between {MinLength} and {MaxLength} characters in length - you entered {Input!.Value.Length} characters";
                    return false;
                }
                if (!string.IsNullOrEmpty(RegEx))
                {
                    try
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(Input!.Value, RegEx))
                        {
                            ErrorMessage = "Input does not match the required format.";
                            return false;
                        }
                    }
                    catch (System.ArgumentException)
                    {
                        ErrorMessage = "Invalid regular expression pattern.";
                        return false;
                    }
                }
            }

            return base.IsValid();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            MinLength = StepDetails!.MinLength!.Value;

            MaxLength = StepDetails.MaxLength!.Value;

            RegEx = StepDetails.RegEx;

            if (WorkflowStep!.IsRequired)
            {
                PlaceHolder = "Further details required...";
            }
            else
            {
                PlaceHolder = "Optional details...";
            }

            NextDisabled = !IsValid();

            if (!Initialized)
            {
                Initialized = true;
            }
            
        }
    }
}
