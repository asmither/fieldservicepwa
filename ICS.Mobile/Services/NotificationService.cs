//using ICS.Mobile.Services.ServiceModels;

//using Microsoft.JSInterop;

//namespace ICS.Mobile.Services
//{
//    public class NotificationsService : JSServiceBase
//    {
//        public NotificationsService(IJSRuntime js, SettingsService settings)
//            : base(js, settings, "notificationsService.js")
//        { }

//        public async Task<NotificationSubscriptionResult> GetNotificationSubscription()
//        {
//            await base.WaitForReferenceAsync();

//            var obj = jsRef.Value.InvokeAsync<object>("getNotificationSubscription");
//            return await jsRef.Value.InvokeAsync<NotificationSubscriptionResult>("getNotificationSubscription");
//        }

//        public async Task<NotificationSubscriptionResult> ShowNotification()
//        {
//            await base.WaitForReferenceAsync();

//            return await jsRef.Value.InvokeAsync<NotificationSubscriptionResult>("getNotificationSubscription");
//        }
//    }
//}