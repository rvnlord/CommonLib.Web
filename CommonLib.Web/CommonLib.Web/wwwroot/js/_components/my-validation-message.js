/// <reference path="../../libs/libman/jquery/dist/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class ValidationMessageInputUtils {
    static hideMessageCol($divValidationMessage) {
        const $itemContainer = $divValidationMessage.parent().closest(".my-css-grid-item, .my-col");
        $itemContainer.addClass("my-d-none");
        //if ($itemContainer.hasClass("my-col")) {
           
        //} else if ($itemContainer.hasClass("my-css-grid-item")) {
        //    $itemContainer.css({ "max-height": "0", "max-width": "0", "display": "none" });
        //}
    }

    static showMessageCol($divValidationMessage) {
        const $itemContainer = $divValidationMessage.parent().closest(".my-css-grid-item, .my-col");
        $itemContainer.removeClass("my-d-none");
        //if ($itemContainer.hasClass("my-col")) {
      
        //} else if ($itemContainer.hasClass("my-css-grid-item")) {
        //    $itemContainer.removeCss([ "max-height", "max-width", "margin-bottom" ]);
        //}
    }
}

export function blazor_ValidationMessage_HideCol(divValidationMessage) {
    ValidationMessageInputUtils.hideMessageCol($(divValidationMessage));
}

export function blazor_ValidationMessage_ShowCol(divValidationMessage) {
    ValidationMessageInputUtils.showMessageCol($(divValidationMessage));
}

$(document).ready(function() {

});