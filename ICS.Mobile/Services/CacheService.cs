using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;

using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace ICS.Mobile.Services
{
    public class CacheBlob
    {
        public byte[]? Data { set; get; }
        public string? ContentType { set; get; }
    }

    public class CacheService : JSServiceBase
    {
        //private const string js = "cacheService.js"; // Using this during development then replace the js text when ready for deployment.
        private const string js = @"
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
}";

        
        public CacheService(IJSRuntime jsRuntime, SettingsService settings)
            : base(jsRuntime, settings, js)
        { }

        public async Task PutStringAsync(string key, string value)
        {
            await base.WaitForReferenceAsync();

            await jsRef.Value.InvokeVoidAsync("putString", key, value);
        }

        public async Task<string> GetStringAsync(string key)
        {
            await base.WaitForReferenceAsync();
            return await jsRef.Value.InvokeAsync<string>("getString", key);
        }

        public async Task PutBlobAsync(string key, byte[] data, string type)
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("putBlob", key, data, data.Length, type);
        }

        public async Task<bool> TryHydrateImage(ImageInsertInput input)
        {
            await base.WaitForReferenceAsync();

            try
            {
                var result = await jsRef.Value.InvokeAsync<CacheBlob>("getBlob", input.GeneratedFileName);
                if(result is not null)
                {
                    input.ContentType = result.ContentType;
                    input.Data = result.Data;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return false;
        }

        public async Task<bool> TryHydrateImage(PresentationMedia media)
        {
            await base.WaitForReferenceAsync();

            try
            {
                var result = await jsRef.Value.InvokeAsync<string>("getString", media.GeneratedFileName);
                if (result is not null)
                {
                    media.Base64 = result;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return false;
        }

        public async Task Purge()
        {
            await base.WaitForReferenceAsync();
            await jsRef.Value.InvokeVoidAsync("purge");
        }
    }
}
