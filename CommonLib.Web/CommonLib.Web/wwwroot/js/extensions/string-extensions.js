//import _ from "../../libs/libman/underscore/underscore-esm.js";

export default class StringExtensions {
    static equalsIgnoreCase(str, otherStr) {
        return str.toLowerCase() === otherStr.toLowerCase();
    }
}

