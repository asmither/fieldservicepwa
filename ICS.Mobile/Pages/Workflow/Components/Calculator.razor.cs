using ICS.Portal.Data.Custom;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Calculator
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

        [Inject]
        public IJSRuntime JS { set; get; } = default!;

        private string? Presentation;

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();

            WorkflowDataTypeDetail details = new WorkflowDataTypeDetail(WorkflowStep.WorkflowDataTypeDetail);
            string calculationItemsString = details.Values[nameof(CalculationItems)];
            CalculationItems calculationItems = new(calculationItemsString);

            var script = calculationItems.GetReplaceScript();
            string presentation = calculationItems.GetReplacedPresentation();

            try
            {


                if (!string.IsNullOrEmpty(script))
                {
                    //TODO: How to handle errors in eval
                    // Force safe code in builder
                    try
                    {
                        var calculatedResult = await JS.InvokeAsync<decimal>("eval", script);
                        Input.Value = calculatedResult.ToString();
                    }
                    catch (Exception ex)
                    {
                        Input.Value = 0.ToString();
                    }
                }

                if (presentation is not null)
                {
                    Presentation = presentation.Replace("[CALCULATION]", Input.Value.ToString());
                }
                else
                {
                    Presentation = Input.Value;
                }

            }
            catch (Exception ex)
            {
                string[] myErr = ex.Message.Split($"\n");
                Presentation = $"{myErr.FirstOrDefault() ?? ex.Message}";
            }

            Initialized = true;

            NextDisabled = !IsValid();
        }
    }
}