export class NavLinkUtils {
    static NavLinkDotNetRefs = {};

    static async navigateAsync(guid) {
        const $contentContainer = $(".my-page-container > .my-page-content > .my-container");
        if ($contentContainer.hasClass("disable-css-transition"))
            $contentContainer.removeClass("disable-css-transition");
        await DotNet.invokeMethodAsync("CommonLib.Web", "NavLink_ClickAsync", guid, sessionStorage.getItem("SessionId"));
        //await NavLinkUtils.NavLinkDotNetRefs[guid].invokeMethodAsync("NavLink_ClickAsync", "MyNavLinkBase");
    }

    static setNavLinkDotNetRefs(navLinkDotNetRefs) {
        NavLinkUtils.NavLinkDotNetRefs = navLinkDotNetRefs;
    }
}

//export function blazor_NavLink_AfterRender(navLinkDotNetRef, guid) {
//    NavLinkUtils.NavLinkDotNetRefs[guid] = navLinkDotNetRef;
//}

$(document).ready(function() {

});

