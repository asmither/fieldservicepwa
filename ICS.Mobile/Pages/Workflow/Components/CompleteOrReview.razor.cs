using ICS.Mobile.Helpers;
using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class CompleteOrReview
    {

        private Mobile.Components.ICSDialogBox DialogBox;

        private string WorkflowPromptComplete
        {
            get
            {
                return $"{Workflow!.Prompt} Complete";
            }
        }

        //protected JSUI jsui { set; get; } = default!;

        public async Task Complete()
        {
            if (OnComplete.HasDelegate)
            {
                if (1 == await DialogBox.WaitForDialogResultAsync(
                    header: "Finish Checklist", prompt: "Are you ready to mark this checklist as complete?", okLabel: "COMPLETE", cancelLabel: "Not Yet", defaultButton: 1))
                    await OnComplete.InvokeAsync();
            }
        }

        public async Task Review()
        {
            if (OnReview.HasDelegate)
                await OnReview.InvokeAsync();
        }

        public async Task Edit()
        {
            if (OnEdit.HasDelegate)
                await OnEdit.InvokeAsync();
        }
        protected override void OnInitialized()
        {
            Initialized = true;
        }

        [Parameter]
        public EventCallback OnComplete { set; get; }

        [Parameter]
        public EventCallback OnReview { set; get; }

        [Parameter]
        public EventCallback OnEdit { set; get; }
    }
}
