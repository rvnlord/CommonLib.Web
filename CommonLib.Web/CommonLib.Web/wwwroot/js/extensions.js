/// <reference path="../libs/libman/jquery/dist/jquery.js" />

import _ from "../libs/libman/underscore/underscore-esm.js";
import utils from "./utils.js";

// #region ObjectExtensions

Object.defineProperty(Object.prototype, "jsonSerialize", {
    value: function () {
        return JSON.stringify(this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "cssDictionaryToString", {
    value: function () {
        const css = this;
        return Object.entries(css).map(kvp => kvp[0] + ": " + kvp[1]).join("; ");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "kvps", {
    value: function () {
        return Object.entries(this).map(kvp => ({ key: kvp[0], value: kvp[1] }));
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "toTimeDateString", {
    value: function () {
        const month = 1 + this.getMonth();
        const day = this.getDate();
        return day < 10 ? 0 + day : day + "-" + 
            month < 10 ? 0 + month : month + "-" + 
            this.getFullYear() + " " + 
            this.getHours() + ":" + 
            this.getMinutes() + ":" + 
            this.getSeconds();
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "equals", {
    value: function (that) {
        return _.isEqual(this, that);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "in", {
    value: function (array = []) {
        return array.contains(this);
    },
    writable: true,
    configurable: true
});


// #endregion

// #region NumberExtensions

Object.defineProperty(Number.prototype, "isNumber", {
    value: function () {
        return utils.isNumber(this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Number.prototype, "px", {
    value: function () {
        return this + "px";
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Number.prototype, "round", {
    value: function (decimalPlaces) {
        decimalPlaces = decimalPlaces || 0;
        const pow = Math.pow(10, decimalPlaces);
        return Math.round((this + Number.EPSILON) * pow) / pow;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Number.prototype, "thousandsSeperator", {
    value: function () {
        return this.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    },
    writable: true,
    configurable: true
});

// #endregion

// #region StringExtensions

Object.defineProperty(String.prototype, "skip", {
    value: function (n) {
        return this.split("").skip(n).join("");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "take", {
    value: function (n) {
        return this.split("").take(n).join("");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "takeLast", {
    value: function (n) {
        return this.split("").takeLast(n).join("");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "takeWhile", {
    value: function (condition) {
        return this.split("").takeWhile(condition).join("");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "cssStringToDictionary", {
    value: function () {
        const css = this;
        return css.split(";").filter(r => !r.isNullOrWhiteSpace())
            .toDictionary(s => s.split(":")[0].trimMultiline(), s => s.split(":")[1].trimMultiline());
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "guidToSelector", {
    value: function () {
        return `[my-guid='${this}']`;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "occurances", {
    value: function (substring) {
        return (this.match(new RegExp(substring, "g")) || []).length;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "prefix", {
    value: function (prefixStr) {
        return `${prefixStr}${this}`;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "endsWithAny", {
    value: function (substrs) {
        for (let substr of substrs) {
            if (this.endsWith(substr)) {
                return true;
            }
        }
        return false;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "startsWithAny", {
    value: function (substrs) {
        const str = this;
        for (let substr of substrs) {
            if (str.startsWith(substr)) {
                return true;
            }
        }
        return false;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "trimMultiline", {
    value: function () {
        return this.replace(/\s\s+/g, " ").trim();
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Object.prototype, "jsonDeserialize", {
    value: function () {
        return JSON.parse(this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "replaceAll", {
    value: function (occurance, replacement) {
        const escapeRegExp = (string) => string.replace(/[.*+\-?^${}()|[\]\\]/g, "\\$&"); // $& means the whole matched string
        return this.replace(new RegExp(escapeRegExp(occurance), "g"), replacement);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "remove", {
    value: function (occurance) {
        return this.replaceAll(occurance, "");
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "removeMany", {
    value: function () {
        let str = this;
        for (let s of arguments) {
            str = str.remove(s);
        }
        return str;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "isNullOrWhiteSpace", {
    value: function () {
        return this.trim().length < 1;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "between", {
    value: function (prefix, suffix) {
        let s = this;
        var i = s.indexOf(prefix);
        if (i >= 0) {
            s = s.substring(i + prefix.length);
        }
        else {
            return "";
        }
        if (suffix) {
            i = s.indexOf(suffix);
            if (i >= 0) {
                s = s.substring(0, i);
            }
            else {
                return "";
            }
        }
        return s;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "removeScientificNotation", {
    value: function () {
        const nsign = Math.sign(this);
        let num = Math.abs(this);
        if (/\d+\.?\d*e[\+\-]*\d+/i.test(num)) {
            const zero = "0";
            const parts = String(num).toLowerCase().split("e");
            const e = parts.pop();
            let l = Math.abs(e);
            const sign = e / l;
            const coeff_array = parts[0].split(".");

            if (sign === -1) {
                l = l - coeff_array[0].length;
                if (l < 0) {
                    num = coeff_array[0].slice(0, l) + "." + coeff_array[0].slice(l) + (coeff_array.length === 2 ? coeff_array[1] : "");
                }
                else {
                    num = zero + "." + new Array(l + 1).join(zero) + coeff_array.join("");
                }
            }
            else {
                const dec = coeff_array[1];
                if (dec)
                    l = l - dec.length;
                if (l < 0) {
                    num = coeff_array[0] + dec.slice(0, l) + "." + dec.slice(l);
                } else {
                    num = coeff_array.join("") + new Array(l + 1).join(zero);
                }
            }
        }

        return nsign < 0 ? `-${num}` : num;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "contains", {
    value: function (search, start) {
        if (typeof start !== "number") {
            start = 0;
        }

        if (start + search.length > this.length) {
            return false;
        } else {
            return this.indexOf(search, start) !== -1;
        }
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "containsIgnoreCase", {
    value: function (search, start) {
        if (typeof start !== "number") {
            start = 0;
        }
        
        if (start + search.length > this.length) {
            return false;
        } else {
            return this.toLowerCase().indexOf(search.toLowerCase(), start) !== -1;
        }
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "equalsIgnoreCase", {
    value: function (that) {
        return this.toLowerCase() === that.toLowerCase();
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "skipLastWhile", {
    value: function (predicate) {
        let i = 0;

        while (i < this.length && predicate(this[this.length - 1 - i])) {
            i++;
        }

        return i === 0 ? this : this.slice(0, -i);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "toInt", {
    value: function () {
        return parseInt(this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "toFloat", {
    value: function () {
        return parseFloat(this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "toBool", {
    value: function () {
        return ["true", "false", true, false].contains(this) && JSON.parse(this) || null;
    },
    writable: true,
    configurable: true
});

// #endregion

// #region ArrayExtensions

Object.defineProperty(Array.prototype, "joinAsString", {
    value: function (separator = "") {
        const arr = this;

        if (!arr.every(el => typeof el === "string" || el instanceof String))
            throw new Error("some array elements are not of type \"string\"");
        let joinedString = "";
        for (let i = 0; i < arr.length; i++) {
            joinedString += arr[i];
            if (i !== arr.length - 1) {
                joinedString += separator;
            }
        }
        return joinedString;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "toDictionary", {
    value: function (keySelector, valSelector) {
        const arr = this;
        const dict = {};
        for (let el of arr) {
            dict[keySelector(el)] = valSelector(el);
        }
        return dict;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "first", {
    value: function (selector) {
        if (selector) {
            return this.filter(el => selector(el))[0];
        }
        return this[0];
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "last", {
    value: function () {
        return this[this.length - 1];
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "arrayTo$", {
    value: function () {
        return $(this).map($.fn.toArray);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "sum", {
    value: function (selector) {
        let arr = this;
        if (selector)
            arr = arr.map(el => selector(el));
        return arr.reduce((a, b) => a + b, 0);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "max", {
    value: function (selector) {
        let arr = this;
        if (selector)
            arr = arr.map(el => selector(el));
        return Math.max(...arr);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "min", {
    value: function () {
        return Math.min(...this);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "singleOrNull", {
    value: function () {
        if (!Array.isArray(this))
            throw new Error("Not an array");
        if (this.length > 1)
            throw new Error("Array should contain only one element");
        if (this.length < 1)
            return null;
        return this[0];
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "except", {
    value: function (exceptArr) {
        if (this.some(o => o instanceof $)) {
            return this.filter(el => !exceptArr.map($el => $el[0]).includes(el[0]));
        }
        return this.filter(el => !exceptArr.includes(el));
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "contains", {
    value: function (el) {
        for (let n of this) {
            if (el instanceof $ ? n[0] === el[0] : n === el) {
                return true;
            }
        }

        return false;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "any", {
    value: function (selector) {
        if (!selector) {
            selector = function() { return true; }; 
        }

        return this.some(selector);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "all", {
    value: function (selector) {
        if (!selector) {
            selector = function() { return true; }; 
        }

        return this.every(selector);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "containsAny", {
    value: function () {
        const elements = arguments.length && arguments[0] instanceof Array ? arguments[0] : arguments;

        for (let el of elements) {
            if (this.contains(el)) {
                return true;
            }
        }

        return false;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "containsAll", {
    value: function () {
        const elements = arguments.length && arguments[0] instanceof Array ? arguments[0] : arguments;

        for (let el of elements) {
            if (!this.contains(el)) {
                return false;
            }
        }

        return true;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "skip", {
    value: function (n) {
        if (typeof n !== "number") {
            throw new Error("n is not a number");
        }
        return this.slice(n);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "take", {
    value: function (n) {
        if (typeof n !== "number") {
            throw new Error("n is not a number");
        }
        return this.slice(0, n);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "takeLast", {
    value: function (n) {
        if (typeof n !== "number") {
            throw new Error("n is not a number");
        }
        return this.slice(Math.max(this.length - n, 0));
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "takeWhile", {
    value: function (condition) {
        if (typeof condition !== "function") {
            throw new Error("condition is not a function");
        }

        const arr = [];
        for (let el of this) {
            if (condition(el))
                arr.push(el);
            else
                break;
        }
        return arr;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "append", {
    value: function (el) {
        return this.concat([el]);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "remove", {
    value: function (value) {
        const arr = this;
        const index = arr.indexOf(value);
        if (index > -1) {
            arr.splice(index, 1);
        }
        return arr;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "removeMany", {
    value: function (values) {
        for (let v of values) {
            this.remove(v);
        }
        return this;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "removeAll", {
    value: function (selector) {
        for (let v of this.filter(selector)) {
            this.remove(v);
        }
        return this;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "clear", {
    value: function () {
        this.length = 0;
        return this;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "sequenceEqual", {
    value: function (otherSequence) {
        const sequence = this;
        if (!(otherSequence instanceof Array)) {
            throw new Error("otherSequence must be an Array");
        }
        if (sequence.length !== otherSequence.length) {
            return false;
        }

        for (let i = 0; i < sequence.length; i++) {
            if (sequence[i] !== otherSequence[i]) {
                return false;
            }
        }

        return true;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "orderby", {
    value: function (...selectors) {
        return utils.orderByProps(this, false, ...selectors);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "orderByDescending", {
    value: function (...selectors) {
        return utils.orderByProps(this, true, ...selectors);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "groupBy", {
    value: function (keySelector = (x, i) => i, elementSelector = (x) => x, resultSelector = (key, items) => ({ key, items: items.toArray() })) {
        return utils.groupBy(this, keySelector, elementSelector, resultSelector);
    },
    writable: true,
    configurable: true
});

Object.defineProperty(Array.prototype, "orderByWithDirection", {
    value: function (...selectorsWithChangingOrder) {
        return utils.orderByPropsWithChangingOrder(this, ...selectorsWithChangingOrder);
    },
    writable: true,
    configurable: true
});

// #endregion

// #region JqueryExtensions

function jQueryArray($selectors) {
    if ($selectors.length === 0) {
        return [];
    } else if ($selectors.length === 1) {
        return [ $selectors ];
    }
    return $selectors.toArray().map(el => $(el));
}

jQuery.fn.extend({
    check: function() {
        return this.each(function() {
            this.checked = true;
        });
    },
    uncheck: function() {
        return this.each(function() {
            this.checked = false;
        });
    },
    removeClassesAndGetRemoved: function(classes) {
        if (classes.isNullOrWhiteSpace())
            throw new Error("No classes no remove specified");
        const removedClasses = [];
        for (let c of classes.split(" ")) {
            if (this.hasClass(c)) {
                this.removeClass(c);
                removedClasses.unshift(c);
            }
        }
        return removedClasses.join(" ");
    },
    addClassesAndGetAdded: function(classes) {
        if (classes.isNullOrWhiteSpace())
            throw new Error("No classes no add specified");
        const addedClases = [];
        for (let c of classes.split(" ")) {
            if (!this.hasClass(c)) {
                this.addClass(c);
                addedClases.unshift(c);
            }
        }
        return addedClases.join(" ");
    },
    $toArray: function() {
        return jQueryArray(this);
    },
    nullifyIfEmpty: function() {
        if (this.length <= 0)
            return null;
        return this;
    },
    firstOrNull: function() {
        return this.nullifyIfEmpty();
    },
    textOnlySelf: function() {
        if (this[0] === window || this[0] === document)
            return "";
        return this.contents().$toArray().filter($n => $n[0].nodeType === 3)
            .map($n => $n[0].nodeValue.removeMany("!", ",").trimMultiline()).join("") ||
            null;
    },
    toBlazorDomElementsCollection: function(originalSelector) {
        return $.makeArray(this).map(el => {
            const guid = $(el).attr("my-guid") || utils.guid();
            if (!$(el).attr("my-guid")) {
                $(el).attr("my-guid", guid);
            }

            return {
                guid: guid,
                originalSelector: (originalSelector || "").trim() || null,
                classes: ($(el).attr("class") || "").trim().split(" ").filter(Boolean),
                id: ($(el).attr("id") || "").trim() || null,
                name: ($(el).attr("name") || "").trim() || null,
                value: ($(el).attr("value") || "").trim() || null,
                text: $(el).textOnlySelf()
                //outerHtml: ($(el)[0].outerHTML.trim() || "") || null
            };
        });
    },
    classes: function() {
        return (this.attr("class") || "").split(" ").filter(c => !c.isNullOrWhiteSpace());
    },
    visible: function() {
        if (this.length === 0) {
            return false;
        }
        const selfAndParents = this.parents().add(this).$toArray();
        selfAndParents.unshift(this);
        return !selfAndParents.some(e => e.css("display") === "none");
    },
    depth: function() {
        return this.parents().length;
    },
    removeCss: function(css) {
        return this.css(css, "");
    },
    backgroundImageSizeAsync: async function() {
        const imageUrl = this.css("background-image").match(/^url\("?(.+?)"?\)$/)[1];
        const image = new Image();

        await $.Deferred(function(task) {
            image.onload = () => task.resolve(image);
            image.onerror = () => task.reject();
            image.src = imageUrl;
        }).promise();

        return {
            width: image.width,
            height: image.height
        };
    },
    cssWithPriority: function(ruleName, value) {
        const css = this[0].style.cssText.cssStringToDictionary();
        css[ruleName] = value;
        this[0].style.cssText = css.cssDictionaryToString();
        return this;
    },
    hiddenDimensions: function(includeMargin) {
        const $item = this;

        const hasNoneClass = $item.hasClass("my-d-none");
        if (hasNoneClass)
            $item.removeClass("my-d-none");

        const props = { position: "absolute !important", visibility: "hidden !important", display: "block !important" };
        const dim = { width: 0, height: 0, innerWidth: 0, innerHeight: 0, outerWidth: 0, outerHeight: 0 };
        includeMargin = !includeMargin ? false : includeMargin;
        const $hiddenParents = $item.parents().addBack().$toArray()
            .filter($i => !$i.visible() &&
                $i.css("display") === "none"); // skip elements hidden because their parent is hidden

        const oldProps = [];
        for (let $parent of $hiddenParents) {
            const old = {};
            for (let [key, value] of Object.entries(props)) {
                old[key] = $parent[0].style[key];
                $parent.cssWithPriority(key, value);
            }
            oldProps.push(old);
        }

        dim.width = $item.width();
        dim.outerWidth = $item.outerWidth(includeMargin);
        dim.innerWidth = $item.innerWidth();
        dim.height = $item.height();
        dim.innerHeight = $item.innerHeight();
        dim.outerHeight = $item.outerHeight(includeMargin);

        if (hasNoneClass)
            $item.addClass("my-d-none");

        let i = 0;
        for (let $parent of $hiddenParents) {
            const old = oldProps[i];
            for (let key of Object.keys(props)) {
                $parent.css(key, old[key]);
            }
            i++;
        }

        return dim;
    },
    textWidth: function() {
        const html_org = $(this).html();
        const html_calc = `<span>${html_org}</span>`;
        $(this).html(html_calc);
        const width = $(this).find("span:first").width();
        $(this).html(html_org);
        return width;
    },
    id: function() {
        return this.attr("id");
    },
    guid: function() {
        return this.attr("my-guid");
    },
    isAtLeastPartiallyWithinViewPort: function() {
        const el = this[0],
            rect = el.getBoundingClientRect();

        return (
            rect.top.round() >= 0 
                || rect.left.round() >= 0 
                || rect.bottom.round() <= (window.innerHeight || document.documentElement.clientHeight) 
                || rect.right.round() <= (window.innerWidth || document.documentElement.clientWidth)
        );
    },
    isFullyWithinViewPort: function () {
        const el = this[0],
            rect = el.getBoundingClientRect();

        return (
            rect.top.round() >= 0 
                && rect.left.round() >= 0 
                && rect.bottom.round() <= (window.innerHeight || document.documentElement.clientHeight) 
                && rect.right.round() <= (window.innerWidth || document.documentElement.clientWidth)
        );
    },
    isBeingShown: function() {
        return this.attr("is-being-shown") === "true";
    },    
    isBeingHidden: function () {
        return this.attr("is-being-hidden") === "true";
    },
    isBeingRemoved: function () {
        return this.attr("is-being-removed") === "true";
    },
    setAsBeingShown: function() {
        return this.attr("is-being-shown", "true");
    },    
    setAsBeingHidden: function () {
        return this.attr("is-being-hidden", "true");
    },
    setAsBeingRemoved: function () {
        return this.attr("is-being-removed", "true");
    },
    setAsNotBeingShown: function() {
        return this.attr("is-being-shown", "false");
    },    
    setAsNotBeingHidden: function () {
        return this.attr("is-being-hidden", "false");
    },
    setAsNotBeingRemoved: function () {
        return this.attr("is-being-removed", "false");
    },
    disableControl: function() {
        this.prop("disabled", true);
        this.addClass("disabled");
        return this;
    },
    enableControl: function() {
        this.prop("disabled", false);
        this.removeClass("disabled");
        return this;
    },
    attrs: function () {
        const rawAttrs = this[0].attributes;
        const arrAttrs = {};
        for (let attr of rawAttrs) {
            arrAttrs[attr.name] = attr.value;
        }
        return arrAttrs;
    },
    equals: function(that) {
        return this[0] === that[0];
    }
});

// #endregion