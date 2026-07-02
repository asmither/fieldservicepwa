using ICS.Mobile.Services.ServiceModels;

namespace ICS.Mobile.Services
{
    public class SettingsService
    {
        public SettingsService(
            string appVersion,
            string appName,
            string appDisplayName,
            int idxdbVersion,
            IDXDBDefinition idxdbDefinition,
            string jsModulePath,
            string cacheName,
            List<string> requiredPermissions,
            int defaultSyncInterval,
            int maxSyncInterval,
            int cacheDays,
            bool debugMode,
            int queryTimeoutDefault,
            int pingTimeoutDefault,
            int pingFrequency,
            int imageTimeoutDefault,
            int commandTimeoutDefault,
            int userOverrideId,
            string environment,
            string apiUrl,
            string imageBaseUrl,
            int maxDispatchesToShow,
            string bluonApiKey,
            string bluonApiRoot
            )
        {
            AppVersion = appVersion;
            AppName = appName;
            AppDisplayName = appDisplayName;
            IDXDBVersion = idxdbVersion;
            IDXDBDefinition = idxdbDefinition;
            JsModulePath = jsModulePath;
            CacheName = cacheName;
            RequiredPermissions = requiredPermissions;
            DefaultSyncInterval = defaultSyncInterval;
            MaxSyncInterval = maxSyncInterval;
            CacheDays = cacheDays;
            DebugMode = debugMode;
            QueryTimeoutDefault = queryTimeoutDefault;
            PingTimeoutDefault = pingTimeoutDefault;
            PingFrequency = pingFrequency;
            ImageTimeoutDefault = imageTimeoutDefault;
            CommandTimeoutDefault = commandTimeoutDefault;
            UserOverrideId = userOverrideId;
            Environment = environment;
            ApiUrl = apiUrl;
            ImageBaseUrl = imageBaseUrl;
            MaxDispatchesToShow = maxDispatchesToShow;
            BluonApiKey = bluonApiKey;
            BluonApiRoot = bluonApiRoot;
        }

        public string AppVersion { get; }
        public string AppName { get; }
        public string AppDisplayName { get; }
        public int IDXDBVersion { get; }
        public IDXDBDefinition IDXDBDefinition { get; }
        public string JsModulePath { get; }
        public string CacheName { get; }
        public List<string> RequiredPermissions { get; }
        public int DefaultSyncInterval { get; }
        public int MaxSyncInterval { get; }
        public int CacheDays { get; }
        public bool DebugMode { get; }
        public int QueryTimeoutDefault { get; }
        public int CommandTimeoutDefault {  get; }
        public int PingTimeoutDefault {  get; }
        public int PingFrequency { get; }
        public int ImageTimeoutDefault { get; }
        // Settable at runtime by the DEBUG-only developer tools in Settings
        // (view-as-technician). Release builds never change it after startup.
        public int UserOverrideId { get; set; }
        public string Environment { get; }
        public string ApiUrl { get; }
        public string ImageBaseUrl { get; }
        public int MaxDispatchesToShow { get; }
        public string BluonApiKey { get; }
        public string BluonApiRoot{ get; }
        
    }
}
