import "../extensions.js";

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

    //static hover(guid) {
    //    const $navLink = $(guid.guidToSelector());
    //    const isAlreadySetAsInitiallyEnabled = ($navLink.attr("set-as-initially-hovered") || "false").toBool();
    //    const isHovered = $navLink.is(":hover");

    //    if (isHovered) {
    //        console.log(`isAlreadySetAsInitiallyEnabled = ${isAlreadySetAsInitiallyEnabled}`);
    //        console.log(`isHovered = ${isHovered}`);
    //        if (!isAlreadySetAsInitiallyEnabled) {
    //            $navLink.attr("set-as-initially-hovered", "true");
    //            $(guid.guidToSelector()).mouseenter();
    //        }
    //    }
    //}
}

export async function blazor_NavLink_AfterFirstRender(guid, dotNetRefNavLink) {
    NavLinkUtils.cacheNavLinkDotNetRef(guid, dotNetRefNavLink);
}

//export async function blazor_NavLink_AfterRender(guid) {
//    NavLinkUtils.hover(guid);
//}

$(document).ready(function() {

});

