using ICS.Portal.Data.Queries.Models;

namespace ICS.Portal.Data.Custom;
public class WorkflowEquipmentBuilder
{
    public enum SourceContexts
    {
        Customer,
        CustomerLocation,
        EquipmentType,
        Task
    }

    public static void UpdateEquipment(DispatchDetailResult.Equipment source, DispatchDetailResult.Equipment destination)
    {
        destination.CustomerLocationId = source.CustomerLocationId;
        destination.EquipmentTypeId = source.EquipmentTypeId;
        destination.TypeName = source.TypeName;
        destination.EqType = source.EqType;
        destination.UnitTag = source.UnitTag;
        destination.EquipLocArea = source.EquipLocArea;
        destination.Make = source.Make;
        destination.Model = source.Model;
        destination.Serial = source.Serial;
        destination.IsRetired = source.IsRetired;
        destination.EquipmentGroup = source.EquipmentGroup;
        destination.MfgDate = source.MfgDate;
        destination.InstallationDate = source.InstallationDate;
        destination.WarrantyEndDate = source.WarrantyEndDate;
        destination.ExternalKey = source.ExternalKey;
        destination.Notes = source.Notes;
        destination.Tags = source.Tags;
        destination.DataPlateFileName = source.DataPlateFileName;
        destination.UnitImage1FileName = source.UnitImage1FileName;
        destination.UnitImage2FileName = source.UnitImage2FileName;
        destination.ModifiedDate = source.ModifiedDate;
        destination.ModifiedBy = source.ModifiedBy;
        destination.IsDirty = source.IsDirty;
        destination.Hash = source.Hash;
    }

    public static void UpdateEquipmentAttributes(WorkflowEquipment source, WorkflowEquipment destination)
    {
        source.Attributes ??= new()!;
        destination.Attributes ??= new()!;

        foreach (var sourceAttribute in source.Attributes)
        {
            EquipmentAttribute? destinationAttribute = null;

            if (sourceAttribute.EquipmentUid.HasValue)
            {
                destinationAttribute = destination.Attributes.FirstOrDefault(a => a.EquipmentUid == sourceAttribute.EquipmentUid && a.Id == sourceAttribute.Id);
            }
            else
            {
                destinationAttribute = destination.Attributes.FirstOrDefault(a => a.EquipmentId == sourceAttribute.EquipmentId && a.Id == sourceAttribute.Id);
            }

            if (destinationAttribute is null)
            {
                destination.Attributes.Add(sourceAttribute);
            }
            else
            {
                if (sourceAttribute.LastModifiedDate > destinationAttribute.LastModifiedDate)
                {
                    UpdateEquipmentAttributeValue(sourceAttribute, destinationAttribute);
                }
            }
        }

        foreach (var sourceConsumable in source.Consumables)
        {
            EquipmentAttribute? destinationConsumable = null;

            if (sourceConsumable.EquipmentUid.HasValue)
            {
                destinationConsumable = destination.Consumables.FirstOrDefault(a => a.EquipmentUid == sourceConsumable.EquipmentUid && a.Id == sourceConsumable.Id);
            }
            else
            {
                destinationConsumable = destination.Consumables.FirstOrDefault(a => a.EquipmentId == sourceConsumable.EquipmentId && a.Id == sourceConsumable.Id);
            }

            if (destinationConsumable is null)
            {
                destination.Consumables.Add(sourceConsumable);
            }
            else
            {
                if (sourceConsumable.LastModifiedDate > destinationConsumable.LastModifiedDate)
                {
                    UpdateEquipmentAttributeValue(sourceConsumable, destinationConsumable);
                }
            }
        }

    }
    public static void UpdateEquipmentAttributeValue(DispatchDetailResult.EquipmentAttributeValue source, DispatchDetailResult.EquipmentAttributeValue destination)
    {
        destination.Quantity = source.Quantity;
        destination.Value = source.Value;
        destination.LastModifiedBy = source.LastModifiedBy;
        destination.LastModifiedDate = source.LastModifiedDate;
    }

