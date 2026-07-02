let definition;
export function init(settings) {
    definition = settings.idxdbDefinition;
    return true;
}
export function save(storeName, value) {
    return new Promise((resolve, reject) => {
        let storeDefinition = definition.stores.find((s) => s.name == storeName);
        if (storeDefinition === undefined) {
            reject("The given store name was not found")
        }
        openIDXDB()
            .then((db) => {
                var storeNames = [];
                storeNames.push(storeName);
                if (storeDefinition.syncPriority != 0) {
                    storeNames.push(syncStoreName(storeName));
                }
                const trx = db.transaction(storeNames, "readwrite", { durability: 'strict' });

                trx.onerror = () => reject(trx.error.message);
                trx.oncomplete = () => resolve();

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
            reject("The given store name was not found")
        }
        openIDXDB()
            .then((db) => {
                var storeNames = [];
                storeNames.push(storeName);
                const trx = db.transaction(storeNames, "readwrite", { durability: 'strict' });

                trx.onerror = () => reject(trx.error.message);
                trx.oncomplete = () => resolve();

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
                const trx = db.transaction([storeName], "readonly");
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
                    let trx = db.transaction([store], "readwrite");
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
                const trx = db.transaction(storeName, "readonly");
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
                const trx = db.transaction(storeName, "readonly");
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
                const trx = db.transaction([storeName], "readwrite");
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
                    let syncStore = idxdb.result.createObjectStore(syncStoreName(s.name), { keyPath: "id", autoIncrement: true });
                    syncStore.createIndex("recordIndex", "recordId", { unique: false });
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
    return storeName.concat("_Purge");
}
function syncStoreName(storeName) {
    if (storeName.endsWith("_Sync")) {
        return storeName;
    }
    return storeName.concat("_Sync");
}