//// All permissions currently available

//var permissions;

//export function init(settings) {
//    permissions = settings.requiredPermissions;
//    return true;
//}

//export async function getCurrentPermissionsStates() {
//    var result = [];
//    for (const permission of permissions) {
//        try {
//            const permsissionState = await navigator.permissions.query({ name: permission });
//            result.push({ permission: permission, state: permsissionState.state })
//        }
//        catch (e) {
//            result.push({ permission: permission, state: "not-supported" });
//        }
//    }
//    return result;
//}

//export async function geolocationPermissionRequest() {
//    var result = { success: null, error: null };
//    var promise = new Promise((resolve,reject) => {
//        if (!navigator.geolocation) {
//            resolve({ success: false, error: "Not Supported" });
//        } else {
//            navigator.geolocation.getCurrentPosition(resolve, reject);
//        }
//    });
//    await promise
//        .then((position) =>  result.success = true)
//        .catch((error) => { result.success = false; result.error = error });
//    return result;
//}
//export function isRunningAsStandalone() {
//    return (navigator.standalone || window.matchMedia('(display-mode: standalone)').matches);
//}
//export function getOS() {

//    const userAgent = navigator.userAgent;

//    if (userAgent.indexOf("Win") !== -1) {

//        return "Windows";

//    } else if (userAgent.indexOf("Mac") !== -1) {

//        return "MacOS";

//    } else if (userAgent.indexOf("Linux") !== -1) {

//        return "Linux";

//    } else if (userAgent.indexOf("Android") !== -1) {

//        return "Android";

//    } else if (userAgent.indexOf("iPhone") !== -1) {

//        return "iOS";

//    } else {

//        return "Unknown";
//    }
//}
// No longer in use.