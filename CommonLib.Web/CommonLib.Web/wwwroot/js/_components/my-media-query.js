export class MediaQueryUtils {
    static MediaQueriesData = {};

    static async changeMediaQueryAsync(guid) {
        await MediaQueryUtils.MediaQueriesData[guid].dotNetRef.invokeMethodAsync("MediaQuery_ChangeAsync", MediaQueryUtils.MediaQueriesData[guid].deviceSize);
    }
}

export function blazor_MediaQuery_AfterFirstRender(mediaQuery, deviceSize, guid, mediaQueryDotNetRef) {
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

$(document).ready(function() {

});

