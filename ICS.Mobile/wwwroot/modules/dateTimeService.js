export function init(settings) {
    // Grab any values needed from settings here.
    return true;
}
export function getTimeZone() {
    const options = Intl.DateTimeFormat().resolvedOptions();
    return options.timeZone;
}