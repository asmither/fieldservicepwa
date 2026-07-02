namespace ICS.Portal.Data.Bundles
{
    public class ServiceBundle
    {
        public ServiceBundle(int DataServiceId, string? InputJSON, string? OutputJSON)
        {
            this.DataServiceId = DataServiceId;
            this.InputJSON = InputJSON;
            this.OutputJSON = OutputJSON;
        }

        public int DataServiceId { get; }
        public string? InputJSON { get; }
        public string? OutputJSON { get; }
    }
}
