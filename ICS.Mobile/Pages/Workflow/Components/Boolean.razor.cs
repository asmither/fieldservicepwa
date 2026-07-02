namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Boolean : WorkflowComponentsBasePage
    {
        #region Fields and Properties

        private ImageMedia? imageRef;
        private string? previousKey = null;
        private string TruePrompt = "True";
        private string FalsePrompt = "False";

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
        
        public bool? ValueAsBool
        {
            set
            {
                SetValue(value.ToString());
            }
            get
            {
                if (!string.IsNullOrEmpty(Input!.Value))
                {
                    return bool.Parse(Input.Value);
                }
                return null;
            }
        }

        private void SetValue(string? value)
        {
            Input!.Value = value;

            if (string.IsNullOrEmpty(Input.Value))
            {
                WorkflowStep!.NoteRequirementId = 0;
                WorkflowStep.NoteLabel = null;
                WorkflowStep.ImageRequirementId = 0;
                WorkflowStep.ImageLabel = null;
                WorkflowStep.JumpConditionTypeId = null;
                WorkflowStep.JumpConditionValue = null;
                WorkflowStep.JumpStepId = null;
                Input.Note = null;
                Input.ImageFileName = null;
            }
            else
            {
                if (bool.Parse(Input.Value))
                {
                    WorkflowStep!.NoteRequirementId = StepDetails!.TrueNoteRequirementId;
                    WorkflowStep.NoteLabel = StepDetails.TrueNoteLabel;
                    WorkflowStep.ImageRequirementId = StepDetails.TrueImageRequirementId;
                    WorkflowStep.ImageLabel = StepDetails.TrueImageLabel;
                    WorkflowStep.JumpConditionTypeId = StepDetails.TrueJumpConditionTypeId;
                    WorkflowStep.JumpConditionValue = StepDetails.TrueJumpConditionValue;
                    WorkflowStep.JumpStepId = StepDetails.TrueJumpStepId;
                }
                else
                {
                    WorkflowStep!.NoteRequirementId = StepDetails!.FalseNoteRequirementId;
                    WorkflowStep.NoteLabel = StepDetails.FalseNoteLabel;
                    WorkflowStep.ImageRequirementId = StepDetails.FalseImageRequirementId;
                    WorkflowStep.ImageLabel = StepDetails.FalseImageLabel;
                    WorkflowStep.JumpConditionTypeId = StepDetails.FalseJumpConditionTypeId;
                    WorkflowStep.JumpConditionValue = StepDetails.FalseJumpConditionValue;
                    WorkflowStep.JumpStepId = StepDetails.FalseJumpStepId;
                }
            }

            if (previousKey is not null)
            {
                if (previousKey != CurrentKey())
                {
                    Input.Note = null;
                    Input.ImageFileName = null;
                }
            }

            previousKey = CurrentKey();

            NextDisabled = !IsValid();
        }

        protected override bool IsValid()
        {
            ErrorMessage = null;

            if (WorkflowStep!.IsRequired && ValueAsBool == null)
            {
                return false;
            }

            return base.IsValid();

        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            previousKey = null;
            TruePrompt = StepDetails.TruePrompt;
            FalsePrompt = StepDetails.FalsePrompt;

            SetValue(Input.Value);

            if (!Initialized)
            {
                Initialized = true;
            }
        }

        private string CurrentKey()
        {
            return $"{Input.WorkflowStepResultId}-{Input.LoopIndex}-{Input.Value}";
        }

    }
}
