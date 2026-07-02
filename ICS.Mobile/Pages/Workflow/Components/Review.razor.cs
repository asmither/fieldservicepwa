using ICS.Mobile.Components;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class Review
    {
        [Parameter]
        [EditorRequired]
        public WorkflowResultDetailResult.Workflow? Workflow { set; get; }

        [Parameter]
        [EditorRequired]
        public List<WorkflowReviewItem>? WorkflowReviewItemList { set; get; }

        [Parameter]
        public EventCallback OnComplete { set; get; }

        [Parameter]
        public EventCallback OnEdit { set; get; }

        [Parameter]
        public bool ShowDownload { set; get; } = true;

        [Inject]
        IWorkflowData dataService { get; set; } = default!;

        ICSDialogBox? DialogBox;

        private string? ErrorMessage = null;

        private bool Initialized = false;

        private string lastEquipmentName = string.Empty;

        public async Task Complete()
        {
            if (await Confirm("Are you sure you want to complete this checklist?", "Checklist Review"))
            {
                if (OnComplete.HasDelegate)
                    await OnComplete.InvokeAsync();
            }
        }
        private async Task<bool> Confirm(string message, string heading = "Please Confirm", string okbtn = "Yes", string cancelbtn = "No")
        {
            if (string.IsNullOrEmpty(heading))
            {
                heading = "FYI";
            }

            int ret = await DialogBox!.WaitForDialogResultAsync(header: heading, prompt: message, okLabel: okbtn, cancelLabel: cancelbtn, defaultButton: 2);

            return ret != 2;
        }

        public string CurrentEquipmentName(WorkflowEquipment? eq, int loopIndex = 0)
        {
            if (eq is not null)
            {
                string name = eq.UnitTag ?? string.Empty;

                if (string.IsNullOrEmpty(name))
                {
                    name = eq.Make ?? string.Empty;
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!string.IsNullOrEmpty(eq.Model))
                        {
                            name += $" {eq.Model.Left(10)}";
                            if (!string.IsNullOrEmpty(eq.Serial))
                            {
                                name += $" {eq.Serial.Right(4)}";
                            }
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(eq.TypeName))
                    {
                        name += $" [{eq.TypeName}]";
                    }
                    if (!string.IsNullOrEmpty(eq.EquipmentGroup))
                    {
                        name += $" in {eq.EquipmentGroup}";
                    }
                }

                if (string.IsNullOrEmpty(name))
                {
                    name = $"Equip #{loopIndex + 1}";
                }

                lastEquipmentName = name;
            }
            else
            {
                if (string.IsNullOrEmpty(lastEquipmentName))
                {
                    lastEquipmentName = string.Empty;
                }
            }
            return lastEquipmentName;
        }

        public async Task Edit()
        {
            await OnEdit.InvokeAsync();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Initialized = true;
        }

        public bool AllowEdit
        {
            get
            {
                if (dataService.AppState is not null && dataService.AppState.AuthorizedUser is not null)
                {
                    return dataService.AppState.AuthorizedUser.EntityId == Workflow!.EmployeeId;
                }
                return false;
            }
        }


        public bool RenderParrPromptHasMultiplevalues(string? nodelist, string SplitByChars = $"\r\n")
        {
            if (string.IsNullOrEmpty(nodelist) || WorkflowReviewItemList is null)
            {
                return false;
            }
            var results = nodelist.Split(SplitByChars);
            return results.Length > 1;
        }
        public string RenderParrPromptValues(string? nodelist, string SplitByChars = $"\r\n")
        {
            if (string.IsNullOrEmpty(nodelist) || WorkflowReviewItemList is null)
            {
                return nodelist ?? string.Empty;
            }
            var results = nodelist.Split(SplitByChars);

            //foreach (var r in results)
            //{
            //    //Console.WriteLine(r);

            //}

            return $"{results.Length}";
        }
    }
}
