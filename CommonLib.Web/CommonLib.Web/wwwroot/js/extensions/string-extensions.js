﻿//import _ from "../../libs/libman/underscore/underscore-esm.js";

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

    static removeHTMLComments(str) {
        if (!str) {
            throw new Error("Empty string");
        }
        return str.replace(/<!--[\s\S]*?-->/g, "");
    }

    static containsHTMLComments(str) {
        if (!str) {
            throw new Error('Input string cannot be null or undefined');
        }
  
        return /<!--.*?-->/.test(str);
    }

    static trimMultiline(str, removeHTMLComments = true) {
        if (!str) {
            throw new TypeError("trimMultiline() called on null or undefined");
        }

        let trimmed = str.split(/\r?\n/).map(line => line.trim()).join("").trim();
        if (removeHTMLComments) {
            trimmed = trimmed.replace(/<!--[\s\S]*?-->/g, "");
        }

        return trimmed;
    }

    static isGuid(str) {
        return /^[0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12}$/gi.test(str);
    }

    static split(str, splitBy) {
        return str.split(splitBy);
    }
}

