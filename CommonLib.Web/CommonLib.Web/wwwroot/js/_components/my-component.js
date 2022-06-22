export class MyComponentBaseUtils {
    static clearValidation() {
        $("input, select").filter(".my-valid").removeClass("my-valid");
    }
}

export function blazor_MyComponentBase_RefreshLayout() {
    MyComponentBaseUtils.clearValidation();
    //MyComponentBaseUtils.showAlerts(); // MyPrompt
}

