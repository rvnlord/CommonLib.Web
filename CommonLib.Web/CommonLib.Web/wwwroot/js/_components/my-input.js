/// <reference path="../../libs/libman/jquery/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";

class TextInputUtils {
    static fixPaddingForInputGroups($input) {
        if (!$input.parent().is(".my-input-group"))
            return;

        const syncPaddingGroup = $input.attr("my-input-sync-padding-group");
        const $tiToSetPadding = syncPaddingGroup ? $(`input[my-input-sync-padding-group="${syncPaddingGroup}"]`).$toArray() : [ $input ];
        const leftPaddings = [];
        const rightPaddings = [];

        for (let $ti of $tiToSetPadding) {
            const $inputGroup = $ti.parent();
            const $inputGroupPrepend = $inputGroup.children(".my-input-group-prepend").first();
            const $inputGroupAppend = $inputGroup.children(".my-input-group-append").first();
            const prependWidth = $inputGroupPrepend.hiddenDimensions().outerWidth || 0;
            const appendWidth = $inputGroupAppend.hiddenDimensions().outerWidth || 0;
            const leftPadding = parseFloat($ti.attr("init-padding-left") || $ti.css("padding-left")); // take init value if assigned, otherwise every element from the same group would get recalculated value and the padding would increase
            const rightPadding = parseFloat($ti.attr("init-padding-right") || $ti.css("padding-right"));
            const IsRightMostPrependedItemAnIcon = $inputGroupPrepend.children().last().is(".my-icon");
            const IsLeftMostAppendedItemAnIcon = $inputGroupAppend.children().first().is(".my-icon");

            const paddingLeft = (IsRightMostPrependedItemAnIcon ? 0 : leftPadding) + prependWidth;
            const paddingRight = (IsLeftMostAppendedItemAnIcon ? 0 : rightPadding) + appendWidth;

            leftPaddings.push(paddingLeft);
            rightPaddings.push(paddingRight);
        }

        for (let $ti of $tiToSetPadding) {
            if (!$ti.css("init-padding-left")) {
                $ti.attr("init-padding-left", $ti.css("padding-left"));
            }
            if (!$ti.css("init-padding-right")) {
                $ti.attr("init-padding-right", $ti.css("padding-right"));
            }
            $ti.css("padding-left", leftPaddings.max().round().px());
            $ti.css("padding-right", rightPaddings.max().round().px());

            const $passwordMask = $ti.siblings(".my-password-mask").first();
            if ($passwordMask) {
                if (!$passwordMask.css("init-padding-left")) {
                    $passwordMask.attr("init-padding-left", $ti.css("padding-left"));
                }
                if (!$passwordMask.css("init-padding-right")) {
                    $passwordMask.attr("init-padding-right", $ti.css("padding-right"));
                }
                $passwordMask.css("padding-left", leftPaddings.max().round().px());
                $passwordMask.css("padding-right", rightPaddings.max().round().px());
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