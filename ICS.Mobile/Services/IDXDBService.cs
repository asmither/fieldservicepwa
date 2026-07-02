using ICS.Mobile.Services.ServiceModels;

using Microsoft.JSInterop;

namespace ICS.Mobile.Services
{
    public class IDXDBService : JSServiceBase
    {
        //private const string js = "idxdbService.js"; // Using this during development then replace the js text when ready for deployment.
        private const string js = @"let definition;
export function init(settings) {
    definition = settings.idxdbDefinition;
    return true;
}
export function save(storeName, value) {
    return new Promise((resolve, reject) => {
        let storeDefinition = definition.stores.find((s) => s.name == storeName);
        if (storeDefinition === undefined) {
            reject(""The given store name was not found"")
        }
        openIDXDB()
            .then((db) => {
                var storeNames = [];
                storeNames.push(storeName);
                if (storeDefinition.syncPriority != 0) {
                    storeNames.push(syncStoreName(storeName));
                }
                const trx = db.transaction(storeNames, ""readwrite"", { durability: 'strict' });

                trx.onerror = () => reject(trx.error.message);
                trx.oncomplete= () => resolve();

                trx.objectStore(storeNames[0]).put(value);
                if (storeDefinition.syncPriority != 0) {
                    trx.objectStore(storeNames[1]).put({ recordId: value.key, size: value.size });
                }
            })
            .catch((e) => {
                reject(e);
            })
    });
}
export function saveWithoutSync(storeName, value) {
    return new Promise((resolve, reject) => {
        let storeDefinition = definition.stores.find((s) => s.name == storeName);
        if (storeDefinition === undefined) {
            reject(""The given store name was not found"")
        }
        openIDXDB()
            .then((db) => {
                var storeNames = [];
                storeNames.push(storeName);
                const trx = db.transaction(storeNames, ""readwrite"", { durability: 'strict' });

                trx.onerror = () => reject(trx.error.message);
                trx.oncomplete= () => resolve();

                trx.objectStore(storeNames[0]).put(value);
            })
            .catch((e) => {
                reject(e);
            })
    });
}

export function getValueByKey(storeName, key) {
    return new Promise((resolve, reject) => {
        openIDXDB()
            .then((db) => {
                const trx = db.transaction([storeName], ""readonly"");
                trx.onerror = () => reject(trx.error.message);
                trx.onblocked = () => console.warn('pending till unblocked');
                const store = trx.objectStore(storeName);
                const item = store.get(key);
                item.onsuccess = function () {
                    resolve(item.result);
                }
            })
            .catch((e) => {
                reject(e);
            })
    });
}

export function clear() {
    return new Promise((resolve, reject) => {
        openIDXDB()
            .then((db) => {
                for (const store of db.objectStoreNames) {
                    let trx = db.transaction([store], ""readwrite"");
                    trx.objectStore(store).clear();
                };
                resolve();
            })
            .catch((e) => {
                reject(e);
            })
    });
}

export function getAll(storeName) {
    return new Promise((resolve, reject) => {
        openIDXDB()
            .then((db) => {
                const trx = db.transaction(storeName, ""readonly"");
                trx.onerror = () => reject(trx.error.message);
                const allRecords = trx.objectStore(storeName).getAll();
                allRecords.onsuccess = function () {
                    resolve(allRecords.result);
                }
            })
            .catch((e) => {
                reject(e);
                console.log(e);
            })
    });
}

export function getCount(storeName) {
    return new Promise((resolve, reject) => {
        openIDXDB()
            .then((db) => {
                const trx = db.transaction(storeName, ""readonly"");
                trx.onerror = () => reject(trx.error.message);
                const count = trx.objectStore(storeName).count();
                count.onsuccess = function () {
                    resolve(count.result);
                }
            })
            .catch((e) => {
                reject(e);
            })
    });
}

export function deleteByKey(storeName, key) {
    return new Promise((resolve, reject) => {
        const db = openIDXDB()
            .then((db) => {
                const trx = db.transaction([storeName], ""readwrite"");
                trx.onerror = () => reject(trx.error.message);
                trx.onblocked = () => console.warn('pending till unblocked');
                const store = trx.objectStore(storeName);
                const result = store.delete(key);
                resolve();
            })
            .catch((e) => {
                reject(e);
            })
    });
}

function openIDXDB() {
    return new Promise((resolve, reject) => {
        const idxdb = indexedDB.open(definition.name, definition.version);
        idxdb.onupgradeneeded = function () {
            for (const store of idxdb.result.objectStoreNames) {
                idxdb.result.deleteObjectStore(store);
            };
            definition.stores.forEach(s => {
                idxdb.result.createObjectStore(s.name, { keyPath: s.keyPath, autoIncrement: false });
                if (s.syncPriority != 0) {
                    let syncStore = idxdb.result.createObjectStore(syncStoreName(s.name), { keyPath: ""id"", autoIncrement: true });
                    syncStore.createIndex(""recordIndex"", ""recordId"", { unique: false });
                }
            });
        }
        idxdb.onsuccess = () => resolve(idxdb.result);
        idxdb.onerror = () => reject(idxdb.error.message);
    });
}

function getExpireDate(retainMinutes) {
    var result = new Date(new Date().getTime() + (retainMinutes * 1000)).toISOString();
    return result;
}
function purgeStoreName(storeName) {
    return storeName.concat(""_Purge"");
}
function syncStoreName(storeName) {
    if (storeName.endsWith(""_Sync"")) {
        return storeName;
    }
    return storeName.concat(""_Sync"");
}";

