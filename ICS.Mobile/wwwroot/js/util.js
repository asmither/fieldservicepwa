window.util = {
    getCoords: function (timeout = 2500) { // Timeout in milliseconds, default is 10 seconds
        return new Promise((resolve, reject) => {
            // Create the timeout promise
            const timer = new Promise((_, reject) => {
                setTimeout(() => {
                    reject(new Error('Geolocation request timed out'));
                    
                }, timeout);
            });

            // Create the geolocation promise
            const geolocation = new Promise((resolve, reject) => {
                navigator.geolocation.getCurrentPosition(
                    position => {
                        resolve({
                            latitude: position.coords.latitude,
                            longitude: position.coords.longitude
                        });
                    },
                    error => reject(error)
                );
            });

            // Race between the geolocation and timeout promises
            Promise.race([geolocation, timer])
                .then(resolve)
                .catch(reject);
        }).catch(error => {
            console.error('Error getting geolocation:', error);
            return null;
        });
    },
    requestGeolocationPermission: function () {
        return new Promise((resolve, reject) => {
            navigator.permissions.query({ name: 'geolocation' }).then(function (result) {
                if (result.state === 'granted') {
                    resolve(true);
                } else if (result.state === 'prompt') {
                    navigator.geolocation.getCurrentPosition(
                        position => resolve(true),
                        error => resolve(false)
                    );
                } else {
                    resolve(false);
                }
            }).catch(error => {
                console.error('Error requesting geolocation permission:', error);
                resolve(false);
            });
        });
    },
    checkInternetStatus: function () {
        if (navigator.onLine) {
            if (!navigator.connection) {
                //console.log(`Effective network type: ${navigator.connection.effectiveType}`);
                //console.log(`Downlink Speed: ${navigator.connection.downlink}Mb/s`);
                //console.log(`Round Trip Time: ${navigator.connection.rtt}ms`);
            }
            return true;
        } else {
            //if (!navigator.connection) console.log('Navigator Connection API not supported');
            if (!navigator.onLine) console.log('Browser offline');
        }
        return false;
    }
};
