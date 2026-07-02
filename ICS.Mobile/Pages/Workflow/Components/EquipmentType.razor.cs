using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components;

using System.Text.Json;

namespace ICS.Mobile.Pages.Workflow.Components
{
    public partial class EquipmentType
    {
        #region Callbacks

        protected override void NoteChanged()
        {
            NextDisabled = !IsValid();
        }

        #endregion

        [Parameter]
        public WorkflowRunnerLogic? Runner { set; get; }

        public List<LookupsResult.EquipmentType>? EquipmentTypes { set; get; }

        private string? Value
        {
            get
            {
                return Input!.Value;
            }
        }

        private DispatchDetailResult.Equipment? SelectedEquipment = null;

        public async Task SetValue(LookupsResult.EquipmentType equipmentType)    
        {
            if (equipmentType.Id == 0)
            {
                WorkflowStep!.NoteRequirementId = 2;
                WorkflowStep.NoteLabel = "Describe the equipment type";
            }
            else
            {
                WorkflowStep!.NoteRequirementId = 0;
                WorkflowStep.NoteLabel = null;
            }

            Input!.Value = equipmentType.TypeName;
            if (SelectedEquipment is not null)
            {
                SelectedEquipment.TypeName = equipmentType.TypeName;
                SelectedEquipment.EquipmentTypeId = equipmentType.Id;
                SelectedEquipment.EqType = equipmentType.ESCEqType ?? equipmentType.TypeName;

                await UpdateWorkflowEquipment(equipmentType);
            }

            NextDisabled = !IsValid();
        }

        protected override bool IsValid()
        {
            if (SelectedEquipment is not null && string.IsNullOrEmpty(Input.Value))
            {
                return false;
            }
            return base.IsValid();
        }

        private WorkflowEquipment? GetWorkflowEquipment(int? id, Guid? uid, DispatchDetailResult dispatch, LookupsResult lookups)
        {
            List<WorkflowEquipment> list = WorkflowEquipmentBuilder.GenerateEquipmentList(dispatch, lookups);
            if (id.HasValue)
            {
                return list.FirstOrDefault(e => e.Id == id);
            }
            else if (uid.HasValue)
            {
                return list.FirstOrDefault(e => e.TemporaryId == uid);
            }

            return null;
        }

        private async Task UpdateWorkflowEquipment(LookupsResult.EquipmentType equipmentType)
        {
            if (SelectedEquipment is not null && 
                Runner is not null && 
                Runner.Dispatch is not null && 
                Runner.Dispatch.EquipmentResult is not null && 
                Runner.Lookups is not null && 
                Runner.Lookups.EquipmentTypeResult is not null && Input != null && 
                Runner.Lookups.AttributeResult is not null &&
                Runner.Lookups.EquipmentTypeAttributeResult is not null
                )
            {
                var workflowEquipment = GetWorkflowEquipment(SelectedEquipment.Id, SelectedEquipment.TemporaryId, Runner.Dispatch!, Runner.Lookups);

                if (workflowEquipment is not null)
                {
                    workflowEquipment.TypeName = equipmentType.TypeName;
                    workflowEquipment.EquipmentTypeId = equipmentType.Id;
                    workflowEquipment.EqType = equipmentType.ESCEqType ?? equipmentType.TypeName;
                    workflowEquipment.SetEquipmentTypeAttributes(Runner.Lookups.AttributeResult, Runner.Lookups.EquipmentTypeAttributeResult, Runner.Dispatch);
                    workflowEquipment.IsDirty = true;

                    var payloadData = WorkflowEquipmentBuilder.GetPayloadEquipment(new List<WorkflowEquipment>() { workflowEquipment });
                    var payloadItem = payloadData.FirstOrDefault();
                    if(payloadItem is not null)
                    {
                        Input.JsonPayload = JsonSerializer.Serialize<WorkflowEquipment>(payloadItem);
                        Runner.UpdateEquipment(workflowEquipment);
                        await Runner.SaveDispatchAsync();
                    }
                }
            }
        }
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Runner != null && Runner.Dispatch != null && Runner.Dispatch.EquipmentResult != null && Runner.Lookups != null && Input != null && Runner.Lookups.EquipmentTypeResult is not null)
            {
                var priorStepEquipment = Runner.GetPriorEquipmentStepValue(Input.SourceWorkflowStepResultId, Input.SourceLoopPath);

                if (priorStepEquipment is not null)
                {
                    
                    if (int.TryParse(priorStepEquipment.Value, out int id) && id !=0)
                    {
                        SelectedEquipment = Runner.Dispatch.GetEquipmentById(id, null);
                    }
                    else if (Guid.TryParse(priorStepEquipment.Value, out Guid uid))
                    {
                        SelectedEquipment = Runner.Dispatch?.EquipmentResult?.FirstOrDefault(e => e.TemporaryId == uid) ?? null;
                        
                    }
                    if (SelectedEquipment is not null)
                    {
                        EquipmentTypes = Runner.Lookups.EquipmentTypeResult;
                        LookupsResult.EquipmentType? currentType;
                        if (!string.IsNullOrEmpty(Input.Value))
                        {
                            currentType = EquipmentTypes.FirstOrDefault(t => t.TypeName == Input.Value);
                        }
                        else
                        {
                            currentType = EquipmentTypes.FirstOrDefault(t => t.TypeName == SelectedEquipment.TypeName);
                        }

                        if (currentType is not null)
                        {
                            Input.Value = currentType.TypeName;
                            EquipmentTypes.Remove(currentType);
                            EquipmentTypes.Insert(0, currentType);
                        }
                    }
                }
            }

            NextDisabled = !IsValid();

            Initialized = true;
        }
    }
}

