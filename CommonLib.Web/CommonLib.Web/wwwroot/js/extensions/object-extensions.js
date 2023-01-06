//import _ from "../libs/libman/underscore/underscore-esm.js";

export default class ObjectExtensions {
    static nullifyIf(o, condition) {
        if (!condition) {
            throw new Error("condition must be defined");
        }
        return condition(o) ? null : o;
    }
}

