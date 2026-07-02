using ICS.Mobile;
using ICS.Mobile.Helpers;
using ICS.Mobile.Services;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Auth.Models;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

#region Configure App Settings

// There are various places in the UI that use this to hide or show features.
const bool DEBUG = false;

// This is used to select the appropriate ApiUrl and ImageBaseUrl
// Debug | Stage | Production and is case sensitive
// TODO: revert to "Production" before a production build/release.
const string ENVIRONMENT = "Debug"; // local Functions host (localhost:7264 → ICSDEV); Stage API blocks the localhost origin via CORS, so local login must use the local host

const string DEBUG_API_URL = "http://localhost:7264";
const string STAGE_API_URL = "https://icsapidev.azurewebsites.net";
const string PRODUCTION_API_URL = "https://icsapiprd2.azurewebsites.net";

const string DEBUG_IMAGE_BASE_URL = "https://interiorcsdevstorage.blob.core.windows.net/images/";
const string STAGE_IMAGE_BASE_URL = "https://interiorcsdevstorage.blob.core.windows.net/images/";
const string PRODUCTION_IMAGE_BASE_URL = "https://interiorcsstorage.blob.core.windows.net/images/";

const int USER_OVERRIDE_ID = 0;
const string APP_VERSION = "7.50";
const string APP_NAME = "ICSTechApp";
const string APP_DISPLAY_NAME = "ICS Tech";
const int IDXDB_VERSION = 502;
const int QUERY_TIMEOUT_DEFAULT = 15;
const int PING_TIMEOUT_DEFAULT = 4;
const int PING_FREQUENCY = 20;
const int IMAGE_TIMEOUT_DEFAULT = 120;
const int COMMAND_TIMEOUT_DEFAULT = 40;
const string JS_MODULE_PATH = @"/modules/";
const string CACHE_NAME = "ics";
const string IDXDB_NAME = "ICSII";
const int CACHE_DAYS = 7;
const int DEFAULT_SYNC_INTERVAL = 5000;
const int MAX_SYNC_INTERVAL = 16000;
const int MAX_DISPATCHES_TO_SHOW = 32;

const string BLUON_API_KEY = ""; // Bluon disconnected 2026-07 — empty disables the integration app-wide
const string BLUON_API_ROOT = "https://hub.bluon.com/gateway/interior-climate-solutions";


// Each permission here requires a corresponding permission request function in browser-permissions.js
List<string> REQUIRED_PERMISSIONS = new(){
    "geolocation",
    "notifications",
    //"camera",
    //"microphone"
};

string apiUrl = string.Empty;
string imageBaseUrl = string.Empty;

switch (ENVIRONMENT)
{
    case "Debug":
        apiUrl = DEBUG_API_URL;
        imageBaseUrl = DEBUG_IMAGE_BASE_URL;
        break;
    case "Stage":
        apiUrl = STAGE_API_URL;
        imageBaseUrl = STAGE_IMAGE_BASE_URL;
        break;
    case "Production":
        apiUrl = PRODUCTION_API_URL;
        imageBaseUrl = PRODUCTION_IMAGE_BASE_URL;
        break;
    default:
        throw new ArgumentOutOfRangeException("ENVIRONMENT", "Must be one of the following: Development, State, Production");
}


// Create the settings service
var settingsService = new SettingsService(
    APP_VERSION,
    APP_NAME,
    APP_DISPLAY_NAME,
    IDXDB_VERSION,
    GetIDXDBDefinition(),
    JS_MODULE_PATH,
    CACHE_NAME,
    REQUIRED_PERMISSIONS,
    DEFAULT_SYNC_INTERVAL,
    MAX_SYNC_INTERVAL,
    CACHE_DAYS,
    DEBUG,
    QUERY_TIMEOUT_DEFAULT,
    PING_TIMEOUT_DEFAULT,
    PING_FREQUENCY,
    IMAGE_TIMEOUT_DEFAULT,
    COMMAND_TIMEOUT_DEFAULT,
    USER_OVERRIDE_ID,
    ENVIRONMENT,
    apiUrl,
    imageBaseUrl,
    MAX_DISPATCHES_TO_SHOW,
    BLUON_API_KEY,
    BLUON_API_ROOT
    );

