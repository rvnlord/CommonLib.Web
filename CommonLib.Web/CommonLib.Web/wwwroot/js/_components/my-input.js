/// <reference path="../../libs/libman/jquery/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class TextInputUtils {
    static initPaddings = { // if I stored it as attribute it might interfere with blazor rendering
        left: {},
        right: {} 
    };

    static fixPaddingForInputGroups($input) {
        if (!$input.parent().is(".my-input-group"))
            return;

        const syncPaddingGroup = $input.attr("my-input-sync-padding-group");
        const $tiToSetPadding = syncPaddingGroup ? $(`input[my-input-sync-padding-group="${syncPaddingGroup}"]`).$toArray() : [ $input ];
        const leftPaddings = {};
        const rightPaddings = {};

        for (let $ti of $tiToSetPadding) {
            const guid = $ti.guid();
            const $inputGroup = $ti.parent();
            const $inputGroupPrepend = $inputGroup.children(".my-input-group-prepend").first();
            const $inputGroupAppend = $inputGroup.children(".my-input-group-append").first();
            const prependWidth = $inputGroupPrepend.hiddenDimensions().outerWidth || 0;
            const appendWidth = $inputGroupAppend.hiddenDimensions().outerWidth || 0;
            const leftPadding = parseFloat(this.initPaddings.left[guid] || $ti.css("padding-left")); // take init value if assigned, otherwise every element from the same group would get recalculated value and the padding would increase
            const rightPadding = parseFloat(this.initPaddings.right[guid] || $ti.css("padding-right"));
            const IsRightMostPrependedItemAnIcon = $inputGroupPrepend.children().last().is(".my-icon");
            const IsLeftMostAppendedItemAnIcon = $inputGroupAppend.children().first().is(".my-icon");

            const paddingLeft = (IsRightMostPrependedItemAnIcon ? 0 : leftPadding) + prependWidth;
            const paddingRight = (IsLeftMostAppendedItemAnIcon ? 0 : rightPadding) + appendWidth;

            leftPaddings[guid] = paddingLeft.round();
            rightPaddings[guid] = paddingRight.round();
        }

        for (let $ti of $tiToSetPadding) {
            const guid = $ti.guid();
            if (!this.initPaddings.left[guid]) {
                this.initPaddings.left[guid] = parseFloat($ti.css("padding-left")).round();
            }
            if (!this.initPaddings.right[guid]) {
                this.initPaddings.right[guid] = parseFloat($ti.css("padding-right")).round();
            }

            $ti.css("padding-left", leftPaddings.values().max().round().px());
            $ti.css("padding-right", rightPaddings[guid].round().px()); // I don't want to inherit right padding from the sync group, it needs to be taken from the input itself (or it's input group)

            const $passwordMask = $ti.siblings(".my-password-mask").first();
            if ($passwordMask) {
                $passwordMask.css("padding-left", leftPaddings.values().max().round().px());
                $passwordMask.css("padding-right", rightPaddings[guid].round().px());
            }
        }
    }
}

export function blazor_Input_AfterRender(input) {
    TextInputUtils.fixPaddingForInputGroups($(input));
}

$(document).ready(function() {
    $(document).on("mouseenter", ".my-input-group .my-btn", function() {
        const $btn = $(this);
        const $inputGroup = $btn.parents(".my-input-group").first();
        const $otherBtns = $inputGroup.find(".my-btn").not($btn);

        $btn.css("z-index", 1);
        $otherBtns.removeCss("z-index");
    });
});