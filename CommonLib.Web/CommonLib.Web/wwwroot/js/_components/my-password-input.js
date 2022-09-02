/// <reference path="../../libs/libman/jquery/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class PasswordInputUtils {
    static ArePasswordsVisible = {};
    static PasswordInputsDotNetRefs = {};
    static PasswordInputsValues = {};
}

export function blazor_PasswordInput_AfterFirstRender(value, guid, passwordInputDotNetRef) {
    PasswordInputUtils.ArePasswordsVisible[guid] = false;
    PasswordInputUtils.PasswordInputsDotNetRefs[guid] = passwordInputDotNetRef;
    PasswordInputUtils.PasswordInputsValues[guid] = value || "";
}

export function blazor_PasswordInput_AfterRender(value, guid) {
    value = value || "";
    const $passwordInput = $(guid.guidToSelector());
   
    PasswordInputUtils.PasswordInputsValues[guid] = value;

    if (PasswordInputUtils.ArePasswordsVisible[guid]) {
        $passwordInput.prop("value", value);
    } else {
        $passwordInput.prop("value", value.split("").map(() => "●").join(""));
    }
}

$(document).ready(function() {

    $(document).on("input", ".my-password-input", async function() {
        const $passwordInput = $(this);
        const newValue = $passwordInput.prop("value");
        const oldValue = PasswordInputUtils.PasswordInputsValues[$passwordInput.guid()] || ""; // first time it will be undefined
        const caretPosition = Math.max($passwordInput[0].selectionStart, $passwordInput[0].selectionEnd);
        const guid = $passwordInput.guid();
        let value;

        if (PasswordInputUtils.ArePasswordsVisible[guid]) {
            value = newValue;
            PasswordInputUtils.PasswordInputsValues[guid] = value;
        } else {
            const newValueUntilCaret = newValue.take(caretPosition);
            const unchangedCharsAtStart = newValueUntilCaret.takeWhile(c => c === "●").length;
            const unchangedCharsAtEnd = newValue.skip(caretPosition).length;
            const insertedValue = newValueUntilCaret.skip(unchangedCharsAtStart);
            value = oldValue.take(unchangedCharsAtStart) + insertedValue + oldValue.takeLast(unchangedCharsAtEnd);
            PasswordInputUtils.PasswordInputsValues[guid] = value;
            $passwordInput.prop("value", value.split("").map(() => "●").join(""));
            $passwordInput[0].setSelectionRange(caretPosition, caretPosition);
        }
   
        await PasswordInputUtils.PasswordInputsDotNetRefs[guid].invokeMethodAsync("PasswordInput_InputAsync", value);
    });

    $(document).on("click", ".my-btn-toggle-password-visibility", function(e) {
        e.preventDefault();

        const $btnTogglePassword = $(this);
        const $iconPasswordShown = $btnTogglePassword.find(".my-icon-password-shown");
        const $iconPasswordHidden = $btnTogglePassword.find(".my-icon-password-hidden");
        const $passwordInput = $btnTogglePassword.parents(".my-input-group").first().children(".my-password-input").first();
        const guid = $passwordInput.guid();
        const value = PasswordInputUtils.PasswordInputsValues[guid];

        if (!PasswordInputUtils.ArePasswordsVisible[guid]) {
            $iconPasswordHidden.removeClass("my-d-flex").addClass("my-d-none");
            $iconPasswordShown.removeClass("my-d-none").addClass("my-d-flex");
            $passwordInput.prop("value", value);
            PasswordInputUtils.ArePasswordsVisible[guid] = true;
        } else {
            $iconPasswordShown.removeClass("my-d-flex").addClass("my-d-none");
            $iconPasswordHidden.removeClass("my-d-none").addClass("my-d-flex");
            $passwordInput.prop("value", value.split("").map(() => "●").join(""));
            PasswordInputUtils.ArePasswordsVisible[guid] = false;
        }
    });
});