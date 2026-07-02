namespace ICS.Mobile.DataModels.Custom
{
    public class ConsumableItem
    {
        public int AttributeId { set; get; }

        public string? ConsumableId { set; get; }
        public string? Name { set; get; }

        public decimal? Quantity { set; get; }

        public string? Notes { set; get; }
    }
}
