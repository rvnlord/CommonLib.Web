/// <reference path="../../libs/libman/jquery/dist/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";
import Semaphore from "../semaphore.js";

class PasswordInputUtils {
    static IsPasswordVisible = {};
    static PasswordInputsDotNetRefs = {};
    static PasswordInputsValues = {};
    static IsServerBeingQueriedOnInputChange = {};
    static SyncPasswordInput = {};
    static SyncPasswordServerQuery = {};
}

export function blazor_PasswordInput_AfterFirstRender(value, guid, passwordInputDotNetRef) {
    PasswordInputUtils.IsPasswordVisible[guid] = false;
    PasswordInputUtils.PasswordInputsDotNetRefs[guid] = passwordInputDotNetRef;
    PasswordInputUtils.PasswordInputsValues[guid] = value || "";
    PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid] = false;
    PasswordInputUtils.SyncPasswordInput[guid] = new Semaphore(1);
    PasswordInputUtils.SyncPasswordServerQuery[guid] = new Semaphore(1);
}

export function blazor_PasswordInput_AfterRender(value, guid) {
    value = value || "";
    const $passwordInput = $(guid.guidToSelector());
    if (!PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid]) {
        PasswordInputUtils.PasswordInputsValues[guid] = value;
    }
    const $btnTogglePassword = $passwordInput.nextAll("div.my-input-group-append").find(".my-btn-toggle-password-visibility");
    const $iconPasswordShown = $btnTogglePassword.find(".my-icon-password-shown");
    const $iconPasswordHidden = $btnTogglePassword.find(".my-icon-password-hidden");
    console.log(`PasswordInputUtils.IsPasswordVisible[guid] = ${PasswordInputUtils.IsPasswordVisible[guid]}`);

    if (PasswordInputUtils.IsPasswordVisible[guid]) {
        $iconPasswordHidden.removeClass("my-d-flex").addClass("my-d-none");
        $iconPasswordShown.removeClass("my-d-none").addClass("my-d-flex");
        if (!PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid]) {
            $passwordInput.prop("value", value);
        }
    } else {
        $iconPasswordShown.removeClass("my-d-flex").addClass("my-d-none");
        $iconPasswordHidden.removeClass("my-d-none").addClass("my-d-flex");
        if (!PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid]) {
            $passwordInput.prop("value", value); $passwordInput.prop("value", value.split("").map(() => "●").join(""));
        }
    }
}

$(document).ready(function () {

    $(document).on("input", ".my-password-input", async function () {
        const $passwordInput = $(this);
        const guid = $passwordInput.guid();
        console.log(`guid = ${guid}`);

        await PasswordInputUtils.SyncPasswordInput[guid].waitAsync();
        console.log(`entered: SyncPasswordInput`);
        const newValue = $passwordInput.prop("value");
        const caretPosition = Math.max($passwordInput[0].selectionStart, $passwordInput[0].selectionEnd);
        let value;

        if (PasswordInputUtils.IsPasswordVisible[guid]) {
            value = newValue;
            PasswordInputUtils.PasswordInputsValues[guid] = value;
        } else {
            const newValueUntilCaret = newValue.take(caretPosition);
            const unchangedCharsAtStart = newValueUntilCaret.takeWhile(c => c === "●").length;
            const unchangedCharsAtEnd = newValue.skip(caretPosition).length;
            const insertedValue = newValueUntilCaret.skip(unchangedCharsAtStart);
            const oldValue = PasswordInputUtils.PasswordInputsValues[guid] || ""; // first time it will be undefined
            console.log(`oldValue = ${oldValue}, as kvp: ${PasswordInputUtils.PasswordInputsValues.kvps().filter(kvp => kvp.key === guid).first().value}`);
            value = oldValue.take(unchangedCharsAtStart) + insertedValue + oldValue.takeLast(unchangedCharsAtEnd);
            console.log(`value = ${value}`);
            PasswordInputUtils.PasswordInputsValues[guid] = value;
            console.log(`setting PasswordInputUtils.PasswordInputsValues[guid] value to: ${value} | PasswordInputUtils.PasswordInputsValues[guid] = ${PasswordInputUtils.PasswordInputsValues[guid]}`);
            $passwordInput.prop("value", value.split("").map(() => "●").join(""));
            console.log(`setting prop value to: ${value} | $passwordInput.prop("value") = ${$passwordInput.prop("value")}`);
            $passwordInput[0].setSelectionRange(caretPosition, caretPosition);
        }

        console.log(`released: SyncPasswordInput`);
        await PasswordInputUtils.SyncPasswordInput[guid].releaseAsync();

        await PasswordInputUtils.SyncPasswordServerQuery[guid].waitAsync();
        PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid] = true;

        const currentValue = PasswordInputUtils.PasswordInputsValues[guid];
        console.log(`value (${value}) ${value === currentValue ? "===" : "!=="} (${currentValue}) PasswordInputUtils.PasswordInputsValues[guid]`);
        if (value === currentValue) {
            await PasswordInputUtils.PasswordInputsDotNetRefs[guid].invokeMethodAsync("PasswordInput_InputAsync", value);
        }

        PasswordInputUtils.IsServerBeingQueriedOnInputChange[guid] = false;
        await PasswordInputUtils.SyncPasswordServerQuery[guid].releaseAsync();
    });

    $(document).on("click", ".my-btn-toggle-password-visibility", function (e) {
        e.preventDefault();

        const $btnTogglePassword = $(this);
        const $iconPasswordShown = $btnTogglePassword.find(".my-icon-password-shown");
        const $iconPasswordHidden = $btnTogglePassword.find(".my-icon-password-hidden");
        const $passwordInput = $btnTogglePassword.parents(".my-input-group").first().children(".my-password-input").first();
        const guid = $passwordInput.guid();
        const value = PasswordInputUtils.PasswordInputsValues[guid];

        if (!PasswordInputUtils.IsPasswordVisible[guid]) {
            $iconPasswordHidden.removeClass("my-d-flex").addClass("my-d-none");
            $iconPasswordShown.removeClass("my-d-none").addClass("my-d-flex");
            $passwordInput.prop("value", value);
            PasswordInputUtils.IsPasswordVisible[guid] = true;
        } else {
            $iconPasswordShown.removeClass("my-d-flex").addClass("my-d-none");
            $iconPasswordHidden.removeClass("my-d-none").addClass("my-d-flex");
            $passwordInput.prop("value", value.split("").map(() => "●").join(""));
            PasswordInputUtils.IsPasswordVisible[guid] = false;
        }
    });
});