    public static void UpdateEquipmentAttributeValue(EquipmentAttribute source, EquipmentAttribute destination)
    {
        destination.Quantity = source.Quantity;
        destination.Value = source.Value;
        destination.LastModifiedBy = 0;
        destination.LastModifiedDate = DateTime.UtcNow;
    }

    public static WorkflowEquipment? EquipmentFromList(List<WorkflowEquipment>? euipmentList, int equipmentId, Guid? temporaryId, string? value)
    {
        WorkflowEquipment? result = null;

        if (euipmentList is not null)
        {
            if (equipmentId != 0)
            {
                result = euipmentList.FirstOrDefault(e => e.Id == equipmentId);
            }
            else if (temporaryId.HasValue)
            {
                result = euipmentList.FirstOrDefault(e => e.TemporaryId == temporaryId);
            }
            else if (!string.IsNullOrEmpty(value))
            {
                if (int.TryParse(value, out int intResult))
                {
                    result = euipmentList!.FirstOrDefault(e => e.Id == intResult);
                }
                else if (Guid.TryParse(value, out Guid guidResult))
                {
                    result = euipmentList!.FirstOrDefault(e => e.TemporaryId == guidResult);
                }
            }
        }

        return result;
    }

    public static DispatchDetailResult.Equipment? EquipmentFromDispatch(DispatchDetailResult dispatch, int equipmentId, Guid? temporaryId, string? value)
    {
        DispatchDetailResult.Equipment? result = null;

        if (dispatch is not null)
        {
            if (equipmentId != 0)
            {
                result = dispatch.EquipmentResult!.FirstOrDefault(e => e.Id == equipmentId);
            }
            else if (temporaryId.HasValue)
            {
                result = dispatch.EquipmentResult!.FirstOrDefault(e => e.TemporaryId == temporaryId);
            }
            else if (!string.IsNullOrEmpty(value))
            {
                if (int.TryParse(value, out int intResult))
                {
                    result = dispatch.EquipmentResult!.FirstOrDefault(e => e.Id == intResult);
                }
                else if (Guid.TryParse(value, out Guid guidResult))
                {
                    result = dispatch.EquipmentResult!.FirstOrDefault(e => e.TemporaryId == guidResult);
                }
            }
        }

        return result;
    }

    public static void RemoveFromDispatch(DispatchDetailResult dispatch, int equipmentId, Guid? temporaryId, string? value)
    {
        var equipment = EquipmentFromDispatch(dispatch, equipmentId, temporaryId, value);

        if (equipment is not null)
        {
            dispatch.EquipmentResult?.Remove(equipment);
        }
        if (equipmentId != 0)
        {
            dispatch.EquipmentAttributeValueResult?.RemoveAll(a => a.EquipmentId == equipmentId);
        }
        else if (temporaryId.HasValue)
        {
            dispatch.EquipmentAttributeValueResult?.RemoveAll(a => a.EquipmentUid == temporaryId);
        }
        else if (!string.IsNullOrEmpty(value))
        {
            if (int.TryParse(value, out int intResult))
            {
                dispatch.EquipmentAttributeValueResult?.RemoveAll(e => e.EquipmentId == intResult);
            }
            else if (Guid.TryParse(value, out Guid guidResult))
            {
                dispatch.EquipmentAttributeValueResult?.RemoveAll(e => e.EquipmentUid == guidResult);
            }
        }
    }

    public static List<string> GetEquipmentGroupList(List<WorkflowEquipment>? workflowEquipmentList)
    {
        List<string> result = new List<string>();

        if (workflowEquipmentList is not null)
        {
            for (int idx = 0; idx != workflowEquipmentList.Count; idx++)
            {
                if (string.IsNullOrEmpty(workflowEquipmentList[idx].EquipmentGroup))
                {
                    continue;
                }
                if (result.Contains(workflowEquipmentList[idx].EquipmentGroup))
                {
                    continue;
                }
                result.Add(workflowEquipmentList[idx].EquipmentGroup!);
            }
        }

        return result;
    }

    public static List<WorkflowEquipment> GetPayloadEquipment(List<WorkflowEquipment> equipmentList)
    {
        List<WorkflowEquipment> result = new List<WorkflowEquipment>();

        foreach (var equipment in equipmentList)
        {
            if (equipment.IsDirty == true)
            {
                result.Add(equipment.GetPayloadVersion());
            }
        }

        return result;
    }

