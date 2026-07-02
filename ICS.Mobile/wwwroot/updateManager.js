// updateManager.js
// Place this in your wwwroot folder and reference it in your index.html

window.UpdateManager = {

    // Check if an update is available
    checkForUpdate: async function () {
        if (!('serviceWorker' in navigator)) {
            return { hasUpdate: false, error: 'Service Worker not supported' };
        }

        try {
            const registration = await navigator.serviceWorker.getRegistration();
            if (!registration) {
                return { hasUpdate: false, error: 'No service worker registered' };
            }

            // Force check for updates
            await registration.update();

            // If there's already a waiting SW, that means an update is available
            if (registration.waiting) {
                return { hasUpdate: true };
            }

            // Send message to service worker to check version
            return new Promise((resolve) => {
                const messageChannel = new MessageChannel();
                messageChannel.port1.onmessage = (event) => {
                    resolve({ hasUpdate: event.data.updateAvailable, error: event.data.error });
                };

                if (registration.active) {
                    registration.active.postMessage(
                        { type: 'CHECK_UPDATE' },
                        [messageChannel.port2]
                    );
                } else {
                    resolve({ hasUpdate: false, error: 'Service worker not active' });
                }
            });
        } catch (error) {
            console.error('Error checking for update:', error);
            return { hasUpdate: false, error: error.message };
        }
    },

    // Force update by clearing cache and reloading
    forceUpdate: async function () {
        try {
            // First, clear all caches
            if ('caches' in window) {
                const cacheNames = await caches.keys();
                await Promise.all(cacheNames.map(name => caches.delete(name)));
                console.log('All caches cleared');
            }

            // Unregister all service workers
            if ('serviceWorker' in navigator) {
                const registrations = await navigator.serviceWorker.getRegistrations();
                for (let registration of registrations) {
                    await registration.unregister();
                    console.log('Service worker unregistered');
                }
            }

            // Clear session storage and local storage
            sessionStorage.clear();
            localStorage.clear();

            // Force reload with cache bypass
            window.location.reload(true);
        } catch (error) {
            console.error('Error during force update:', error);
            // Even if there's an error, try to reload
            window.location.reload(true);
        }
    },

    // Soft update - try to activate waiting service worker
    softUpdate: async function () {
        if (!('serviceWorker' in navigator)) {
            return false;
        }

        try {
            const registration = await navigator.serviceWorker.getRegistration();
            if (!registration) {
                return false;
            }

            // Check for updates
            await registration.update();

            if (registration.waiting) {
                // Tell the waiting service worker to skip waiting
                registration.waiting.postMessage({ type: 'SKIP_WAITING' });

                // Listen for the new service worker to take control
                return new Promise((resolve) => {
                    navigator.serviceWorker.addEventListener('controllerchange', () => {
                        console.log('New service worker activated, reloading...');
                        window.location.reload();
                        resolve(true);
                    });

                    // Timeout after 5 seconds
                    setTimeout(() => resolve(false), 5000);
                });
            } else {
                console.log('No waiting service worker found');
                return false;
            }
        } catch (error) {
            console.error('Error during soft update:', error);
            return false;
        }
    },

    // Clear only the service worker cache
    clearServiceWorkerCache: async function () {
        try {
            if ('caches' in window) {
                const cacheNames = await caches.keys();
                const swCaches = cacheNames.filter(name => name.includes('offline-cache'));
                await Promise.all(swCaches.map(name => caches.delete(name)));
                console.log('Service worker caches cleared');
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error clearing service worker cache:', error);
            return false;
        }
    },

    // Register update check on visibility change (when app comes to foreground)
    // AUTO-CHECKS DISABLED - Uncomment the function below to enable automatic checks
    registerAutoUpdateCheck: function (callback) {
        /* DISABLED - Remove this comment block to enable auto-checks
        // Check when the app becomes visible
        document.addEventListener('visibilitychange', async () => {
            if (!document.hidden) {
                console.log('App became visible, checking for updates...');
                const result = await this.checkForUpdate();
                if (result.hasUpdate && callback) {
                    callback();
                }
            }
        });

        // Also check periodically (every 30 minutes)
        setInterval(async () => {
            console.log('Periodic update check...');
            const result = await this.checkForUpdate();
            if (result.hasUpdate && callback) {
                callback();
            }
        }, 30 * 60 * 1000); // 30 minutes
        */

        // Currently disabled - no auto-checks
        console.log('Auto-update checks are disabled');
    },

    // Get service worker registration info
    getRegistrationInfo: async function () {
        if (!('serviceWorker' in navigator)) {
            return null;
        }

        try {
            const registration = await navigator.serviceWorker.getRegistration();
            if (!registration) {
                return null;
            }

            return {
                scope: registration.scope,
                hasActive: !!registration.active,
                hasWaiting: !!registration.waiting,
                hasInstalling: !!registration.installing,
                updateFound: false
            };
        } catch (error) {
            console.error('Error getting registration info:', error);
            return null;
        }
    }
};