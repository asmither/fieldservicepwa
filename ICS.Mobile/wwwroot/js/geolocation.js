let watchID = null;
let previousPosition = null;
let geoPosition = {
    latitude: null,
    longitude: null
};
let initialized = false;
let realTimeUpdate = false;

function startGeoWatch(dotNetHelper) {
    if (navigator.geolocation) {
        initiateWatch(dotNetHelper);
    } else {
        console.error("Geolocation is not supported by this browser.");
    }
}

function enableRealTimeUpdate() {
    realTimeUpdate = true;
}

function disableRealTimeUpdate() {
    realTimeUpdate = false;
}
function initiateWatch(dotNetHelper) {
    watchID = navigator.geolocation.watchPosition(
        position => {
            if (shouldUpdatePosition(position)) {
                //console.log("Watcher received geolocation update:", position.coords.latitude, position.coords.longitude);

                geoPosition.latitude = position.coords.latitude;
                geoPosition.longitude = position.coords.longitude;
               
                if (dotNetHelper && (!initialized || realTimeUpdate)) {
                    dotNetHelper.invokeMethodAsync('UpdateLocation',
                        position.coords.latitude, position.coords.longitude);
                    initialized = true;
                }
                
                previousPosition = position;
                //alert('Geolocation updated. ' + position.coords.latitude.toString() + ', ' + position.coords.longitude.toString())
            }
        },
        error => {
            handleGeoError(error, dotNetHelper);
        },
        {
            enableHighAccuracy: false, // Consider setting this to false for testing
            maximumAge: 60000,
            timeout: 45000 // Increased timeout to 45 seconds
        }
    );
}

function handleGeoError(error, dotNetHelper) {
    console.error("Geolocation error:", error);
    switch (error.code) {
        case error.PERMISSION_DENIED:
            alert("Access to location was denied.   Please restart app and allow permissions.");
            break;
        case error.POSITION_UNAVAILABLE:
            console.log("Position Unavailable.");
            break;
        case error.TIMEOUT:
            console.log("Geolocation request timed out.");
            retryGeoWatch(dotNetHelper); // Retry on timeout
            break;
        default:
            //alert("Unknown error occurred.");
            break;
    }
}

function retryGeoWatch(dotNetHelper) {
    stopGeoWatch();
    initiateWatch(dotNetHelper);
}
function shouldUpdatePosition(newPosition) {
    
    const minDistance = 0.003; // Minimum change in degrees
    if (!previousPosition) return true;

    const distance = calculateDistance(
        previousPosition.coords.latitude,
        previousPosition.coords.longitude,
        newPosition.coords.latitude,
        newPosition.coords.longitude
    );

    return distance > minDistance;
}

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371; // Radius of the Earth in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon1 - lon2) * Math.PI / 180;
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
}



function stopGeoWatch() {
    if (watchID !== null) {
        navigator.geolocation.clearWatch(watchID);
        //alert('Geolocation watch stopped.');
        initialized = false;
        watchID = null;
    }
}

function getGeoPosition() {
    
    return geoPosition;
}


window.geoLocationInterop = {
    initialize: function (dotNetHelper) {
        if (!watchID) { // Ensure not to duplicate watchers
            startGeoWatch(dotNetHelper);
        }
    },
    reinitialize: function (dotNetHelper) {
        stopGeoWatch(); // Stop existing watcher if any
        startGeoWatch(dotNetHelper); // Restart with new reference
    },
    startGeoWatch: startGeoWatch,
    stopGeoWatch: stopGeoWatch,
    getGeoPosition: getGeoPosition,

    // Other methods remain unchanged.
    getDateTimeFormatOptions: function () {
        return Intl.DateTimeFormat.getDateTimeFormatOptions();
    }
};