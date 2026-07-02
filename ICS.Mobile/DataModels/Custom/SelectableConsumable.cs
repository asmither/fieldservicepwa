namespace ICS.Portal.Data.Custom
{
    public interface IConsumableDTO
    {
        int Id { get; set; }
        Guid Uid { get; set; }
        int EntityId { get; set; }
        Guid? EntityUid { get; set; }
        decimal? Quantity { get; set; }
        int AttributeId { get; set; }
        int Context { get; set; }

        string? Value { set; get; }
    }
    public class ConsumableDTO : IConsumableDTO
    {
        public ConsumableDTO(int id, Guid uid, int entityId, Guid? entityUid, decimal? quantity, int attributeId, int context, string? value)
        {
            Id = id;
            Uid = uid;
            EntityId = entityId;
            EntityUid = entityUid;
            Quantity = quantity;
            OriginalQuantity = quantity;
            AttributeId = attributeId;
            Context = context;
            Value = value;
        }

        public int Id { get; set; }
        public Guid Uid { get; set; }
        public int EntityId { get; set; }
        public Guid? EntityUid { get; set; }
        public decimal? OriginalQuantity { get; set; }
        public decimal? Quantity { get; set; }
        public int AttributeId { get; set; }
        public int Context { get; set; }
        public string? Value { get; set; }
    }
    public class SelectableConsumable : IConsumableDTO
    {
        public SelectableConsumable(
        int id,
        Guid uid,
        int entityId,
        Guid? entityUid,
        decimal? quantity,
        string? value,
        string? notes,
        int attributeId,
        int context,
        int sourceContext)
        {
            Id = id;
            Uid = uid;
            EntityId = entityId;
            EntityUid = entityUid;
            Quantity = quantity;
            OriginalQuantity = quantity;
            Value = value;
            Notes = notes;
            AttributeId = attributeId;
            Context = context;
            SourceContext = sourceContext;
        }

        public int Id { get; set; }
        public Guid Uid { get; set; }
        public int EntityId { get; set; }
        public Guid? EntityUid { get; set; }
        public decimal? OriginalQuantity { get; set; }
        public decimal? Quantity { get; set; }
        public string? Value { get; set; }
        public string? Notes { get; set; }
        public int LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int AttributeId { get; set; }
        public int Context { get; set; }
        public int SourceContext { get; set; }
        public bool IsSelected { set; get; }
        public bool IsDirty { set; get; }

        private string[] keyPair = null;

        public string? ValueKey
        {
            get
            {
                if(TryGetParts())
                {
                    return keyPair[1];
                }

                return Value;
            }
        }

        public string? ValueValue
        {
            get
            {
                if (TryGetParts())
                {
                    return keyPair[0];
                }

                return Value;
            }
        }

        private bool TryGetParts()
        {
            if (Value is not null && Value.Contains("~") && keyPair is null)
            {
                string[] parts = Value.Split('~');
                if (parts.Length == 2)
                {
                    keyPair = parts;
                }
            }
            return keyPair != null;
        }
    }
}