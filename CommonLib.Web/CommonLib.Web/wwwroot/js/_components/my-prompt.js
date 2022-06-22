/// <reference path="../../libs/libman/@types/jquery/index.d.ts" />
/// <reference path="../../libs/libman/@types/animejs/index.d.ts" />
import animeUtils from "../anime-utils.js";
import "../extensions.js";
import Semaphore from "../semaphore.js";
import utils from "../utils.js";
import { NavBarUtils } from "../navbar-utils.js";

export class PromptUtils {
    static _isShowAnimationBeingPrepared = false;
    static _rowHeights = {};
    static _promptAnims = [];
    static _syncAnimationBatch = new Semaphore(1);

    static async showTestNotificationsAsync() {
        await DotNet.invokeMethodAsync("CommonLib.Web", "ShowTestNotificationsAsync");
    }

    static performInitialCleanup($arrAllNotifications) {
        const $prompt = $arrAllNotifications.first().closest(".my-prompt");
        const $clearAll = $prompt.children(".notifications-actions-container");
        const $arrNotificationRows = $prompt.find(".my-notification").closest(".my-row").$toArray();

        //const anims = animeUtils.getRunningAnimations([ $prompt, $clearAll, ...$arrNotificationRows ]);

        animeUtils.finishAnimations(this._promptAnims);
        if (!this._promptAnims.all(a => a.completed)) {
            throw new Error();
        }
        this._promptAnims.clear();

        const allNotificationsGuids = $arrAllNotifications.map($n => $n.guid());
        const storedNotificationsGuids = this._rowHeights.kvps().map(kvp => kvp.key);
        const notificationsToCleanGuids = storedNotificationsGuids.except(allNotificationsGuids);
        for (let $notificationToCleanGuid of notificationsToCleanGuids) {
            delete this._rowHeights[$notificationToCleanGuid]; 
        }
    }

    static setAlreadyShownAnimationRow($notification) {
        const $renderedNotificationRow = $notification.closest(".my-row").first();
        $renderedNotificationRow.removeClass("my-d-none");
        $renderedNotificationRow.removeCss("height");
        $renderedNotificationRow.removeCss("opacity");
    }

