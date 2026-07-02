using ICS.Mobile.Helpers;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Queries.Models;
using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class NoteMedia
    {
        #region Parameters

        [Parameter, EditorRequired]
        public WorkflowResultDetailResult.Workflow? Workflow { set; get; } = null;

        [Parameter, EditorRequired]
        public WorkflowResultDetailResult.WorkflowStep? WorkflowStep { set; get; } = null;

        [Parameter, EditorRequired]
        public WorkflowStepResultValueSaveInput? Input { set; get; } = null;

        [Parameter]
        public EventCallback NoteChanged { set; get; }

        [Parameter]
        public string? NoteLabel { set; get; }

        #endregion Parameters

        //[Inject]
        //JSUI ui { set; get; } = default!;
        private Mobile.Components.ICSDialogBox DialogBox;
        private bool IgnoreChange = false;

        #region Properties

        private string PlaceHolder
        {
            get
            {
                if (WorkflowStep!.NoteRequirementId == 1)
                {
                    return "Optional details...";
                }

                if (WorkflowStep.NoteRequirementId == 2)
                {
                    return "Further details needed...";
                }

                return string.Empty;
            }
        }

        private string? Note
        {
            set
            {
                bool changed = (string.IsNullOrEmpty(Input.Note) && !string.IsNullOrEmpty(value) || (!string.IsNullOrEmpty(Input.Note) && string.IsNullOrEmpty(value)));
                Input.Note = value;
                if(changed)
                {
                    NoteChanged.InvokeAsync(value);
                }
            }
            get
            {
                return Input!.Note!;
            }
        }

        private string NoteLabelOrDefault
        {
            get
            {
                if (!string.IsNullOrEmpty(NoteLabel))
                {
                    return NoteLabel;
                }
                return "Note";
            }
        }

        private bool Initialized { set; get; } = false;

        #endregion

        private async void DeleteNote()
        {
            if (await Confirm("Are you sure you want to clear this note text?","Checklist Notes"))
            {
                Note = null;
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!IgnoreChange)
            {
                bool initialized = Initialized;
                bool shouldBeInitialized = WorkflowStep!.NoteRequirementId != 0;

                if (shouldBeInitialized)
                {
                    if (!Initialized)
                    {
                        Initialized = true;
                    }
                }
                else
                {
                    if (Initialized)
                    {
                        Initialized = false;
                    }
                }
            }

            IgnoreChange = false;
        }
  
        private async Task<bool> Confirm(string message, string heading = "Please Confirm", string okbtn = "Yes", string cancelbtn = "No")
        {
            if (string.IsNullOrEmpty(heading)) heading = "FYI";
            int ret = await DialogBox.WaitForDialogResultAsync(header: heading, prompt: message, okLabel: okbtn, cancelLabel: cancelbtn, defaultButton: 2);
            if (ret == 2)
                return false;
            else
                return true;
        }
    }
}
