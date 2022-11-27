import _ from "../libs/libman/underscore/underscore-esm.js";
import ObjectConverter from "./converters/object-converter.js";
import ObjectExtensions from "./extensions/object-extensions.js";
import ArrayConverter from "./converters/collections/array-converter.js";
import ArrayExtensions from "./extensions/collections/array-extensions.js";
import JQueryConverter from "./converters/jquery-converter.js";
import JQueryExtensions from "./extensions/jquery-extensions.js";
import StringConverter from "./converters/string-converter.js";
import StringExtensions from "./extensions/string-extensions.js";

export default class Wrapper {
    _o;

    constructor(o) {
        if (_.isNaN(o) || _.isUndefined(o)) {
            throw new Error("Can't wrap an invalid object");
        }
        this._o = o;
    }

    static object(object) {
        return new ObjectWrapper(object);
    }

    static array(array) {
        return new ArrayWrapper(array);
    }

    static $($selectors) {
        return new JQueryWrapper($selectors);
    }

    static string(str) {
        return new StringWrapper(str);
    }

    static bool(bool) {
        return new BoolWrapper(bool);
    }

    unwrap = () => this._o || null;
}

export class ObjectWrapper extends Wrapper {
    constructor(obj) {
        super(obj);
    }

    to$ = () => Wrapper.$(this._o);
    to$OrNull = () => Wrapper.$(_.isNull(this._o) ? null : this._o);
}

export class ArrayWrapper extends Wrapper {
    _array;

    constructor(arr) {
        if (!_.isArray(arr) && !_.isNull(arr)) {
            throw new Error("This is not an Array");
        }
        super(arr);
        this._array = arr;
    }

    singleOrNull = (selector = x => x) => Wrapper.object(ArrayExtensions.singleOrNull(this._array, selector));
    select = (selector) => Wrapper.array(ArrayExtensions.select(this._array, selector));
    where = (filter) => Wrapper.array(ArrayExtensions.where(this._array, filter));
    groupBy = (keySelector = (x, i) => i, elementSelector = (x) => x, resultSelector = (key, items) => ({ key, items: _.toArray(items) })) => Wrapper.array(ArrayExtensions.groupBy(this._array, keySelector, elementSelector, resultSelector));
    distinctBy = (selector = x => x) => Wrapper.array(ArrayExtensions.distinctBy(this._array, selector));
    forEach = (action) => Wrapper.array(ArrayExtensions.forEach(this._array, action));
}

export class JQueryWrapper extends Wrapper {
    _$selectors;

    constructor($selectors) {
        if (!($selectors instanceof jQuery) && !_.isNull($selectors)) {
            throw new Error("This is not a JQuery Object");
        }
        super($selectors);
        this._$selectors = $selectors;
    }

    $toArray = () => Wrapper.array(JQueryConverter.$toArray(this._$selectors));

    attrOrNull = (attrName) => Wrapper.string(JQueryExtensions.attrOrNull(this._$selectors, attrName));
}

export class StringWrapper extends Wrapper {
    _str;

    constructor(str) {
        if (!_.isString(str) && !_.isNull(str)) {
            throw new Error("This is not a String Object");
        }
        super(str);
        this._str = str;
    }

    toLower = () => Wrapper.string(StringConverter.toLower(this._str));
    toLowerOrNull = () => Wrapper.string(StringConverter.toLowerOrNull(this._str));

    equalsIgnoreCase = (otherStr) => Wrapper.bool(StringExtensions.equalsIgnoreCase(this._str, otherStr));
}

export class BoolWrapper extends Wrapper {
    _bool;

    constructor(bool) {
        if (!_.isBoolean(bool) && !_.isNull(bool)) {
            throw new Error("This is not a Boolean Object");
        }
        super(bool);
        this._bool = bool;
    }
}