#endregion Settings

#region IndexedDB

IDXDBDefinition GetIDXDBDefinition()
{
    int purgeImmediately = 0, noRefresh = 0;
    int minute = 60;
    int hour = minute * 60;
    int day = hour * 24;

    List<IDXDBStore> stores = new()
    {
        //Persisted Queries
        new(nameof(DispatchDetailOutput), "key", 0, 5, day * 3),
        new(nameof(WorkflowResultDetailOutput), "key", 0, noRefresh, day * 3),
        new(nameof(LookupsOutput), "key", 0, minute * 10, 0),
        new(nameof(TechKPIListOutput), "key", 0, hour * 1, 0),
        new(nameof(TechNewsListOutput), "key", 0, hour * 1, 0),
        new(nameof(VersionTrackerOutput), "key", 0, hour * 1, 0),
        new(nameof(DispatchListOutput), "key", 0, 5, 0),
        new(nameof(CompanyDirectoryOutput), "key", 0,day, 0),
        new(nameof(ClientSecretsOutput), "key", 0, day, 0),
        new(nameof(BluonCacheByIdOutput), "key", 0, 7 * day, purgeAfter: 7 * day),
        new(nameof(WorkflowListAvailableOutput),"key", 0, minute * 5, day * 7),
        new(nameof(WorkflowResultListOutput), "key", 0, 5, day * 7),

        // all the Non Sync Commands
        
        // scratchpad data - to be synced someday
        

        // log
        new(nameof(LogInsertInput), "key", 0, 0,day * 1),

        //Sync Commands - note that sync records are always purged immediately
        new(nameof(WorkflowStepResultValueSaveInput), "key", 1, noRefresh, purgeImmediately),
        new(nameof(WorkflowResultCompleteInput), "key", 2, noRefresh, purgeImmediately),
        new(nameof(ImageInsertInput), "key", 3, noRefresh, purgeImmediately),
        new(nameof(AuthorizedUserSubscriptionSaveInput), "key",4,noRefresh, purgeImmediately),
        new(nameof(ScratchPadData), "key", 5, 0, 0),
        new(nameof(ScratchPadBlob), "key", 6, 0, 365*day), 

        //Custom AppState
        new(nameof(AppState), "key", 0,0,0),
        new(nameof(AuthorizedUser), "key", 0,0,day * 90),
        new(nameof(ServiceErrorInsertInput), "key", 0, 0, 0),
    };

    return new IDXDBDefinition(IDXDB_VERSION, IDXDB_NAME, stores);
}

#endregion IndexedDB


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<JSUI>();
builder.Services.AddSingleton<JSUtil>();
builder.Services.AddSingleton<ThemeService>();

builder.Services.AddUpdateService();
builder.Services.AddSingleton<SettingsService>(settingsService);
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<IDXDBService>();
//builder.Services.AddSingleton<NotificationsService>(); No longer in use
//builder.Services.AddSingleton<PermissionsService>(); No longer in use
//builder.Services.AddSingleton<ConnectionStateService>(); No longer in use
builder.Services.AddSingleton<IWorkflowData, DataService>();
builder.Services.AddSingleton<LocationService>();
builder.Services.AddSingleton<DateTimeService>();
builder.Services.AddSingleton<ScratchPadService>();
builder.Services.AddSingleton<HTTPService>();

await builder.Build().RunAsync();

// Vapid Public Key
//const string PUSH_NOTIFICATIONS_PUBLIC_KEY = "BKsQhMpeiuGvLMJhGvTcHGsRWBfpUhA0ZCg33KmKaCc7QSGoeAhVaZqokJaV1DEXt4ynephmjjeXtIiWFaVh40E";
//const string PUSH_NOTIFICATIONS_PRIVATE_KEY = "GUjOIfMMRMjRATmcfRVIDc68DxvP0g-6x83270jDnOc";
