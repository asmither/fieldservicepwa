using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;

namespace ICS.Portal.Data.Custom
{
    public enum CallBackTypes
    {
        Error,
        IDXDB,
        API
    }

    public class CallBack<T>
    {
        public CallBackTypes CallBackType { get; }
        public T Data { get; }

        public CallBack(CallBackTypes callBackType, T data)
        {
            CallBackType = callBackType;
            Data = data;
        }
    }

    public interface IWorkflowData
    {
        #region SyncTimer
        bool SyncEnabled { set; get; }

        #endregion

        #region SyncCount and InternetConnected

        int SyncCount { set; get; }
        bool InternetConnected { set; get; }
        event Action<bool> OnInternetConnectionStateChanged;
        event Action<int>? OnSyncCountChanged;
        Task CheckStatuses();
        Task<Dictionary<string, List<IDXDBSyncRecord>>> GetSyncRecordsAsync();
        Task<bool> Ping();
        #endregion SyncCount and InternetConnected

        #region AppState
        Task<bool> CheckInternet();
        AppState AppState { get; }
        Task<bool> LoadAppStateAsync();
        Task SaveAppStateInstance();

        #endregion AppState

        #region PushChanges

        Task<bool> PushChangeAsync(string storeName, IDXDBSyncRecord syncRecord);

        #endregion

        #region Commands
        Task SaveAndPostWorkflowStepAsync(WorkflowStepResultValueSaveInput input);

        Task SaveAndPostWorkflowResultCompleteAsync(WorkflowResultCompleteInput input);

        Task DeleteWorkflowAsync(int workflowResultId, int DispatchId, int DispatchTechId);

        #endregion

        Task<T?> GetIDXDBRecord<T>(string key);

        #region Errors

        Task ClearErrors();

        Task<bool> SendError(ServiceErrorInsertInput input);

        Task TryWriteError(Exception ex, string methodName);

        Task TryWriteError(string error, string methodName);

        #endregion Errors

        #region Queries

        Task<List<WorkflowListAvailableResult>?> GetWorkflowListAvailable(int dispatchId, int dispatchTechId);
        Task<VersionTrackerResult> GetVersionTrackerAsync(Action<VersionTrackerResult> apiCallback);
        Task<List<TechNewsListResult>> GetNewsAsync(Action<List<TechNewsListResult>> apiCallback, int? employeeId);
        Task<List<TechKPIListResult>> GetTechKPIListAsync(Action<List<TechKPIListResult>> apiCallback, int? employeeId);
        Task<List<CompanyDirectoryResult>> GetCompanyDirectoryAsync(Action<List<CompanyDirectoryResult>>? apiCallback);
        Task<List<DispatchListResult>> GetDispatchesForTechnicianAsync(Action<List<DispatchListResult>> apiCallback, int employeeId, int daysAgo = 17);

        #endregion

        Task HydrateImageAsync(ImageInsertInput imageInsertInput);

        Task<WorkflowResultDetailOutput> GetWorkflowAsync(int workflowResultId);

        Task SaveDispatchAsync(DispatchDetailResult dispatch);

        //Task<DispatchDetailResult?> GetDispatchAsync(int dispatchId, int dispatchTechId, bool forceRefresh = false);

        Task<DispatchDetailResult?> GetDispatchDataAsync(int dispatchId, int dispatchTechId, bool forceRefresh = false);


        Task<List<WorkflowResultListResult>?> GetWorkflowResultList(int dispatchId, int dispatchTechId);
        Task SaveWorkflowAsync(WorkflowResultDetailOutput workflow);

        Task<LookupsResult?> GetLookupsAsync();

        Task DeleteImageAsync(ImageInsertInput input);

        Task SaveAndPostImageAsync(ImageInsertInput input);

        Task<WorkflowSequenceQueryValueResult?> WorkflowSequenceQueryValueAsync(WorkflowSequenceQueryValueInput input);

        //Task DeleteLocalWorkflowAsync(int workflowResultId);

        Task<bool> TryHydrateFromCache(PresentationMedia media);

        Task<List<WorkflowDataQueryOptionsResult>?> GetWorkflowDataQueryOptionsAsync(WorkflowDataQueryOptionsInput input);
        
        

       

        Task<string?> QueryWorkflowStepResultValueAsync(int workflowResultId, int workflowStepId, int loopIndex);

        Task CancelWorkflowAsync(int workflowResultId);

        Task<List<WorkflowResultDetailResult.Workflow>> GetLocalWorkflows(int? dispatchTeckId = null, int? workflowId = null, int? employeeId = null);

        Task<Dictionary<int, int>> GetLocalDispatchTechIds();



        Task UpdateDispatchListItem(int id, int dispatchTechId, string status, DateTimeOffset? workStart, DateTimeOffset? workStop, DateTimeOffset? dispatched);

        Task SaveAndPostSubscriptionAsync(NotificationSubscription subscription);
        

        

        Task<List<ServiceErrorInsertInput>> GetErrors();

        Task GetScratchPad();
    }
}