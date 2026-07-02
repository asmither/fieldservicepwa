// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('message', event => onMessage(event));

const cacheNamePrefix = 'offline-cache-';
const version = self.assetsManifest.version;
const cacheName = `${cacheNamePrefix}${version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.woff2$/, /\.ttf$/, /\.otf$/, /\.eot$/, /\.svg$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Skip waiting to activate immediately
    //self.skipWaiting();

    // Fetch and cache all of the content from the manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Take control of all clients immediately
    await self.clients.claim();

    // Delete old unused caches
    const cacheWhitelist = [cacheName];
    const cacheKeys = await caches.keys();
    const deletePromises = cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix))
        .filter(key => !cacheWhitelist.includes(key))
        .map(key => caches.delete(key));

    await Promise.all(deletePromises);

    // Notify all clients that a new version is active
    const clients = await self.clients.matchAll();
    clients.forEach(client => {
        client.postMessage({ type: 'SW_ACTIVATED', version: version });
    });
}

async function onMessage(event) {
    // Handle messages from the client
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    } else if (event.data && event.data.type === 'CHECK_UPDATE') {
        // Force check for updates
        try {
            const response = await fetch('/service-worker-assets.js', {
                cache: 'no-cache',
                headers: {
                    'Cache-Control': 'no-cache, no-store, must-revalidate',
                    'Pragma': 'no-cache'
                }
            });

            if (response.ok) {
                const text = await response.text();
                // Check if version has changed - FIXED to match actual format
                const versionMatch = text.match(/"version":\s*"([^"]+)"/);
                if (versionMatch && versionMatch[1] !== version) {
                    event.ports[0].postMessage({ updateAvailable: true });
                } else {
                    event.ports[0].postMessage({ updateAvailable: false });
                }
            }
        } catch (error) {
            console.error('Error checking for update:', error);
            event.ports[0].postMessage({ updateAvailable: false, error: error.message });
        }
    } else if (event.data && event.data.type === 'CLEAR_CACHE') {
        // Clear all caches
        const cacheNames = await caches.keys();
        await Promise.all(cacheNames.map(name => caches.delete(name)));
        event.ports[0].postMessage({ cleared: true });
    }
}

async function onFetch(event) {
    const request = event.request;
    const requestUrl = new URL(request.url);

    // Special handling for service-worker-assets.js - always fetch fresh
    if (request.url.includes('service-worker-assets.js')) {
        return fetch(request, { cache: 'no-cache' });
    }

    // If this is a navigation request, always serve index.html
    if (request.mode === 'navigate') {
        const cache = await caches.open(cacheName);
        const cachedResponse = await cache.match('index.html') || await cache.match('/index.html');
        if (cachedResponse) {
            return cachedResponse;
        }
    }

    // Special handling for blazor.webassembly.js - this file is critical
    if (request.url.includes('blazor.webassembly.js')) {
        try {
            const response = await fetch(request, { cache: 'no-cache' });
            if (response.ok) {
                const cache = await caches.open(cacheName);
                cache.put(request, response.clone());
                return response;
            }
        } catch (error) {
            console.error('Failed to fetch blazor.webassembly.js, trying cache...');
        }

        // Try cache if network fails
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // Critical error - Blazor won't work without this
        console.error('CRITICAL: blazor.webassembly.js not available!');
        return new Response('console.error("blazor.webassembly.js could not be loaded");', {
            headers: { 'Content-Type': 'application/javascript' }
        });
    }

    

    // For API calls, always go to network and don't cache
    if (request.url.includes('/api/')) {
        try {
            return await fetch(request);
        } catch (error) {
            return new Response(JSON.stringify({ error: 'Network error' }), {
                status: 503,
                statusText: 'Service Unavailable',
                headers: { 'Content-Type': 'application/json' }
            });
        }
    }

    // For all other requests, use cache-first strategy
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(request);

    if (cachedResponse) {
        return cachedResponse;
    }

    try {
        const response = await fetch(request);
        if (request.method === 'GET' && response.status === 200) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        console.error('Fetch failed:', error);

        if (request.url.endsWith('.js')) {
            return new Response('', {
                status: 200,
                headers: { 'Content-Type': 'application/javascript' }
            });
        }

        if (request.headers.get('accept')?.includes('text/html')) {
            const offlinePage = await cache.match('/offline.html');
            if (offlinePage) {
                return offlinePage;
            }
        }

        return new Response('Offline', {
            status: 503,
            statusText: 'Service Unavailable'
        });
    }
}