/// <reference path="../../libs/libman/jquery/jquery.js" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />

import "../extensions.js";
import animeUtils from "../anime-utils.js";
import { NavBarUtils } from "../navbar-utils.js";

class ModalUtils {
    static moveModalToBodyAndHide($modal) {
        const $modalBg = $modal.parents(".my-modal-background").first();
        $modalBg.prependTo("body");
        $modal.draggable({ "containment": $modalBg });
        ModalUtils.resetAndHideModals($modalBg);
    }

    static resetAndHideModals($modalBgs) {
        $modalBgs.removeClass("my-d-flex").addClass("my-d-none");
        $modalBgs.children(".my-modal").removeClass("shown");
        $(".my-body, #app").removeCss("filter");
        $(".my-body, #app").removeCss("transform");
    }

    static createShowModalAnims($modalBg) {
        const $modal = $modalBg.children(".my-modal");

        return [ anime({
            targets: $modalBg[0],
            opacity: [0, 1],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function() {
                $modalBg.removeClass("my-d-none").addClass("my-d-flex");
                $(".my-body, #app").css("filter", "blur(8px)"); // animating blur is too intensive
            },
            complete: function(anim) {
                if (!anim.began) {
                    $modalBg.removeClass("my-d-none").addClass("my-d-flex");
                    $(".my-body, #app").css("filter", "blur(8px)");
                }
            }
        }), anime({
            targets: $modal[0],
            scaleX: [0.5, 1],
            scaleY: [0.5, 1],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false
        }) ];
    }

    static createHideModalAnims($modalBg) {
        const $modal = $modalBg.children(".my-modal");

        return [ anime({
            targets: $modalBg[0],
            opacity: [1, 0],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function() {
                $(".my-body, #app").removeCss("filter"); // animating blur is too intensive
            },
            complete: function(anim) {
                if (!anim.began) {
                    $(".my-body, #app").removeCss("filter");
                }

                $modalBg.removeClass("my-d-flex").addClass("my-d-none");
            }
        }), anime({
            targets: $modal[0],
            scaleX: [1, 0.5],
            scaleY: [1, 0.5],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false
        }) ];
    }

    static adjustToDeviceSize() {
        const $modals = $(".my-modal");
        $modals.removeCss("left"); // remove css set by draggable to center modal if user moved it and then resized the window
        $modals.removeCss("top");
        //ModalUtils.resetAndHideModals($modalBgs); // modals should specifically be kept open (for instance when modal is showing login in progress it shouldn't be hidden if user resizes the windoww)
    }

    static async hideModalAsync($modalToHide) {
        $modalToHide.removeClass("shown");
        const $modalBgToHide = $modalToHide.parent().closest(".my-modal-background");
        animeUtils.finishRunningAnimations([ $modalBgToHide, $modalToHide ]);
        const anims = ModalUtils.createHideModalAnims($modalBgToHide);
        await animeUtils.runAndAwaitAnimationsAsync(anims);
    }

    static async showModalAsync($modalToShow) {
        const $modalBg = $modalToShow.parents(".my-modal-background").first();
        const showModal = !$modalBg.is(".shown");
       
        const anims = [];

        $modalToShow.toggleClass("shown");
        animeUtils.finishRunningAnimations([ $modalBg, $modalToShow ]);
        if (showModal) {
            anims.push(...ModalUtils.createShowModalAnims($modalBg));
        } else {
            anims.push(...ModalUtils.createHideModalAnims($modalBg));
        }

        $("#promptMain").prependTo("body");
        $("#promptMain").css("z-index", "101");
        $("#promptMain").css("top", "0");

        await animeUtils.runAndAwaitAnimationsAsync(anims);
    }

}

export function blazor_Modal_AfterFirstRender(modal) {
    ModalUtils.moveModalToBodyAndHide($(modal));
}

export function blazor_Modal_AfterRender() {
    $("#promptMain").appendTo($(".my-navbar").first().parent());
    $("#promptMain").css("z-index", "9");
    NavBarUtils.setStickyNavBarStyles($(".my-navbar").first());
}

