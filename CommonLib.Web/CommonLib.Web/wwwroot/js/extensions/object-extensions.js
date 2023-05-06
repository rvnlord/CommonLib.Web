//import _ from "../libs/libman/underscore/underscore-esm.js";

export default class ObjectExtensions {
    static nullifyIf(o, condition) {
        if (!condition) {
            throw new Error("condition must be defined");
        }
        return condition(o) ? null : o;
    }

    static addIfNotExists(o, key, value) {
        const dict = o;
        if (!dict[key])
            dict[key] = value;
        return dict;
    }

    static addIfNotExistsAndGet(o, key, value) {
        const dict = o;
        return dict.addIfNotExists(key, value)[key];
    }
}