    public static void MergePayloadEquipment(List<WorkflowEquipment> dispatchEquipmentList, List<WorkflowEquipment> payloadEquipmentList)
    {
        foreach (var payloadEquipment in payloadEquipmentList)
        {
            WorkflowEquipment? dispatchEquipment = null;

            if (payloadEquipment.Id != 0)
            {
                dispatchEquipment = dispatchEquipmentList.FirstOrDefault(e => e.Id == payloadEquipment.Id);
            }
            else
            {
                dispatchEquipment = dispatchEquipmentList.FirstOrDefault(e => e.TemporaryId == payloadEquipment.TemporaryId);
            }

            if (dispatchEquipment is not null)
            {
                // If this equipment is in the payload it was modified during this workflow
                // We keep the dispatch version but mark it as dirty so the user knows it was modified during this workflow
                if (dispatchEquipment.ModifiedDate == payloadEquipment.ModifiedDate)
                {
                    dispatchEquipment.IsDirty = true;
                }
            }
            else
            {
                dispatchEquipmentList.Insert(0, payloadEquipment);
            }
        }
    }

    public static WorkflowEquipment CreateNewEquipment(
        int userId,
        DispatchDetailResult dispatch,
        LookupsResult lookups
        )
    {

        var customerAttributeList = dispatch.CustomerAttributeResult;
        var customerLocationAttributeList = dispatch.CustomerLocationAttributeResult;
        var workOrderTaskCodeAttributeList = dispatch.WorkOrderTaskCodeAttributeResult;
        var attributeList = lookups.AttributeResult ?? new();
        int customerLocationId = dispatch?.CustomerLocationResult?.Id ?? 0;

        var composedEquipment = WorkflowEquipment.Create(customerLocationId, userId);

        if (customerAttributeList is not null)
        {
            foreach (var customerAttribute in customerAttributeList)
            {
                var attribute = attributeList.FirstOrDefault(a => a.Id == customerAttribute.AttributeId);

                if (attribute is null || attribute.Context != 4)
                {
                    continue;
                }
                composedEquipment.AddAttribute(attribute, null, customerAttribute, null, null, null);
            }
        }

        if (customerLocationAttributeList is not null)
        {
            foreach (var customerLocationAttribute in customerLocationAttributeList)
            {
                var attribute = attributeList.FirstOrDefault(a => a.Id == customerLocationAttribute.AttributeId);

                if (attribute is null || attribute.Context != 4)
                {
                    continue;
                }
                composedEquipment.AddAttribute(attribute, null, null, customerLocationAttribute, null, null);
            }
        }

        if (workOrderTaskCodeAttributeList is not null)
        {
            foreach (var workOrderTaskCodeAttribute in workOrderTaskCodeAttributeList)
            {
                var attribute = attributeList.FirstOrDefault(a => a.Id == workOrderTaskCodeAttribute.AttributeId);

                if (attribute is null || attribute.Context != 4)
                {
                    continue;
                }
                composedEquipment.AddAttribute(attribute, null, null, null, workOrderTaskCodeAttribute, null);
            }
        }

        return composedEquipment;
    }

