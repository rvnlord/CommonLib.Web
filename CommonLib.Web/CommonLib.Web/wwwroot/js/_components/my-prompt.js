/// <reference path="../../libs/libman/@types/jquery/index.d.ts" />
/// <reference path="../../libs/libman/@types/animejs/index.d.ts" />
import animeUtils from "../anime-utils.js";
import "../extensions.js";
import Semaphore from "../semaphore.js";
import utils from "../utils.js";
import { NavBarUtils } from "../navbar-utils.js";
import { Notification } from "../notification.js";

export class Prompt {
    static _isShowAnimationBeingPrepared = false;
    static _rowHeights = {};
    static _promptAnims = [];
    static _syncAnimationBatch = new Semaphore(1);

    _guid = null;
    _id = null;
    _newFor = 5;
    _removeAfter = null;
    _max = 3;
    _renderClasses = null;
    _renderStyle = null;
    _renderAttributes = null;

    _$prompt = null;
    _notifications = [];

    constructor(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes, notifications) {
        this._guid = guid || null;
        this._id = promptId || null;
        this._newFor = newFor || 5;
        this._removeAfter = removeAfter || 0;
        this._max = max || 3;
        this._renderClasses = renderClasses || null;
        this._renderStyle = renderStyle || null;
        this._renderAttributes = renderAttributes || null;
        this._notifications = notifications ? notifications.map(n => n instanceof Notification ? n : new Notification(n.type, n.icon, n.message, n.newFor, n.removeAfter, n.guid, n.iconSet, n.timeStamp, n.isShown)) : [];
    }

