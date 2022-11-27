//import _ from "../libs/libman/underscore/underscore-esm.js";

export default class JQueryExtensions {
    static attrOrNull($selectors, attrName) {
        return !$selectors ? null : $selectors.attr(attrName) || null;
    }
}

