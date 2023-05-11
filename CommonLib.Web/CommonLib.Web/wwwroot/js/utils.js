import _ from "../libs/libman/underscore/underscore-esm.js";
import Wrapper from "./wrapper.js";

export default class utils {
    static guid = () => {
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0;
            const v = c === "x" ? r : r & 0x3 | 0x8;
            return v.toString(16);
        });
    };

    static startsWithAny = (str, substrs) => {
        for (let substr of substrs) {
            if (str.startsWith(substr)) {
                return true;
            }
        }
        return false;
    };

    static endsWithAny = (str, substrs) => {
        for (let substr of substrs) {
            if (str.endsWith(substr)) {
                return true;
            }
        }
        return false;
    };

    static iif = (condition, functions) => {
        for (let func of functions) {
            if (condition()) {
                func();
            } else {
                return;
            }
        }
    };

    static wait = (ms) => {
        const start = Date.now();
        let now = start;
        while (now - start < ms)
            now = Date.now();
    };

    static waitAsync = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    static waitUntilAsync = (condition, ms) => new Promise(async resolve => {
        ms = ms || 25;
        while (!condition())
            await this.waitAsync(ms);
        return resolve();
    });

    static origin = () => {
        return window.location.protocol + "//" + window.location.hostname + (window.location.port ? `:${window.location.port}` : "");
    };

    static getRandomInt = (min, max) => {
        min = Math.ceil(min);
        max = Math.floor(max);
        return Math.floor(Math.random() * (max - min) + min); // The maximum is exclusive and the minimum is inclusive
    };

    static getIconAsync = async (iconSet, iconName) => {
        const iconsCache = localStorage.getItem("IconsCache").jsonDeserialize();
        Wrapper.object(iconsCache).addIfNotExistsAndGet(iconSet, {}).addIfNotExistsAndGet(iconName, null).unwrap();

        if (!iconsCache[iconSet][iconName]) {
            const backendBaseUrl = sessionStorage.getItem("BackendBaseUrl");
            const iconResp = await $.ajax({
                url: `${backendBaseUrl}/api/upload/GetRenderedIconAsync`,
                contentType: "application/json",
                dataType: 'text',
                data: {
                    SetName: iconSet,
                    IconName: iconName
                }.jsonSerialize(),
                type: "POST"
            });
            var jIcon = iconResp.jsonDeserialize();
            iconsCache[iconSet][iconName] = Wrapper.string(jIcon["Result"]).trimMultiline().unwrap();
            localStorage.setItem("IconsCache", iconsCache.jsonSerialize());
        } else {
            const icon = iconsCache[iconSet][iconName];
            const iconHasHTMLComments = Wrapper.string(icon).containsHTMLComments().unwrap();

            if (iconHasHTMLComments) {
                iconsCache[iconSet][iconName] = Wrapper.string(icon).trimMultiline().unwrap();
                localStorage.setItem("IconsCache", iconsCache.jsonSerialize());
            }
        }

        return iconsCache[iconSet][iconName];
    };

    static $getIconAsync = async (iconSet, iconName) => {
        return $(await this.getIconAsync(iconSet, iconName));
    };

    static toTimeDateString(date) {
        if (date instanceof Date === false && utils.isNumber(date)) {
            date = new Date(date);
        }

        const day = date.getDate();
        const month = 1 + date.getMonth();
        const minutes = date.getMinutes();
        const seconds = date.getSeconds();

        return date.getHours() + ":" +
            (minutes < 10 ? "0" + minutes : minutes) + ":" +
            (seconds < 10 ? "0" + seconds : seconds) + " " +
            (day < 10 ? "0" + day : day) + "-" +
            (month < 10 ? "0" + month : month) + "-" +
            date.getFullYear();
    }

    static toDateTimeString(date) {
        if (date instanceof Date === false && utils.isNumber(date)) {
            date = new Date(date);
        }

        const day = date.getDate();
        const month = 1 + date.getMonth();
        const minutes = date.getMinutes();
        const seconds = date.getSeconds();

        return (day < 10 ? "0" + day : day) + "-" +
            (month < 10 ? "0" + month : month) + "-" +
            date.getFullYear() + " " +
            date.getHours() + ":" +
            (minutes < 10 ? "0" + minutes : minutes) + ":" +
            (seconds < 10 ? "0" + seconds : seconds);
    }

    static isNumber(o) {
        return !isNaN(o) && isFinite(o);
    }

    static order = (array, descending, func = (x) => x) => {
        return array.sort((a, b) => {
            const first = func(a);
            const second = func(b);
            let result = 0;
            if (first < second) {
                result = descending ? 1 : -1;
            }
            if (first > second) {
                result = descending ? -1 : 1;
            }
            return result;
        });
    };

    static orderByProps(array, descending, ...selectors) {
        let newArray = [...array];
        if (selectors.length === 0) {
            newArray = this.order(newArray, descending);
        } else {
            selectors.reverse();
            selectors.forEach((selector) => {
                newArray = this.order(newArray, descending, selector);
            });
        }
        return newArray;
    }

    static orderByPropsWithChangingOrder(array, ...selectorsWithOrder) {
        let newArray = [...array];
        if (selectorsWithOrder.length === 0) {
            newArray = this.order(newArray, false);
        } else {
            selectorsWithOrder.reverse();
            selectorsWithOrder.forEach((selectorWithOrder) => {
                newArray = this.order(newArray, selectorWithOrder.descending, selectorWithOrder.selector);
            });
        }
        return newArray;
    }

    static deepCopy(obj) {
        if (Array.isArray(obj)) {
            return [...obj];
        } else {
            return JSON.parse(JSON.stringify(obj));
        }
    }

    static groupBy(arr, keySelector = (x, i) => i, elementSelector = (x) => x, resultSelector = (key, items) => ({ key, items: items.toArray() })) {
        let keys = utils.deepCopy(arr).map((...params) => ({
            key: keySelector(...params),
            element: elementSelector(...params)
        }));
        const result = [];
        while (keys.length !== 0) {
            const toRemove = keys.filter((item) => item.key.equals(keys[0].key));
            result.push(resultSelector(keys[0].key, toRemove.map(x => x.element)));
            keys = keys.filter((el) => toRemove.indexOf(el) < 0);
        }
        return result;
    }

    static readAsDataURLAsync(file) {
        return new Promise((resolve, reject) => {
            var fr = new FileReader();
            fr.onload = () => {
                resolve(fr.result);
            };
            fr.onerror = reject;
            fr.readAsDataURL(file);
        });
    }

    static isJSON(str) {
        try {
            var obj = JSON.parse(str);
            if (obj && typeof obj === "object" && obj !== null) {
                return true;
            }
        } catch (ex) {
            return false;
        }
    }

    static isString(str) {
        return _.isString(str);
    }

    static is$($selector) {
        return $selector instanceof jQuery;
    }

    static isNull(item) {
        return _.isNull(item);
    }
}