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
        const promptMain = this.getFromSessionCacheById("promptMain");

        await promptMain.addNotificationWithTypeAndMessage("success", "Test Success Message");
        await promptMain.addNotificationWithTypeAndMessage("warning", "Test Warning Message");
        await promptMain.addNotificationWithTypeAndMessage("error", "Test Error Message");
        await promptMain.addNotificationWithTypeAndMessage("primary", "Test Primary Message");
        await promptMain.addNotificationWithTypeAndMessage("info", "Test Long Info Message - asdg sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt hdf sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt h sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt h");
        await promptMain.addNotificationWithTypeAndMessage("loading", "Test Loading Message");
    }

    static setAlreadyShownAnimationRow($notification) {
        const $renderedNotificationRow = $notification.closest(".my-row").first();
        $renderedNotificationRow.removeClass("my-d-none");
        $renderedNotificationRow.removeCss("height");
        $renderedNotificationRow.removeCss("opacity");
    }

    animateShowNotificationRow(notification) {
        const $notification = notification._$notification;
        const $notificationRow = $notification.closest(".my-row").first();

        animeUtils.finishRunningAnimations($notification);

        $notificationRow.removeClass("my-d-none");
        $notificationRow.removeCss("height");
        $notificationRow.css("opacity", "0");
        const originalHeight = Prompt._rowHeights[$notification.guid()];
        Prompt._rowHeights[$notification.guid()] = originalHeight;

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

    animateHideNotificationRow(notification, removeNotification = false) {
        const $notification = notification._$notification;
        const $notificationRow = $notification.closest(".my-row").first();

        animeUtils.finishRunningAnimations($notification);

        $notification.find(".my-close").disableControl(); // for blazor sake because it reuses the existing parts of the HTML
        const originalheight = Prompt._rowHeights[$notification.guid()];
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

                if (removeNotification) {
                    $notification.remove();
                }
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    animateShowPrompt(prompt) {
        const $prompt = prompt._$prompt;

        animeUtils.finishRunningAnimations($prompt);

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

    animateHidePrompt(prompt) {
        const $prompt = prompt._$prompt;

        animeUtils.finishRunningAnimations($prompt);

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

    animateShowActionsNotificationsContainer(prompt) {
        const $actionsNotificationsContainer = prompt._$prompt.find(".actions-notifications-container");

        animeUtils.finishRunningAnimations($actionsNotificationsContainer);

        $actionsNotificationsContainer.css("opacity", "0");
        $actionsNotificationsContainer.removeClass("my-d-none");

        const anim = anime({
            targets: $actionsNotificationsContainer[0],
            opacity: [0, 1],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function () {
                $actionsNotificationsContainer.css("opacity", "0");
                $actionsNotificationsContainer.removeClass("my-d-none");
            },
            complete: function () {
                $actionsNotificationsContainer.css("opacity", "1");
                $actionsNotificationsContainer.enableControl();
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    animateHideActionsNotificationsContainer(prompt) {
        const $actionsNotificationsContainer = prompt._$prompt.find(".actions-notifications-container");

        animeUtils.finishRunningAnimations($actionsNotificationsContainer);

        $actionsNotificationsContainer.css("opacity", "1");
        $actionsNotificationsContainer.removeClass("my-d-none");

        const anim = anime({
            targets: $actionsNotificationsContainer[0],
            opacity: [1, 0],
            duration: 500,
            easing: "easeOutCirc",
            autoplay: false,
            begin: function () {
                $actionsNotificationsContainer.css("opacity", "1");
                $actionsNotificationsContainer.removeClass("my-d-none");
            },
            complete: function () {
                $actionsNotificationsContainer.css("opacity", "0");
                $actionsNotificationsContainer.addClass("my-d-none");
            }
        });
        this._promptAnims.push(anim);
        return anim;
    }

    animatePromptHeight(prompt, arrNotificationsToShow, arrNotificationsAlreadyShown, arrNotificationsToRemove, arrNotificationsToHide) {
        const $prompt = prompt._$prompt;
        const $arrNotificationsToShow = arrNotificationsToShow.map(n => n._$notification);
        const $arrNotificationsAlreadyShown = arrNotificationsAlreadyShown.map(n => n._$notification);
        const $arrNotificationsToRemove = arrNotificationsToRemove.map(n => n._$notification);
        const $arrNotificationsToHide = arrNotificationsToHide.map(n => n._$notification);

        animeUtils.finishRunningAnimations($prompt);

        const toShowHeight = $arrNotificationsToShow.sum($n => parseFloat(Prompt._rowHeights[$n.guid()]));
        const alreadyShownHeight = $arrNotificationsAlreadyShown.sum($n => parseFloat(Prompt._rowHeights[$n.guid()]));
        const toRemoveHeight = $arrNotificationsToRemove.concat($arrNotificationsToHide).sum($n => parseFloat(Prompt._rowHeights[$n.guid()]));

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

    async showRemoveNotificationsAsync(notificationsToShow, notificationsToRemove) { //, $notificationsAlreadyShown 
        await Prompt._syncAnimationBatch.waitAsync(); // each animation batch has to be prepared and started then force finished before a new batch can run, thats the secret of not fucking up the animations

        const anims = [];
        let arrNotificationsToShow = notificationsToShow.filter(n => !n._isShown).orderByDescending(n => n._timeStamp);
        const arrNotificationsToRemove = notificationsToRemove.filter(n => !n._isRemoved);
        let i = 0;

        for (let notificationToShow of arrNotificationsToShow) {
            notificationToShow._isShown = i++ < this._max;
            this._notifications.push(notificationToShow);
            await notificationToShow.renderAsync(this._id);
        }
        arrNotificationsToShow = arrNotificationsToShow.filter(n => n._isShown);

        for (let notificationToRemove of arrNotificationsToRemove) {
            notificationToRemove._isShown = false;
            notificationToRemove._isRemoved = true;
            this._notifications.remove(notificationToRemove);
        }
        this.saveToSessionCache();

        let arrNotificationsAlreadyShown = this._notifications.filter(n => n._isShown).except(arrNotificationsToShow).orderByDescending(n => n._timeStamp);
        const arrNotificationsToHide = arrNotificationsToShow.concat(arrNotificationsAlreadyShown).skip(this._max);
        arrNotificationsAlreadyShown = arrNotificationsAlreadyShown.except(arrNotificationsToHide);

        const arrHiddenNotifications = this._notifications.filter(n => !n._isShown);
        if (arrNotificationsToShow.concat(arrNotificationsAlreadyShown).length < this.__max && arrHiddenNotifications.any()) {
            const toRestoreCount = this._max - arrNotificationsToShow.concat(arrNotificationsAlreadyShown).length;
            arrNotificationsToShow = arrNotificationsToShow.concat(arrHiddenNotifications.take(toRestoreCount)).orderByDescending(n => n._timeStamp);
        }

        const arrNotificationsToAnimate = arrNotificationsToShow.concat(arrNotificationsToRemove).concat(arrNotificationsToHide);

        console.log(`notifications to show: [ ${arrNotificationsToShow.map(n => n._type).joinAsString(", ")} ]`);
        console.log(`notifications already shown: [ ${arrNotificationsAlreadyShown.map(n => n._type).joinAsString(", ")} ]`);
        console.log(`notifications to remove: [ ${notificationsToRemove.map(n => n._type).joinAsString(", ")} ]`);
        console.log(`notifications to hide: [ ${arrNotificationsToHide.map(n => n._type).joinAsString(", ")} ]`);

        if (arrNotificationsToAnimate.length === 0) {
            await Prompt._syncAnimationBatch.releaseAsync();
            return;
        }
        // TODO: start testing here
        this._$prompt.removeCss("height"); // this has to be reset now despite that it is being reset just before the naimation to get proper heights now
        for (let notification of arrNotificationsToAnimate) {
            Prompt._rowHeights[notification._guid] = notification._$notification.closest(".my-row").removeCss("height").hiddenDimensions().height.px();
        }

        for (let notificationToHide of arrNotificationsToHide) {
            anims.push(this.animateHideNotificationRow(notificationToHide, false));
        }

        for (let notificationToRemove of arrNotificationsToRemove) {
            anims.push(this.animateHideNotificationRow(notificationToRemove, true));
        }

        for (let notificationToShow of arrNotificationsToShow) {
            anims.push(this.animateShowNotificationRow(notificationToShow));
        }

        anims.push(this.animatePromptHeight(prompt, arrNotificationsToShow, arrNotificationsAlreadyShown, arrNotificationsToRemove, arrNotificationsToHide));

        if (arrNotificationsToShow.any() && !arrNotificationsAlreadyShown.any()) {
            anims.push(this.animateShowPrompt(prompt));
            anims.push(this.animateShowActionsNotificationsContainer(prompt));
        } else if (arrNotificationsToRemove.any() && !arrNotificationsToShow.any() && !arrNotificationsAlreadyShown.any()) {
            anims.push(this.animateHidePrompt(prompt));
            anims.push(this.animateHideActionsNotificationsContainer(prompt));
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
        await this.showRemoveNotificationsAsync([ notification ], []);
    }

    async addNotificationWithTypeAndMessage(notificationType, message) {
        return this.addNotificationAsync(notificationType, null, null, message);
    }

    async removeNotificationAsync(notification) {
        await this.showRemoveNotificationsAsync([], [ notification ]);
    }

    static getFromSessionCacheById(promptId) {
        const sessionPrompts = (sessionStorage.getItem("NotificationsCache") || "{}").jsonDeserialize();
        if (!sessionPrompts[promptId]) {
            return null;
        }
        const sessionPrompt = sessionPrompts[promptId];
        const prompt = new Prompt(sessionPrompt.guid, sessionPrompt.id, sessionPrompt.newFor, sessionPrompt.removeAfter, sessionPrompt.max, sessionPrompt.renderClasses, sessionPrompt.renderStyle, sessionPrompt.renderAttributes, sessionPrompt.notifications);
        const $prompt = $(`div#${promptId}`);

        if ($prompt.length === 1) {
            prompt._$prompt = $prompt;
        }

        return prompt;
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
        const prompt = await Prompt.getFromSessionCacheById(promptId); // queue and order rendering by call?
        await prompt.addNotificationAsync(notificationType, iconSet, iconType, message);
    }
}

export async function blazor_Prompt_AddNotificationAsync(promptId, notificationType, iconSet, iconType, message) {
    await PromptUtils.addNotificationAsync(promptId, notificationType, iconSet, iconType, message);
}

export async function blazor_Prompt_AfterFirstRenderAsync(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes) {
    const prompt = await PromptUtils.addPromptAsync(guid, promptId, newFor, removeAfter, max, renderClasses, renderStyle, renderAttributes);
    prompt.ensureCorrectStyles(promptId);
}

$(document).ready(async function () {
    $(document).on("click", ".my-brand", async function (e) {
        if (e.which !== 1) {
            return;
        }

        await Prompt.showTestNotificationsAsync();
    });

    $(document).on("click", ".my-notification > .my-close", async function (e) {
        if (e.which !== 1) {
            return;
        }

        const prompt = await Prompt.getFromSessionCacheById($(this).closest(".my-prompt").id());
        const notification = prompt._notifications.first(n => n.$notification.equals($(this).closest(".my-notification")));
        await prompt.removeNotificationAsync(notification);
    });
});