window.ui = {

    
    isIOSPWA: function () {
        var ok = false;

        if (/iPad|iPhone|iPod/.test(navigator.userAgent) && navigator.maxTouchPoints > 1) {
            // Commented out as we are ok if pwa or not - just looking for iOS
            //if (window.navigator.standalone === true) {
            //    // The app is running standalone (no browser UI)
            //    ok = true;
            //}
            ok = true;
        }

        return ok;
    },
    getBodyWidth: function () {
        var elm = document.body;
        var computedStyle = getComputedStyle(elm);
        var width = elm.clientWidth;
        width -= parseFloat(computedStyle.paddingLeft) + parseFloat(computedStyle.paddingRight) + (parseFloat(computedStyle.fontSize) * 2.5);
        return width;
    },

    setBodyModalState: function (onOff) {
        var elm = document.body;
        var className = "modal-open";
        if (elm) {
            if (onOff == "on") {
                if (!elm.classList.contains(className)) {
                    elm.classList.add(className)
                }
                elm.style.overflow = "hidden";
                elm.style.paddingRight = "0px"
            }
            else {
                if (elm.classList.contains(className)) {
                    elm.classList.remove(className);
                    elm.style.overflow = null;
                    elm.style.paddingRight = null;
                }
            }
        }
    },
    setBodyDarkTheme: function (onOff) {
        var elm = document.body;
        var className = "dark-mode";
        if (elm) {
            if (onOff == "on") {
                if (!elm.classList.contains(className)) {
                    elm.classList.add(className)
                }
            }
            else {
                if (elm.classList.contains(className)) {
                    elm.classList.remove(className);
                }
            }
        }
    },
    getSignaturePadData: function (canvas) {
        return canvas.toDataURL("image/png");
    },

    setSignaturePadElement() {
        var canvas = document.getElementById("signature-pad");
        if (canvas) {
            var signaturePad = new SignaturePad(canvas, {
                backgroundColor: 'rgba(255, 255, 255, 255)',
                penColor: 'rgb(0, 0, 0)'
            });
        }
    },

    readClipboardText: function () {
        var clipTxt = '';
        navigator.clipboard
            .readText()
            .then((clipText) => (clipTxt = clipText));
        alert(clipTxt);
        return clipTxt;
    },

    copyText: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            alert(text + " :: Copied to Clipboard");
        })
            .catch(function (error) {
                alert(error);
            });
    },

    copyTextSilent: function (text) {
        navigator.clipboard.writeText(text)
            .catch(function (error) {
                alert(error);
            });
    },


    navigateByLatLong: function (lat, lng) {
        // If it's an iPhone..
        if ((navigator.platform.indexOf("iPhone") !== -1) || (navigator.platform.indexOf("iPod") !== -1)) {
            function iOSversion() {
                if (/iP(hone|od|ad)/.test(navigator.platform)) {
                    // supports iOS 2.0 and later
                    var v = (navigator.appVersion).match(/OS (\d+)_(\d+)_?(\d+)?/);
                    return [parseInt(v[1], 10), parseInt(v[2], 10), parseInt(v[3] || 0, 10)];
                }
            }
            var ver = iOSversion() || [0];

            var protocol = 'http://';
            if (ver[0] >= 6) {
                protocol = 'maps://';
            }
            window.location = protocol + 'maps.apple.com/maps?daddr=' + lat + ',' + lng + '&amp;ll=';
        }
        else {
            window.open('http://maps.google.com?daddr=' + lat + ',' + lng + '&amp;ll=');
        }
    },

    openInMaps: function (lat, lng, addrLine) {
        // If it's an iPhone..
        var postfix = '';

        if (lat == null || lng == null || lat.length == 0 || lng.length == 0) {
            if (addrLine == null || addrLine.length == 0) return;
            postfix = addrLine;
        }

        if ((navigator.platform.indexOf("iPhone") !== -1) || (navigator.platform.indexOf("iPod") !== -1)) {
            function iOSversion() {
                if (/iP(hone|od|ad)/.test(navigator.platform)) {
                    // supports iOS 2.0 and later
                    var v = (navigator.appVersion).match(/OS (\d+)_(\d+)_?(\d+)?/);
                    return [parseInt(v[1], 10), parseInt(v[2], 10), parseInt(v[3] || 0, 10)];
                }
            }
            var ver = iOSversion() || [0];
            var protocol = 'http://';
            if (ver[0] >= 6) { protocol = 'maps://'; }
            if (postfix.length > 0)
                window.location = protocol + 'maps.apple.com/maps?q=' + postfix;
            else
                window.location = protocol + 'maps.apple.com/maps?q=' + lat + ',' + lng + '&amp;ll=';
        }
        else {
            if (postfix.length > 0)
                window.open('http://maps.google.com?q=' + postfix);
            else
                window.open('http://maps.google.com?q=' + lat + ',' + lng + '&amp;ll=');
        }
    },
    clearServiceWorkerCache: function () {
        console.log('Entered');
        try {
            caches.delete('dotnet-resources-/');
            caches.delete('dotnet-resources');
            console.log('Done');
        }
        catch (e) {
            console.log(e);
        }
    }
};

window.NavigationManagerExtensions = {};
window.NavigationManagerExtensions.openInNewWindow = (url, message) => {
    var newwindow = window.open('', '_blank');
    newwindow.document.write(message);
    newwindow.location.href = url;
}