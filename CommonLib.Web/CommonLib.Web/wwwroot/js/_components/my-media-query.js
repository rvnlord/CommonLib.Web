export class MediaQueryUtils {
    static MediaQueriesData = {};

    static async setupMediaQueryAsync(mediaQuery, deviceSize, guid, mediaQueryDotNetRef) {
        MediaQueryUtils.MediaQueriesData[guid] = {
            "dotNetRef": mediaQueryDotNetRef,
            "query": mediaQuery, 
            "deviceSize": deviceSize
        };
        const query = window.matchMedia(mediaQuery);
        query.onchange = async function(e) {
            if (e.matches) {
                await MediaQueryUtils.changeMediaQueryAsync(guid);
            }
        };
    }

    static async changeMediaQueryAsync(guid, deviceSize) {
        await MediaQueryUtils.MediaQueriesData[guid].dotNetRef.invokeMethodAsync("MediaQuery_ChangeAsync", deviceSize || MediaQueryUtils.MediaQueriesData[guid].deviceSize);
    }
}

export async function blazor_MediaQuery_AfterFirstRender(mediaQuery, deviceSize, guid, mediaQueryDotNetRef) {
    await MediaQueryUtils.setupMediaQueryAsync(mediaQuery, deviceSize, guid, mediaQueryDotNetRef);
}

export async function blazor_MediaQuery_SetupForAllDevicesAndGetDeviceSizeAsync(deviceSizes, guid, dotNetRef) {
    let deviceNames = deviceSizes.keys();

    MediaQueryUtils.MediaQueriesData[guid] = {
        "dotNetRef": dotNetRef,
        "deviceSizesWithQueries": deviceSizes
    };

    let currentDeviceSize = null;
    for (let i = deviceNames.length - 1; i >= 0; i--) {
        const deviceName = deviceNames[i]; // to avoid captured closure of "i"
        const query = window.matchMedia(deviceSizes[deviceName]);
        if (query.matches) {
            currentDeviceSize = deviceNames[i];
        }
        query.onchange = async function(e) {
            if (e.matches) {
                await MediaQueryUtils.changeMediaQueryAsync(guid, deviceName);
            }
        };
    }

    return currentDeviceSize;
}

$(document).ready(function() {

});

