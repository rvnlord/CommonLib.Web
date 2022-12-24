/// <reference path="../../libs/libman/jquery/dist/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class InputUtils {
    static initPaddings = { // if I stored it as an attribute it might interfere with blazor rendering
        left: {},
        right: {} 
    };

    static fixPaddingForInputGroups($input) {
        if (!$input.parent().is(".my-input-group") && !$input.closest(".k-input").parent().is(".my-input-group"))
            return;

        //if ($input.parents(".k-input").length === 1) {
        //    let t = 0;
        //}

        const syncPaddingGroup = $input.attr("my-input-sync-padding-group") || $input.closest(".k-input").classes().singleOrNull(c => c.startsWith("my-input-sync-padding-group_"))?.split("_").last();
        const $tiToSetPadding = syncPaddingGroup ? $(`[my-input-sync-padding-group="${syncPaddingGroup}"], .my-input-sync-padding-group_${syncPaddingGroup}`).$toArray() : [ $input ];
        const leftPaddings = {};
        const rightPaddings = {};

        for (let $ti of $tiToSetPadding) { 
            const guid = $ti.guid();
            const $inputGroup = $ti.parent();
            const $inputGroupPrepend = $inputGroup.children(".my-input-group-prepend").first();
            const $inputGroupAppend = $inputGroup.children(".my-input-group-append").first();
            const prependWidth = $inputGroupPrepend.hiddenDimensions().outerWidth || 0;
            const appendWidth = $inputGroupAppend.hiddenDimensions().outerWidth || 0;
            const leftPadding = parseFloat([ null, undefined ].contains(this.initPaddings.left[guid]) ? $ti.css("padding-left") : this.initPaddings.left[guid]); // take init value if assigned, otherwise every element from the same group would get recalculated value and the padding would increase
            const rightPadding = parseFloat([ null, undefined ].contains(this.initPaddings.right[guid]) ? $ti.css("padding-right") : this.initPaddings.right[guid]); // can't use: 'this.initPaddings.right[guid] || $ti.css("padding-right")' because it would not only move to the right of '||' for 'undefined' and 'null' but also for '0' ('0' is valid padding value)
            const IsRightMostPrependedItemAnIcon = $inputGroupPrepend.children().last().is(".my-icon");
            const IsLeftMostAppendedItemAnIcon = $inputGroupAppend.children().first().is(".my-icon");

            const paddingLeft = (IsRightMostPrependedItemAnIcon ? 0 : leftPadding) + prependWidth;
            let paddingRight = (IsLeftMostAppendedItemAnIcon ? 0 : rightPadding) + appendWidth;

            const IsLeftMostAppendedItemABtn = $inputGroupAppend.children().first().is(".my-btn");

            if ($ti.is(".k-input") && $ti.children(".k-input-spinner").length === 1 && IsLeftMostAppendedItemABtn) {
                paddingRight--; // merge borders of adjacent btns
            }

            leftPaddings[guid] = paddingLeft;
            rightPaddings[guid] = paddingRight;
        }

        for (let $ti of $tiToSetPadding) {
            const guid = $ti.guid();
            if (this.initPaddings.left[guid] === undefined || this.initPaddings.left[guid] === null) { // 0 is valid so can't simply use '!'
                this.initPaddings.left[guid] = parseFloat($ti.css("padding-left"));
            }
            if (this.initPaddings.right[guid] === undefined || this.initPaddings.right[guid] === null) {
                this.initPaddings.right[guid] = parseFloat($ti.css("padding-right"));
            }

            let $ddlConteiner = null;
            if ($ti.is(".my-dropdown")) {
                $ddlConteiner = $ti.children(".my-dropdown-value-and-icon-container").first();
            }

            ($ddlConteiner || $ti).css("padding-left", leftPaddings.values().max().px());
            ($ddlConteiner || $ti).css("padding-right", rightPaddings[guid].px()); // I don't want to inherit right padding from the sync group, it needs to be taken from the input itself (or it's input group)

            const $passwordMask = $ti.siblings(".my-password-mask").first();
            if ($passwordMask) {
                $passwordMask.css("padding-left", leftPaddings.values().max().px());
                $passwordMask.css("padding-right", rightPaddings[guid].px());
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

$(document).ready(function() {
    $(document).on("mouseenter", ".my-input-group .my-btn", function() {
        const $btn = $(this);
        const $inputGroup = $btn.parents(".my-input-group").first();
        const $otherBtns = $inputGroup.find(".my-btn, .k-button").not($btn);

        $btn.css("z-index", "1");
        $otherBtns.css("z-index", "0");
    });
});