let watchId = null;
let minDistance = 0.003;

// Constants here map to enumerated values.
let STATE_NONE = 0;
let STATE_INITILIAZING = 1;
let STATE_ERROR = 2;
let STATE_RUNNING = 3;

// This is return object in all cases
let positionResult = {
    latLong:
    {
        latitude: 0,
        longitude: 0
    },
    geoState: STATE_NONE
};

let positionOptions = {
    enableHighAccuracy: false,
    maximumAge: 60000,
    timeout: 45000
};
export function init(settings) {
    //TODO: Add accuracey, age, timeout, minDistance to settings??
    //GEO_ENABLE_HIGH_ACCURACY
    //GEO_MAXIMUM_AGE
    //GEO_TIMEOUT
    //GEO_MIN_DISTANCE

    positionOptions.enableHighAccuracy = false,
    positionOptions.maximumAge = 60000,
    positionOptions.timeout = 45000
    minDistance = 0.003;

    return true;
}
export function startOrResetGeoWatch() {
    // If in an error state stop existing
    if (positionResult.geoState === STATE_ERROR) {
        stopGeoWatch();
    }
    if (watchId === null) {
        positionResult.geoState = STATE_INITILIAZING;
        watchId = navigator.geolocation.watchPosition(
            p => {
                positionResult.geoState = STATE_RUNNING;
                setCoords(p.coords);
            },
            e => handleGeoError(e),
            positionOptions
        );
    }
}

export function getPositionOneShot() {
    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            p => {
                setCoords(p.coords);
                resolve(positionResult);
            },
            e => reject(e),
            positionOptions)
    });
}

export function getPosition() {
    return positionResult;
}

export function stopGeoWatch() {
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
        positionResult.geoState = STATE_NONE;
    }
}

function setCoords(coords) {
    if (shouldUpdatePosition(coords)) {
        positionResult.latLong.latitude = coords.latitude;
        positionResult.latLong.longitude = coords.longitude;
    } 
}
function shouldUpdatePosition(coords) {
    return calculateDistance(
        positionResult.latLong.latitude,
        positionResult.latLong.longitude,
        coords.latitude,
        coords.longitude
    ) > minDistance;
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


function handleGeoError(error) {
    console.error("Geolocation error:", error);
    positionResult.geoState == STATE_ERROR;
    switch (error.code) {
        case error.PERMISSION_DENIED:
            alert("Access to location was denied. Please restart app and allow permissions.");
            break;
        case error.POSITION_UNAVAILABLE:
            console.log("Position Unavailable.");
            break;
        case error.TIMEOUT:
            console.log("Geolocation request timed out.");
            startOrResetGeoWatch(); // Retry on timeout
            break;
        default:
            //alert("Unknown error occurred.");
            break;
    }
}
