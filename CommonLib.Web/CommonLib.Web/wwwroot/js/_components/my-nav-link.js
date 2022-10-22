export class NavLinkUtils {
    static _navLinksCache = {};

    static cacheNavLinkDotNetRef(guid, dotNetRefNavLink) {
        this._navLinksCache.addIfNotExistsAndGet(guid, {}).dotNetRef = dotNetRefNavLink;
    }

    static async navigateAsync(guid) {
        const $contentContainer = $(".my-page-container > .my-page-content > .my-container");
        if ($contentContainer.hasClass("disable-css-transition"))
            $contentContainer.removeClass("disable-css-transition");

        await this._navLinksCache[guid].dotNetRef.invokeMethodAsync("NavLink_Click", guid);
    }
}

export async function blazor_NavLink_AfterFirstRender(guid, dotNetRefFileUpload) {
    NavLinkUtils.cacheNavLinkDotNetRef(guid, dotNetRefFileUpload);
}

$(document).ready(function() {

});

