var cacheName;
var cacheDays;
export function init(settings) {
    cacheName = settings.cacheName;
    cacheDays = settings.cacheDays;
    return true;
}

export async function putString(key, value) {
    const cache = await caches.open(cacheName);
    if (cache) {
        const response = new Response(value, options());
        await cache.put(key, response);
    }
}

export async function putBlob(key, data, size, type) {
    const cache = await caches.open(cacheName);
    if (cache) {
        const blob = new Blob([data], { type: type })
        const response = new Response(blob, options());
        await cache.put(key, response);
    }
}

function options() {
    var date = new Date();
    date.setDate(date.getDate() + cacheDays);
    const options = {
        headers: {
            'Content-Expires': date
        }
    }
    return options;
}

export async function deleteItem (key){
    const cache = await caches.open(cacheName);
    if (cache) {
        await cache.delete(key);
    }
}

export async function purge() {
    var date = new Date();
    const cache = await caches.open(cacheName);
    var keys = await cache.keys();
    for (const key of keys) {
        const item = await cache.match(new Request(key));
        if (item) {
            var expireDate = await item.headers.get('Content-Expires');
            if (expireDate < date) {
                cache.delete(key);
            }
        }
    }
}

export async function getBlob(key) {
    const cache = await caches.open(cacheName);
    const item = await cache.match(new Request(key));
    if (item) {
        const blob = await item.blob();
        const contentType = blob.contentType;
        const arrayBuffer = await blob.arrayBuffer();
        var bytes = new Uint8Array(arrayBuffer);
        return { data: bytes, contentType: blob.type };
    }
    return null;
}

export async function getString(key) {
    const cache = await caches.open(cacheName);
    const item = await cache.match(new Request(key));
    if (item) {
        const text = await item.text();
        return text;
    }
}

