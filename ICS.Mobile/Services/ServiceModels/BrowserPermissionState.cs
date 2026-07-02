namespace ICS.Mobile.Services.ServiceModels
{
    public class BrowserPermissionState
    {
        public BrowserPermissionState(string permission, string state)
        {
            Permission = permission;
            State = state;
        }

        public string Permission { get; }
        public string State { get; set; }

        public bool IsGranted()
        {
            return State == "granted";
        }
    }
}
