import "../extensions.js";

export class CssGridUtils {
    static async getDeviceSizeAsync(deviceSizes) {
        var deviceNames = deviceSizes.keys();
        for (var i = deviceNames.length - 1; i >= 0; i--) {
            const query = window.matchMedia(deviceSizes[deviceNames[i]]);
            if (query.matches) {
                return deviceNames[i];
            }
        }

        return null;
    }
}

export async function blazor_CssGrid_GetDeviceSizeAsync(deviceSizes) {
    return await CssGridUtils.getDeviceSizeAsync(deviceSizes);
}

$(document).ready(function() {

});

