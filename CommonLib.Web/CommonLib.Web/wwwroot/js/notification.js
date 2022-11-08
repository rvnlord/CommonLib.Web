/// <reference path="../libs/libman/@types/jquery/index.d.ts" />
import utils from "./utils.js";
//import "../extensions.js";

export class Notification {
    _guid;
    _type;
    _icon;
    _message;
    _timeStamp;
    _isShown;
    _newFor;
    _removeAfter;
    _iconSet;
    _$notification;
    _isRemoved;
    _removalTimeoutId;
    _newTimeoutId;

    constructor(type, icon, message, newFor, removeAfter, guid, iconSet, timeStamp, isShown, isRemoved, removalTimeoutId, newTimeoutId) {
        this._guid = guid || utils.guid();
        this._type = type.toLowerCase() || null;
        this._icon = icon || this.getIconFromType();
        this._message = message || null;

        console.log(`this._timeStamp = ${timeStamp} || ${Date.now()}`);

        this._timeStamp = timeStamp || Date.now();
        this._isShown = isShown || false;
        this._newFor = newFor || 5;
        this._removeAfter = removeAfter || 0;
        this._iconSet = iconSet || "light";
        this._isRemoved = isRemoved || false;

        this._removalTimeoutId = removalTimeoutId || null;
        this._newTimeoutId = newTimeoutId || null;
    }

    static createWithTypeAndMessage(type, message) {
        return new Notification(type, null, message, null, null, null, null, null, null);
    }

    getIconFromType() {
        if (this._icon || !this._type) {
            return null;
        }

        switch (this._type.toLowerCase()) {
            case "success":
                return "badge-check";
            case "error":
                return "do-not-enter";
            case "warning":
                return "exclamation-triangle";
            case "info":
                return "info-square";
            case "primary":
                return "comment-lines";
            default:
                return null;
        }
    }

    async renderAsync(promptId) { // TODO: start here
        const decorationIcon = await utils.getIconAsync("light", "grip-lines");
        const timeStamp = utils.toTimeDateString(this._timeStamp);

        let newBadgeContainer = "";
        if (Date.now() - this._timeStamp <= this._newFor * 1000) {
            newBadgeContainer = ` 
                <div class="new-badge-container">
                    <div class="new-badge">NEW</div>
                </div>
            `;
        }

        const icon = this._type === "loading" ? `
            <div class="my-image my-line-sized" style="background-image: url('./_content/CommonLib.Web/images/content-loader.gif'); padding-top: 0"></div>
        ` : `
            <div class="my-icon">
                ${await utils.getIconAsync(this._iconSet, this._icon)}
            </div>
        `;

        const closeIcon = await utils.getIconAsync("light", "times");

        const $notificationRow = $(`
            <div class="my-row my-d-none" style="opacity: 0; height: auto;">
                <div class="my-col-12">
                    <div my-guid="${this._guid}" class="my-notification ${this._type}">

                        <div class="decoration">
                            <div class="my-icon">
                                ${decorationIcon}
                            </div>
                        </div>

                        <div class="date-container">
                            <div class="date">${timeStamp}</div>
                        </div>
                        
                        ${newBadgeContainer}

                        ${icon}

                        <div class="my-notification-message">
                            ${this._message}
                        </div>

                        <button class="my-btn my-btn-clear my-quadratic my-close my-font-sized">
                            <div class="my-icon">
                                ${closeIcon}
                            </div>
                        </button>

                    </div>
                </div>
            </div>
        `);

        const $svgDecoration = $notificationRow.find(".decoration").find("svg");
        $svgDecoration.css("width", "100%");
        $svgDecoration.css("height", "auto");

        const $pathDecoration = $svgDecoration.find("path");
        $pathDecoration.css("fill", "white");

        if (this._type !== "loading") {
            const $svgMyIcon = $notificationRow.find(".my-notification > .my-icon").find("svg");
            const vbDims = $svgMyIcon.attr("viewBox").split(" ");
            const [,, vbWidth, vbHeight] = vbDims;
            if (vbWidth < vbHeight) {
                $svgMyIcon.css("width", "100%");
                $svgMyIcon.css("height", "auto");
            } else {
                $svgMyIcon.css("width", "auto");
                $svgMyIcon.css("height", "100%");
            }

            const $pathMyIcon = $svgMyIcon.find("path");
            $pathMyIcon.css("fill", "white");
        }

        const $svgMyBtnClose = $notificationRow.find(".my-btn.my-close").find("svg");
        $svgMyBtnClose.css("width", "100%");
        $svgMyBtnClose.css("height", "auto");

        const $prompt = $(`div#${promptId}`);
        const $notificationsContainer = $prompt.find(".notifications-container");
        $notificationsContainer.prepend($notificationRow);

        this._$notification = $notificationRow.find(".my-notification");
    }

    convertToSessionCacheFormat() {
        return {
            guid: this._guid,
            type: this._type,
            icon: this._icon,
            message: this._message,
            timeStamp: this._timeStamp,
            isShown: this._isShown,
            newFor: this._newFor,
            removeAfter: this._removeAfter,
            iconSet: this._iconSet,
            sRemoved: this._isRemoved,
            removalTimeoutId: this._removalTimeoutId,
            newTimeoutId: this._newTimeoutId
        };
    }
}
