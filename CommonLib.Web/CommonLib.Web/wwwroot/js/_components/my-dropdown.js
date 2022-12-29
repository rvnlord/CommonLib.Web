/// <reference path="../../libs/libman/jquery/dist/jquery.js" />

import "../extensions.js";

export class DropdownUtils {
    static _dropDownDotNetRefs = {};

    static async selectDdlOptionAsync(index, ddlGuid) {
        index = index || "";

        const $ulOptionsContainer = $(`div[my-guid='${ddlGuid}'] > ul.my-dropdown-options-container`);
        const $ddl = $ulOptionsContainer.closest(".my-dropdown");
        const $liOption = $ulOptionsContainer.children(`li[value='${index}']`).first();
        const val = $liOption.attr("value"); // not 'val()' because it would be 'int' and would default to '0' for empty value
        const $ddlValAndIconContainer = $ddl.children(".my-dropdown-value-and-icon-container").first();
        const $ddlValue = $ddlValAndIconContainer.children(".my-dropdown-value").first(); // use 'find' to look recursively

        $ddlValAndIconContainer.focus();
    
        $ddlValue.attr("value", val);
        $ddlValue.text($liOption.text());

        $ulOptionsContainer.finish().animate({
            height: ["hide", "swing"],
            opacity: "hide"
        }, 250, "linear");
        $ddlValAndIconContainer.closest(".my-input-group").find(".my-dropdown-open-icon").css("display", "flex");
        $ddlValAndIconContainer.closest(".my-input-group").find(".my-dropdown-close-icon").css("display", "none");

        this._dropDownDotNetRefs[ddlGuid].invokeMethodAsync("DdlOption_ClickAsync", parseInt(index) || null);
    }
}

export function blazor_Dropdown_AfterFirstRender(guid, dropDownDotNetRef) {
    return DropdownUtils._dropDownDotNetRefs[guid] = dropDownDotNetRef;
}

//export async function blazor_DdlOption_ClickAsync(e, index, ddlGuid) {
//    return await DropdownUtils.selectDdlOptionAsync(e, index, ddlGuid);
//}

$(document).ready(function() {
    $(document).on("mousedown", ".my-input-group.my-dropdown-input-group", e => {
        const $clickedPart = $(e.target);
        if ($clickedPart.parents().addBack().is(".my-dropdown-option")) {
            return;
        }

        const $ddl = $(e.currentTarget).children(".my-dropdown:not(.disabled):not([disabled])").first();
        if (!$ddl || e.which !== 1) {
            return;
        }

        const $ulOptionsContainer = $ddl.children(".my-dropdown-options-container").$toArray()[0];
        const $otherOpenedDdls = $(".my-dropdown-options-container").not($ulOptionsContainer).filter(":visible");

        $otherOpenedDdls.finish().animate({
            height: ["hide", "swing"],
            opacity: "hide"
        }, 250, "linear", function() {
            $(this).closest(".my-dropdown").closest(".my-input-group").find(".my-dropdown-open-icon").css("display", "flex");
            $(this).closest(".my-dropdown").closest(".my-input-group").find(".my-dropdown-close-icon").css("display", "none");
        });

        $ulOptionsContainer.finish().animate({
            height: ["toggle", "swing"],
            opacity: "toggle"
        }, 250, "linear");
        $ddl.closest(".my-input-group").find(".my-dropdown-open-icon").css("display") === "flex" 
            ? $ddl.closest(".my-input-group").find(".my-dropdown-open-icon").css("display", "none") 
            : $ddl.closest(".my-input-group").find(".my-dropdown-open-icon").css("display", "flex");
        $ddl.closest(".my-input-group").find(".my-dropdown-close-icon").css("display") === "flex" 
            ? $ddl.closest(".my-input-group").find(".my-dropdown-close-icon").css("display", "none") 
            : $ddl.closest(".my-input-group").find(".my-dropdown-close-icon").css("display", "flex");
    });

    $(document).on("mousedown", "body", e => {
        if (e.which !== 1 || $(e.target).parents().add($(e.target)).is(".my-dropdown-input-group, .my-dropdown, .my-dropdown-options-container")) {
            return;
        }

        $(".my-dropdown-options-container").filter(":visible").finish().animate({
            height: ["toggle", "swing"],
            opacity: "toggle"
        }, 250, "linear");
        for (let $ddl of $(".my-dropdown").$toArray()) {
            $ddl.closest(".my-input-group").find(".my-dropdown-open-icon").css("display", "flex");
            $ddl.closest(".my-input-group").find(".my-dropdown-close-icon").css("display", "none");
        }
    });

    $(document).on("mousedown", ".my-dropdown:not([disabled]):not(.disabled) .my-dropdown-option", async e => {
        if (e.which !== 1 || e.detail > 1) {
            return;
        }

        const $ddlOption = $(e.currentTarget);
        const index = parseInt($ddlOption.attr("value")) || null;
        const ddlGuid = $ddlOption.closest(".my-dropdown").guid();
        await DropdownUtils.selectDdlOptionAsync(index, ddlGuid);
    });
});