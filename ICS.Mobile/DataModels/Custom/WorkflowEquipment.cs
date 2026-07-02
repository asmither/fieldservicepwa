using ICS.Portal.Data.Queries.Models;

using System.Data.Common;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ICS.Portal.Data.Custom;

public class WorkflowEquipment : DispatchDetailResult.Equipment
{
    public WorkflowEquipment GetPayloadVersion()
    {
        WorkflowEquipment result = new WorkflowEquipment(
            Id: this.Id,
            CustomerLocationId: this.CustomerLocationId,
            EquipmentTypeId: this.EquipmentTypeId,
            TypeName: this.TypeName,
            EqType: this.EqType,
            UnitTag: this.UnitTag,
            EquipLocArea: this.EquipLocArea,
            Make: this.Make,
            Model: this.Model,
            Serial: this.Serial,
            IsRetired: this.IsRetired,
            EquipmentGroup: this.EquipmentGroup,
            MfgDate: this.MfgDate,
            InstallationDate: this.InstallationDate,
            WarrantyEndDate: this.WarrantyEndDate,
            TemporaryId: this.TemporaryId,
            ExternalKey: this.ExternalKey,
            Notes: this.Notes,
            Tags: this.Tags,
            DataPlateFileName: this.DataPlateFileName,
            UnitImage1FileName: this.UnitImage1FileName,
            UnitImage2FileName: this.UnitImage2FileName,
            CreatedBy: this.CreatedBy,
            CreatedDate: this.CreatedDate,
            ModifiedDate: this.ModifiedDate,
            ModifiedBy: this.ModifiedBy,
            IsDirty: this.IsDirty,
            Hash: this.Hash,
            Attributes: null,
            Consumables: null,
            AvailableConsumables: null
        );

        if (this.Attributes is not null && this.Attributes.Count != 0)
        {
            foreach (var attribute in this.Attributes)
            {
                if (attribute.Value is not null)
                {
                    var clone = attribute.Clone();
                    clone.OptionList = null;
                    result.Attributes.Add(clone);
                }
            }
        }

        if (this.Consumables is not null && this.Consumables.Count != 0)
        {
            result.Consumables = new();

            foreach (var consumable in this.Consumables)
            {
                if (consumable.Value is not null)
                {
                    var clone = consumable.Clone();
                    clone.OptionList = null;
                    result.Consumables.Add(clone);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a COPY of all attributes and consumables that have a value.
    /// </summary>
    /// <returns></returns>
    public List<DispatchDetailResult.EquipmentAttributeValue> GetEquipmentAttributeAndConsumableValues()
    {
        List<DispatchDetailResult.EquipmentAttributeValue> result = new List<DispatchDetailResult.EquipmentAttributeValue>();
        foreach (var consumable in Consumables)
        {
            var consumableValue = consumable.GetEquipmentAttributeValue();
            if (!string.IsNullOrEmpty(consumable.Value))
            {
                result.Add(consumableValue);
            }
        }
        foreach (var attribute in Attributes)
        {
            var attributeValue = attribute.GetEquipmentAttributeValue();
            if (!string.IsNullOrEmpty(attributeValue.Value))
            {
                result.Add(attributeValue);
            }
        }

        return result;
    }
    public void SortAttributesAndConsumables()
    {
        Attributes.Sort((a, b) => a.Name.CompareTo(b.Name));
        Consumables.Sort((a, b) => a.Name.CompareTo(b.Name));
        AvailableConsumables.Sort((a, b) => a.Name.CompareTo(b.Name));
    }
    public static WorkflowEquipment Create(DispatchDetailResult.Equipment existing)
    {
        WorkflowEquipment result = new(
        Id: existing.Id,
        CustomerLocationId: existing.CustomerLocationId,
        EquipmentTypeId: existing.EquipmentTypeId,
        TypeName: existing.TypeName,
        EqType: existing.EqType,
        UnitTag: existing.UnitTag,
        EquipLocArea: existing.EquipLocArea,
        Make: existing.Make,
        Model: existing.Model,
        Serial: existing.Serial,
        IsRetired: existing.IsRetired,
        EquipmentGroup: existing.EquipmentGroup,
        MfgDate: existing.MfgDate,
        InstallationDate: existing.InstallationDate,
        WarrantyEndDate: existing.WarrantyEndDate,
        TemporaryId: existing.TemporaryId,
        ExternalKey: existing.ExternalKey,
        Notes: existing.Notes,
        Tags: existing.Tags,
        DataPlateFileName: existing.DataPlateFileName,
        UnitImage1FileName: existing.UnitImage1FileName,
        UnitImage2FileName: existing.UnitImage2FileName,
        CreatedBy: existing.CreatedBy,
        CreatedDate: existing.CreatedDate,
        ModifiedDate: existing.ModifiedDate,
        ModifiedBy: existing.ModifiedBy,
        IsDirty: existing.IsDirty,
        Hash: existing.Hash,
        null, null, null);

        return result;
    }
    public static WorkflowEquipment Create(int CustomerLocation, int UserId)
    {
        WorkflowEquipment result = new(
        Id: 0,
        CustomerLocationId: CustomerLocation,
        EquipmentTypeId: 0,
        TypeName: null,
        EqType: null,
        UnitTag: null,
        EquipLocArea: null,
        Make: null,
        Model: null,
        Serial: null,
        IsRetired: false,
        EquipmentGroup: null,
        MfgDate: null,
        InstallationDate: null,
        WarrantyEndDate: null,
        TemporaryId: Guid.NewGuid(),
        ExternalKey: null,
        Notes: null,
        Tags: null,
        DataPlateFileName: null,
        UnitImage1FileName: null,
        UnitImage2FileName: null,
        CreatedBy: UserId,
        CreatedDate: DateTime.UtcNow,
        ModifiedDate: DateTime.UtcNow,
        ModifiedBy: UserId,
        IsDirty: true,
        Hash: 0, null, null, null);

        return result;
    }
    public EquipmentAttribute CreateNewConsumable(EquipmentAttribute source)
    {
        return new EquipmentAttribute(
            id: 0,
            uid: Guid.NewGuid(),
            equipmentId: this.Id,
            equipmentUid: this.TemporaryId,
            quantity: null,
            value: null,
            notes: null,
            lastModifiedBy: 0,
            lastModifiedDate: DateTime.UtcNow,
            attributeId: source.AttributeId,
            context: source.Context,
            sourceContext: source.SourceContext,
            name: source.Name,
            description: source.Description,
            dataTypeId: source.DataTypeId,
            optionList: source.OptionList,
            isConsumable: source.IsConsumable,
            isItem: source.IsItem,
            isChecklist: source.IsChecklist,
            required: source.Required
        );
    }
    public EquipmentAttribute AddAttribute(LookupsResult.Attribute attribute, LookupsResult.EquipmentTypeAttribute? equipmentTypeAttribute, DispatchDetailResult.CustomerAttribute? customerAttribute, DispatchDetailResult.CustomerLocationAttribute? customerLocationAttribute, DispatchDetailResult.WorkOrderTaskCodeAttribute? workOrderTaskCodeAttribute, DispatchDetailResult.EquipmentAttributeValue? eav)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        if (equipmentTypeAttribute is null && customerAttribute is null && customerLocationAttribute is null && workOrderTaskCodeAttribute is null)
        {
            throw new ArgumentNullException("Must provide one of the type attributes");
        }

        int id = 0;
        Guid uid = Guid.NewGuid();
        int equipmentId = this.Id;
        Guid? equipmentUid = this.TemporaryId;
        int attributeId = attribute.Id;
        decimal? quantity = null;
        string? value = null;
        string? notes = null;
        int lastModifiedBy = 0;
        DateTime lastModifiedDate = DateTime.UtcNow;
        int rowUserId = 0;
        bool required = false;
        int displayOrder = 0;
        int sourceContext = 0;
        int context = attribute.Context;

        if (eav is not null)
        {
            id = eav.Id;
            uid = eav.Uid;
            quantity = eav.Quantity;
            value = eav.Value;
            notes = eav.Notes;
            lastModifiedDate = eav.LastModifiedDate;
            lastModifiedBy = eav.LastModifiedBy;
        }

        if (customerAttribute is not null)
        {
            sourceContext = ((int)WorkflowEquipmentBuilder.SourceContexts.Customer);
            required = customerAttribute.Required;
            displayOrder = customerAttribute.DisplayOrder;
        }

        if (customerLocationAttribute is not null)
        {
            sourceContext = ((int)WorkflowEquipmentBuilder.SourceContexts.CustomerLocation);
            required = customerLocationAttribute.Required; ;
            displayOrder = customerLocationAttribute.DisplayOrder;
        }

        if (workOrderTaskCodeAttribute is not null)
        {
            sourceContext = ((int)WorkflowEquipmentBuilder.SourceContexts.Task);
            required = workOrderTaskCodeAttribute.Required; ;
            displayOrder = workOrderTaskCodeAttribute.DisplayOrder;
        }

        if (equipmentTypeAttribute is not null)
        {
            sourceContext = ((int)WorkflowEquipmentBuilder.SourceContexts.EquipmentType);
            required = equipmentTypeAttribute.Required;
            displayOrder = equipmentTypeAttribute.DisplayOrder;
        }

        EquipmentAttribute result = new(
            id: id,
            uid: uid,
            equipmentId: equipmentId,
            equipmentUid: equipmentUid,
            quantity: quantity,
            value: value,
            notes: notes,
            lastModifiedBy: lastModifiedBy,
            lastModifiedDate: lastModifiedDate,
            attributeId: attribute.Id,
            context: attribute.Context,
            sourceContext: sourceContext,
            name: attribute.Name,
            description: attribute.Description,
            dataTypeId: attribute.DataTypeId,
            optionList: attribute.OptionList,
            isConsumable: attribute.IsConsumable,
            isItem: attribute.IsItem,
            isChecklist: attribute.IsChecklist,
            required: required
            );

        if (result.IsConsumable)
        {
            TryAddAvailableConsumable(result);
            TryAddConsumable(result);
        }
        else
        {
            TryAddAttribute(result);
        }

        return result;
    }
    private void TryAddAttribute(EquipmentAttribute equipmentAttribute)
    {
        if (!Attributes.Any(a => a.Id == equipmentAttribute.AttributeId))
        {
            Attributes.Add(equipmentAttribute);
        }
    }
    private void TryAddAvailableConsumable(EquipmentAttribute equipmentAttribute)
    {
        if (!AvailableConsumables.Any(c => c.AttributeId == equipmentAttribute.AttributeId))
        {
            AvailableConsumables.Add(equipmentAttribute);
        }
    }
    private void TryAddConsumable(EquipmentAttribute equipmentAttribute)
    {
        if (!string.IsNullOrEmpty(equipmentAttribute.Value))
        {
            if (!Consumables.Any(c => c.Uid == equipmentAttribute.Uid))
            {
                Consumables.Add(equipmentAttribute);
            }
        }
    }
    public void SetEquipmentTypeAttributes(List<LookupsResult.Attribute> attributeList, List<LookupsResult.EquipmentTypeAttribute> equipmentTypeAttributeList, DispatchDetailResult dispatch)
    {
        List<DispatchDetailResult.EquipmentAttributeValue> allAttributeValues = GetEquipmentAttributeAndConsumableValues();
        List<DispatchDetailResult.EquipmentAttributeValue> existingAttributeValuesFromDispatch = new();

        if (dispatch.EquipmentAttributeValueResult is not null)
        {
            existingAttributeValuesFromDispatch = dispatch.EquipmentAttributeValueResult.FindAll(v => (v.EquipmentId != 0 && v.EquipmentId == this.Id) || v.EquipmentUid == this.TemporaryId);

            foreach (var val in existingAttributeValuesFromDispatch)
            {
                var attributeValue = allAttributeValues.FirstOrDefault(c => c.Uid == val.Uid);
                if (attributeValue is null)
                {
                    allAttributeValues.Add(val);
                }
            }

            dispatch.EquipmentAttributeValueResult.RemoveAll(v => (v.EquipmentId != 0 && v.EquipmentId == this.Id) || v.EquipmentUid == this.TemporaryId);
        }

        Attributes.RemoveAll(a => a.SourceContext == ((int)WorkflowEquipmentBuilder.SourceContexts.EquipmentType));
        Consumables.RemoveAll(a => a.SourceContext == ((int)WorkflowEquipmentBuilder.SourceContexts.EquipmentType));
        AvailableConsumables.RemoveAll(a => a.SourceContext == ((int)WorkflowEquipmentBuilder.SourceContexts.EquipmentType));

        foreach (var equipmentTypeAttribute in equipmentTypeAttributeList.FindAll(eta => eta.EquipmentTypeId == this.EquipmentTypeId))
        {
            var attribute = attributeList.FirstOrDefault(a => a.Id == equipmentTypeAttribute.AttributeId);
            if (attribute is not null)
            {
                if (attribute.IsConsumable)
                {
                    var attributeValues = allAttributeValues.FindAll(v => v.AttributeId == attribute.Id);
                    if (attributeValues.Count != 0)
                    {
                        foreach (var val in attributeValues)
                        {
                            AddAttribute(attribute, equipmentTypeAttribute, null, null, null, val);
                        }
                    }
                    else
                    {
                        AddAttribute(attribute, equipmentTypeAttribute, null, null, null, null);
                    }
                }
                else
                {
                    var val = allAttributeValues.FirstOrDefault(a => a.AttributeId == attribute.Id);
                    AddAttribute(attribute, equipmentTypeAttribute, null, null, null, val);
                }
            }
        }
    }
    public bool GetHasMissingValues()
    {
        if (string.IsNullOrEmpty(UnitImage1FileName)
            || string.IsNullOrEmpty(UnitImage2FileName)
            || string.IsNullOrEmpty(DataPlateFileName)
            || string.IsNullOrEmpty(EqType)
            || string.IsNullOrEmpty(Make)
            || string.IsNullOrEmpty(UnitTag)
            || string.IsNullOrEmpty(EquipLocArea)
            || string.IsNullOrEmpty(Model)
            || string.IsNullOrEmpty(Serial)
            || string.IsNullOrEmpty(EquipmentGroup)
            || !MfgDate.HasValue
            || !InstallationDate.HasValue)
        {
            return true;
        }
        return false;
    }
    public bool HasMissingAttributesValues()
    {
        foreach (var attribute in Attributes.Where(a=>a.Required))
        {
            if (string.IsNullOrEmpty(attribute.Value))
            {
                return true;
            }
        }
        return false;
    }
    public bool HasAvailableConsumables()
    {
        return AvailableConsumables.Count != 0;
    }
    public bool HasAttributes()
    {
        return Attributes.Count != 0;
    }
    public int NextMissingAttributeIndex(int currentIndex)
    {
        int startPosition = 0;
        if (currentIndex != 0)
        {
            startPosition = currentIndex + 1;
        }
        for (int idx = startPosition; idx < Attributes.Count; idx++)
        {
            if (string.IsNullOrEmpty(Attributes[idx].Value) && Attributes[idx].Required)
            {
                return idx;
            }
        }

        return -1;
    }
    public int PreviousMissingAttributeIndex(int currentIndex)
    {
        int startPosition = Attributes.Count - 1;

        if (currentIndex != Attributes.Count)
        {
            startPosition = currentIndex - 1;
        }
        for (int idx = startPosition; idx >= 0; idx--)
        {
            if (Attributes[idx].Required && string.IsNullOrEmpty(Attributes[idx].Value))
            {
                return idx;
            }
        }
        return -1;
    }


    #region Additional Properties for derived class

    public List<EquipmentAttribute> Attributes { set; get; }
    public List<EquipmentAttribute> Consumables { set; get; }
    public List<EquipmentAttribute> AvailableConsumables { set; get; }

    #endregion

    public WorkflowEquipment(int Id,
                             int CustomerLocationId,
                             int EquipmentTypeId,
                             string TypeName,
                             string EqType,
                             string UnitTag,
                             string EquipLocArea,
                             string Make,
                             string Model,
                             string Serial,
                             bool IsRetired,
                             string? EquipmentGroup,
                             DateTime? MfgDate,
                             DateTimeOffset? InstallationDate,
                             DateTimeOffset? WarrantyEndDate,
                             Guid? TemporaryId,
                             string? ExternalKey,
                             string? Notes,
                             string? Tags,
                             string? DataPlateFileName,
                             string? UnitImage1FileName,
                             string? UnitImage2FileName,
                             int CreatedBy,
                             DateTime CreatedDate,
                             DateTime ModifiedDate,
                             int ModifiedBy,
                             bool? IsDirty,
                             int Hash,
                             List<EquipmentAttribute>? Attributes,
                             List<EquipmentAttribute>? Consumables,
                             List<EquipmentAttribute>? AvailableConsumables
                             )
        : base(Id, CustomerLocationId, EquipmentTypeId, TypeName, EqType, UnitTag, EquipLocArea, Make, Model, Serial, IsRetired, EquipmentGroup, MfgDate, InstallationDate, WarrantyEndDate, TemporaryId, ExternalKey, Notes, Tags, DataPlateFileName, UnitImage1FileName, UnitImage2FileName, CreatedBy, CreatedDate, ModifiedDate, ModifiedBy, IsDirty, Hash)
    {

        this.Attributes = Attributes ?? new();
        this.Consumables = Consumables ?? new();
        this.AvailableConsumables = AvailableConsumables ?? new();
    }
}