    static animateShowNotificationRow($notification) {
        const $notificationRow = $notification.closest(".my-row").first();

        $notificationRow.removeClass("my-d-none");
        $notificationRow.removeCss("height");
        $notificationRow.css("opacity", "0");
        const originalHeight = this._rowHeights[$notification.guid()];
        this._rowHeights[$notification.guid()] = originalHeight;

        const anim = anime({
            targets: $notificationRow[0],
            opacity: [0, 1],
            height: ["0.00000001px", originalHeight],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $notificationRow.removeClass("my-d-none");
                $notificationRow.removeCss("height");
                $notificationRow.css("opacity", "0");
            },
            complete: function() {
                //$notificationRow.css("height", originalHeight); // would break height on window resize
                $notificationRow.removeCss("height");
                $notificationRow.css("opacity", "1");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animateHideNotificationRow($notification) {
        const $notificationRow = $notification.closest(".my-row").first();

        $notification.find(".my-close").disableControl(); // for blazor sake because it reuses the existing parts of the HTML
        const originalheight = this._rowHeights[$notification.guid()];
        $notificationRow.removeClass("my-d-none");
        $notificationRow.css("height", originalheight);
        $notificationRow.css("opacity", "1");
        
        const anim = anime({
            targets: $notificationRow[0],
            opacity: [1, 0],
            height: [originalheight, "0.00000001px"],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $notificationRow.removeClass("my-d-none");
                $notificationRow.css("height", originalheight);
                $notificationRow.css("opacity", "1");
                $notification.find(".my-close").disableControl();
            },
            complete: function() {
                $notificationRow.addClass("my-d-none");
                $notificationRow.removeCss("height");
                $notificationRow.removeCss("opacity");
                $notification.find(".my-close").enableControl();
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animateShowPrompt($prompt) {
        $prompt.css("margin-top", "0px"); // revert from blazor value if re-rendered
        $prompt.css("margin-bottom", "0px");

        const anim = anime({
            targets: $prompt[0],
            marginTop: ["0px", "10px"],
            marginBottom: ["0px", "10px"],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $prompt.css("margin-top", "0px"); // revert from blazor value if re-rendered
                $prompt.css("margin-bottom", "0px");
            },
            complete: function() {
                $prompt.css("margin-top", "10px"); // revert from blazor value if re-rendered
                $prompt.css("margin-bottom", "10px");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animateHidePrompt($prompt) {
        $prompt.css("margin-top", "10px");
        $prompt.css("margin-bottom", "10px");

        const anim = anime({
            targets: $prompt[0],
            marginTop: ["10px", "0px"],
            marginBottom: ["10px", "0px"],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $prompt.css("margin-top", "10px");
                $prompt.css("margin-bottom", "10px");
            },
            complete: function() {
                $prompt.css("margin-top", "0px");
                $prompt.css("margin-bottom", "0px");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animateShowClearAll($clearAll) {
        $clearAll.css("opacity", "0");
        $clearAll.removeClass("my-d-none");

        const anim = anime({
            targets: $clearAll[0],
            opacity: [0, 1],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $clearAll.css("opacity", "0");
                $clearAll.removeClass("my-d-none");
            },
            complete: function() {
                $clearAll.css("opacity", "1");
                $clearAll.enableControl();
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animateHideClearAll($clearAll) {
        $clearAll.css("opacity", "1");
        $clearAll.removeClass("my-d-none");

        const anim = anime({
            targets: $clearAll[0],
            opacity: [1, 0],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $clearAll.css("opacity", "1");
                $clearAll.removeClass("my-d-none");
            },
            complete: function() {
                $clearAll.css("opacity", "0");
                $clearAll.addClass("my-d-none");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static animatePromptHeight($prompt, $arrNotificationsToShow, $arrNotificationsAlreadyShown, $arrNotificationsToRemove) {
        const toShowHeight = $arrNotificationsToShow.sum($n => parseFloat(this._rowHeights[$n.guid()]));
        const alreadyShownHeight = $arrNotificationsAlreadyShown.sum($n => parseFloat(this._rowHeights[$n.guid()]));
        const toRemoveHeight = $arrNotificationsToRemove.sum($n => parseFloat(this._rowHeights[$n.guid()]));

        const currentHeight = Math.max(0, -10 + alreadyShownHeight + toRemoveHeight);
        const expectedheight = Math.max(0, -10 + alreadyShownHeight + toShowHeight);

        const anim = anime({
            targets: $prompt[0],
            height: [currentHeight + "px", expectedheight + "px"],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function() {
                $prompt.css("height", currentHeight + "px");
            },
            complete: function() {
                //$prompt.css("height", expectedheight + "px"); // would break height on window resize
                $prompt.removeCss("height");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static async showNotificationsAsync($notificationsToShow, $notificationsAlreadyShown, $notificationsToRemove) {
        await PromptUtils._syncAnimationBatch.waitAsync(); // each animation batch has to be prepared and started then force finished before a new batch can run, thats the secret of not fucking up the animations

        const anims = [];
        const $arrNotificationsToShow = $notificationsToShow.$toArray();
        const $arrNotificationsAlreadyShown = $notificationsAlreadyShown.$toArray();
        const $arrNotificationsToRemove = $notificationsToRemove.$toArray();
        const $arrAllNotifications = $arrNotificationsToShow.concat($arrNotificationsAlreadyShown).concat($arrNotificationsToRemove);

        if ($arrAllNotifications.length === 0) {
            await PromptUtils._syncAnimationBatch.releaseAsync();
            return;
        }

        const $prompt = $arrAllNotifications.first().closest(".my-prompt");
        const $clearAll = $prompt.children(".notifications-actions-container");

        this.performInitialCleanup($arrAllNotifications);

        for (let $notification of $arrAllNotifications) {
            $prompt.removeCss("height"); // this has to be reset now despite that it is being reset jkust before the naimation to get proper heights now
            this._rowHeights[$notification.guid()] = $notification.closest(".my-row").removeCss("height").hiddenDimensions().height.px(); 
        }

        for (let $notificationToRemove of $arrNotificationsToRemove) {
            anims.push(this.animateHideNotificationRow($notificationToRemove));
        }
        
        for (let $notificationAlreadyShown of $arrNotificationsAlreadyShown) {
            this.setAlreadyShownAnimationRow($notificationAlreadyShown);
        }

        for (let notificationToShow of $arrNotificationsToShow) {
            anims.push(this.animateShowNotificationRow(notificationToShow));
        }

        anims.push(this.animatePromptHeight($prompt, $arrNotificationsToShow, $arrNotificationsAlreadyShown, $arrNotificationsToRemove));

        if ($arrNotificationsToShow.any() && !$arrNotificationsAlreadyShown.any()) {
            anims.push(this.animateShowPrompt($prompt));
            anims.push(this.animateShowClearAll($clearAll));
        } else if ($arrNotificationsToRemove.any() && !$arrNotificationsToShow.any() && !$arrNotificationsAlreadyShown.any()) {
            anims.push(this.animateHidePrompt($prompt));
            anims.push(this.animateHideClearAll($clearAll));
        }

        if (anims.any()) {
            await animeUtils.runAnimationsAndWaitUntilAllStarted(anims);
        }

        await PromptUtils._syncAnimationBatch.releaseAsync();
    }
}

export async function blazor_Prompt_ShowNotificationsAsync(notificationsToShowGuids, notificationsAlreadytShownGuids, notificationsToRemoveGuids) {
    const notificationsToShow$Selector = notificationsToShowGuids.map(guid => guid.guidToSelector()).joinAsString(", ");
    const notificationsAlreadytShown$Selector = notificationsAlreadytShownGuids.map(guid => guid.guidToSelector()).joinAsString(", ");
    const notificationsToRemove$Selector = notificationsToRemoveGuids.map(guid => guid.guidToSelector()).joinAsString(", ");
    await PromptUtils.showNotificationsAsync($(notificationsToShow$Selector), $(notificationsAlreadytShown$Selector), $(notificationsToRemove$Selector));
}

export async function blazor_Prompt_AfterRenderAsync(prompt) {
    const $prompt = $(prompt);
    const $modals = $(".my-modal");
    const isAnyModalShown = $modals.is(".shown");

    $prompt.css("margin-top", "10px");
    $prompt.css("margin-bottom", "10px");

    if (isAnyModalShown) {
        $prompt.prependTo("body");
        $prompt.css("z-index", "101");
        $prompt.css("top", "0");
    } else {
        $prompt.appendTo($(".my-navbar").first().parent());
        $prompt.css("z-index", "9");
        NavBarUtils.setStickyNavBarStyles($(".my-navbar").first());
    }
}

export async function blazor_Prompt_SetNotificationsDisplayIfDataDidntChangeAsync(prompt, shownNotificationsGuids) {
    const $prompt = $(prompt);

    const $notificationRows = $prompt.find(".my-notification").filter(shownNotificationsGuids.map(guid => guid.guidToSelector()).joinAsString(", ")).closest(".my-row");
    $notificationRows.removeClass("my-d-none");
    $notificationRows.removeCss("opacity");
}

$(document).ready(function() {
    $(document).on("click", ".my-brand", async function(e) {
        if (e.which !== 1) {
            return;
        }

        await PromptUtils.showTestNotificationsAsync();
    });
});