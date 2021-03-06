/// <reference path="../../libs/libman/jquery/jquery.js" />

import "../extensions.js";

window.blazorDdlOptionOnClick = (e, index, ddlGuid) => {
    if (e.button !== 0) {
        return;
    }
    index = index || "";

    const $ulOptionsContainer = $(`ul[my-guid='${ddlGuid}']`);
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
    $ddlValAndIconContainer.find(".my-dropdown-icon").css("display", "flex");
    $ddlValAndIconContainer.find(".my-dropdown-open-icon").css("display", "none");
};

$(document).ready(() => {

    $(document).on("mousedown", ".my-dropdown-value-and-icon-container", e => {
        if (e.which !== 1) {
            return;
        }

        const $ddl = $(e.target).closest(".my-dropdown");
        const $ulOptionsContainer = $ddl.children(".my-dropdown-options-container").$toArray()[0];
        const $otherOpenedDdls = $(".my-dropdown-options-container").not($ulOptionsContainer).filter(":visible");

        $otherOpenedDdls.finish().animate({
            height: ["hide", "swing"],
            opacity: "hide"
        }, 250, "linear", function() {
            $(this).closest(".my-dropdown").find(".my-dropdown-icon").css("display", "flex");
            $(this).closest(".my-dropdown").find(".my-dropdown-open-icon").css("display", "none");
        });

        $ulOptionsContainer.finish().animate({
            height: ["toggle", "swing"],
            opacity: "toggle"
        }, 250, "linear");
        $ddl.find(".my-dropdown-icon").css("display") === "flex" 
            ? $ddl.find(".my-dropdown-icon").css("display", "none") 
            : $ddl.find(".my-dropdown-icon").css("display", "flex");
        $ddl.find(".my-dropdown-open-icon").css("display") === "flex" 
            ? $ddl.find(".my-dropdown-open-icon").css("display", "none") 
            : $ddl.find(".my-dropdown-open-icon").css("display", "flex");
    });

    $(document).on("mousedown", "body", e => {
        if (e.which !== 1 || $(e.target).parents().add($(e.target)).is(".my-dropdown, .my-dropdown-options-container")) {
            return;
        }

        $(".my-dropdown-options-container").filter(":visible").finish().animate({
            height: ["toggle", "swing"],
            opacity: "toggle"
        }, 250, "linear");
        for (let $ddl of $(".my-dropdown").$toArray()) {
            $ddl.find(".my-dropdown-icon").css("display", "flex");
            $ddl.find(".my-dropdown-open-icon").css("display", "none");
        }
    });
});