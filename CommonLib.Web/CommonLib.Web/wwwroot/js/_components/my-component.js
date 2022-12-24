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
    $(document).on("mouseenter", ".k-button", function (e) {
        const $btn = $(e.currentTarget);
        const $inputGroup = $btn.closest(".my-input-group");
        let $otherBtns = $btn.siblings(".k-button");

        if ($inputGroup.length === 1) {
            const $igBtns = $inputGroup.find(".my-btn, .k-button").not($btn);
            $.uniqueSort($.merge($otherBtns, $igBtns));
        }
        
        $btn.css("z-index", "1");
        $otherBtns.css("z-index", "0");
    });
});