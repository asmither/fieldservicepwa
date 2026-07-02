namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Numeric
    {
        #region Fields and Properties

        private decimal MinValue = decimal.MinValue;
        private decimal MaxValue = decimal.MaxValue;
        private int MaxDecimalPlaces = 0;

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

        public decimal? ValueAsDecimal
        {
            set
            {
                if (value is null)
                {
                    Input.Value = null;
                }
                else
                {
                    Input.Value = value.ToString();
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
                    return Decimal.Parse(Input.Value);
                }
            }
        }

        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (WorkflowStep!.IsRequired && ValueAsDecimal == null)
            {
                return false;
            }

            if (ValueAsDecimal.HasValue)
            {
                if (ValueAsDecimal.Value < MinValue || ValueAsDecimal.Value > MaxValue)
                {
                    ErrorMessage = $"Value must be between {MinValue} and {MaxValue}";
                    return false;
                }
                var stringValue = ValueAsDecimal.ToString();
                if (stringValue.Contains("."))
                {
                    string[] strings = stringValue.Split(".");
                    int decimalPlaces = strings[1].Length;
                    if (decimalPlaces > MaxDecimalPlaces)
                    {
                        ErrorMessage = $"Value must have no more than {MaxDecimalPlaces} decimal places.";
                        return false;
                    }
                }
            }

            return base.IsValid();

        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (StepDetails is not null)
            {
                if (StepDetails.MinValue.HasValue)
                {
                    MinValue = StepDetails.MinValue.Value;
                }

                if (StepDetails.MaxValue.HasValue)
                {
                    MaxValue = StepDetails.MaxValue.Value;
                }
                if (StepDetails.DecimalPlaces.HasValue)
                {
                    MaxDecimalPlaces = StepDetails.DecimalPlaces.Value;
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;

        }
    }
}
