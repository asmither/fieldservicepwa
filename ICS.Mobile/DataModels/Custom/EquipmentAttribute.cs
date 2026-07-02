using ICS.Portal.Data.Queries.Models;

using System.Text.Json.Serialization;

namespace ICS.Portal.Data.Custom;

public class EquipmentAttribute/* : LookupsResult.Attribute*/
{
    public EquipmentAttribute(
        int id,
        Guid uid,
        int equipmentId,
        Guid? equipmentUid,
        decimal? quantity,
        string? value,
        string? notes,
        int lastModifiedBy,
        DateTime lastModifiedDate,
        int attributeId,
        int context,
        int sourceContext,
        string name,
        string description,
        int dataTypeId,
        string? optionList,
        bool isConsumable,
        bool isItem,
        bool isChecklist,
        bool required)
    {
        Id = id;
        Uid = uid;
        EquipmentId = equipmentId;
        EquipmentUid = equipmentUid;
        Quantity = quantity;
        Value = value;
        Notes = notes;
        LastModifiedBy = lastModifiedBy;
        LastModifiedDate = lastModifiedDate;
        AttributeId = attributeId;
        Context = context;
        SourceContext = sourceContext;
        Name = name;
        Description = description;
        DataTypeId = dataTypeId;
        OptionList = optionList;
        IsConsumable = isConsumable;
        IsItem = isItem;
        IsChecklist = isChecklist;
        Required = required;
    }

    public int Id { get; }
    public Guid Uid { get; }
    public int EquipmentId { get; }
    public Guid? EquipmentUid { get; }
    public decimal? Quantity { get; set; }
    public string? Value { get; set; }
    public string? Notes { get; set; }
    public int LastModifiedBy { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public int AttributeId { get; }
    public int Context { get; }
    public int SourceContext { get; set; }
    public string Name { get; }
    public string Description { get; }
    public int DataTypeId { get; }
    public string? OptionList { get; set; }
    public bool IsConsumable { get; }
    public bool IsItem { get; }
    public bool IsChecklist { get; }
    public bool Required { get; set; }

    public bool IsValid()
    {
        if (IsConsumable)
        {
            if (string.IsNullOrEmpty(Value) || Quantity is null)
            {
                return false;
            }
            return true;
        }

        if (Required)
        {
            return string.IsNullOrEmpty(Value) == false;
        }

        return true;
    }

    private List<string>? _options = null;

    [JsonIgnore]
    public List<string>? Options
    {
        get
        {
            if (_options is null)
            {
                if (!string.IsNullOrEmpty(OptionList))
                {
                    string[] optionsArray = OptionList.Split(',', StringSplitOptions.None);
                    if (optionsArray.Length != 0)
                    {
                        _options = optionsArray.ToList();
                    }
                }
            }
            return _options;
        }
    }



    #region Setters and Getters

    public bool SetDateOnlyValue(DateOnly? value)
    {
        string? valueAsString = null;
        if (value.HasValue)
        {
            valueAsString = value.ToString();
        }
        if (Value != valueAsString)
        {
            Value = valueAsString;
            return true;
        }
        return false;
    }

    public DateOnly? GetDateOnlyValue()
    {
        if (string.IsNullOrEmpty(Value))
        {
            return null;
        }
        if (DateOnly.TryParse(Value, out DateOnly result))
        {
            return result;
        }
        return null;
    }

    public bool SetDecimalValue(decimal? value)
    {
        string? valueAsString = null;
        if (value.HasValue)
        {
            valueAsString = value.ToString();
        }
        if (Value != valueAsString)
        {
            Value = valueAsString;
            return true;
        }
        return false;
    }

    public decimal? GetDecimalValue()
    {
        if (string.IsNullOrEmpty(Value))
        {
            return null;
        }
        if (decimal.TryParse(Value, out decimal result))
        {
            return result;
        }
        return null;
    }

    public bool SetStringValue(string value)
    {
        if (Value != value)
        {
            Value = value;
            return true;
        }
        return false;
    }

    public string? GetStringValue()
    {
        return Value;
    }

    #endregion Setters and Getters

    public DispatchDetailResult.EquipmentAttributeValue GetEquipmentAttributeValue()
    {
        return new(
            Id,
            Uid,
            AttributeId,
            EquipmentId,
            EquipmentUid,
            Quantity,
            Value,
            Notes,
            LastModifiedBy,
            LastModifiedDate
        );
    }

    public EquipmentAttribute Clone()
    {
        return new EquipmentAttribute(Id, Uid, EquipmentId, EquipmentUid, Quantity, Value, Notes, LastModifiedBy, LastModifiedDate, AttributeId, Context, SourceContext, Name, Description, DataTypeId, OptionList, IsConsumable, IsItem, IsChecklist, Required);
    }
}