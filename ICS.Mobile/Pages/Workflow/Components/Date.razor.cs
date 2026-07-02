using System;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Date
    {
        #region Fields

        private System.DateOnly MinDate;
        private System.DateOnly MaxDate;

        #endregion Fields

        #region Callbacks

        private bool NoteChanging = false;
        protected override void NoteChanged()
        {
            NextDisabled = !IsValid();
        }
        protected override void ImageChanged()
        {
            NextDisabled = !IsValid();
        }

        #endregion Callbacks

        public System.DateOnly? ValueAsDateOnly
        {
            set
            {
                if (value is null)
                {
                    Input!.Value = null;
                }
                else
                {
                    Input!.Value = value.ToString();
                }
                NextDisabled = !IsValid();
            }
            get
            {
                if (Input!.Value is null)
                {
                    return null;
                }
                else
                {
                    return System.DateOnly.Parse(Input.Value);
                }
            }
        }

        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (WorkflowStep!.IsRequired && ValueAsDateOnly == null)
            {
                return false;
            }
            if (ValueAsDateOnly.HasValue)
            {
                if (ValueAsDateOnly.Value < MinDate || ValueAsDateOnly.Value > MaxDate)
                {
                    ErrorMessage = $"Value must be between {MinDate} and {MaxDate}";
                    return false;
                }
            }

            return base.IsValid();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (StepDetails is not null)
            {
                if (StepDetails.MinDate.HasValue)
                {
                    MinDate = StepDetails.MinDate.Value;
                }
                if (StepDetails.MaxDate is not null)
                {
                    MaxDate = StepDetails.MaxDate.Value;
                }
            }

            bool nextDisable = !IsValid();
            if(NextDisabled != nextDisable)
            {
                NextDisabled = nextDisable;
            }
            
            if(!Initialized)
            {
                Initialized = true;
            }
        }
    }
}
