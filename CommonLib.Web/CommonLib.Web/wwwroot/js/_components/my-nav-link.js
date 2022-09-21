export class NavLinkUtils {
    static async navigateAsync(guid) {
        const $contentContainer = $(".my-page-container > .my-page-content > .my-container");
        if ($contentContainer.hasClass("disable-css-transition"))
            $contentContainer.removeClass("disable-css-transition");

        await DotNet.invokeMethodAsync("CommonLib.Web", "UseNavLinkByGuidAsync", sessionStorage.getItem("SessionId"), guid);
        //await (await NavLinkUtils.getLayoutDotNetRefAsync()).invokeMethodAsync("UseNavLinkByGuidAsync", guid);
    }
}

$(document).ready(function() {

});

