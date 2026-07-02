//using ICS.Mobile.Services.ServiceModels;

//using Microsoft.JSInterop;

//using System.Diagnostics;
//using System.Reflection.Metadata.Ecma335;

//namespace ICS.Mobile.Services
//{
//    public class PermissionsService : JSServiceBase
//    {

//        public const string WINDOWS = "Windows";
//        public const string MACOS = "MacOS";
//        public const string LINUX = "Linux";
//        public const string ANDROID= "Android";
//        public const string IPHONE = "iPhone";

//        public IEnumerable<string> GetSupportedDevices()
//        {
//            yield return WINDOWS;
//            yield return MACOS;
//            yield return LINUX;
//            yield return ANDROID;
//            yield return IPHONE;
//        }

//        public PermissionsService(IJSRuntime js, SettingsService settings)
//            : base(js, settings, "permissionsService.js")
//        { }

//        protected override async Task WaitForReferenceAsync()
//        {
//            await base.WaitForReferenceAsync();
//        }

//        public async Task<PermissionRequestResult> RequestPermission(string permission)
//        {
//            await WaitForReferenceAsync();
//            return await jsRef.Value.InvokeAsync<PermissionRequestResult>($"{permission.Replace("-", "")}PermissionRequest");
//        }

//        public async Task<NotificationSubscriptionResult> GetNotificationSubscription()
//        {
//            await WaitForReferenceAsync();
//            return await jsRef.Value.InvokeAsync<NotificationSubscriptionResult>("getNotificationSubscription");
//        }

//        public async Task<List<BrowserPermissionState>> GetCurrentPermissionsStates()
//        {
//            await WaitForReferenceAsync();
//            return await jsRef.Value.InvokeAsync<List<BrowserPermissionState>>("getCurrentPermissionsStates");
//        }

//        public async Task<bool> IsRunningAsStandalone()
//        {
//            await WaitForReferenceAsync();
//            return await jsRef.Value.InvokeAsync<bool>("isRunningAsStandalone");
//        }
//        private async Task<bool> GetPermissionState(string permission)
//        {
//            var permissions = await GetCurrentPermissionsStates();
//            if(permissions is not null)
//            {
//                var instance = permissions.FirstOrDefault(p => p.Permission == permission);
//                if(instance is not null)
//                {
//                    return instance.IsGranted();
//                }
//            }
//            return false;
//        }

//        public async Task<bool> LocationAllowed()
//        {
//            await WaitForReferenceAsync();
//            return await GetPermissionState("geolocation");
//        }

//        public async Task<bool> NotificationsAllowed()
//        {
//            await WaitForReferenceAsync();
//            return await GetPermissionState("notifications");
//        }

//        public async Task<bool> AllPermissionsGranted()
//        {
//            await WaitForReferenceAsync();
//            var permissions = await jsRef.Value.InvokeAsync<List<BrowserPermissionState>>("getCurrentPermissionsStates");
//            foreach( var permission in permissions)
//            {
//                if(permission.IsGranted() == false)
//                {
//                    return false;
//                }
//            }
//            return true;
//        }
//        public async Task<string> GetOS()
//        {
//            await WaitForReferenceAsync();
//            var os = await jsRef.Value.InvokeAsync<string>("getOS");
//            return os;
//        }
        
//    }
//}
