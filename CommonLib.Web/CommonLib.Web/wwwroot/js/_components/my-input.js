/// <reference path="../../libs/libman/jquery/dist/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";
import utils from "../utils.js";

class InputUtils {
    static initPaddings = { // if I stored it as an attribute it might interfere with blazor rendering
        left: {},
        right: {}
    };

    static fixPaddingForInputGroups($input) {
        if (!$input.parent().is(".my-input-group") && !$input.parents(".k-input").last().parent().is(".my-input-group"))
            return;

        //if ($ti.parents(".k-input").length > 0 && $ti.parents(".k-input").last().classes().contains("k-datepicker")) {
        if ($input.parents(".k-input").length > 0) {
            let cc = $input.parents(".k-input").last().classes();
            let t = 0;
        }

        const syncPaddingGroup = $input.attr("my-input-sync-padding-group") || $input.parents(".k-input").last().classes().singleOrNull(c => c.startsWith("my-input-sync-padding-group_"))?.split("_").last();
        const $tiToSetPadding = syncPaddingGroup ? $(`[my-input-sync-padding-group="${syncPaddingGroup}"], .my-input-sync-padding-group_${syncPaddingGroup}`).$toArray() : [$input];
        const leftPaddings = {};
        const rightPaddings = {};

        for (let $ti of $tiToSetPadding) {
            const guid = $ti.guid();
            const $inputGroup = $ti.parent();
            const $inputGroupPrepend = $inputGroup.children(".my-input-group-prepend").first();
            const $inputGroupAppend = $inputGroup.children(".my-input-group-append").first();
            const prependWidth = $inputGroupPrepend.hiddenDimensions().outerWidth || 0;
            const appendWidth = $inputGroupAppend.hiddenDimensions().outerWidth || 0;

            leftPaddings[guid] = prependWidth;
            rightPaddings[guid] = appendWidth;

            // TEST
            if ($ti.is(".k-datepicker")) {
                let cc = $ti.classes();
                let t = 0;
            }

            if (this.initPaddings.left[guid] === undefined || this.initPaddings.left[guid] === null) { // 0 is valid so can't simply use '!'
                this.initPaddings.left[guid] = parseFloat($ti.css("padding-left"));
            }
            if (this.initPaddings.right[guid] === undefined || this.initPaddings.right[guid] === null) {
                this.initPaddings.right[guid] = parseFloat($ti.css("padding-right"));
            }
        }

        for (let $ti of $tiToSetPadding) {
            const guid = $ti.guid();

            let $ddlConteiner = null;
            if ($ti.is(".my-dropdown")) {
                $ddlConteiner = $ti.children(".my-dropdown-value-and-icon-container").first();
            }
            if ($ti.find(".k-input-inner").length > 0) {
                $ti.find(".k-input-inner").first().css("padding-left", 0);
            }

            const leftInitInputPadding = this.initPaddings.left[guid]; //parseFloat([ null, undefined ].contains(this.initPaddings.left[guid]) ? $ti.css("padding-left") : this.initPaddings.left[guid]); // take init value if assigned, otherwise every element from the same group would get recalculated value and the padding would increase
            const rightInitInputPadding = this.initPaddings.right[guid]; //parseFloat([ null, undefined ].contains(this.initPaddings.right[guid]) ? $ti.css("padding-right") : this.initPaddings.right[guid]); // can't use: 'this.initPaddings.right[guid] || $ti.css("padding-right")' because it would not only move to the right of '||' for 'undefined' and 'null' but also for '0' ('0' is valid padding value)          
            const $inputGroup = $ti.parent();
            const $inputGroupPrepend = $inputGroup.children(".my-input-group-prepend").first();
            const $inputGroupAppend = $inputGroup.children(".my-input-group-append").first();
            const IsRightMostPrependedItemAnIcon = $inputGroupPrepend.children().last().is(".my-icon");
            const IsLeftMostAppendedItemAnIcon = $inputGroupAppend.children().first().is(".my-icon");
            const IsLeftMostAppendedItemABtn = $inputGroupAppend.children().first().is(".my-btn");
            const IsKInputWithAppendedBtn = $ti.is(".k-input") && $ti.children(".k-input-spinner, .k-input-button").length > 0;

            const paddingLeft = leftPaddings.values().max() + (IsRightMostPrependedItemAnIcon ? 0 : leftInitInputPadding);
            const paddingRight = rightPaddings[guid] + (IsLeftMostAppendedItemAnIcon ? 0 : rightInitInputPadding) - (IsKInputWithAppendedBtn && IsLeftMostAppendedItemABtn ? 1 : 0);

            // ------- TEST -------
            if ($ti.classes().contains("k-datepicker")) {
                let cc = $ti.classes();
                let t = 0;
            }

            ($ddlConteiner || $ti).css("padding-left", paddingLeft.px());
            ($ddlConteiner || $ti).css("padding-right", paddingRight.px()); // I don't want to inherit right padding from the sync group, it needs to be taken from the input itself (or it's input group)

            const $passwordMask = $ti.siblings(".my-password-mask").first();
            if ($passwordMask) {
                $passwordMask.css("padding-left", paddingLeft.px());
                $passwordMask.css("padding-right", paddingRight.px());
            }
        }
    }
}