        public IDXDBService(IJSRuntime jsRuntime, SettingsService settings)
        : base(jsRuntime, settings, js)
        { }

        public IDXDBDefinition Definition => settings.IDXDBDefinition;

        /// <summary>
        /// Saves the typed record to the appropriate store, optionally adds a sync record.
        /// </summary>
        /// <typeparam name="T">Type of object to store.</typeparam>
        /// <param name="value">Instance of value to store.</param>
        public async Task Save<T>(T value)
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("save", StoreName<T>(), value);
        }

        public async Task SaveWithoutSync<T>(T value)
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("saveWithoutSync", StoreName<T>(), value);
        }

        #region Sync Records

        public async Task<List<IDXDBSyncRecord>> GetSyncRecords(string valueStoreName)
        {
            await base.WaitForReferenceAsync();

            List<IDXDBSyncRecord> all = await jsRef.Value.InvokeAsync<List<IDXDBSyncRecord>>("getAll", SyncStoreName(valueStoreName));
            List<IDXDBSyncRecord> keepers = new();
            foreach (var record in all.OrderByDescending(s => s.Id))
            {
                if (keepers.Any(k => k.RecordId == record.RecordId))
                {
                    await DeleteSync(valueStoreName, record.Id);
                }
                else
                {
                    keepers.Add(record);
                }
            }
            keepers.Sort((a, b) => a.Id.CompareTo(b.Id));
            return keepers;
        }

        public async Task DeleteSync(string valueStoreName, int id)
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("deleteByKey", SyncStoreName(valueStoreName), id);
        }


        public async Task<int> GetSyncsCount(string valueStoreName)
        {
            try
            {
                await base.WaitForReferenceAsync();
                return await jsRef.Value.InvokeAsync<int>("count", SyncStoreName(valueStoreName));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        #endregion Sync Records


        #region Purge Records

        public async Task<List<IDXDBSyncRecord>> GetPurgeRecords(string valueStoreName)
        {
            await base.WaitForReferenceAsync();

            List<IDXDBSyncRecord> all = await jsRef.Value.InvokeAsync<List<IDXDBSyncRecord>>("getAll", PurgeStoreName(valueStoreName));
            List<IDXDBSyncRecord> keepers = new();
            foreach (var record in all.OrderByDescending(s => s.Id))
            {
                if (keepers.Any(k => k.RecordId == record.RecordId))
                {
                    await DeleteSync(valueStoreName, record.Id);
                }
                else
                {
                    keepers.Add(record);
                }
            }
            keepers.Sort((a, b) => a.Id.CompareTo(b.Id));
            return keepers;
        }

        public async Task DeletePurge(string storeName, int id)
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("deleteByKey", PurgeStoreName(storeName), id);
        }

        #endregion Purge Records

        public async Task DeleteValue<T>(string key)
        {
            await base.WaitForReferenceAsync();

            await DeleteValue(StoreName<T>(), key);
        }

        public async Task DeleteSync<T>(int id)
        {
            await base.WaitForReferenceAsync();

            await DeleteSync(SyncStoreName(StoreName<T>()), id);
        }
        public async Task DeleteValue(string storeName, string key)
        {
            await base.WaitForReferenceAsync();

            await jsRef.Value.InvokeVoidAsync("deleteByKey", storeName, key);
        }

        public async Task<T> GetValueByKey<T>(string key)
        {
            await base.WaitForReferenceAsync();
            return await jsRef.Value.InvokeAsync<T>("getValueByKey", StoreName<T>(), key);
        }

        public async Task<int> Count<T>(string key)
        {
            await base.WaitForReferenceAsync();
            return await jsRef.Value.InvokeAsync<int>("getCount", StoreName<T>(), key);
        }

        public async Task<List<T>> GetValueRecords<T>()
        {
            await base.WaitForReferenceAsync();
            return await jsRef.Value.InvokeAsync<List<T>>("getAll", StoreName<T>());
        }

        public async Task<int> GetValuesCount<T>()
        {
            await base.WaitForReferenceAsync();
            return await jsRef.Value.InvokeAsync<int>("count", StoreName<T>());
        }
        
      
        private string StoreName<T>()
        {
            return typeof(T).Name;
        }

        private string PurgeStoreName(string storeName)
        {
            return $"{storeName}_Purge";
        }
        private string SyncStoreName(string storeName)
        {
            return $"{storeName}_Sync";
        }

        public async Task Clear()
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("clear");
        }
    }
}
