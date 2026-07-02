using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components;
public partial class Choice
{
    #region Fields and Properties

    [Parameter]
    [EditorRequired]
    public List<WorkflowResultDetailResult.WorkflowStepOption>? StepOptions { set; get; }

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

    private WorkflowResultDetailResult.WorkflowStepOption? previousOption = null;

    private string? Value
    {
        get
        {
            return Input.Value;
        }
        set
        {
            SetValue(value);
        }
    }

    private string? PreviousValue;

    private void SetValue(string? value)
    {
        Input.Value = value;

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
            var option = StepOptions!.FirstOrDefault(o => o.Value == Input.Value)!;

            if (option is not null)
            {
                WorkflowStep!.NoteRequirementId = option.NoteRequirementId;
                WorkflowStep.NoteLabel = option.NoteLabel;
                WorkflowStep.ImageRequirementId = option.ImageRequirementId;
                WorkflowStep.ImageLabel = option.ImageLabel;
                WorkflowStep.JumpConditionTypeId = option.JumpConditionTypeId;
                WorkflowStep.JumpConditionValue = option.JumpConditionValue;
                WorkflowStep.JumpStepId = option.JumpStepId;

                if (previousOption is not null)
                {
                    if (previousOption.WorkflowStepOptionId != option.WorkflowStepOptionId)
                    {
                        Input.Note = null;
                        Input.ImageFileName = null;
                    }
                }
            }

            previousOption = option;
        }

        NextDisabled = !IsValid();

    }

    protected override bool IsValid()
    {
        if (WorkflowStep!.IsRequired)
        {
            if (Input!.Value is null)
            {
                return false;
            }
            return base.IsValid();
        }
        else
        {
            return base.IsValid();
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Initialized)
        {
            Initialized = true;
        }

        previousOption = null;
        SetValue(Input!.Value);
    }
}