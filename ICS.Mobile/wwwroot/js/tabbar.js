// Bottom tab bar auto-hide: fades the bar out while the page scrolls and
// brings it back when scrolling settles. Toggleable from Settings; the
// preference persists in localStorage. CSS hook: html.tabbar-fade .AppTabBar.
window.icsTabBar = (function () {
    var KEY = 'ics-tabbar-autohide';
    var timer = null;
    var enabled = true;
    try { enabled = localStorage.getItem(KEY) !== 'off'; } catch (e) { }

    function show() { document.documentElement.classList.remove('tabbar-fade'); }

    window.addEventListener('scroll', function () {
        if (!enabled) return;
        document.documentElement.classList.add('tabbar-fade');
        clearTimeout(timer);
        timer = setTimeout(show, 350);
    }, { passive: true });

    return {
        getAutoHide: function () { return enabled; },
        setAutoHide: function (v) {
            enabled = !!v;
            try { localStorage.setItem(KEY, enabled ? 'on' : 'off'); } catch (e) { }
            if (!enabled) show();
        }
    };
})();
