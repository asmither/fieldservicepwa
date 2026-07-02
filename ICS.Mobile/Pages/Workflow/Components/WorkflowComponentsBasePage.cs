using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components;
public class WorkflowComponentsBasePage : ComponentBase
{
    #region Parameters

    [Parameter]
    [EditorRequired]
    public WorkflowResultDetailResult.Workflow? Workflow { set; get; }

    [Parameter]
    [EditorRequired]
    public WorkflowResultDetailResult.WorkflowStep? WorkflowStep { set; get; }

    [Parameter]
    [EditorRequired]
    public WorkflowStepResultValueSaveInput? Input { set; get; }

    [Parameter]
    [EditorRequired]
    public EventCallback OnMoveNext { set; get; }

    [Parameter]
    [EditorRequired]
    public EventCallback OnMovePrevious { set; get; }

    [Parameter]
    [EditorRequired]
    public WorkflowDataTypeDetail? StepDetails { set; get; }

    [Inject]
    protected IWorkflowData DataService { set; get; } = default!;

    public bool ChildComponentIsBusy { set; get; }

    public void OnImageWorkingStateChanged(bool isBusy)
    {
        ChildComponentIsBusy = isBusy;
    }

    #endregion Parameters 

    #region Protected Fields

    protected bool Initialized = false;

    protected string? ErrorMessage = null;
    protected bool PreviousDisabled { set; get; }
    protected bool NextDisabled { set; get; }

    #endregion Protected Fields

    protected async Task MoveNext()
    {
        await OnMoveNext.InvokeAsync();
    }

    protected async Task MovePrevious()
    {
        await OnMovePrevious.InvokeAsync();
    }

    protected virtual bool IsValid()
    {
        if (WorkflowStep!.IsRequired)
        {
            if (WorkflowStep!.NoteRequirementId == 2)
            {
                if (string.IsNullOrEmpty(Input!.Note))
                {
                    return false;
                }
            }

            if (WorkflowStep!.ImageRequirementId == 2)
            {
                if (string.IsNullOrEmpty(Input!.ImageFileName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected virtual void NoteChanged() { }

    protected virtual void ImageChanged() { }

    protected override void OnParametersSet()
    {
        if (Workflow is null || WorkflowStep is null || Input is null)
        {
            return;
        }

        PreviousDisabled = Input.SourceWorkflowStepResultId is null;
    }
}