    static async showTestNotificationsAsync() {
        await DotNet.invokeMethodAsync("CommonLib.Web", "ShowTestNotificationsAsync", sessionStorage.getItem("SessionId"));
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
            begin: function () {
                $notificationRow.removeClass("my-d-none");
                $notificationRow.removeCss("height");
                $notificationRow.css("opacity", "0");
            },
            complete: function () {
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
            begin: function () {
                $notificationRow.removeClass("my-d-none");
                $notificationRow.css("height", originalheight);
                $notificationRow.css("opacity", "1");
                $notification.find(".my-close").disableControl();
            },
            complete: function () {
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
            begin: function () {
                $prompt.css("margin-top", "0px"); // revert from blazor value if re-rendered
                $prompt.css("margin-bottom", "0px");
            },
            complete: function () {
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
            begin: function () {
                $prompt.css("margin-top", "10px");
                $prompt.css("margin-bottom", "10px");
            },
            complete: function () {
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
            begin: function () {
                $clearAll.css("opacity", "0");
                $clearAll.removeClass("my-d-none");
            },
            complete: function () {
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
            begin: function () {
                $clearAll.css("opacity", "1");
                $clearAll.removeClass("my-d-none");
            },
            complete: function () {
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
            begin: function () {
                $prompt.css("height", currentHeight + "px");
            },
            complete: function () {
                //$prompt.css("height", expectedheight + "px"); // would break height on window resize
                $prompt.removeCss("height");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    static async showNotificationsAsync($notificationsToShow, $notificationsAlreadyShown, $notificationsToRemove) {
        await Prompt._syncAnimationBatch.waitAsync(); // each animation batch has to be prepared and started then force finished before a new batch can run, thats the secret of not fucking up the animations

        const anims = [];
        const $arrNotificationsToShow = $notificationsToShow.$toArray();
        const $arrNotificationsAlreadyShown = $notificationsAlreadyShown.$toArray();
        const $arrNotificationsToRemove = $notificationsToRemove.$toArray();
        const $arrAllNotifications = $arrNotificationsToShow.concat($arrNotificationsAlreadyShown).concat($arrNotificationsToRemove);

        if ($arrAllNotifications.length === 0) {
            await Prompt._syncAnimationBatch.releaseAsync();
            return;
        }

        const $prompt = $arrAllNotifications.first().closest(".my-prompt");
        const $clearAll = $prompt.children(".notifications-actions-container");

        this.performInitialCleanup($arrAllNotifications);

        for (let $notification of $arrAllNotifications) {
            $prompt.removeCss("height"); // this has to be reset now despite that it is being reset just before the naimation to get proper heights now
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

        await Prompt._syncAnimationBatch.releaseAsync();
    }

    ensureCorrectStyles() {
        const $prompt = this._$prompt;
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

    saveToSessionCache() {
        const sessionPrompts = (sessionStorage.getItem("NotificationsCache") || "{}").jsonDeserialize();

        sessionPrompts[this._id] = {
            guid: this._guid,
            id: this._id,
            newFor: this._newFor,
            removeAfter: this._removeAfter,
            max: this._max,
            renderClasses: this._renderClasses,
            renderStyle: this._renderStyle,
            renderAttributes: this._renderAttributes,
            notifications: this._notifications.map(n => n.convertToSessionCacheFormat())
        };

        sessionStorage.setItem("NotificationsCache", sessionPrompts.jsonSerialize());
    }

    async renderAsync() {
        const $existingPrompt = $(`div#${this._id}`);
        let $prompt = $existingPrompt;

        if (!$existingPrompt.attr("rendered").toBool()) {
            const $newPrompt = $(`<div my-guid="${this._guid}" id="${this._id}" class="${this._renderClasses}" style="${this._renderStyle}" rendered="true"></div>`);
            for (let attr of this._renderAttributes.kvps())
                $newPrompt.attr(attr.key, attr.value);
            for (let cls of this._renderClasses.split(" "))
                $newPrompt.addClass(cls);

            for (let attr of $existingPrompt.attrs().kvps())
                $newPrompt.attr(attr.key, attr.value);
            for (let cls of $existingPrompt.classes())
                $newPrompt.addClass(cls);

            $newPrompt.attr("rendered", "true");
            $existingPrompt.before($newPrompt).remove();
            $prompt = $newPrompt;
        }

        this._$prompt = $prompt;

        // render top panel if not rendered
        const $newNotificationsActionsContainer = $(`
            <div class="notifications-actions-container">
                <div class="notifications-counter-container">
                    <div class="notifications-counter">${this._notifications.length || 0}</div>
                </div>
                <div class="clear-description-container">
                    <div class="clear-description">Clear:</div>
                </div>
                <div class="clear-visible-container" @onclick="DivClearVisible_ClickAsync">
                    <div class="clear-visible">Visible</div>
                </div>
                <div class="clear-all-container" @onclick="DivClearAll_ClickAsync">
                    <div class="clear-all">All</div>
                </div>
            </div>
        `);

        const $newNotificationsContainer = $(`<div class="notifications-container my-container my-container-no-gutter"></div>`);

        if (!this._notifications || this._notifications.length === 0) {
            $newNotificationsActionsContainer.addClass("my-d-none");
        }

        const $notificationsActionsContainer = $prompt.find(".notifications-actions-container");
        if ($notificationsActionsContainer.length === 0) {
            $prompt.prepend($newNotificationsActionsContainer);
        } else {
            $notificationsActionsContainer.before($newNotificationsActionsContainer).remove();
        }

        const $notificationsContainer = $prompt.find(".notifications-container");
        if ($notificationsContainer.length === 0) {
            $newNotificationsActionsContainer.after($newNotificationsContainer);
        } else {
            $notificationsContainer.before($newNotificationsContainer).remove();
        }

        for (let notification of this._notifications) {
            await notification.renderAsync(this._id);
            const $notificationRow = notification._$notification.closest(".my-row");
            $notificationRow.removeClass("my-d-none");
            $notificationRow.css("opacity", "1");
        }
    }

    async addAsync() {
        this.saveToSessionCache();
        await this.renderAsync();
    }

    async addNotificationAsync(notificationType, iconSet, iconType, message) {
        const notification = new Notification(notificationType, iconType, message, this._newFor, this._removeAfter, null, iconSet, null, false);
        this._notifications.push(notification);
        this.saveToSessionCache();
        await notification.renderAsync(this._id);
        await Prompt.showNotificationsAsync(notification._$notification, $(), $()); // test for now TODO: notification.$notification
    }

    static getFromSessionCacheById(promptId) {
        const sessionPrompts = (sessionStorage.getItem("NotificationsCache") || "{}").jsonDeserialize();
        if (!sessionPrompts[promptId]) {
            return null;
        }
        const sessionPrompt = sessionPrompts[promptId];
        return new Prompt(sessionPrompt.guid, sessionPrompt.id, sessionPrompt.newFor, sessionPrompt.removeAfter, sessionPrompt.max, sessionPrompt.renderClasses, sessionPrompt.renderStyle, sessionPrompt.renderAttributes, sessionPrompt.notifications);
    }
}

export class PromptUtils {
    static ensureCorrectStyles(promptId) {
        this.getPromptById(promptId).ensureCorrectStyles();
    }

    static getorCreatePrompt(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes, notifications) {
        let prompt = Prompt.getFromSessionCacheById(promptId);
        if (!prompt) {
            prompt = new Prompt(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes, notifications);
        }
        return prompt;
    }

    static async addPromptAsync(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes) {
        const prompt = this.getorCreatePrompt(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes);
        await prompt.addAsync();
        return prompt;
    }

    static async addNotificationAsync(promptId, notificationType, iconSet, iconType, message) {
        const prompt = await Prompt.getFromSessionCacheById(promptId); // queue and order rendering by call
        await prompt.addNotificationAsync(notificationType, iconSet, iconType, message); // TODO: animations
    }
}

export async function blazor_Prompt_AddNotificationAsync(promptId, notificationType, iconSet, iconType, message) {
    await PromptUtils.addNotificationAsync(promptId, notificationType, iconSet, iconType, message);
}

export async function blazor_Prompt_AfterFirstRenderAsync(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes) {
    const prompt = await PromptUtils.addPromptAsync(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes);
    prompt.ensureCorrectStyles(promptId);
}

//export async function blazor_Prompt_SetNotificationsDisplayIfDataDidntChangeAsync(prompt, shownNotificationsGuids) {
//    const $prompt = $(prompt);

//    const $notificationRows = $prompt.find(".my-notification").filter(shownNotificationsGuids.map(guid => guid.guidToSelector()).joinAsString(", ")).closest(".my-row");
//    $notificationRows.removeClass("my-d-none");
//    $notificationRows.removeCss("opacity");
//}

$(document).ready(async function () {
    $(document).on("click", ".my-brand", async function (e) {
        if (e.which !== 1) {
            return;
        }

        await Prompt.showTestNotificationsAsync();
    });

    // TESTS
    let $icon = await utils.$getIconAsync("light", "acorn");
    var t = $icon;
});