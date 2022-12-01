//import _ from "../../libs/libman/underscore/underscore-esm.js";

export default class StringExtensions {
    static equalsIgnoreCase(str, otherStr) {
        return str.toLowerCase() === otherStr.toLowerCase();
    }

    static isAbsoluteUrl(str) {
        return new RegExp('^(?:[a-z+]+:)?//', 'i').test(str);
    }

    static contains(str, sub) {
        if (sub.length > str.length) {
            return false;
        } else {
            return str.indexOf(sub, 0) !== -1;
        }
    }

    static trimStart(str, strToTrimFromStart) {
        if (!this.contains(str, strToTrimFromStart)) {
            return str;
        } else {
            const trimLength = strToTrimFromStart.length;
            while (str.startsWith(strToTrimFromStart)) {
                str = str.substring(trimLength);
            }
            return str;
        }
    }

    static trimEnd(str, strToTrimFromEnd) {
        if (!this.contains(str, strToTrimFromEnd)) {
            return str;
        } else {
            const trimLength = strToTrimFromEnd.length;
            while (str.endsWith(strToTrimFromEnd)) {
                str = str.substring(0, str.length - trimLength);
            }
            return str;
        }
    }

    static isGuid(str) {
        return /^[0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12}$/gi.test(str);
    }

    static split(str, splitBy) {
        return str.split(splitBy);
    }
}

