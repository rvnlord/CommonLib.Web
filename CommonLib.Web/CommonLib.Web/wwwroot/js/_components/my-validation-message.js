/// <reference path="../../libs/libman/jquery/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class ValidationMessageInputUtils {
    static hideMessageCol($divValidationMessage) {
        $divValidationMessage.parent().closest("[class^='my-col'").addClass("my-d-none");
    }

    static showMessageCol($divValidationMessage) {
        $divValidationMessage.parent().closest("[class^='my-col'").removeClass("my-d-none");
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