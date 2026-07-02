using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Data.Custom
{
    public class AttributeValue
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public int Context { get; set; }
        public int EntityId { get; set; }
        public Guid? EntityTemporaryId { get; set; }
        public decimal? Quantity { get; set; }
        public string? Value { get; set; }
        public string? Note { get; set; }
        public int WorkOrderDispatchId { get; set; }
        public DateTime LastModifiedData { get; set; }

        public AttributeValue(int id, int attributeId, int context, int entityId, Guid? entityTemporaryID, decimal? quantity, string? value, string? note, int workOrderDispatchId, DateTime lastModifiedData)
        {
            Id = id;
            AttributeId = attributeId;
            Context = context;
            EntityId = entityId;
            EntityTemporaryId = entityTemporaryID;
            Quantity = quantity;
            Value = value;
            Note = note;
            WorkOrderDispatchId = workOrderDispatchId;
            LastModifiedData = lastModifiedData;
        }
    }
}
