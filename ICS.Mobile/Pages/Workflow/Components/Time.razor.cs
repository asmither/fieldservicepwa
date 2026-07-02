namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Time
    {
        #region Callbacks

        protected override void NoteChanged()
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

        public System.TimeOnly? ValueAsTimeOnly
        {
            set
            {
                if (value is null)
                {
                    Input.Value = null;
                }
                else
                {
                    Input.Value = value.Value.ToString("HH:mm");
                }
                NextDisabled = !IsValid();
            }
            get
            {
                if (Input.Value is null)
                {
                    return null;
                }
                else
                {
                    if(System.TimeOnly.TryParse(Input.Value, out System.TimeOnly result))
                    {
                        return result;
                    }
                    Input.Value = null;
                    return null;
                }
            }
        }
        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(Input!.Value))
            {
                if (WorkflowStep!.IsRequired)
                {
                    return false;
                }
                return true;
            }

            return base.IsValid();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            base.OnInitialized();

            if (string.IsNullOrEmpty(Input!.Value))
            {
                if (!string.IsNullOrEmpty(WorkflowStep!.DefaultValue))
                {
                    Input.Value = WorkflowStep.DefaultValue;
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}
