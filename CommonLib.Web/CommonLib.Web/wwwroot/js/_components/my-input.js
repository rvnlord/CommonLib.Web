/// <reference path="../../libs/libman/jquery/dist/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";
import utils from "../utils.js";

class InputUtils {
    static initPaddings = { // if I stored it as an attribute it might interfere with blazor rendering
        left: {},
        right: {}
    };

    static kSteppers = {};

    static _scrollBoundComponents = {};

    static fixPaddingForInputGroups($input) {
        if (!$input.parent().is(".my-input-group") && !$input.parents(".k-input").last().parent().is(".my-input-group"))
            return;

        const syncPaddingGroup = $input.attr("my-input-sync-padding-group") || $input.classes().singleOrNull(c => c.startsWith("my-input-sync-padding-group_"))?.split("_").last().nullifyIf(x => x === "") || null; //.parents(".k-input").last()
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

            if ($ti.find(".k-input-inner").length > 0) {
                const $kInputInner = $ti.find(".k-input-inner").first();

                if ($kInputInner.parents().is(".my-k-autocomplete-asset") && $inputGroupPrepend.length === 1) { // input group children may not be loaded at this point as they belong to outer component: $inputGroupPrepend.children().length > 0
                    if (!$kInputInner.is(".my-ml--5px")) {
                        $kInputInner.addClass("my-ml--5px");
                    }
                } else {
                    $kInputInner.css("padding-left", "0");
                }
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

    static async bindOverlayScrollBarAsync(guidOrComponent) {
        const $component = utils.is$(guidOrComponent) ? guidOrComponent : $(guidOrComponent.guidToSelector());
        const guid = $component.guid();
        const popupId = $component.attr("aria-controls");
        let $scrollableContent = null;
        if ($component.is(".k-grid")) {
            $scrollableContent = $component.find(".k-grid-content");
        } else if ($component.is(".k-dropdownlist")) {    
            await utils.waitUntilAsync(() => $(`#${popupId}`).find(".k-list-content.k-list-scroller").length > 0);
            $scrollableContent = $(`#${popupId}`).find(".k-list-content.k-list-scroller").first();
        } else if ($component.is(".k-editor")) {    
            $scrollableContent = $component.find(".k-editor-content");
        }

        const alreadyHasScrollbar = $scrollableContent.is(".os-host");
        if ($scrollableContent && $scrollableContent.length === 1 && !alreadyHasScrollbar) {
            this._scrollBoundComponents[guid] = $scrollableContent.addClass("os-host-flexbox").overlayScrollbars({
                className: "os-theme-dark",
                scrollbars: {
                    dragScrolling: true
                },
                callbacks: {
                    onScroll: function () { }
                }
            }).overlayScrollbars();
        }
    }

    static fixNonNativeInputGroupZIndices($btn, wasClicked) {
        const $inputGroup = $btn.closest(".my-input-group");
        let $otherBtns = $btn.siblings(".k-button");

        if ($inputGroup.length === 1) {
            const $igBtns = $inputGroup.find(".my-btn, .k-button").not($btn);
            $.uniqueSort($.merge($otherBtns, $igBtns));
        }

        const $pagerWrap = $btn.closest(".k-pager-numbers-wrap");
        if ($pagerWrap.length > 0) {
            const $navBtns = $pagerWrap.children(".k-pager-nav.k-button");
            const isBtnNav = $btn.is(".k-pager-nav"); // btn can be nav or page number
            const $pagerNumbers = $pagerWrap.children(".k-pager-numbers");
            const $pagerBtns = $pagerNumbers.children(".k-button");
            let $selectedBtn = $pagerWrap.find(".k-button.k-selected");
            if (wasClicked) {
                if (isBtnNav) {
                    if ($navBtns[0] === $btn[0]) { // first page
                        $selectedBtn = $.uniqueSort($selectedBtn.prevAll(".k-button")).first()
                    } else if ($navBtns[1] === $btn[0]) { // next page
                        $selectedBtn = $selectedBtn.next(".k-button")
                    } else if ($navBtns[2] === $btn[0]) { // previous page
                        $selectedBtn = $selectedBtn.prev(".k-button")
                    } else if ($navBtns[3] === $btn[0]) { // last page
                        $selectedBtn = $.uniqueSort($selectedBtn.nextAll(".k-button")).last()
                    }
                } else {
                    $selectedBtn = $btn;
                }
            }
         
            if (isBtnNav) {
                $otherBtns = $.uniqueSort($.merge($otherBtns, $pagerBtns).not($btn).not($selectedBtn));
                $pagerNumbers.css("z-index", "0");
            } else {
                $otherBtns = $.uniqueSort($.merge($otherBtns, $navBtns).not($btn).not($selectedBtn));
                $pagerNumbers.css("z-index", "2");
            }

            if ($selectedBtn) {
                const isSelectedBtnNav = $selectedBtn.is(".k-pager-nav"); 
                if (wasClicked && !isSelectedBtnNav) {
                     $pagerNumbers.css("z-index", "2");
                }
                $selectedBtn.css("z-index", "4");
            }
        }

        $btn.css("z-index", "3");
        $otherBtns.css("z-index", "0");
    }
}

export function blazor_Input_FixInputSyncPaddingGroup(guid) {
    InputUtils.fixPaddingForInputGroups($(guid.guidToSelector()).single());
}

export function blazor_NonNativeInput_FixInputSyncPaddingGroup(guid) {
    InputUtils.fixPaddingForInputGroups($(guid.guidToSelector()).single()); // .find("input.k-input-inner")
}

export function blazor_ExtEditor_FixPlaceholder(guid) {
    const $editor = $(guid.guidToSelector());
    const $placeholder = $(guid.guidToSelector()).next(".k-editor-placeholder");
    if ($placeholder.length === 1) {
        const $editorContent = $editor.children(".k-editor-content").first();
        $placeholder.appendTo($editorContent);
        $placeholder.css("opacity", "1");
    }
}

export async function blazor_ExtComponent_BindOverlayScrollBar(guid) {
    await InputUtils.bindOverlayScrollBarAsync(guid);
}

export function blazor_ExtStepper_AfterFirstRender(guid, kStepperDotDetObj) {
    InputUtils.kSteppers[guid] = kStepperDotDetObj;
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
        InputUtils.fixNonNativeInputGroupZIndices($(this), false);
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

    $(document).on("click", ".k-dropdownlist", async function (e) {
        if (e.button !== 0 || e.detail > 1) {
            return;
        }

         await InputUtils.bindOverlayScrollBarAsync($(this));
    });

    $(document).on("input", ".k-autocomplete > .k-input-inner", async function (e) {
        const id = $(this).attr("aria-controls");
        const $autoCompleteListContainer = $(`.k-animation-container#${id}`);

        await utils.waitUntilAsync(() => $autoCompleteListContainer.find(".k-list-ul > li > *:not(.k-placeholder-line)").length > 0 && $autoCompleteListContainer.css("display") !== "none" || $autoCompleteListContainer.find(".k-no-data, .k-nodata").length > 0);

        if ($autoCompleteListContainer.find(".k-no-data, .k-nodata").length > 0) {
            return;
        }

        const height = $autoCompleteListContainer.find(".k-list-ul > li").$toArray().sum($li => $li.outerHeight());
        $autoCompleteListContainer.find(".k-popup.k-list-container").first().css("height", `${height}px`);
        $autoCompleteListContainer.css("height", `${height}px`);
    });

    $(document).on("click", ".k-grid .k-grid-pager .k-button", async function (e) {
        e.preventDefault();
        const $btn = $(this);
        InputUtils.fixNonNativeInputGroupZIndices($btn, true);
        //const $pagerWrap = $btn.closest(".k-pager-numbers-wrap");
        //const $otherBtns = $pagerWrap.find(".k-button").not($btn);
        //$btn.css("z-index", "4");
        //$otherBtns.css("z-index", "0");
        const guid = $btn.closest(".k-grid").guid();
        InputUtils._scrollBoundComponents[guid].scroll({ y: 0 });
    });

    // this doesn't fix click not always registering for k-step anyway
    //$(document).on("click", ".k-stepper > .k-step-list > .k-step", async function (e) {
    //    if (e.button !== 0 || e.detail > 1) {
    //        return;
    //    }

    //    e.preventDefault();

    //    const $currentStep = $(this);
    //    const $kStepper = $currentStep.parents(".k-stepper").single();
    //    const $steps = $kStepper.find(".k-step");
    //    const currenStepIndex = $steps.index($currentStep);

    //    $steps.removeClass("k-step-done my-k-step-failed k-step-focus");
    //    await InputUtils.kSteppers[$kStepper.guid()].invokeMethodAsync("Stepper_StepChangedAsync", currenStepIndex);
    //    $currentStep.addClass("k-step-current k-step-focus");
    //    $currentStep.prevAll(".k-step").addClass("k-step-done");
    //});
});