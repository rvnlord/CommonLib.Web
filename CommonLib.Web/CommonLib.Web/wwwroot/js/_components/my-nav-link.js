export class NavLinkUtils {
    static NavLinkDotNetRefs = {};

    static async navigateAsync(guid) {
        await DotNet.invokeMethodAsync("CommonLib.Web", "NavLink_ClickAsync", guid);
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

