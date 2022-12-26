export class MyComponentBaseUtils {
    static clearValidation() {
        $("input, select").filter(".my-valid").removeClass("my-valid");
    }
}

export function blazor_MyComponentBase_RefreshLayout() {
    MyComponentBaseUtils.clearValidation();
    //MyComponentBaseUtils.showAlerts(); // MyPrompt
}

$(document).ready(function () { // ensure to not end up here more than once if utlizing this modulke somewhere else than in Layout

});