    public static List<WorkflowEquipment> GenerateEquipmentList(DispatchDetailResult dispatch, LookupsResult lookups)
    {
        try
        {
            var customerAttributeList = dispatch.CustomerAttributeResult;
            var customerLocationAttributeList = dispatch.CustomerLocationAttributeResult;
            var workOrderTaskCodeAttributeList = dispatch.WorkOrderTaskCodeAttributeResult;
            var equipmentAttributeValueList = dispatch.EquipmentAttributeValueResult;
            var equipmentList = dispatch.EquipmentResult;
            var equipmentTypeAttributeList = lookups.EquipmentTypeAttributeResult;
            var attributeList = lookups.AttributeResult;

            List<WorkflowEquipment> result = new List<WorkflowEquipment>();

            if (equipmentList is not null)
            {
                foreach (var equipment in equipmentList)
                {
                    var composedEquipment = WorkflowEquipment.Create(equipment);

                    if (customerAttributeList is not null)
                    {
                        foreach (var customerAttribute in customerAttributeList)
                        {
                            var attribute = attributeList?.FirstOrDefault(a => a.Id == customerAttribute.AttributeId);

                            if (attribute is null || attribute.Context != 4)
                            {
                                continue;
                            }

                            var existingEquipmentAttributeValue = equipmentAttributeValueList?.FirstOrDefault(eav => eav.EquipmentUid == equipment.TemporaryId && eav.EquipmentId == equipment.Id && eav.AttributeId == customerAttribute.AttributeId);
                            composedEquipment.AddAttribute(attribute, null, customerAttribute, null, null, existingEquipmentAttributeValue);
                        }
                    }

                    if (customerLocationAttributeList is not null)
                    {
                        foreach (var customerLocationAttribute in customerLocationAttributeList)
                        {
                            var attribute = attributeList?.FirstOrDefault(a => a.Id == customerLocationAttribute.AttributeId);

                            if (attribute is null || attribute.Context != 4)
                            {
                                continue;
                            }

                            var existingEquipmentAttributeValue = equipmentAttributeValueList?.FirstOrDefault(eav => eav.EquipmentUid == equipment.TemporaryId && eav.EquipmentId == equipment.Id && eav.AttributeId == customerLocationAttribute.AttributeId);
                            composedEquipment.AddAttribute(attribute, null, null, customerLocationAttribute, null, existingEquipmentAttributeValue);
                        }
                    }

                    if (workOrderTaskCodeAttributeList is not null)
                    {
                        foreach (var workOrderTaskCodeAttribute in workOrderTaskCodeAttributeList)
                        {
                            var attribute = attributeList?.FirstOrDefault(a => a.Id == workOrderTaskCodeAttribute.AttributeId);
                            if (attribute is null || attribute.Context != 4)
                            {
                                continue;
                            }

                            var existingEquipmentAttributeValue = equipmentAttributeValueList?.FirstOrDefault(eav => eav.EquipmentUid == equipment.TemporaryId && eav.EquipmentId == equipment.Id && eav.AttributeId == workOrderTaskCodeAttribute.AttributeId);
                            composedEquipment.AddAttribute(attribute, null, null, null, workOrderTaskCodeAttribute, null);
                        }
                    }

                    if (equipmentTypeAttributeList is not null)
                    {
                        foreach (var equipmentTypeAttribute in equipmentTypeAttributeList)
                        {
                            if (equipmentTypeAttribute.EquipmentTypeId == equipment.EquipmentTypeId)
                            {
                                var attribute = attributeList?.FirstOrDefault(a => a.Id == equipmentTypeAttribute.AttributeId);
                                if (attribute is null || attribute.Context != 4)
                                {
                                    continue;
                                }
                                if(attribute.IsConsumable)
                                {
                                    composedEquipment.AddAttribute(attribute, equipmentTypeAttribute, null, null, null, null);

                                    var existingEquipmentAttributeValues = equipmentAttributeValueList?.FindAll(eav => eav.EquipmentUid == equipment.TemporaryId && eav.EquipmentId == equipment.Id && eav.AttributeId == equipmentTypeAttribute.AttributeId);
                                    
                                    if(existingEquipmentAttributeValues is not null)
                                    {
                                        foreach(var existingEquipmentAttributeValue in existingEquipmentAttributeValues)
                                        {
                                            composedEquipment.AddAttribute(attribute, equipmentTypeAttribute,null,null,null, existingEquipmentAttributeValue);
                                        }
                                    }
                                }
                                else
                                {
                                    var existingEquipmentAttributeValue = equipmentAttributeValueList?.FirstOrDefault(eav => eav.EquipmentUid == equipment.TemporaryId && eav.EquipmentId == equipment.Id && eav.AttributeId == equipmentTypeAttribute.AttributeId);
                                    composedEquipment.AddAttribute(attribute, equipmentTypeAttribute, null, null, null, existingEquipmentAttributeValue);
                                }
                            }
                        }
                    }

                    composedEquipment.SortAttributesAndConsumables();

                    result.Add(composedEquipment);
                }

            }

            return result;
        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }
        return null;
    }
}