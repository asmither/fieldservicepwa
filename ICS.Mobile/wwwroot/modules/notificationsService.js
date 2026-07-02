//var publicKey;
//export function init(settings) {
//    publicKey = settings.pushNotificationsPublicKey;
//    return true;
//}
//export async function getNotificationSubscription() {
//    if (!("serviceWorker" in navigator)) {
//        return subscriptionFailureResult("Not Supported");
//    }

//    const worker = await navigator.serviceWorker.getRegistration();
//    var existingSubscription = await worker.pushManager.getSubscription();

//    // this can be used in a new subscription scenerio
//    //if (existingSubscription) {
//    //    await existingSubscription.unsubscribe();
//    //    existingSubscription = null;
//    //}

//    if (existingSubscription) {
//        return subscriptionSuccessResult(existingSubscription, false);
//    } else {
//        try {
//            const newSubscription = await worker.pushManager.subscribe({
//                userVisibleOnly: true,
//                applicationServerKey: publicKey
//            });
//            if (newSubscription) {
//                return subscriptionSuccessResult(newSubscription, true);
//            }
//        }
//        catch (error) {
//            return subscriptionFailureResult(error.name)
//        }
//    }
//}
//export function showNotification(title, options) {
//    Notification.showNotification(title, options)
//}

//function subscriptionFailureResult(error) {
//    return {
//        result: {
//            success: false,
//            error: error
//        },
//        value: null
//    }
//}

//function subscriptionSuccessResult(subscription, isNew) {
//    return {
//        result: {
//            success: true,
//            error: null
//        },
//        value: {
//            IsNew: isNew,
//            EndPoint: subscription.endpoint,
//            P256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
//            Auth: arrayBufferToBase64(subscription.getKey('auth'))
//        }
//    }
//};
//function arrayBufferToBase64(buffer) {
//    var binary = '';
//    var bytes = new Uint8Array(buffer);
//    var len = bytes.byteLength;
//    for (var i = 0; i < len; i++) {
//        binary += String.fromCharCode(bytes[i]);
//    }
//    var result = window.btoa(binary)
//    return result;
//}
// This is no longer in use