using ICS.Mobile.Helpers;
using ICS.Mobile.Http;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Data.Commands.Models;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ICS.Mobile.Services
{
    public class DataService : IWorkflowData, IBluonCache, IDisposable
    {
        #region Constructor

        private readonly SettingsService settings;
        private readonly IDXDBService idxdb;
        private readonly CacheService cache;
        private readonly HTTPService httpService;
        private readonly System.Timers.Timer syncTimer;
        public DataService(SettingsService settings, IDXDBService idxdb, CacheService cache, HTTPService httpService)
        {
            this.settings = settings;
            this.idxdb = idxdb;
            this.cache = cache;
            this.httpService = httpService;

            syncTimer = new() { Interval = settings.DefaultSyncInterval };
            syncTimer.Elapsed += SyncTimer_Elapsed;
        }

        #endregion Constructor

        #region SyncTimer

        private bool _SyncEnabled = false;

        /// <summary>
        /// Set: Sets the value and starts or stops the timer
        /// Get: Returns the value
        /// </summary>
        public bool SyncEnabled
        {
            set
            {
                if (value != _SyncEnabled)
                {
                    _SyncEnabled = value;
                    if (_SyncEnabled)
                    {
                        if (!syncTimer.Enabled)
                        {
                            syncTimer.Start();
                        }
                    }
                    else
                    {
                        if (syncTimer.Enabled)
                        {
                            syncTimer.Stop();
                        }
                    }
                }
            }
            get
            {
                return _SyncEnabled;
            }
        }
        private async void SyncTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (SyncEnabled)
            {
                SyncCount = await GetSyncCountsAsync();

                if (await CheckInternet())
                {
                    syncTimer.Stop();

                    if (SyncCount != 0 && InternetConnected)
                    {
                        await PushChangesAsync();
                        syncTimer.Interval = settings.DefaultSyncInterval;
                    }

                    syncTimer.Start();
                }
            }
        }

        #endregion

        #region SyncCount and Internet Connectivity

        private int _SyncCount = 0;
        private bool? _InternetConnected;

        public event Action<bool>? OnInternetConnectionStateChanged;
        public event Action<int>? OnSyncCountChanged;

        /// <summary>
        /// Number of records in the Sync Tables
        /// Change triggers OnSyncCountChanged
        /// </summary>
        public int SyncCount
        {
            set
            {
                if (value != _SyncCount)
                {
                    _SyncCount = value;
                    OnSyncCountChanged?.Invoke(_SyncCount);
                }
            }
            get
            {
                return _SyncCount;
            }
        }

        /// <summary>
        /// Indicates whether or not there is internet connectivity
        /// Change triggers OnInternetConnectionStateChanged
        /// </summary>
        public bool InternetConnected
        {
            set
            {
                if (value != _InternetConnected)
                {
                    _InternetConnected = value;
                    OnInternetConnectionStateChanged?.Invoke(_InternetConnected.GetValueOrDefault(false));
#if DEBUG
                    Console.WriteLine($"Connection state changed:{_InternetConnected}");
#endif

                }
            }
            get
            {
                return _InternetConnected.GetValueOrDefault(false); // .GetValueOrDefault(false);
            }
        }

        public async Task<bool> Ping()
        {
            return await httpService.GetHttpPingClient().PingAsync();
        }
        public async Task<bool> CheckInternet()
        {
            try
            {
                if (_InternetConnected is null || (InternetConnected == false && AppState.LastPing < DateTime.UtcNow.AddSeconds(-settings.PingFrequency)))
                {
                    var result = await httpService.GetHttpPingClient().PingAsync();
                    if (result)
                    {
                        InternetSucceeded();
                    }
                    else
                    {
                        _InternetConnected = false;
                        UpdateLastPing(); // Prevent rapid re-pinging
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(CheckInternet));
            }

            return InternetConnected;
        }

        /// <summary>
        /// Checks SyncCount and Internet Connectivity
        /// Starts 
        /// </summary>
        public async Task CheckStatuses()
        {
            SyncCount = await GetSyncCountsAsync();
            await CheckInternet();
        }
        private void HandleException(Exception ex, string methodName)
        {
            var t = ex.GetType();
            if (ex is Azure.RequestFailedException)
            {
                InternetFailed();
            }
            else if (ex.InnerException is not null && (ex.InnerException is TimeoutException || ex.InnerException is HttpRequestException || ex is Azure.RequestFailedException || ex.InnerException.Message == "TypeError: Failed to fetch"))
            {
                InternetFailed();
            }
            _ = TryWriteError(ex, methodName);
        }
        private bool HandleResponse(HttpResult httpResponse, string method)
        {
            if (httpResponse.StatusCode == HttpStatusCode.ServiceUnavailable || httpResponse.StatusCode == HttpStatusCode.GatewayTimeout || httpResponse.StatusCode == 0)
            {
                InternetFailed();
                _ = TryWriteError($"Post value failed with status code: {httpResponse.StatusCode}, ErrorMessage: {httpResponse.ErrorMessage}", method);
            }
            else
            {
                InternetSucceeded();
            }

            return InternetConnected;
        }
        private void InternetFailed()
        {
            InternetConnected = false;
            UpdateLastPing();
        }
        private void InternetSucceeded()
        {
            InternetConnected = true;
            UpdateLastPing();
        }
        private void UpdateLastPing()
        {
            AppState.LastPing = DateTime.UtcNow;
            _ = SaveAppStateInstance();
        }

        /// <summary>
        /// Gets the number of records in the sync tables
        /// </summary>
        /// <returns>Actual count or -1 if errors occurred</returns>
        private async Task<int> GetSyncCountsAsync()
        {
            try
            {
                var records = await GetSyncRecordsAsync();
                return records.Sum(sum => sum.Value.Count);
            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(GetSyncCountsAsync));
            }

            return -1;
        }

        /// <summary>
        /// Gets all available sync records s.
        /// </summary>
        /// <returns>Dictionary where key is store name and value is list of record IDXDBSyncRecord</returns>
        public async Task<Dictionary<string, List<IDXDBSyncRecord>>> GetSyncRecordsAsync()
        {
            try
            {
                Dictionary<string, List<IDXDBSyncRecord>> result = new();

                foreach (var store in settings.IDXDBDefinition.Stores.Where(s => s.SyncPriority != 0).OrderBy(s => s.SyncPriority))
                {
                    List<IDXDBSyncRecord> records = await idxdb.GetSyncRecords(store.Name);
                    if (records is not null && records.Count != 0)
                    {
                        records = records.OrderBy(r => r.Size).ThenBy(r => r.Id).ToList();
                        result.Add(store.Name, records);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetSyncRecordsAsync));
            }

            return new();
        }

        #endregion SyncCount and Internet Connectivity

        #region AppState

        private AppState? _AppStateInstance = null;
        public AppState AppState
        {
            get
            {
                if (_AppStateInstance is null)
                {
                    throw new InvalidOperationException("Must call LoadAppStateAsync()");
                }
                return _AppStateInstance!;
            }
        }

        /// <summary>
        /// Gets or creates an _AppStateInstance
        /// </summary>
        /// <returns>Whether or not the user is authorized</returns>
        public async Task<bool> LoadAppStateAsync()
        {
            if (_AppStateInstance is null)
            {
                var instance = await idxdb.GetValueByKey<AppState>("1");
                if (instance is null)
                {
                    instance = new();
                }
                _AppStateInstance = instance;
            }

            return (_AppStateInstance!.AuthorizedUser is not null && _AppStateInstance.AuthorizedUser.IsTokenExpired() == false);
        }
        public async Task SaveAppStateInstance()
        {
            await idxdb.Save<AppState>(_AppStateInstance ?? new());
        }

        #endregion AppState

        #region Push Changes

        /// <summary>
        /// Main sync loop called by timer.
        /// Setting SyncEnable to false or losing internet will force the loop to terminate.
        /// </summary>
        private async Task PushChangesAsync()
        {
            var syncRecordCollections = await GetSyncRecordsAsync();
            SyncCount = syncRecordCollections.Sum(sum => sum.Value.Count);

            foreach (var syncRecordCollection in syncRecordCollections)
            {
                if (await CheckInternet() == false || SyncEnabled == false) return;

                foreach (var syncRecord in syncRecordCollection.Value)
                {
                    if (await CheckInternet() == false || SyncEnabled == false) return;

                    bool pushSuccess = false;

                    switch (syncRecordCollection.Key)
                    {
                        case nameof(WorkflowStepResultValueSaveInput):
                            pushSuccess = await PushRecordAsync<WorkflowStepResultValueSaveInput, WorkflowStepResultValueSaveOutput>(syncRecord);
                            break;
                        case nameof(WorkflowResultCompleteInput):
                            pushSuccess = await PushRecordAsync<WorkflowResultCompleteInput, WorkflowResultCompleteOutput>(syncRecord);
                            break;
                        case nameof(ServiceErrorInsertInput):
                            pushSuccess = await PushRecordAsync<ServiceErrorInsertInput, ServiceErrorInsertOutput>(syncRecord);
                            break;
                        case nameof(AuthorizedUserSubscriptionSaveOutput):
                            pushSuccess = await PushRecordAsync<AuthorizedUserSubscriptionSaveInput, AuthorizedUserSubscriptionSaveOutput>(syncRecord);
                            break;
                        case nameof(ImageInsertInput):
                            pushSuccess = await PushImageAsync(syncRecord);
                            break;
                        case nameof(ScratchPadData):
                            pushSuccess = await PushScratchPadDataAsync(syncRecord);
                            break;
                        case nameof(ScratchPadBlob):
                            pushSuccess = await PushScratchPadBlobAsync(syncRecord);
                            break;
                        default:
                            // No handler for type?
                            break; ;
                    }
                    if (pushSuccess)
                    {
                        SyncCount--;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            await PurgeCache();
        }

        /// <summary>
        /// This method is used to determine the types of the input and output based on the store name.
        /// It is also utilized in the sync page to push single records.
        /// </summary>
        /// <param name="storeName">Name of the idxdb store</param>
        /// <param name="syncRecord">IDXDBSyncRecord with Id (identity column) and RecordId (Id of value store record</param>
        /// <returns>True is successfully pushed.</returns>
        public async Task<bool> PushChangeAsync(string storeName, IDXDBSyncRecord syncRecord)
        {
            if (await CheckInternet())
            {
                switch (storeName)
                {
                    case nameof(WorkflowStepResultValueSaveInput):
                        return await PushRecordAsync<WorkflowStepResultValueSaveInput, WorkflowStepResultValueSaveOutput>(syncRecord);
                    case nameof(WorkflowResultCompleteInput):
                        return await PushRecordAsync<WorkflowResultCompleteInput, WorkflowResultCompleteOutput>(syncRecord);
                    case nameof(ServiceErrorInsertInput):
                        return await PushRecordAsync<ServiceErrorInsertInput, ServiceErrorInsertOutput>(syncRecord);
                    case nameof(AuthorizedUserSubscriptionSaveOutput):
                        return await PushRecordAsync<AuthorizedUserSubscriptionSaveInput, AuthorizedUserSubscriptionSaveOutput>(syncRecord);
                    case nameof(ImageInsertInput):
                        return await PushImageAsync(syncRecord);
                    case nameof(ScratchPadData):
                        return await PushScratchPadDataAsync(syncRecord);
                    case nameof(ScratchPadBlob):
                        return await PushScratchPadBlobAsync(syncRecord);
                }
            }
            return false;
        }

        /// <summary>
        /// Pushes Record and deletes sync info on success.
        /// *Note that this is called in the push changes loop only after the internet check has passed.
        /// </summary>
        private async Task<bool> PushRecordAsync<I, O>(IDXDBSyncRecord syncRecord)
        {
            try
            {
                var valueRecord = await idxdb.GetValueByKey<I>(syncRecord.RecordId);
                if (valueRecord is null)
                {
                    return await TryDeleteRecordAsync<I>(syncRecord);
                }

                var httpResponse = await CommandsClient().PostAsync<O>(valueRecord);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    InternetSucceeded();
                    return await TryDeleteRecordAsync<I>(syncRecord);
                }
                else
                {
                    HandleResponse(httpResponse, nameof(PushRecordAsync));
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(PushChangesAsync));
            }

            return false;
        }

        private ByteArrayContent GetByteArrayContent(string contentType, byte[] data)
        {
            ByteArrayContent result = new ByteArrayContent(data);
            result.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            SetBlockBlobHeader(result.Headers);
            return result;
        }

        private StringContent GetStringContent(string contentType, string content)
        {
            StringContent result = new(content);
            result.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            SetBlockBlobHeader(result.Headers);
            return result;
        }
        private void SetBlockBlobHeader(HttpContentHeaders headers)
        {
            headers.Add("x-ms-blob-type", "BlockBlob");
        }

        /// <summary>
        /// Pushes Record and deletes sync info on success.
        /// </summary>
        private async Task<bool> PushImageAsync(IDXDBSyncRecord syncRecord)
        {
            var clientSecrets = await GetClientSecrets();
            if (clientSecrets is null)
            {
                return false;
            }

            var valueRecord = await idxdb.GetValueByKey<ImageInsertInput>(syncRecord.RecordId);
            if (valueRecord is null || valueRecord.Data is null || valueRecord.ContentType is null)
            {
                return await TryDeleteRecordAsync<ImageInsertInput>(syncRecord);
            }

            var requestUri = clientSecrets.BlobStorageUrl.Replace("?", $"/{valueRecord.GeneratedFileName}?");
            var content = GetByteArrayContent(valueRecord.ContentType, valueRecord.Data);

            try
            {
                
                using var client = httpService.GetImagesHttpClient();
                using var response = await client.PutAsync(requestUri, content);
                if (!response.IsSuccessStatusCode)
                {
                    InternetFailed();
                    if (!string.IsNullOrEmpty(response.ReasonPhrase))
                    {
                        _ = TryWriteError(response.ReasonPhrase, nameof(PushImageAsync));
                    }
                    return false;
                }
                InternetSucceeded();
                return await TryDeleteRecordAsync<ImageInsertInput>(syncRecord);
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(PushImageAsync));
            }

            return false;
        }

        private async Task<bool> PushScratchPadDataAsync(IDXDBSyncRecord syncRecord)
        {
            var clientSecrets = await GetClientSecrets();
            if (clientSecrets is null) return false;

            var valueRecord = await idxdb.GetValueByKey<ScratchPadData>(syncRecord.RecordId);
            if (valueRecord is null) return await TryDeleteRecordAsync<ScratchPadData>(syncRecord);

            try
            {
                string url = clientSecrets.BlobStorageUrl.Replace("?", $"/scratchpad/{valueRecord.Key}?");
                string json = JsonSerializer.Serialize(valueRecord);

                
                using var client = httpService.GetImagesHttpClient();
                using var response = await client.PutAsync(url, GetStringContent("application/json", json));

                if (!response.IsSuccessStatusCode)
                {
                    InternetFailed();
                    if (!string.IsNullOrEmpty(response.ReasonPhrase))
                    {
                        _ = TryWriteError(response.ReasonPhrase, nameof(PushScratchPadDataAsync));
                    }
                    return false;
                }
                InternetSucceeded();
                return await TryDeleteRecordAsync<ScratchPadData>(syncRecord);
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(PushScratchPadDataAsync));
            }

            return false;
        }

        public async Task GetScratchPad()
        {
            // URI to data blob

            try
            {
                var sasUrl = $"{settings.ImageBaseUrl}scratchpad/ScratchPadData-{this.AppState.AuthorizedUser.EntityId}?data={DateTime.Now.Ticks}";
                
                using var client = httpService.GetImagesHttpClient();
                string content = await client.GetStringAsync(sasUrl);
                if (!string.IsNullOrEmpty(content))
                {
                    ScratchPadData? data = JsonSerializer.Deserialize<ScratchPadData>(content);
                    if (data is not null)
                    {
                        // Other checks??
                        await idxdb.SaveWithoutSync<ScratchPadData>(data);
                        await LoadScratchPadImages(data);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetScratchPad));
            }
        }

        private async Task LoadScratchPadImages(ScratchPadData scratchPadData)
        {
            
            using var client = httpService.GetImagesHttpClient();
            try
            {
                foreach (var topic in scratchPadData.Topics)
                {
                    foreach (var item in topic.Items)
                    {
                        if (item.HasStoredImage)
                        {
                            var sasUrl = $"{settings.ImageBaseUrl}scratchpad/{this.AppState.AuthorizedUser.EntityId}_{topic.Id}_{item.Id}?data={DateTime.Now.Ticks}";

                            string content = await client.GetStringAsync(sasUrl);
                            if (!string.IsNullOrEmpty(content))
                            {
                                ScratchPadBlob? data = JsonSerializer.Deserialize<ScratchPadBlob>(content);
                                if (data is not null)
                                    await idxdb.SaveWithoutSync<ScratchPadBlob>(data);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(LoadScratchPadImages));
            }
        }

        private async Task<bool> PushScratchPadBlobAsync(IDXDBSyncRecord syncRecord)
        {
            var clientSecrets = await GetClientSecrets();
            if (clientSecrets is null) return false;

            var valueRecord = await idxdb.GetValueByKey<ScratchPadBlob>(syncRecord.RecordId);
            if (valueRecord is null || valueRecord.ImageBase64 is null) return await TryDeleteRecordAsync<ScratchPadBlob>(syncRecord);

            var uri = new Uri(clientSecrets.BlobStorageUrl);
            try
            {
                string url = clientSecrets.BlobStorageUrl.Replace("?", $"/scratchpad/{syncRecord.RecordId}?");
                string json = JsonSerializer.Serialize(valueRecord);

                
                using var client = httpService.GetImagesHttpClient();
                using var response = await client.PutAsync(url, GetStringContent("application/json", json));

                if (!response.IsSuccessStatusCode)
                {
                    InternetFailed();
                    if (!string.IsNullOrEmpty(response.ReasonPhrase))
                    {
                        _ = TryWriteError(response.ReasonPhrase, nameof(PushScratchPadDataAsync));
                    }
                    return false;
                }
                InternetSucceeded();
                return await TryDeleteRecordAsync<ScratchPadData>(syncRecord);

            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(PushScratchPadBlobAsync));
            }

            return false;
        }

        private async Task<bool> TryDeleteRecordAsync<T>(IDXDBSyncRecord record)
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<T>();
                await idxdb.DeleteSync(store.Name, record.Id);
                if (store.PurgeAfter == 0)
                {
                    await idxdb.DeleteValue(store.Name, record.RecordId);
                }
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(TryDeleteRecordAsync));
                return false;
            }
        }

        #endregion Push Changes

        #region Save and Post

        // These methods save to idxdb value and sync records
        public async Task SaveAndPostImageAsync(ImageInsertInput input)
        {
            try
            {
                IDXDBStoreGetOrThrow<ImageInsertInput>();
                await idxdb.Save(input);
                await cache.PutBlobAsync(input.GeneratedFileName, input.Data!, input.ContentType!);
                SyncCount++;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(SaveAndPostImageAsync));
            }
        }
        public async Task SaveAndPostWorkflowStepAsync(WorkflowStepResultValueSaveInput input)
        {
            try
            {
                IDXDBStoreGetOrThrow<WorkflowStepResultValueSaveInput>();
                await idxdb.Save<WorkflowStepResultValueSaveInput>(input);
                SyncCount++;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(SaveAndPostWorkflowStepAsync));
            }
        }
        public async Task SaveAndPostWorkflowResultCompleteAsync(WorkflowResultCompleteInput input)
        {
            try
            {
                IDXDBStoreGetOrThrow<WorkflowResultCompleteInput>();
                await idxdb.Save<WorkflowResultCompleteInput>(input);
                SyncCount++;

                // remove the workflow if it is not associated with a work order dispatch
                var record = await idxdb.GetValueByKey<WorkflowResultDetailOutput>(input.WorkflowResultId.ToString());
                if (record is not null && (record.ResultData?.WorkflowResult?.WorkOrderDispatchId ?? 0) == 0)
                {
                    await idxdb.DeleteValue("WorkflowResultDetailOutput", input.WorkflowResultId.ToString());
                }

            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(SaveAndPostWorkflowResultCompleteAsync));
            }
        }

        #endregion Save and Post

        public async Task CancelWorkflowAsync(int workflowResultId)
        {
            await idxdb.DeleteValue<WorkflowResultDetailOutput>(workflowResultId.ToString());
        }

        public async Task DeleteWorkflowAsync(int workflowResultId, int dispatchId, int dispatchTechId)
        {
            try
            {
                if (await CheckInternet())
                {
                    var input = new WorkflowResultDeleteInput() { WorkflowResultId = workflowResultId };
                    var httpResponse = await CommandsClient().PostAsync<WorkflowResultDeleteOutput>(input);
                    if (httpResponse.IsSuccess)
                    {
                        await idxdb.DeleteValue<WorkflowResultDetailOutput>(workflowResultId.ToString());

                        var output = await idxdb.GetValueByKey<WorkflowResultListOutput>(dispatchTechId.ToString());
                        if (output != null)
                        {
                            var result = output.ResultData!;
                            if (result != null)
                            {
                                var existing = result.FirstOrDefault(w => w.WorkflowResultId == workflowResultId);
                                if (existing is not null)
                                {
                                    result.Remove(existing);
                                    await idxdb.Save(output);
                                }
                            }
                        }
                        AppState.WorkflowCount--;
                        await SaveAppStateInstance();
                    }
                    else
                    {
                        HandleResponse(httpResponse, nameof(DeleteWorkflowAsync));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(DeleteWorkflowAsync));
            }
        }

        #region Errors

        public async Task<bool> SendError(ServiceErrorInsertInput input)
        {
            try
            {
                var result = await CommandsClient().PostAsync<ServiceErrorInsertOutput>(input);
                if (result.IsSuccess)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(SendError));
            }
            return false;
        }
        public async Task TryWriteError(Exception ex, string methodName)
        {
            await TryWriteError(ex.Message, methodName);
        }
        public async Task TryWriteError(string error, string methodName)
        {
            int maxRecords = 10;
            int userId = 0;

            if (_AppStateInstance is not null && _AppStateInstance.AuthorizedUser is not null)
            {
                userId = _AppStateInstance.AuthorizedUser.Id;
            }

            var errors = await idxdb.GetValueRecords<ServiceErrorInsertInput>() ?? new();
            var existingError = errors.FirstOrDefault(s => s.Error == error && s.Method == methodName);
            if (existingError is not null)
            {
                existingError.DateTimeUTC = DateTime.UtcNow;
                await idxdb.Save(existingError);
            }
            else
            {
                var errorInput = new ServiceErrorInsertInput(Guid.NewGuid(), methodName, error, userId, DateTime.UtcNow);
                errors.Add(errorInput);
                await idxdb.Save<ServiceErrorInsertInput>(errorInput);
            }
            if (errors.Count > maxRecords)
            {
                errors.Sort((a, b) => a.DateTimeUTC!.Value.CompareTo(b.DateTimeUTC!));
                while (errors.Count > maxRecords)
                {
                    var oldestError = errors.First();
                    await idxdb.DeleteValue<ServiceErrorInsertInput>(oldestError.Key);
                    errors.Remove(oldestError);
                }
            }
        }
        public async Task ClearErrors()
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<ServiceErrorInsertInput>();
                var records = await idxdb.GetValueRecords<ServiceErrorInsertInput>();
                if (records is not null)
                {
                    foreach (var record in records)
                    {
                        await idxdb.DeleteValue<ServiceErrorInsertInput>(record.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(ClearErrors));
            }
        }

        #endregion

        #region Helper Methods

        public async Task<T?> GetIDXDBRecord<T>(string key)
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<T>();
                var record = await idxdb.GetValueByKey<T>(key);
                return record ?? default;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetIDXDBRecord));
            }
            return default;
        }

        #endregion



        #region Queries

        private List<string> ServiceQueue = new List<string>();
        private bool ServiceQueueAddItem<T>()
        {
            if (ServiceQueue.Contains(nameof(T)))
            {
                return false;
            }
            ServiceQueue.Add(nameof(T));
            return true;
        }
        private void ServiceQueueRemoveItem<T>()
        {
            if (ServiceQueue.Contains(nameof(T)))
            {
                ServiceQueue.Remove(nameof(T));
            }
        }

        public async Task<O?> GetQueryAsync<O>(string idxdbKey, Action<O>? apiCallback, object apiInput) where O : IDXDBRecordBase
        {
            async Task GetApiData(IDXDBStore store)
            {
                try
                {
                    if (await CheckInternet() && ServiceQueueAddItem<O>())
                    {
                        var response = await QueriesClient().PostAsync<O>(apiInput);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var record = response.Data!;

                            if (record.IsSuccess())
                            {
                                record.SetExpiredDateUTC(store.RefreshAfter);
                                record.Key = idxdbKey;
                                await idxdb.Save(record);
                                if (apiCallback is not null)
                                {
                                    apiCallback(record);
                                }
                            }
                            else
                            {
                                if (typeof(O).Name == nameof(TechKPIListOutput))
                                {
                                    TechKPIListOutput temp = new()
                                    {
                                        ResultData = new(),
                                        ReturnValue = TechKPIListOutput.Returns.Ok,
                                        Key = idxdbKey
                                    };
                                    temp.SetExpiredDateUTC(store.RefreshAfter);
                                    await idxdb.Save(temp);
                                }
                                if (typeof(O).Name == nameof(TechNewsListOutput))
                                {
                                    TechNewsListOutput temp = new()
                                    {
                                        ResultData = new(),
                                        ReturnValue = TechNewsListOutput.Returns.Ok,
                                        Key = idxdbKey
                                    };
                                    temp.SetExpiredDateUTC(store.RefreshAfter);
                                    await idxdb.Save(temp);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, nameof(GetDispatchesForTechnicianAsync));
                }
                finally
                {
                    ServiceQueueRemoveItem<O>();
                }
            }

            try
            {
                var store = IDXDBStoreGetOrThrow<O>();
                var localRecord = await idxdb.GetValueByKey<O>(idxdbKey);

                if (localRecord is null || localRecord.IsExpired()) // apiCallback is not null)
                {
                    if (apiCallback is not null)
                        _ = GetApiData(store);
                    else
                    {
                        await GetApiData(store);
                        localRecord = await idxdb.GetValueByKey<O>(idxdbKey);
                    }
                }

                return localRecord ?? default;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetQueryAsync));
            }

            return default;
        }
        public async Task<List<TechNewsListResult>> GetNewsAsync(Action<List<TechNewsListResult>>? apiCallback, int? employeeId)
        {
            var result = await GetQueryAsync<TechNewsListOutput>(employeeId.GetValueOrDefault(0).ToString(), (e) =>
            {
                if (apiCallback is not null)
                {
                    apiCallback(e.ResultData!);
                }
            }, new TechNewsListInput(employeeId, false));

            return result?.ResultData ?? new();
        }
        public async Task<List<TechKPIListResult>> GetTechKPIListAsync(Action<List<TechKPIListResult>>? apiCallback, int? employeeId)
        {
            var result = await GetQueryAsync<TechKPIListOutput>(employeeId.GetValueOrDefault(0).ToString(), (e) =>
            {
                if (apiCallback is not null)
                {
                    apiCallback(e.ResultData!);
                }
            }, new TechKPIListInput(employeeId, false));

            return result?.ResultData ?? new();
        }
        public async Task<List<CompanyDirectoryResult>> GetCompanyDirectoryAsync(Action<List<CompanyDirectoryResult>>? apiCallback)
        {
            var result = await GetQueryAsync<CompanyDirectoryOutput>("1", (e) =>
            {
                if (apiCallback is not null)
                {
                    apiCallback(e.ResultData!);
                }
            }, new());

            return result?.ResultData ?? new();
        }
        public async Task<List<DispatchListResult>> GetDispatchesForTechnicianAsync(Action<List<DispatchListResult>> apiCallback, int employeeId, int daysAgo = 17)
        {
            var input = new DispatchListInput(employeeId, null, DateTime.Today.AddDays(-daysAgo));
            var result = await GetQueryAsync<DispatchListOutput>(employeeId.ToString(), (e) =>
            {
                if (apiCallback is not null)
                {
                    apiCallback(e.ResultData!);
                }
            }, new DispatchListInput(employeeId, null, DateTime.Today.AddDays(-daysAgo)));

            return result?.ResultData ?? new();
        }

        private async Task<T?> GetAndSaveApiResult<T>(string key, object input) where T : IDXDBRecordBase
        {
            try
            {
                if (await CheckInternet())
                {
                    var result = await QueriesClient().PostAsync<T>(input);
                    if (result.IsSuccess)
                    {
                        var output = result.Data!;
                        if (output.IsSuccess())
                        {
                            var store = IDXDBStoreGetOrThrow<T>();
                            output.SetExpiredDateUTC(store.RefreshAfter);
                            output.Key = key;
                            await idxdb.Save<T>(output);
                            return output;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetAndSaveApiResult));
            }

            return default;
        }
        public async Task<List<WorkflowResultListResult>?> GetWorkflowResultList(int dispatchId, int dispatchTechId)
        {
            var input = new WorkflowResultListInput()
            {
                WorkOrderDispatchId = dispatchId,
                WorkOrderDispatchTechId = dispatchTechId
            };

            var result = await GetAndSaveApiResult<WorkflowResultListOutput>(dispatchTechId.ToString(), input);

            return result?.ResultData ?? new();
        }

        public async Task<List<WorkflowListAvailableResult>?> GetWorkflowListAvailable(int dispatchId, int dispatchTechId)
        {
            try
            {
                var input = new WorkflowListAvailableInput
                {
                    WorkOrderDispatchId = dispatchId,
                    WorkOrderDispatchTechId = dispatchTechId
                };

                var result = await GetAndSaveApiResult<WorkflowListAvailableOutput>(dispatchTechId.ToString(), input);

                return result?.ResultData ?? default;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetWorkflowListAvailable));
            }

            return default;
        }

        public async Task<LookupsResult?> GetLookupsAsync()
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<LookupsOutput>();
                var record = await idxdb.GetValueByKey<LookupsOutput>("1");
                if (record is null || record.IsExpired())
                {
                    if (await CheckInternet())
                    {
                        var response = await QueriesClient().PostAsync<LookupsOutput>();

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            record = response.Data!;
                            record.ResultData ??= new();
                            record.ResultData.AttributeResult ??= new();
                            record.ResultData.EquipmentManufacturerResult ??= new();
                            record.ResultData.EquipmentTypeAttributeResult ??= new();
                            record.ResultData.EquipmentTypeResult ??= new();
                            record.ResultData.EquipmentLocationResult ??= new();
                            record.SetExpiredDateUTC(store.RefreshAfter);
                            await idxdb.Save<LookupsOutput>(record);
                        }
                    }

                }
                return record is null ? null : record.ResultData!;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetLookupsAsync));
            }
            return default;
        }

        private async Task<ClientSecretsResult?> GetClientSecrets()
        {
            try
            {
                IDXDBStoreGetOrThrow<ClientSecretsOutput>();
                var record = await idxdb.GetValueByKey<ClientSecretsOutput>("1");
                if (record is null || IsExpiring(record))
                {
                    record = await GetAndSaveApiResult<ClientSecretsOutput>("1", null);
                }
                return record?.ResultData;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetLookupsAsync));
            }

            return default;
        }

        private bool IsExpiring(ClientSecretsOutput record)
        {
            int days = 3;

            if (record is not null && record.ResultData is not null && !string.IsNullOrEmpty(record.ResultData?.BlobStorageUrl))
            {
                var query = System.Web.HttpUtility.ParseQueryString(new Uri(record.ResultData.BlobStorageUrl).Query);
                return !DateTime.TryParse(query["se"], out var expires) || expires < DateTime.UtcNow.AddDays(days);
            }

            return false;
        }

        public async Task<VersionTrackerResult> GetVersionTrackerAsync(Action<VersionTrackerResult>? apiCallback)
        {

            try
            {
                var store = IDXDBStoreGetOrThrow<VersionTrackerOutput>();
                var localRecord = await idxdb.GetValueByKey<VersionTrackerOutput>("1");

                async Task GetApiData()
                {
                    try
                    {
                        if (ServiceQueue.Contains(nameof(GetVersionTrackerAsync))) return;

                        ServiceQueue.Add(nameof(GetVersionTrackerAsync));
                        var response = await httpService.GetHttpQueriesClient(AppState.AuthorizedUser!).PostAsync<VersionTrackerOutput>();

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var record = response.Data!;
                            if (record.ReturnValue == VersionTrackerOutput.Returns.Ok)
                            {
                                record.SetExpiredDateUTC(store.RefreshAfter);
                                await idxdb.Save(record);
                                if (apiCallback is not null)
                                {
                                    apiCallback.Invoke(record.ResultData!);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, nameof(GetVersionTrackerAsync));
                    }
                    finally
                    {
                        ServiceQueue.Remove(nameof(GetVersionTrackerAsync));
                    }
                }
                if (localRecord is null || localRecord.IsExpired())
                {
                    _ = GetApiData();
                }

                return localRecord?.ResultData ?? new("0");
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetVersionTrackerAsync));
                return new("0");
            }
        }
        public async Task<WorkflowResultDetailOutput> GetWorkflowAsync(int id)
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<WorkflowResultDetailOutput>();
                var record = await idxdb.GetValueByKey<WorkflowResultDetailOutput>(id.ToString());

                if (record is null || record.IsExpired())
                {
                    if (await CheckInternet())
                    {
                        var httpResult = await QueriesClient().PostAsync<WorkflowResultDetailOutput>(new WorkflowResultDetailInput(id));
                        if (httpResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            record = httpResult.Data!;
                            record.ResultData!.WorkflowStepOptionResult ??= new();
                            record.ResultData.WorkflowStepValueResult ??= new();
                            record.SetExpiredDateUTC(store.RefreshAfter);
                            record.Key = id.ToString();
                            await idxdb.Save<WorkflowResultDetailOutput>(record);

                            foreach (var step in record.ResultData.WorkflowStepResult!)
                            {
                                if (step.PresentationMedia is not null)
                                {
                                    List<ImageFileArrayItem>? imageList = JsonSerializer.Deserialize<List<ImageFileArrayItem>>(step.PresentationMedia);
                                    if (imageList is not null)
                                    {
                                        foreach (var image in imageList)
                                        {
                                            await TryHydrateAndCachePresentationMedia(image);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            HandleResponse(httpResult, nameof(GetWorkflowAsync));
                        }
                    }
                }

                return record ??
                    new WorkflowResultDetailOutput()
                    {
                        ReturnValue = WorkflowResultDetailOutput.Returns.NotFound
                    };
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetWorkflowAsync));
                return new WorkflowResultDetailOutput()
                {
                    ReturnValue = WorkflowResultDetailOutput.Returns.NotFound
                };
            }
        }
        public async Task<List<int>> GetLocalDispatchIds()
        {
            try
            {
                IDXDBStoreGetOrThrow<DispatchDetailOutput>();
                var result = new List<int>();
                var records = await idxdb.GetValueRecords<DispatchDetailOutput>();
                if (records is not null)
                {
                    foreach (var record in records)
                    {
                        result.Add((int)record.ResultData!.DispatchResult!.Id);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(GetLocalDispatchIds));
                return new();
            }
        }
        public async Task<Dictionary<int, int>> GetLocalDispatchTechIds()
        {
            try
            {
                IDXDBStoreGetOrThrow<DispatchDetailOutput>();
                Dictionary<int, int> dispatchIds = new Dictionary<int, int>();
                var records = await idxdb.GetValueRecords<DispatchDetailOutput>();
                if (records is not null)
                {
                    foreach (var record in records)
                    {
                        if (record.ResultData is not null && record.ResultData.DispatchResult is not null && record.ResultData.DispatchTechsResult is not null)
                        {
                            foreach (var subitem in record.ResultData.DispatchTechsResult)
                            {
                                if (!dispatchIds.ContainsKey((int)subitem.Id))
                                {
                                    dispatchIds.Add((int)subitem.Id, (int)record.ResultData.DispatchResult.Id);
                                }
                            }
                        }
                    }
                }
                return dispatchIds;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetLocalDispatchTechIds));
                return new();
            }
        }

        public async Task UpdateDispatchListItem(int id, int dispatchTechId, string status, DateTimeOffset? workStart, DateTimeOffset? workStop, DateTimeOffset? dispatched)
        {
            var output = await idxdb.GetValueByKey<DispatchListOutput>(id.ToString());
            if (output is not null)
            {
                var list = output.ResultData!;

                var dispatchListItem = list.FirstOrDefault(d => d.Id == dispatchTechId);
                if (dispatchListItem is not null)
                {
                    dispatchListItem.Status = status;
                    dispatchListItem.WorkStart = workStart;
                    dispatchListItem.Dispatched = dispatched;
                    dispatchListItem.WorkStop = workStop;
                    output.Key = id.ToString();
                    await idxdb.Save(output);
                }
            }
        }
        public async Task<DispatchDetailResult?> GetDispatchDataAsync(int dispatchId, int dispatchTechId, bool forceRefresh)
        {
            try
            {
                var store = IDXDBStoreGetOrThrow<DispatchDetailOutput>();
                DispatchDetailOutput localRecord = await idxdb.GetValueByKey<DispatchDetailOutput>(dispatchId.ToString());

                if (localRecord is null || localRecord.IsExpired() || forceRefresh)
                {
                    var input = new DispatchDetailInput(dispatchId, dispatchTechId);
                    var response = await QueriesClient().PostAsync<DispatchDetailOutput>(input);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        DispatchDetailOutput output = response.Data!;
                        if (output.ReturnValue == DispatchDetailOutput.Returns.Ok)
                        {
                            output.ResultData!.DispatchTechsResult ??= new();
                            output.ResultData.CustomerLocationContactResult ??= new();
                            output.ResultData.EquipmentResult ??= new();
                            output.ResultData.EquipmentAttributeValueResult ??= new();
                            output.ResultData.CustomerAttributeResult ??= new();
                            output.ResultData.CustomerLocationAttributeResult ??= new();
                            output.ResultData.WorkOrderTaskCodeAttributeResult ??= new();
                            output.ResultData.AllAttributeValueResult ??= new();

                            if (localRecord is not null)
                            {
                                var localCopy = localRecord.ResultData!;
                                var localEquipmentList = localCopy.EquipmentResult!;
                                var localEquipmentAttributeList = localCopy.EquipmentAttributeValueResult!;

                                foreach (DispatchDetailResult.Equipment localEquipment in localEquipmentList)
                                {
                                    DispatchDetailResult.Equipment? serverEquipment;

                                    if (localEquipment.Id == 0)
                                    {
                                        serverEquipment = output.ResultData!.EquipmentResult.FirstOrDefault(e => e.TemporaryId == localEquipment.TemporaryId);
                                    }
                                    else
                                    {
                                        serverEquipment = output.ResultData!.EquipmentResult.FirstOrDefault(e => e.Id == localEquipment.Id);
                                    }

                                    if (serverEquipment is null)
                                    {
                                        // Equipment that only exists locally gets added

                                        output.ResultData!.EquipmentResult.Add(localEquipment);

                                        var localEquipmentAttributes = localEquipmentAttributeList.FindAll(a => a.EquipmentUid == localEquipment.TemporaryId);
                                        if (localEquipmentAttributes.Count != 0)
                                        {
                                            output.ResultData!.EquipmentAttributeValueResult.AddRange(localEquipmentAttributes);
                                        }
                                    }
                                    else
                                    {
                                        // Equipment with local and server versions

                                        if (localEquipment.IsDirty == true)
                                        {
                                            if (localEquipment.ModifiedDate > serverEquipment.ModifiedDate)
                                            {
                                                WorkflowEquipmentBuilder.UpdateEquipment(localEquipment, serverEquipment);
                                            }

                                            List<DispatchDetailResult.EquipmentAttributeValue>? localEquipmentAttributeValues;

                                            if (localEquipment.Id == 0)
                                            {
                                                localEquipmentAttributeValues = localEquipmentAttributeList.FindAll(a => a.EquipmentUid == localEquipment.TemporaryId);
                                            }
                                            else
                                            {
                                                localEquipmentAttributeValues = localEquipmentAttributeList.FindAll(a => a.EquipmentId == localEquipment.Id);
                                            }

                                            foreach (var localEquipmentAttributeValue in localEquipmentAttributeValues)
                                            {
                                                DispatchDetailResult.EquipmentAttributeValue? serverEquipmentAttributeValue;
                                                if (localEquipment.Id == 0)
                                                {
                                                    serverEquipmentAttributeValue = output.ResultData!.EquipmentAttributeValueResult.FirstOrDefault(a => a.EquipmentUid == serverEquipment.TemporaryId);
                                                }
                                                else
                                                {
                                                    serverEquipmentAttributeValue = output.ResultData!.EquipmentAttributeValueResult.FirstOrDefault(a => a.EquipmentId == serverEquipment.Id);
                                                }

                                                if (serverEquipmentAttributeValue is null)
                                                {
                                                    output.ResultData!.EquipmentAttributeValueResult.Add(localEquipmentAttributeValue);
                                                }
                                                else
                                                {
                                                    if (localEquipmentAttributeValue.LastModifiedDate > serverEquipmentAttributeValue.LastModifiedDate)
                                                    {
                                                        WorkflowEquipmentBuilder.UpdateEquipmentAttributeValue(localEquipmentAttributeValue, serverEquipmentAttributeValue);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                foreach (var localAttribute in localCopy.AllAttributeValueResult!)
                                {
                                    var serverAttribute = output.ResultData.AllAttributeValueResult.FirstOrDefault(a => a.Id == localAttribute.Id);
                                    if (serverAttribute is null)
                                    {
                                        output.ResultData.AllAttributeValueResult.Add(localAttribute);
                                    }
                                    else
                                    {
                                        if (localAttribute.LastModifiedDate > serverAttribute.LastModifiedDate)
                                        {
                                            UpdateAllAttributeValue(localAttribute, serverAttribute);
                                        }
                                    }
                                }
                            }
                        }

                        localRecord = output;
                        localRecord.Key = dispatchId.ToString();
                        localRecord.SetExpiredDateUTC(store.RefreshAfter);
                        await idxdb.Save<DispatchDetailOutput>(localRecord);
                    }
                }

                return localRecord?.ResultData ?? default;
            }
            catch (Exception ex)
            {
                HandleException(ex, nameof(GetDispatchDataAsync));
            }

            return default;
        }

        #endregion Queries


        #region Server Only Queries

        public async Task<WorkflowSequenceQueryValueResult?> WorkflowSequenceQueryValueAsync(WorkflowSequenceQueryValueInput input)
        {
            var httpResult = await QueriesClient().PostAsync<WorkflowSequenceQueryValueOutput>(input);
            if (httpResult.IsSuccess)
            {
                var output = httpResult.Data;
                return output!.ResultData;
            }
            return null;
        }


        #endregion Server Only Queries

        public async Task<bool> TryHydrateFromCache(PresentationMedia presentationMedia)
        {
            string? result = await cache.GetStringAsync(presentationMedia.GeneratedFileName);
            if (result is not null)
            {
                presentationMedia.Base64 = result;
                return true;
            }
            return false;
        }



        private string BlobUrl(string fileName)
        {
            return $"{settings.ImageBaseUrl}{fileName}?data={DateTime.Now.Ticks}";
        }
        private async Task TryHydrateAndCachePresentationMedia(ImageFileArrayItem item)
        {
            if (await CheckInternet())
            {
                if (!string.IsNullOrEmpty(item.ContentType) && !item.ContentType.Contains("image"))
                {
                    // Use the Icon for non-images
                    item.Data = GeneralFunctions.GetPdfLogoBytes();
                    await cache.PutStringAsync(item.GeneratedFileName, item.Base64Version("image/png"));
                }
                else
                {
                    // Hydrate the presentation media from Blob Storage
                    try
                    {
                        
                        using var client = httpService.GetImagesHttpClient();
                        using var response = await client.GetAsync(BlobUrl(item.GeneratedFileName), HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                        {
                            item.Data = await response.Content.ReadAsByteArrayAsync();
                            await cache.PutStringAsync(item.GeneratedFileName, item.Base64Version());
                            InternetSucceeded();
                        }
                        else
                        {
                            InternetFailed();
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, nameof(TryHydrateAndCachePresentationMedia));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the workflows that are currently loaded in idxdb
        /// </summary>
        /// <param name="dispatchTeckId">Current dispatch tech id, null to include all records</param>
        /// <returns>Returns list of the workflow portion of the WorkflowResultDetailResult</returns>
        public async Task<List<WorkflowResultDetailResult.Workflow>> GetLocalWorkflows(int? dispatchTeckId = null, int? workflowId = null, int? employeeId = null)
        {
            List<WorkflowResultDetailResult.Workflow> result = new List<WorkflowResultDetailResult.Workflow>();
            var output = await idxdb.GetValueRecords<WorkflowResultDetailOutput>();
            if (output is not null)
            {
                foreach (var item in output)
                {
                    if (item is not null && item.ResultData is not null && item.ResultData.WorkflowResult is not null)
                    {
                        if (!dispatchTeckId.HasValue && !workflowId.HasValue && !employeeId.HasValue)
                        {
                            result.Add(item.ResultData.WorkflowResult);
                        }
                        else if (dispatchTeckId.HasValue && item.ResultData.WorkflowResult.WorkOrderDispatchTechId.Equals(dispatchTeckId))
                        {
                            result.Add(item.ResultData.WorkflowResult);
                        }
                        else if (workflowId.HasValue && item.ResultData.WorkflowResult.WorkflowId.Equals(workflowId))
                        {
                            if (employeeId.HasValue && item.ResultData.WorkflowResult.EmployeeId.Equals(employeeId))
                            {
                                result.Add(item.ResultData.WorkflowResult);
                            }
                            else if (!employeeId.HasValue)
                            {
                                result.Add(item.ResultData.WorkflowResult);
                            }
                        }
                        else if (employeeId.HasValue && item.ResultData.WorkflowResult.EmployeeId.Equals(employeeId))
                        {
                            result.Add(item.ResultData.WorkflowResult);
                        }
                    }
                }
            }
            return result;
        }
        public void UpdateAllAttributeValue(DispatchDetailResult.AllAttributeValue source, DispatchDetailResult.AllAttributeValue destination)
        {
            destination.Value = source.Value;
            destination.LastModifiedDate = source.LastModifiedDate;
        }
        public async Task SaveDispatchAsync(DispatchDetailResult dispatch)
        {
            var valueRecord = await idxdb.GetValueByKey<DispatchDetailOutput>(dispatch.DispatchResult!.Id.ToString());
            if (valueRecord is not null)
            {
                valueRecord.ResultData = dispatch;
                await idxdb.Save<DispatchDetailOutput>(valueRecord);
            }
        }
        public async Task SaveWorkflowAsync(WorkflowResultDetailOutput workflow)
        {
            await idxdb.Save<WorkflowResultDetailOutput>(workflow);
        }

        public async Task SaveAndPostSubscriptionAsync(NotificationSubscription subscription)
        {
            try
            {
                IDXDBStoreGetOrThrow<AuthorizedUserSubscriptionSaveInput>();
                var input = new AuthorizedUserSubscriptionSaveInput(subscription.AuthUserId, subscription.AuthUserId, JsonSerializer.Serialize(subscription));
                await idxdb.Save(input);
            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(SaveAndPostSubscriptionAsync));
            }
        }

        public async Task<List<WorkflowDataQueryOptionsResult>?> GetWorkflowDataQueryOptionsAsync(WorkflowDataQueryOptionsInput input)
        {
            var httpResult = await httpService.GetHttpQueriesClient(AppState.AuthorizedUser!).PostAsync<WorkflowDataQueryOptionsOutput>(input);
            if (httpResult.IsSuccess)
            {
                var output = httpResult.Data;
                return output!.ResultData;
            }
            return null;
        }

        public async Task<string?> QueryWorkflowStepResultValueAsync(int workflowResultId, int workflowStepId, int loopIndex)
        {
            var valueRecord = await idxdb.GetValueByKey<WorkflowResultDetailOutput>(workflowResultId.ToString());
            if (valueRecord is not null)
            {
                var step = valueRecord.ResultData!.WorkflowStepValueResult?.FirstOrDefault(s => s.WorkflowStepResultId == workflowStepId && s.LoopIndex == loopIndex);
                if (step is not null)
                {
                    return step.Value;
                }
            }
            return null;
        }

        public async Task<List<ServiceErrorInsertInput>> GetErrors()
        {
            return await idxdb.GetValueRecords<ServiceErrorInsertInput>();
        }
        private async Task<bool> TryHydrateImageFromCacheAsync(ImageInsertInput input)
        {
            return await cache.TryHydrateImage(input);
        }



        private async Task<bool> HydrateImageFromServiceII(ImageInsertInput input)
        {
            try
            {
                
                using var client = httpService.GetImagesHttpClient();
                using var response = await client.GetAsync(BlobUrl(input.GeneratedFileName), HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    input.Data = await response.Content.ReadAsByteArrayAsync();
                    InternetSucceeded();
                    return true;
                }
                else
                {
                    InternetFailed();
                    
                }
                
            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(HydrateImageFromServiceII));
            }

            return false;
        }



        public async Task HydrateImageAsync(ImageInsertInput input)
        {
            try
            {
                if (await TryHydrateImageFromCacheAsync(input)) return;
                if (await HydrateImageFromServiceII(input))
                {
                    await cache.PutBlobAsync(input.GeneratedFileName, input.Data!, input.ContentType!);
                }
                else
                {
                    if (input.GeneratedFileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    input.GeneratedFileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        input.Data = notFoundImage64Jpg;
                        input.ContentType = "image/jpeg";
                    }
                    else
                    {
                        input.Data = notFoundImage64png; // PNG fallback
                        input.ContentType = "image/png";
                    }
                }
            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(HydrateImageAsync));
            }
        }
        public async Task DeleteImageAsync(ImageInsertInput input)
        {
            try
            {
                if (input?.Data is not null && !string.IsNullOrWhiteSpace(input.GeneratedFileName))
                {
                    await idxdb.DeleteValue<ImageInsertInput>(input.GeneratedFileName);
                }

            }
            catch (Exception ex)
            {
                await TryWriteError(ex, nameof(DeleteImageAsync));
            }
        }

        public void Dispose()
        {
            syncTimer.Elapsed -= SyncTimer_Elapsed;
            syncTimer.Dispose();
        }

        #region Helpers
        private IDXDBStore IDXDBStoreGetOrThrow<T>()
        {
            return IDXDBStoreGetOrThrow(typeof(T).Name);
        }
        private IDXDBStore IDXDBStoreGetOrThrow(string storeName)
        {
            var store = idxdb.Definition.Stores.FirstOrDefault(s => s.Name == storeName);
            if (store is null)
            {
                throw new InvalidOperationException("The store name is not registered in program.cs");
            }
            return store;
        }
        private HttpCommands CommandsClient()
        {
            return httpService.GetHttpCommandsClient(AppState.AuthorizedUser!);
        }
        private HttpQueries QueriesClient()
        {
            return httpService.GetHttpQueriesClient(AppState.AuthorizedUser!);
        }

        private HttpPing PingClient()
        {
            return httpService.GetHttpPingClient();
        }

        #endregion Helpers

        #region Bluon

        // You could return BluonCacheByIdOutput so you have access to the full record
        public async Task<string?> GetIfCachedAsync(int urlHash, bool allowStale = false)
        {
            string key = urlHash.ToString();

            var result = await idxdb.GetValueByKey<BluonCacheByIdOutput>(key);
            if (result is not null)
            {
                return result.ResultData!.JsonData!;
            }

            var httpResult = await httpService.GetHttpQueriesClient(AppState.AuthorizedUser!).PostAsync<BluonCacheByIdOutput>(new BluonCacheByIdInput(urlHash, allowStale));
            if (httpResult.IsSuccess)
            {
                var output = httpResult.Data!;
                if (output.ReturnValue == BluonCacheByIdOutput.Returns.Ok)
                {
                    output.Key = urlHash.ToString();
                    await idxdb.Save<BluonCacheByIdOutput>(output);
                    return output.ResultData!.JsonData;
                }
            }
            return null;
        }

        public async Task<string?> GetIfCachedByURLAsync(string url, bool allowStale = false)
        {
            return await GetIfCachedAsync(SQLHashFunctions.GetDeterministicHashCodeSQL(url), allowStale);
        }

        public async Task SaveCacheCopyAsync(int urlHash, string url, string json, int expirationMinutes = 2880)
        {
            var store = IDXDBStoreGetOrThrow<BluonCacheByIdOutput>();
            BluonCacheByIdOutput output = new BluonCacheByIdOutput()
            {
                ReturnValue = BluonCacheByIdOutput.Returns.Ok,
                ResultData = new(urlHash, url, json, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(store.RefreshAfter))
            };

            // save to indexdb
            output.Key = urlHash.ToString();
            output.SetExpiredDateUTC(store.RefreshAfter);
            await idxdb.Save<BluonCacheByIdOutput>(output);


            // save to server cache to be kind

            var BluonServerCacheSave = httpService.GetHttpCommandsClient(AppState.AuthorizedUser!).PostAsync<BluonCacheSaveOutput>(new BluonCacheSaveInput()
            {
                URLHash = urlHash,
                JsonData = json,
                URL = url,
            });

            if (BluonServerCacheSave.IsCompletedSuccessfully && BluonServerCacheSave.Result.IsSuccess && BluonServerCacheSave.Result?.Data is not null)
            {
                if (BluonServerCacheSave.Result.Data.ReturnValue != BluonCacheSaveOutput.Returns.Inserted)
                {
                    Console.WriteLine("Bluon Cache Save failure: {BluonServerCacheSave.Result.Data.ReturnValue}");
                }
            }

        }

        #endregion
        #region Logs (For development testing)

        private LogInsertInput LogInsertInput(string method, string data)
        {
            return new LogInsertInput()
            {
                UId = Guid.NewGuid(),
                Method = method,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Writes to the Log Tables in idxdb
        /// </summary>
        private void DebugLog(string method, string info)
        {
            if (settings.DebugMode)
            {
                _ = idxdb.Save<LogInsertInput>(LogInsertInput(method, info.Length > 512 ? info.Substring(0, 512) : info));
            }
        }

        /// <summary>
        /// Writes to the Log Tables in idxdb
        /// </summary>
        private void DebugLogError(string method, Exception ex)
        {
            DebugLog(method, ex.Message);
        }

        #endregion Logs

        #region Cache
        private async Task PurgeCache()
        {
            if (AppState.LastCachePurge is null || AppState.LastCachePurge.Value.AddDays(1) < DateTime.UtcNow)
            {
                await cache.Purge();
                AppState.LastCachePurge = DateTime.UtcNow;
                await SaveAppStateInstance();
            }
        }

        #endregion

        #region NotFouind Images
        public static readonly byte[] notFoundImage64png = new byte[]
{
    137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,1,0,0,0,1,0,8,2,0,0,0,211,16,63,
    49,0,0,20,65,73,68,65,84,120,156,237,157,235,111,27,85,222,199,127,190,197,137,237,36,110,110,144,123,147,52,73,
    73,67,47,9,109,233,131,104,11,5,66,160,20,74,91,169,18,2,196,127,176,47,34,132,86,240,106,165,213,46,91,105,
    95,172,246,213,22,193,11,222,64,225,209,163,114,41,208,110,65,165,64,75,105,46,219,146,100,99,183,105,210,52,113,83,
    231,214,36,118,28,199,246,243,226,208,195,212,177,29,199,73,60,51,231,124,63,66,226,120,60,57,158,129,239,103,230,156,
    25,251,55,134,11,23,46,16,0,178,98,84,123,3,0,80,19,51,251,87,123,123,187,186,219,1,64,154,105,106,106,34,
    156,1,128,228,152,149,47,152,19,0,136,141,114,188,131,51,0,144,26,8,0,164,6,2,0,169,129,0,64,106,32,0,
    144,26,8,0,164,6,2,0,169,129,0,64,106,32,0,144,26,8,0,164,6,2,0,169,129,0,64,106,32,0,144,26,
    8,0,164,6,2,0,169,129,0,64,106,32,0,144,26,8,0,164,6,2,0,169,129,0,64,106,32,0,144,26,8,0,
    164,6,2,0,169,129,0,64,106,32,0,144,26,8,0,164,6,2,0,169,129,0,64,106,32,0,144,26,8,0,164,6,
    2,0,169,129,0,64,106,32,0,144,26,8,0,164,70,77,1,188,109,109,42,126,58,208,14,42,38,65,53,1,216,62,
    195,1,160,110,18,212,17,64,185,183,112,64,102,84,79,130,58,2,20,28,59,166,124,9,7,228,36,234,255,123,84,42,
    210,131,106,67,32,56,32,57,90,72,63,169,59,9,134,3,210,162,145,244,147,234,151,65,225,128,132,104,39,253,164,186,
    0,4,7,36,67,83,233,39,45,8,64,112,64,26,180,150,126,210,136,0,4,7,36,64,131,233,39,237,8,64,112,64,
    104,180,153,126,210,148,0,4,7,4,69,179,233,39,173,9,64,112,64,56,180,156,126,210,160,0,4,7,4,66,227,233,
    39,109,10,64,112,64,8,180,159,126,210,172,0,4,7,116,142,46,210,79,90,22,128,224,128,110,209,75,250,73,227,2,
    16,28,208,33,58,74,63,105,95,0,130,3,186,66,95,233,39,93,8,64,112,64,39,232,46,253,164,23,1,8,14,104,
    30,61,166,159,116,36,0,193,1,13,163,211,244,147,190,4,32,56,160,73,244,155,126,210,157,0,4,7,52,134,174,211,
    79,122,20,128,224,128,102,208,123,250,73,167,2,16,28,208,0,2,164,159,244,43,0,193,1,85,17,35,253,164,107,1,
    8,14,168,132,48,233,39,189,11,64,112,32,237,136,148,126,18,64,0,130,3,105,68,176,244,147,24,2,16,28,72,11,
    226,165,159,132,17,128,224,192,26,35,100,250,73,36,1,8,14,172,25,162,166,159,4,19,128,224,192,26,32,112,250,73,
    60,1,8,14,172,42,98,167,159,132,20,128,224,192,42,33,124,250,73,84,1,8,14,172,24,25,210,79,2,11,64,112,
    96,5,72,146,126,18,91,0,130,3,41,33,79,250,73,120,1,8,14,44,19,169,210,79,50,8,64,112,32,105,100,75,
    63,73,34,0,193,129,36,144,48,253,36,143,0,4,7,18,34,103,250,73,42,1,8,14,196,65,218,244,147,108,2,16,
    28,88,132,204,233,39,9,5,32,56,160,64,242,244,147,156,2,16,28,32,34,164,159,136,164,21,128,164,119,0,233,103,
    200,43,0,73,236,0,210,207,145,90,0,146,210,1,164,95,137,236,2,144,100,14,32,253,81,64,0,34,105,28,64,250,
    23,3,1,126,67,120,7,144,254,152,64,128,223,17,216,1,164,63,30,16,224,62,132,116,0,233,79,0,4,136,70,48,
    7,144,254,196,64,128,24,8,227,0,210,191,36,16,32,54,2,56,128,244,39,3,4,136,139,174,29,64,250,147,4,2,
    36,66,167,14,32,253,201,3,1,150,64,119,14,32,253,203,2,2,44,141,142,28,64,250,151,11,4,72,10,93,56,128,
    244,167,0,4,72,22,141,59,128,244,167,134,89,237,13,88,19,222,123,239,189,72,36,194,218,14,135,227,200,145,35,38,
    147,137,191,123,233,210,165,174,174,46,214,126,252,241,199,235,235,235,147,236,182,224,216,49,101,206,188,109,109,139,115,166,
    252,232,195,135,15,59,157,206,148,246,96,121,32,253,41,35,254,25,96,102,102,166,167,167,103,181,122,211,224,121,0,233,
    95,9,226,11,64,68,157,157,157,193,96,112,181,122,211,148,3,72,255,10,145,66,128,185,185,185,43,87,174,172,98,135,
    26,113,0,233,95,57,98,206,1,22,115,229,202,149,134,134,134,204,204,204,196,171,205,207,207,247,246,246,14,14,14,78,
    76,76,4,131,65,179,217,156,147,147,83,86,86,214,208,208,96,179,217,148,107,38,51,31,224,76,76,76,124,250,233,167,
    172,93,89,89,185,119,239,222,203,151,47,247,247,247,207,205,205,101,103,103,215,215,215,55,54,54,26,12,134,27,55,110,
    92,189,122,117,108,108,44,18,137,228,229,229,109,218,180,169,166,166,70,217,207,212,212,212,224,224,224,208,208,208,228,228,
    164,127,102,134,234,235,45,225,176,35,16,120,96,118,118,123,44,9,61,30,79,71,71,199,232,232,168,178,195,247,223,127,
    63,20,10,17,81,86,86,214,43,175,188,162,92,63,24,12,242,221,159,159,159,183,88,44,235,214,173,171,174,174,174,175,
    175,87,78,162,68,66,112,1,44,22,75,78,78,206,216,216,88,48,24,236,236,236,124,244,209,71,19,172,60,58,58,122,
    230,204,25,159,207,199,151,204,207,207,123,189,94,175,215,251,235,175,191,238,221,187,183,178,178,82,185,254,178,28,80,246,
    249,217,103,159,141,143,143,179,151,147,147,147,23,47,94,28,31,31,183,217,108,124,106,206,54,102,116,116,116,126,126,254,
    161,135,30,226,11,79,156,56,241,123,71,6,3,17,5,76,166,128,205,54,102,179,13,124,242,201,11,47,188,224,112,56,
    248,251,125,125,125,223,127,255,61,159,145,179,14,239,220,185,19,111,195,188,94,239,233,211,167,103,103,103,249,146,64,32,
    224,241,120,60,30,79,111,111,111,75,75,139,221,110,95,114,239,116,135,224,67,32,131,193,176,125,251,118,214,238,233,233,
    153,153,153,137,183,230,236,236,236,215,95,127,205,210,111,181,90,91,91,91,223,120,227,141,23,95,124,49,59,59,155,136,
    130,193,224,217,179,103,199,198,198,162,254,42,133,177,208,200,200,136,205,102,59,122,244,232,209,163,71,115,115,115,217,66,
    151,203,213,213,213,181,121,243,230,87,95,125,245,249,231,159,55,26,127,251,255,210,222,222,174,252,219,252,252,252,93,187,
    118,237,189,113,163,213,229,58,240,223,255,182,92,187,246,144,215,203,183,255,226,197,139,124,205,169,169,169,31,126,248,129,
    165,159,239,78,107,107,107,95,95,31,59,252,71,225,243,249,190,250,234,43,150,254,204,204,204,103,158,121,230,245,215,95,
    111,109,109,101,231,204,241,241,241,111,190,249,134,187,36,18,130,11,64,68,101,101,101,197,197,197,68,20,10,133,46,95,
    190,28,111,181,174,174,174,64,32,192,218,219,182,109,43,45,45,53,153,76,133,133,133,59,118,236,96,11,67,161,208,47,
    191,252,178,248,15,163,143,250,75,165,196,104,52,238,217,179,199,225,112,56,28,142,245,235,215,243,229,78,167,115,199,142,
    29,86,171,181,184,184,184,168,168,136,45,244,251,253,211,211,211,124,157,131,7,15,62,240,254,251,206,185,57,107,40,100,
    140,68,178,130,193,199,222,122,107,221,186,117,236,221,155,55,111,242,140,94,185,114,133,7,125,235,214,173,108,119,74,75,
    75,183,110,221,26,111,247,231,230,230,88,123,215,174,93,21,21,21,22,139,165,180,180,244,145,71,30,97,11,199,198,198,
    174,93,187,150,120,215,244,136,248,2,16,17,63,9,184,221,238,201,201,201,152,235,12,14,14,242,118,85,85,21,111,87,
    86,86,242,227,241,240,240,112,204,195,231,178,102,159,121,121,121,89,89,89,172,173,156,147,148,149,149,241,54,95,129,136,
    148,215,175,134,222,122,171,167,176,240,92,101,229,151,181,181,39,235,235,255,111,227,198,227,199,143,79,76,76,176,119,23,
    22,22,120,136,111,221,186,197,255,74,169,153,114,215,148,12,12,12,176,134,209,104,84,142,244,30,124,240,193,197,235,136,
    132,224,115,0,70,81,81,81,101,101,229,192,192,64,36,18,185,116,233,210,226,155,83,145,72,132,143,142,12,6,131,114,
    190,107,52,26,51,51,51,217,208,40,20,10,249,124,62,54,40,138,34,106,62,144,0,101,184,149,51,75,229,135,134,195,
    97,229,182,177,198,181,183,223,62,95,85,21,76,56,25,229,127,168,28,202,43,199,238,49,199,241,202,221,15,135,195,31,
    124,240,65,204,206,239,222,189,155,224,163,117,138,20,2,16,209,246,237,219,7,7,7,35,145,200,192,192,192,252,252,252,
    90,124,68,146,14,240,243,73,146,203,25,222,182,182,255,84,84,240,244,63,252,240,195,155,55,111,102,46,157,60,121,114,
    116,116,116,249,219,187,108,86,241,94,138,118,144,69,0,167,211,89,91,91,219,215,215,71,68,35,35,35,81,239,26,12,
    6,135,195,193,142,130,145,72,196,231,243,241,35,101,56,28,230,227,10,147,201,20,117,49,52,138,130,99,199,232,248,113,
    254,114,226,221,119,157,127,254,243,202,55,222,219,214,22,33,26,191,247,209,153,153,153,59,119,238,228,239,198,156,217,59,
    28,14,126,192,246,251,253,124,119,148,103,6,142,114,247,205,102,243,107,175,189,150,216,70,145,144,101,63,137,168,185,185,
    57,193,197,236,138,138,10,222,238,239,239,231,237,129,129,1,62,174,40,41,41,89,250,114,184,193,160,124,181,242,123,100,
    147,127,255,59,235,150,79,174,149,233,28,25,25,81,94,183,229,148,150,150,242,182,114,236,174,220,53,37,124,247,23,22,
    22,132,28,235,199,67,34,1,236,118,123,67,67,67,188,119,183,108,217,98,181,90,89,187,163,163,227,214,173,91,225,112,
    248,206,157,59,63,255,252,51,91,104,50,153,154,155,155,83,248,220,85,185,79,108,136,68,156,247,78,68,62,159,239,234,
    213,171,193,96,208,227,241,124,247,221,119,49,215,111,108,108,228,174,118,116,116,140,140,140,132,195,225,91,183,110,117,118,
    118,198,92,95,185,251,231,207,159,239,235,235,243,251,253,11,11,11,211,211,211,236,110,218,201,147,39,149,19,107,97,144,
    101,8,196,216,186,117,107,111,111,111,204,177,172,221,110,111,105,105,57,125,250,180,223,239,15,4,2,167,78,157,82,190,
    107,177,88,246,238,221,91,80,80,144,218,231,78,28,59,70,113,46,191,36,207,99,135,14,157,58,117,138,157,142,46,92,
    184,112,225,194,5,34,42,46,46,206,202,202,242,222,187,27,192,201,205,205,125,236,177,199,216,141,48,191,223,255,197,23,
    95,176,229,141,141,141,61,61,61,139,175,101,217,237,246,103,159,125,246,204,153,51,179,179,179,129,64,224,220,185,115,139,
    55,64,200,251,0,114,9,96,181,90,55,111,222,28,239,110,64,81,81,209,225,195,135,123,123,123,111,222,188,185,228,87,
    33,210,12,187,210,186,127,255,254,246,246,246,219,183,111,135,195,225,236,236,236,154,154,154,45,91,182,240,112,71,81,87,
    87,151,147,147,19,245,85,136,234,234,234,171,87,175,178,21,50,50,50,148,235,23,22,22,30,62,124,184,175,175,111,96,
    96,96,98,98,34,16,8,152,205,230,172,172,44,187,221,94,92,92,92,90,90,90,88,88,184,214,187,153,126,12,236,64,
    194,238,56,54,53,53,169,189,61,162,145,218,247,213,214,238,91,110,147,147,147,159,124,242,9,107,151,151,151,183,180,180,
    172,86,207,58,66,153,118,137,230,0,170,144,194,119,37,214,244,59,158,189,189,189,188,29,245,213,38,57,129,0,107,206,
    178,28,88,197,244,119,116,116,156,63,127,126,120,120,120,118,118,54,28,14,79,79,79,95,186,116,137,143,127,214,173,91,
    87,87,87,151,114,231,194,32,215,28,64,45,146,252,222,232,234,30,251,217,87,187,149,135,124,78,97,97,225,83,79,61,
    37,207,197,254,4,224,63,65,154,88,242,60,176,234,35,159,166,166,166,61,123,246,148,151,151,231,228,228,152,76,38,179,
    217,156,157,157,93,85,85,181,111,223,190,3,7,14,8,249,221,230,20,192,25,32,125,36,56,15,172,197,184,223,98,177,
    212,214,214,214,214,214,174,188,43,129,193,25,32,173,196,60,15,224,151,141,42,2,1,210,77,226,177,16,210,159,102,32,
    128,10,196,75,57,210,159,126,32,128,58,44,206,58,210,175,10,16,64,29,150,188,10,4,210,3,4,80,129,120,89,135,
    3,233,7,2,164,155,196,179,94,56,144,102,112,31,96,109,81,126,249,172,178,178,114,219,215,95,179,246,116,70,198,191,
    171,171,137,136,142,31,175,108,105,225,203,41,233,250,66,81,61,63,253,244,211,171,187,190,36,72,33,192,221,187,119,63,
    254,248,99,229,146,67,135,14,241,106,34,105,99,254,215,95,227,189,149,90,141,45,176,114,164,24,2,185,92,174,37,151,
    164,153,117,111,190,25,181,4,99,33,85,144,84,0,183,219,173,226,239,155,146,188,15,0,7,210,128,248,2,140,140,140,
    240,186,9,252,103,178,62,159,47,61,191,112,117,58,157,47,245,246,178,127,118,14,13,209,82,215,251,225,64,154,17,127,
    14,192,74,161,48,182,111,223,206,126,1,71,68,46,151,139,23,99,235,236,236,228,101,15,159,120,226,9,101,77,230,96,
    48,248,225,135,31,178,31,209,230,229,229,189,252,242,203,108,249,125,133,154,253,126,34,178,90,173,185,185,185,229,229,229,
    13,13,13,22,139,133,173,214,255,199,63,254,123,227,70,214,46,158,153,121,254,15,127,88,114,131,45,239,188,211,243,207,
    127,142,218,237,119,173,214,121,147,137,254,245,47,171,205,182,184,231,40,66,161,80,87,87,215,181,107,215,102,102,102,50,
    50,50,202,202,202,154,154,154,98,214,240,138,137,132,117,161,25,130,11,176,176,176,112,227,198,13,214,118,56,28,141,141,
    141,221,221,221,172,96,14,171,144,197,126,23,91,87,87,215,222,222,206,126,111,238,118,187,149,2,92,191,126,157,255,132,
    124,227,189,40,83,84,161,102,34,34,242,251,253,126,191,223,227,241,116,119,119,179,66,205,222,182,54,82,252,238,54,99,
    211,166,100,182,249,196,137,19,116,175,54,104,188,158,163,254,36,24,12,126,254,249,231,188,242,179,223,239,119,185,92,131,
    131,131,207,61,247,92,126,126,254,146,159,40,103,93,104,134,224,2,244,247,247,243,26,16,213,213,213,68,84,83,83,211,
    209,209,65,68,11,11,11,253,253,253,236,1,97,54,155,173,188,188,156,213,195,25,26,26,242,251,253,188,128,161,219,237,
    102,13,179,217,188,97,195,6,222,115,126,126,126,93,93,221,3,15,60,96,183,219,173,86,171,207,231,115,185,92,236,231,
    246,172,80,243,150,251,235,74,36,15,239,57,240,183,191,89,194,225,128,217,60,152,155,219,83,80,192,123,222,183,111,95,
    212,159,12,15,15,23,21,21,29,57,114,196,110,183,247,245,245,253,248,227,143,68,20,8,4,206,158,61,123,232,208,161,
    196,63,124,97,117,161,89,241,175,204,204,204,221,187,119,23,23,23,143,142,142,126,251,237,183,115,115,115,172,46,244,75,
    47,189,100,184,191,222,145,48,8,62,7,80,78,127,89,124,149,71,119,229,187,252,232,30,137,68,120,25,228,217,217,89,
    143,199,195,218,85,85,85,202,50,10,7,15,30,220,180,105,83,65,65,65,86,86,150,209,104,116,56,28,219,182,109,227,
    151,86,7,175,95,79,121,138,205,123,46,253,235,95,89,9,232,122,175,55,231,94,229,106,101,9,104,37,187,119,239,206,
    205,205,53,155,205,13,13,13,252,199,190,83,83,83,241,42,97,113,164,173,11,205,16,249,12,48,59,59,203,171,32,58,
    157,206,188,188,60,222,96,207,167,240,120,60,119,239,222,205,201,201,33,162,178,178,50,187,221,206,134,1,110,183,187,177,
    177,145,238,191,88,20,245,48,73,159,207,215,221,221,61,60,60,60,53,53,21,12,6,149,229,108,137,40,100,48,204,155,
    76,214,88,165,164,151,228,190,158,27,26,162,122,102,37,160,149,21,118,217,78,41,43,254,174,95,191,158,87,119,27,30,
    30,142,122,204,76,20,73,214,133,86,158,253,68,66,100,1,92,46,23,143,175,50,4,53,53,53,252,1,45,46,151,139,
    213,123,51,24,12,117,117,117,108,116,228,245,122,39,39,39,157,78,39,63,242,57,157,78,101,32,198,198,198,190,252,242,
    75,254,60,129,152,132,13,6,98,215,251,239,221,127,77,134,164,122,190,95,9,186,191,178,116,212,203,4,207,4,33,185,
    235,66,51,4,23,128,183,47,95,190,28,179,30,150,219,237,230,5,15,235,235,235,59,59,59,153,51,110,183,187,186,186,
    154,123,18,117,248,255,233,167,159,120,70,149,133,154,255,247,31,255,24,87,28,155,11,142,29,139,247,56,130,120,36,211,
    243,248,159,254,100,255,203,95,148,127,181,214,3,116,33,235,66,51,132,21,96,116,116,116,106,106,106,201,213,166,167,167,
    71,70,70,216,35,100,28,14,71,105,105,233,208,208,16,17,185,221,110,126,241,199,104,52,42,127,89,27,137,68,110,223,
    190,205,218,202,66,205,222,182,54,159,98,156,144,247,206,59,203,221,230,120,61,19,209,92,126,62,41,138,224,122,219,218,
    204,111,191,205,95,70,213,124,86,150,203,93,124,201,72,137,204,117,161,25,194,10,160,188,252,127,228,200,17,254,52,46,
    198,244,244,244,71,31,125,196,218,46,151,139,9,64,68,27,55,110,100,2,204,204,204,116,119,119,179,133,235,215,175,87,
    62,202,37,18,137,240,145,21,143,139,183,173,205,107,179,205,153,87,244,223,51,102,207,20,167,4,244,196,187,239,18,251,
    58,29,209,228,228,36,27,179,177,151,252,202,47,17,149,148,148,36,254,208,138,138,10,182,167,172,46,116,188,71,200,136,
    138,152,186,135,66,161,235,215,175,179,182,205,102,139,74,63,17,101,103,103,243,155,68,253,253,253,11,11,11,172,93,81,
    81,193,231,151,49,47,255,19,145,209,104,228,85,114,89,161,102,207,155,111,142,217,108,151,239,89,148,50,139,123,78,92,
    2,90,201,185,115,231,166,166,166,22,22,22,122,122,122,248,188,54,39,39,103,201,64,75,91,23,154,33,230,25,64,249,
    24,152,120,135,192,226,226,98,246,252,185,96,48,120,227,198,13,118,149,195,104,52,214,213,213,41,31,87,154,157,157,189,
    184,135,157,59,119,42,11,53,83,93,29,17,21,248,124,214,80,104,114,169,71,17,39,38,170,231,196,37,160,57,37,37,
    37,193,96,48,234,222,92,70,70,198,147,79,62,185,228,144,70,218,186,208,12,49,207,0,202,233,111,113,156,3,179,50,
    214,202,241,82,212,124,55,234,37,239,115,255,254,253,101,101,101,230,112,216,20,137,100,207,207,63,116,231,206,255,220,188,
    105,93,113,181,77,222,179,197,98,49,153,76,78,167,179,185,185,185,181,181,85,249,125,132,197,179,11,139,197,178,127,255,
    254,166,166,166,220,220,92,246,80,179,13,27,54,28,60,120,48,201,122,238,172,46,244,174,93,187,74,74,74,216,109,141,
    140,140,140,220,220,220,146,146,146,230,230,230,3,7,14,40,31,183,33,24,168,14,157,58,234,86,52,65,61,149,148,65,
    117,232,85,64,245,252,225,123,163,171,2,4,72,5,213,211,31,243,115,225,64,10,64,128,101,163,145,244,199,252,116,56,
    176,92,32,192,242,208,84,250,99,110,3,28,88,22,16,96,25,104,48,253,12,56,144,50,16,32,89,52,155,126,6,28,
    72,13,8,144,20,26,79,63,3,14,164,0,4,88,26,93,164,159,1,7,150,11,4,88,2,29,165,159,1,7,150,5,
    4,72,132,238,210,207,128,3,201,3,1,226,162,211,244,51,224,64,146,64,128,216,232,58,253,12,56,144,12,16,32,6,
    2,164,159,1,7,150,4,2,68,35,76,250,25,112,32,49,16,224,62,4,75,63,3,14,36,0,2,252,142,144,233,103,
    192,129,120,64,128,223,16,56,253,12,56,16,19,8,64,36,65,250,25,112,96,49,16,64,150,244,51,224,64,20,178,11,
    32,85,250,25,112,64,137,212,2,72,152,126,6,28,224,200,43,128,180,233,103,192,1,134,164,2,72,158,126,6,28,32,
    57,5,64,250,57,112,64,58,1,144,254,40,36,119,64,46,1,144,254,152,200,236,128,68,2,32,253,9,144,214,1,89,
    4,64,250,151,68,78,7,164,16,0,233,79,18,9,29,16,95,0,164,127,89,200,230,128,224,2,32,253,41,32,149,3,
    34,11,128,244,167,140,60,14,8,43,0,210,191,66,36,113,64,76,1,144,254,85,65,6,7,4,20,0,233,95,69,132,
    119,64,52,1,144,254,85,71,108,7,132,18,0,233,95,35,4,118,64,28,1,144,254,53,69,84,7,4,17,0,233,79,
    3,66,58,32,130,0,72,127,218,16,207,1,221,11,128,244,167,25,193,28,208,183,0,72,191,42,136,228,128,142,5,64,
    250,85,68,24,7,244,42,0,210,175,58,98,56,160,75,1,144,126,141,32,128,3,250,19,0,233,215,20,122,119,64,103,
    2,32,253,26,68,215,14,232,73,0,164,95,179,232,215,1,221,8,128,244,107,28,157,58,160,15,1,144,126,93,160,71,
    7,116,32,0,210,175,35,116,231,128,214,5,64,250,117,135,190,28,208,180,0,72,191,78,209,145,3,218,21,0,233,215,
    53,122,113,64,163,2,32,253,2,160,11,7,180,40,0,210,47,12,218,119,64,115,2,32,253,130,161,113,7,180,37,0,
    210,47,36,90,118,64,67,2,32,253,2,163,89,7,180,34,0,210,47,60,218,116,64,19,2,32,253,146,160,65,7,212,
    23,0,233,151,10,173,57,160,178,0,72,191,132,104,202,1,53,5,64,250,165,69,59,14,168,38,0,210,47,57,26,113,
    64,29,1,144,126,64,218,112,64,29,1,148,123,142,244,203,140,234,73,80,109,8,196,246,22,233,7,234,38,65,205,73,
    48,210,15,24,42,38,65,253,251,0,0,168,8,4,0,82,3,1,128,212,64,0,32,53,16,0,72,13,4,0,82,3,
    1,128,212,64,0,32,53,16,0,72,13,4,0,82,3,1,128,212,64,0,32,53,16,0,72,13,4,0,82,3,1,128,
    212,64,0,32,53,16,0,72,13,4,0,82,3,1,128,212,64,0,32,53,16,0,72,13,4,0,82,3,1,128,212,64,
    0,32,53,16,0,72,13,4,0,82,3,1,128,212,64,0,32,53,16,0,72,13,4,0,82,3,1,128,212,64,0,32,
    53,16,0,72,141,89,249,162,189,189,93,173,237,0,64,21,112,6,0,82,243,219,25,160,169,169,73,221,237,0,64,21,
    112,6,0,82,243,255,152,159,125,183,90,171,123,165,0,0,0,0,73,69,78,68,174,66,96,130
};

        public static readonly byte[] notFoundImage64Jpg = new byte[]
        {
    255,216,255,224,0,16,74,70,73,70,0,1,1,0,0,1,0,1,0,0,255,219,0,67,0,10,7,7,8,7,6,10,
    8,8,8,11,10,10,11,14,24,16,14,13,13,14,29,21,22,17,24,35,31,37,36,34,31,34,33,38,43,55,47,38,
    41,52,41,33,34,48,65,49,52,57,59,62,62,62,37,46,68,73,67,60,72,55,61,62,59,255,219,0,67,1,10,11,
    11,14,13,14,28,16,16,28,59,40,34,40,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,
    59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,59,255,192,
    0,17,8,1,0,1,0,3,1,34,0,2,17,1,3,17,1,255,196,0,31,0,0,1,5,1,1,1,1,1,1,0,
    0,0,0,0,0,0,0,1,2,3,4,5,6,7,8,9,10,11,255,196,0,181,16,0,2,1,3,3,2,4,3,5,
    5,4,4,0,0,1,125,1,2,3,0,4,17,5,18,33,49,65,6,19,81,97,7,34,113,20,50,129,145,161,8,35,
    66,177,193,21,82,209,240,36,51,98,114,130,9,10,22,23,24,25,26,37,38,39,40,41,42,52,53,54,55,56,57,58,
    67,68,69,70,71,72,73,74,83,84,85,86,87,88,89,90,99,100,101,102,103,104,105,106,115,116,117,118,119,120,121,122,
    131,132,133,134,135,136,137,138,146,147,148,149,150,151,152,153,154,162,163,164,165,166,167,168,169,170,178,179,180,181,182,183,
    184,185,186,194,195,196,197,198,199,200,201,202,210,211,212,213,214,215,216,217,218,225,226,227,228,229,230,231,232,233,234,241,
    242,243,244,245,246,247,248,249,250,255,196,0,31,1,0,3,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,1,
    2,3,4,5,6,7,8,9,10,11,255,196,0,181,17,0,2,1,2,4,4,3,4,7,5,4,4,0,1,2,119,0,
    1,2,3,17,4,5,33,49,6,18,65,81,7,97,113,19,34,50,129,8,20,66,145,161,177,193,9,35,51,82,240,21,
    98,114,209,10,22,36,52,225,37,241,23,24,25,26,38,39,40,41,42,53,54,55,56,57,58,67,68,69,70,71,72,73,
    74,83,84,85,86,87,88,89,90,99,100,101,102,103,104,105,106,115,116,117,118,119,120,121,122,130,131,132,133,134,135,136,
    137,138,146,147,148,149,150,151,152,153,154,162,163,164,165,166,167,168,169,170,178,179,180,181,182,183,184,185,186,194,195,196,
    197,198,199,200,201,202,210,211,212,213,214,215,216,217,218,226,227,228,229,230,231,232,233,234,242,243,244,245,246,247,248,249,
    250,255,218,0,12,3,1,0,2,17,3,17,0,63,0,235,73,185,154,242,120,227,184,242,214,61,184,27,1,234,41,255,
    0,103,188,255,0,159,239,252,132,40,183,255,0,143,251,191,248,7,242,171,84,1,87,236,247,159,243,253,255,0,144,133,
    31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,
    42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,
    253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,
    0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,
    87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,
    133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,
    132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,
    243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,
    255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,
    1,87,236,247,159,243,253,255,0,144,133,31,103,188,255,0,159,239,252,132,42,213,20,1,87,236,247,159,243,253,255,0,
    144,133,48,27,152,111,32,142,75,143,49,100,221,145,176,14,130,174,213,91,143,248,255,0,180,255,0,129,255,0,42,0,
    45,255,0,227,254,239,254,1,252,170,213,85,183,255,0,143,251,191,248,7,242,171,84,0,81,69,20,0,81,69,20,0,
    81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,
    81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,85,91,143,248,255,0,180,255,0,129,255,0,42,181,85,110,
    63,227,254,211,254,7,252,168,0,183,255,0,143,251,191,248,7,242,171,85,86,223,254,63,238,255,0,224,31,202,173,80,
    1,69,20,80,1,77,87,87,25,70,12,50,70,65,207,32,224,254,180,58,135,70,67,144,24,96,224,144,127,49,210,185,
    79,244,175,9,223,127,20,250,116,205,248,131,253,27,244,35,244,153,75,148,222,149,47,107,116,158,189,23,115,173,162,163,
    130,120,174,160,73,224,112,241,184,202,176,239,82,85,24,180,211,179,10,40,162,129,5,20,81,64,5,20,81,64,5,20,
    81,64,5,20,81,64,5,20,81,64,5,20,81,64,5,20,81,64,5,20,83,93,214,52,103,118,10,170,50,88,156,0,
    40,0,119,88,209,157,216,42,168,201,98,112,0,167,87,41,119,119,115,226,123,211,99,98,76,118,72,65,146,66,62,247,
    185,254,131,241,62,221,45,173,186,90,90,199,111,25,98,145,168,80,93,178,106,99,43,189,54,55,171,71,217,197,115,63,
    121,244,236,77,69,20,85,24,5,85,184,255,0,143,251,79,248,31,242,171,85,86,227,254,63,237,63,224,127,202,128,11,
    127,248,255,0,187,255,0,128,127,42,181,85,109,255,0,227,254,239,254,1,252,170,213,0,20,81,69,0,21,28,240,69,
    117,3,193,58,7,141,198,25,79,122,146,138,6,155,78,232,228,191,210,188,39,125,252,83,233,211,55,226,15,244,111,208,
    143,211,169,130,120,174,160,73,224,112,241,184,202,176,239,68,240,69,117,3,193,58,7,141,198,25,79,122,229,191,210,188,
    39,125,252,83,233,211,55,226,15,244,111,208,143,211,47,131,208,237,211,18,191,191,249,255,0,193,58,218,42,56,39,138,
    234,4,158,7,15,27,140,171,14,245,37,106,113,52,211,179,10,40,162,129,5,20,81,64,5,20,81,64,5,20,81,64,
    5,20,81,64,5,20,81,64,5,20,83,93,214,52,103,118,10,170,50,88,156,0,40,0,119,88,209,157,216,42,168,201,
    98,112,0,174,90,238,238,231,196,247,166,198,196,152,236,144,131,36,132,125,239,115,253,7,226,125,139,187,187,159,19,222,
    155,27,18,99,178,66,12,146,17,247,189,207,244,31,137,246,232,236,108,96,211,237,86,222,221,54,162,245,39,171,31,83,
    239,89,124,122,45,142,228,150,25,115,75,227,232,187,121,191,48,177,177,131,79,181,91,123,116,218,139,212,158,172,125,79,
    189,88,162,138,215,99,137,183,39,118,20,81,69,2,10,171,113,255,0,31,246,159,240,63,229,86,170,173,199,252,127,218,
    127,192,255,0,149,0,22,255,0,241,255,0,119,255,0,0,254,85,106,170,219,255,0,199,253,223,252,3,249,85,170,0,
    40,162,138,0,40,162,138,0,42,57,224,138,234,7,130,116,15,27,140,50,158,245,37,20,13,54,157,209,201,127,165,120,
    78,251,248,167,211,166,111,196,31,232,223,161,31,167,83,4,241,93,64,147,192,225,227,113,149,97,222,137,224,138,234,7,
    130,116,15,27,140,50,158,245,203,127,165,120,78,251,248,167,211,166,111,196,31,232,223,161,31,166,95,7,161,219,166,37,
    127,127,243,255,0,130,117,180,84,112,79,21,212,9,60,14,30,55,25,86,29,234,74,212,226,105,167,102,20,81,69,2,
    10,40,162,128,10,40,162,128,10,40,162,128,10,40,166,187,172,104,206,236,21,84,100,177,56,0,80,0,238,177,163,59,
    176,85,81,146,196,224,1,92,181,221,221,207,137,239,77,141,137,49,217,33,6,73,8,251,222,231,250,15,196,251,23,119,
    119,62,39,189,54,54,36,199,100,132,25,36,35,239,123,159,232,63,19,237,209,216,216,193,167,218,173,189,186,109,69,234,
    79,86,62,167,222,178,248,244,91,29,201,44,50,230,151,199,209,118,243,126,97,99,99,6,159,106,182,246,233,181,23,169,
    61,88,250,159,122,177,69,21,174,199,19,110,78,236,40,162,138,4,20,81,69,0,21,86,227,254,63,237,63,224,127,202,
    173,85,91,143,248,255,0,180,255,0,129,255,0,42,0,45,255,0,227,254,239,254,1,252,170,213,85,183,255,0,143,251,
    191,248,7,242,171,84,0,81,69,20,0,81,69,20,0,81,69,20,0,84,115,193,21,212,15,4,232,30,55,24,101,61,
    234,74,40,26,109,59,163,146,255,0,74,240,157,247,241,79,167,76,223,136,63,209,191,66,63,78,166,9,226,186,129,39,
    129,195,198,227,42,195,189,19,193,21,212,15,4,232,30,55,24,101,61,235,150,255,0,74,240,157,247,241,79,167,76,223,
    136,63,209,191,66,63,76,190,15,67,183,76,74,254,255,0,231,255,0,4,235,104,168,224,158,43,168,18,120,28,60,110,
    50,172,59,212,149,169,196,211,78,204,40,162,138,4,20,81,69,0,20,81,77,119,88,209,157,216,42,168,201,98,112,0,
    160,1,221,99,70,119,96,170,163,37,137,192,2,185,107,187,187,159,19,222,155,27,18,99,178,66,12,146,17,247,189,207,
    244,31,137,246,46,238,238,124,79,122,108,108,73,142,201,8,50,72,71,222,247,63,208,126,39,219,163,177,177,131,79,181,
    91,123,116,218,139,212,158,172,125,79,189,101,241,232,182,59,146,88,101,205,47,143,162,237,230,252,194,198,198,13,62,213,
    109,237,211,106,47,82,122,177,245,62,245,98,138,43,93,142,38,220,157,216,81,69,20,8,40,162,138,0,40,162,138,0,
    42,173,199,252,127,218,127,192,255,0,149,90,170,183,31,241,255,0,105,255,0,3,254,84,0,91,255,0,199,253,223,252,
    3,249,85,170,171,111,255,0,31,247,127,240,15,229,86,168,0,162,138,40,0,162,138,40,0,162,138,40,1,172,202,136,
    93,216,42,168,201,36,224,1,76,134,234,222,231,62,68,241,203,183,174,199,13,143,202,162,213,63,228,19,121,255,0,92,
    31,255,0,65,53,199,232,151,18,105,179,69,122,223,241,237,43,152,100,246,224,28,254,191,161,160,14,213,110,173,222,99,
    2,207,27,74,189,80,56,44,63,10,89,224,138,234,7,130,116,15,27,140,50,158,245,205,233,252,248,206,235,232,223,210,
    173,199,173,94,234,55,147,67,165,193,1,142,30,178,76,78,15,229,64,211,105,221,25,255,0,233,94,19,190,254,41,244,
    233,155,241,7,250,55,232,71,233,212,193,60,87,80,36,240,56,120,220,101,88,119,172,173,63,82,143,89,91,141,62,250,
    217,86,84,4,72,153,200,96,14,14,61,48,107,47,253,43,194,119,223,197,62,157,51,126,32,255,0,70,253,8,253,50,
    248,61,14,221,49,43,251,255,0,159,252,19,167,134,234,222,231,62,69,196,82,237,235,177,195,99,242,169,171,148,240,113,
    193,189,35,178,175,245,169,236,53,221,83,80,183,184,120,173,173,139,66,55,22,59,128,199,60,99,60,158,61,171,83,132,
    233,40,174,122,219,94,212,47,236,164,154,214,214,16,208,41,105,89,216,237,62,202,61,106,238,155,171,155,253,38,91,179,
    26,172,145,110,12,165,176,164,129,158,167,160,160,13,39,117,141,25,221,130,170,140,150,39,0,10,229,174,238,238,124,79,
    122,108,108,73,142,201,8,50,72,71,222,247,63,208,126,39,219,63,83,215,47,53,88,218,2,99,142,20,59,136,67,141,
    252,241,215,147,244,252,113,199,27,246,215,246,90,95,134,163,186,183,132,132,110,2,19,203,63,67,147,248,126,66,179,119,
    150,157,14,184,56,208,92,205,123,253,60,188,253,77,75,27,24,52,251,85,183,183,77,168,189,73,234,199,212,251,213,138,
    231,159,89,213,161,210,126,223,37,173,190,199,198,204,19,149,201,234,195,61,15,177,244,171,73,173,249,122,4,122,149,202,
    41,119,200,8,156,2,114,64,28,253,43,77,142,86,220,157,217,175,69,115,239,172,234,208,89,38,161,53,165,191,217,159,
    7,106,177,222,1,232,125,43,110,218,226,59,187,104,238,34,63,36,139,145,154,4,75,69,20,80,1,69,20,80,1,69,
    20,80,1,85,110,63,227,254,211,254,7,252,170,213,85,184,255,0,143,251,79,248,31,242,160,2,223,254,63,238,255,0,
    224,31,202,173,85,91,127,248,255,0,187,255,0,128,127,42,181,64,5,20,81,64,5,20,81,64,5,20,81,64,21,181,
    21,103,211,46,145,20,179,52,46,0,3,36,157,166,177,52,141,41,238,60,59,113,105,115,19,196,237,41,100,243,20,130,
    14,6,15,53,210,81,64,28,143,135,108,239,97,214,76,151,54,243,32,17,149,46,232,64,237,222,167,211,161,187,240,253,
    229,202,53,148,247,16,75,141,143,10,238,60,103,31,206,186,122,40,3,3,66,211,174,151,81,185,212,174,162,48,153,183,
    109,140,245,228,230,182,231,130,43,168,30,9,208,60,110,48,202,123,212,148,80,52,218,119,71,9,167,93,94,232,83,25,
    39,180,153,109,230,249,31,124,101,126,152,39,191,94,63,253,99,83,194,177,72,52,219,198,49,182,217,7,200,113,195,112,
    71,30,181,209,207,4,87,80,60,19,160,120,220,97,148,247,174,91,253,43,194,119,223,197,62,157,51,126,32,255,0,70,
    253,8,253,33,183,23,119,177,211,78,156,107,67,150,58,73,126,63,240,73,244,43,121,173,116,77,64,92,66,240,229,88,
    254,241,74,241,183,222,177,44,34,212,111,237,164,176,179,82,97,4,203,38,56,4,227,128,79,225,192,255,0,14,53,174,
    238,238,124,79,122,108,108,73,142,201,8,50,72,71,222,247,63,208,126,39,219,163,177,177,131,79,181,91,123,116,218,139,
    212,158,172,125,79,189,43,243,61,54,27,130,161,27,203,226,125,59,127,193,57,85,180,151,251,9,237,151,70,144,93,47,
    47,51,69,206,51,252,61,201,237,197,92,26,85,205,215,132,162,183,17,178,79,27,151,17,184,218,79,39,142,125,141,116,
    213,28,241,25,160,120,214,71,136,176,225,208,242,43,67,144,230,47,110,174,155,195,31,101,146,198,104,76,74,136,239,32,
    218,184,4,1,142,228,244,169,99,211,165,212,124,31,109,20,88,243,17,139,168,60,103,150,227,242,53,118,125,14,230,240,
    44,119,186,172,147,66,14,118,8,194,231,241,21,173,12,73,4,41,20,74,21,16,97,64,236,40,3,149,182,178,180,88,
    18,59,143,15,222,53,194,140,51,41,109,172,125,115,154,233,108,33,88,44,162,141,32,48,0,191,234,203,110,219,158,113,
    154,177,69,0,20,81,69,0,20,81,69,0,20,81,69,0,21,86,227,254,63,237,63,224,127,202,173,85,91,143,248,255,
    0,180,255,0,129,255,0,42,0,45,255,0,227,254,239,254,1,252,170,213,85,183,255,0,143,251,191,248,7,242,171,84,
    0,81,69,20,0,81,69,20,0,81,69,20,0,85,29,79,85,183,210,163,71,156,59,121,135,10,16,2,127,82,42,245,
    114,122,148,208,106,30,40,142,25,229,68,182,183,225,139,176,0,145,201,28,251,241,64,29,22,159,168,67,169,90,139,136,
    55,5,201,82,24,114,8,171,85,202,248,110,100,182,212,238,244,225,40,120,220,147,27,43,103,56,244,35,219,249,84,218,
    69,252,214,87,183,246,119,243,201,39,146,11,171,72,196,156,15,175,168,193,160,14,146,138,192,240,228,183,55,9,117,168,
    93,220,72,99,102,33,85,156,237,81,212,144,63,207,74,161,45,224,150,25,100,130,251,86,157,227,4,153,35,92,68,15,
    184,236,40,3,163,212,239,191,179,172,30,235,203,243,54,17,242,238,198,114,113,214,185,45,91,90,187,214,96,101,138,22,
    134,214,32,12,160,54,114,73,227,39,235,208,126,61,184,134,231,85,212,181,43,15,38,66,90,8,0,50,184,24,221,147,
    129,184,255,0,79,199,183,26,178,70,145,248,24,108,80,187,176,205,129,212,239,234,106,26,230,208,233,132,149,31,123,118,
    214,158,95,240,74,122,125,221,214,131,229,93,108,103,211,174,142,118,231,37,79,215,143,155,143,161,31,167,99,4,241,93,
    64,147,192,225,227,113,149,97,222,176,226,54,255,0,240,135,70,183,50,249,81,186,224,182,221,199,239,103,0,122,241,248,
    117,174,124,79,123,166,147,45,140,151,81,218,51,230,54,117,42,172,125,199,66,120,253,40,73,197,249,5,74,145,173,27,
    191,137,126,63,240,78,179,87,214,127,178,229,183,79,179,249,190,113,60,239,219,140,99,216,250,214,165,113,254,33,185,55,
    112,105,119,42,84,180,136,79,7,128,223,46,71,231,86,181,147,125,163,152,47,35,212,38,148,179,97,209,207,202,79,94,
    7,97,214,172,230,52,181,125,103,251,46,91,116,251,63,155,231,19,206,253,184,198,61,143,173,106,87,43,226,121,4,210,
    105,146,129,128,224,176,31,93,181,103,196,122,172,214,183,48,89,195,55,144,28,6,146,80,50,64,39,31,208,208,7,67,
    69,114,7,88,22,23,208,27,77,74,107,216,27,137,86,108,146,62,153,21,215,208,1,69,20,80,1,69,20,80,1,69,
    20,80,1,85,110,63,227,254,211,254,7,252,170,213,85,184,255,0,143,251,79,248,31,242,160,2,223,254,63,238,255,0,
    224,31,202,173,85,91,127,248,255,0,187,255,0,128,127,42,181,64,5,20,81,64,5,20,81,64,5,20,81,64,13,125,
    219,27,102,55,99,140,244,205,98,233,58,7,217,140,242,106,41,111,115,36,172,8,37,119,1,215,61,71,189,110,81,64,
    24,119,154,20,159,218,150,215,154,114,219,192,34,198,228,198,208,121,246,29,193,197,55,92,208,39,212,46,214,230,214,72,
    227,98,155,100,222,72,207,228,15,106,222,166,187,172,104,206,236,21,84,100,177,56,0,80,5,72,172,97,182,209,190,196,
    238,22,49,9,89,31,56,3,35,230,63,204,215,33,99,5,238,162,36,210,236,101,221,106,178,23,121,8,218,167,208,158,
    253,184,31,225,198,141,221,221,207,137,239,77,141,137,49,217,33,6,73,8,251,222,231,250,15,196,251,116,118,54,48,105,
    246,171,111,110,155,81,122,147,213,143,169,247,172,238,228,244,216,235,112,141,8,251,223,19,233,219,212,198,135,195,247,113,
    232,55,22,6,72,124,217,101,14,8,99,183,28,123,123,85,153,52,139,135,240,218,233,161,227,243,128,31,54,78,223,189,
    159,79,233,91,20,86,135,33,139,46,135,36,254,31,135,79,121,85,101,136,238,12,50,87,60,255,0,67,85,238,180,77,
    86,243,77,138,214,91,155,108,67,128,138,160,128,64,24,201,56,235,248,87,69,69,0,121,254,167,164,93,105,38,33,59,
    23,129,142,67,167,64,221,199,177,227,241,199,229,189,54,155,121,174,45,187,203,125,4,150,75,202,188,106,67,63,212,118,
    63,203,210,183,167,130,43,168,30,9,208,60,110,48,202,123,215,45,254,149,225,59,239,226,159,78,153,191,16,127,163,126,
    132,126,153,182,225,232,118,70,49,196,45,62,63,207,254,9,167,173,104,211,106,18,90,27,102,137,18,12,130,28,145,199,
    29,48,15,165,77,171,232,231,80,146,43,152,38,242,110,97,63,43,17,144,121,200,253,107,66,9,226,186,129,39,129,195,
    198,227,42,195,189,73,90,28,141,52,236,204,200,227,214,217,208,77,113,104,136,8,220,99,70,36,143,198,180,232,162,129,
    5,20,81,64,5,20,81,64,5,20,81,64,5,85,184,255,0,143,251,79,248,31,242,171,85,86,227,254,63,237,63,224,
    127,202,128,11,127,248,255,0,187,255,0,128,127,42,181,85,109,255,0,227,254,239,254,1,252,170,213,0,20,81,69,0,
    20,81,69,0,20,81,69,0,20,81,77,119,88,209,157,216,42,168,201,98,112,0,160,1,221,99,70,119,96,170,163,37,
    137,192,2,185,107,187,187,159,19,222,155,27,18,99,178,66,12,146,17,247,189,207,244,31,137,246,46,238,238,124,79,122,
    108,108,73,142,201,8,50,72,71,222,247,63,208,126,39,219,163,177,177,131,79,181,91,123,116,218,139,212,158,172,125,79,
    189,101,241,232,182,59,146,88,101,205,47,143,162,237,230,252,194,198,198,13,62,213,109,237,211,106,47,82,122,177,245,62,
    245,98,138,43,93,142,38,220,157,216,81,69,20,8,40,162,138,0,42,57,224,138,234,7,130,116,15,27,140,50,158,245,
    37,20,13,54,157,209,201,127,165,120,78,251,248,167,211,166,111,196,31,232,223,161,31,167,83,4,241,93,64,147,192,225,
    227,113,149,97,222,137,224,138,234,7,130,116,15,27,140,50,158,245,203,127,165,120,78,251,248,167,211,166,111,196,31,232,
    223,161,31,166,95,7,161,219,166,37,127,127,243,255,0,130,117,180,84,112,79,21,212,9,60,14,30,55,25,86,29,234,
    74,212,226,105,167,102,20,81,69,2,10,40,162,128,10,40,162,128,10,171,113,255,0,31,246,159,240,63,229,86,170,173,
    199,252,127,218,127,192,255,0,149,0,22,255,0,241,255,0,119,255,0,0,254,85,106,170,219,255,0,199,253,223,252,3,
    249,85,170,0,40,162,138,0,40,162,138,0,40,162,154,238,177,163,59,176,85,81,146,196,224,1,64,3,186,198,140,238,
    193,85,70,75,19,128,5,114,215,119,119,62,39,189,54,54,36,199,100,132,25,36,35,239,123,159,232,63,19,236,93,221,
    220,248,158,244,216,216,147,29,146,16,100,144,143,189,238,127,160,252,79,183,71,99,99,6,159,106,182,246,233,181,23,169,
    61,88,250,159,122,203,227,209,108,119,36,176,203,154,95,31,69,219,205,249,133,141,140,26,125,170,219,219,166,212,94,164,
    245,99,234,125,234,197,20,86,187,28,77,185,59,176,162,138,40,16,81,69,20,0,81,69,20,0,81,69,20,0,84,115,
    193,21,212,15,4,232,30,55,24,101,61,234,74,40,26,109,59,163,146,255,0,74,240,157,247,241,79,167,76,223,136,63,
    209,191,66,63,78,166,9,226,186,129,39,129,195,198,227,42,195,189,19,193,21,212,15,4,232,30,55,24,101,61,235,150,
    255,0,74,240,157,247,241,79,167,76,223,136,63,209,191,66,63,76,190,15,67,183,76,74,254,255,0,231,255,0,4,235,
    104,168,224,158,43,168,18,120,28,60,110,50,172,59,212,149,169,196,211,78,204,40,162,138,4,20,81,69,0,21,86,227,
    254,63,237,63,224,127,202,173,85,91,143,248,255,0,180,255,0,129,255,0,42,0,45,255,0,227,254,239,254,1,252,170,
    213,85,183,255,0,143,251,191,248,7,242,171,84,0,81,69,20,0,81,69,53,221,99,70,119,96,170,163,37,137,192,2,
    128,7,117,141,25,221,130,170,140,150,39,0,10,229,174,238,238,124,79,122,108,108,73,142,201,8,50,72,71,222,247,63,
    208,126,39,216,187,187,185,241,61,233,177,177,38,59,36,32,201,33,31,123,220,255,0,65,248,159,110,142,198,198,13,62,
    213,109,237,211,106,47,82,122,177,245,62,245,151,199,162,216,238,73,97,151,52,190,62,139,183,155,243,11,27,24,52,251,
    85,183,183,77,168,189,73,234,199,212,251,213,138,40,173,118,56,155,114,119,97,69,20,80,32,162,138,40,0,162,138,40,
    0,162,138,40,0,162,138,40,0,162,138,40,0,168,231,130,43,168,30,9,208,60,110,48,202,123,212,148,80,52,218,119,
    71,37,254,149,225,59,239,226,159,78,153,191,16,127,163,126,132,126,157,76,19,197,117,2,79,3,135,141,198,85,135,122,
    39,130,43,168,30,9,208,60,110,48,202,123,215,45,254,149,225,59,239,226,159,78,153,191,16,127,163,126,132,126,153,124,
    30,135,110,152,149,253,255,0,207,254,9,214,209,81,193,60,87,80,36,240,56,120,220,101,88,119,169,43,83,137,166,157,
    152,81,69,20,8,42,173,199,252,127,218,127,192,255,0,149,90,170,183,31,241,255,0,105,255,0,3,254,84,0,91,255,
    0,199,253,223,252,3,249,85,170,171,111,255,0,31,247,127,240,15,229,86,168,0,162,138,40,1,174,235,26,51,187,5,
    85,25,44,78,0,21,203,93,221,220,248,158,244,216,216,147,29,146,16,100,144,143,189,238,127,160,252,79,183,87,80,219,
    218,193,105,25,142,222,37,137,11,22,33,70,57,53,50,139,122,116,55,163,86,52,239,43,94,93,60,134,216,216,193,167,
    218,173,189,186,109,69,234,79,86,62,167,222,172,81,69,86,198,45,185,59,176,162,138,40,16,81,69,20,0,81,69,20,
    0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,81,69,20,0,84,115,193,21,212,15,4,232,30,55,24,
    101,61,234,74,40,26,109,59,163,146,255,0,74,240,157,247,241,79,167,76,223,136,63,209,191,66,63,78,166,9,226,186,
    129,39,129,195,198,227,42,195,189,19,193,21,212,15,4,232,30,55,24,101,61,233,200,139,26,42,34,133,85,24,10,6,
    0,21,17,143,47,161,189,90,170,170,77,175,123,171,239,255,0,4,117,20,81,86,115,133,85,184,255,0,143,251,79,248,
    31,242,171,85,86,227,254,63,237,63,224,127,202,128,11,127,248,255,0,187,255,0,128,127,42,181,85,109,255,0,227,254,
    239,254,1,252,170,213,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,
    81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,20,81,69,0,21,
    86,227,254,63,237,63,224,127,202,173,85,91,143,248,255,0,180,255,0,129,255,0,42,0,45,255,0,227,254,239,254,1,
    252,170,213,82,34,230,27,201,228,142,223,204,89,54,224,239,3,160,167,253,162,243,254,124,127,242,40,160,11,84,85,95,
    180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,
    254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,
    94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,
    124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,
    127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,
    127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,
    207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,
    242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,
    143,254,69,20,125,162,243,254,124,127,242,40,160,11,84,85,95,180,94,127,207,143,254,69,20,125,162,243,254,124,127,242,
    40,160,11,85,86,227,254,63,237,63,224,127,202,143,180,94,127,207,143,254,69,20,192,46,102,188,130,73,45,252,181,143,
    118,78,240,122,138,0,255,217
        };
        #endregion

    }
}