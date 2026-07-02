namespace ICS.Mobile.Services.ServiceModels
{
    public class NotificationSubscriptionResult
    {
        public PermissionRequestResult? Result { set; get; }

        public NotificationSubscription? Value { set; get; }
    }

    public class NotificationSubscription : IDXDBRecordBase
    {
        public int? AuthUserId { set; get; }
        public string? EndPoint { get; set; }
        public string? P256DH { get; set; }
        public string? Auth { get; set; }
        public bool IsNew { get; set; }

        public override bool IsSuccess()
        {
            throw new NotImplementedException();
        }
    }
}