export function blazor_Input_AfterRender(input) {
    InputUtils.fixPaddingForInputGroups($(input));
}

export function blazor_NonNativeInput_FixInputSyncPaddingGroup(guid) {
    InputUtils.fixPaddingForInputGroups($(guid.guidToSelector()).find("input.k-input-inner").single());
}

$(document).ready(function () {
    $(document).on("mouseenter", ".my-input-group .my-btn", function () {
        const $btn = $(this);
        const $inputGroup = $btn.parents(".my-input-group").first();
        const $otherBtns = $inputGroup.find(".my-btn, .k-button").not($btn);

        $btn.css("z-index", "3");
        $otherBtns.css("z-index", "0");
    });

    $(document).on("mouseenter", ".k-button", function (e) {
        const $btn = $(e.currentTarget);
        const $inputGroup = $btn.closest(".my-input-group");
        let $otherBtns = $btn.siblings(".k-button");

        if ($inputGroup.length === 1) {
            const $igBtns = $inputGroup.find(".my-btn, .k-button").not($btn);
            $.uniqueSort($.merge($otherBtns, $igBtns));
        }

        $btn.css("z-index", "3");
        $otherBtns.css("z-index", "0");
    });


    //const MutationObserver = window.MutationObserver || window.WebKitMutationObserver;
    //var observer = new MutationObserver(function (mutations, observer) {
    //    console.log(mutations, observer);
    //    for (const mutation of mutations) {
    //        const isKCalendarContainer = $(mutation.target).is(".k-animation-container-shown") && $(mutation.target).find(".k-calendar").length > 0;
    //        if (isKCalendarContainer) {
    //            const $calendarContainer = $(mutation.target);
    //            const calendarOffset = $calendarContainer.offset();
    //            const x1 = calendarOffset.left;
    //            const y1 = calendarOffset.top;
    //            const $datePickers = $(".k-datepicker, .k-datetimepicker").$toArray();
    //            const $closestDatePicker = $datePickers.select($c => {
    //                const dpOffset = $c.offset();
    //                const x2 = dpOffset.left;
    //                const y2 = dpOffset.top;
    //                return Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2))
    //            });
    //            var t = 0;
    //        }
            
    //        if (mutation.type === 'childList') {

    //            var t1 = mutation.addedNodes;
    //            var t = $(mutation.addedNodes);
    //        }
    //    }
    //});
    //observer.observe(document, { subtree: true, attributes: true, childList: true }); // 


    $(document).on("click", ".k-datepicker > .k-dateinput + .k-button, .k-datetimepicker > .k-dateinput + .k-button", async function (e) {
        if (e.button !== 0 || e.detail > 1) {
            return;
        }

        const $kHiddenAnimationContainers = $(".k-animation-container:not(.k-animation-container-shown)").$toArray();
        const $kShownAnimationContainers = $(".k-animation-container.k-animation-container-shown").$toArray();
        await utils.waitUntilAsync(() => $kHiddenAnimationContainers.any($c => $c.find(".k-calendar").length > 0 || $kShownAnimationContainers.any($c => !$c.is(".k-animation-container-shown"))));
        const $container = $kHiddenAnimationContainers.singleOrNull($c => $c.find(".k-calendar").length > 0);
        if ($container) {
            const width = $(this).closest(".k-datepicker, .k-datetimepicker").outerWidth();
            $container.css("min-width", width.px());
            $container.css("width", width.px()); // or '.k-datetime-wrap' for datetimepicker
        }
    });
});