export async function blazor_Modal_HideAsync(modal) {
    await ModalUtils.hideModalAsync($(modal));
}

export async function blazor_Modal_ShowAsync(modal) {
    await ModalUtils.showModalAsync($(modal));
}

$(document).ready(function() {
    $(document).on("click", ".my-modal .my-close", async function(e) {
        const $modalBgToHide = $(this).parents(".my-modal-background").first();
        const $modalToHide = $modalBgToHide.children(".my-modal").first();

        if (e.which !== 1 || !$modalToHide.is(".shown")) {
            return;
        }

        $("#promptMain").appendTo($(".my-navbar").first().parent());
        $("#promptMain").css("z-index", "9");
        NavBarUtils.setStickyNavBarStyles($(".my-navbar").first());

        await ModalUtils.hideModalAsync($modalToHide);
    });

    $(document).on("click", ".my-modal-background", function(e) {
        const $modalBgToHide = $(this);
        const $modalToHide = $modalBgToHide.children(".my-modal").first();
        const $btnClose = $modalToHide.find(".my-close"); // it will find all (`dismiss` and `x`)
        
        if (e.which !== 1 || $(e.target).parents().add($(e.target)).is(".my-modal, .my-modal .my-close, .my-nav-item.my-login") || $btnClose.is(":disabled")) {
            return;
        }

        if (!$modalToHide.is(".shown")) { // special case, speed up hiding if user clicked the bg when closing the modal
            animeUtils.finishRunningAnimations([ $modalBgToHide, $modalToHide ]);
            return;
        }

        $("#promptMain").appendTo($(".my-navbar").first().parent());
        $("#promptMain").css("z-index", "9");
        NavBarUtils.setStickyNavBarStyles($(".my-navbar").first());

        $modalToHide.removeClass("shown");

        animeUtils.finishRunningAnimations([ $modalBgToHide, $modalToHide ]);
        const anims = ModalUtils.createHideModalAnims($modalBgToHide);
        animeUtils.runAnimations(anims);
    });

    $(document).on("click", "body", function(e) {
        const $modalBgsToHide = $(".my-modal.shown").$toArray().map($m => $m.parents(".my-modal-background"));
        const isAnyModalShown = $modalBgsToHide.length > 0;
        
        if (e.which !== 1 || $(e.target).parents().add($(e.target)).is(".my-modal-background, .my-modal, .my-modal .my-close, .my-nav-item.my-login, .my-prompt") 
            || !isAnyModalShown) {
            return;
        }

        $("#promptMain").appendTo($(".my-navbar").first().parent());
        $("#promptMain").css("z-index", "9");
        NavBarUtils.setStickyNavBarStyles($(".my-navbar").first());

        for (let $modalBgToHide of $modalBgsToHide) {
            const $modalToHide = $modalBgToHide.children(".my-modal").first();
            $modalToHide.removeClass("shown");
            animeUtils.finishRunningAnimations([ $modalBgToHide, $modalToHide ]);
            const anims = ModalUtils.createHideModalAnims($modalBgToHide);
            animeUtils.runAnimations(anims);
        }
    });

    $(document).on("click", "[my-opens-modal]", async function(e) {
        const $btnToggle = $(this);
        const $modal = $($btnToggle.attr("my-opens-modal"));
        const $nbs = $(".my-navbar");
        const $searchContainers = $nbs.find(".my-nav-search-container");
        const $otherModals = $(".my-modal").not($modal);
        if (e.which !== 1 || $searchContainers.is(".shown") || $otherModals.is(".shown")) {
            return;
        }

        await ModalUtils.showModalAsync($modal);
    });

    $(window).on("resize", function() {
        ModalUtils.adjustToDeviceSize(); // modals should specifically be kept open (for instance when modal is showing login in progress it shouldn't be hidden if user resizes the windoww)
    });
});