//import _ from "../../libs/libman/underscore/underscore-esm.js";

export default class StringConverter {
    static toLower(str) {
        return str.toLowerCase();
    }

    static toLowerOrNull(str) {
        return str === null ? null : str.toLowerCase();
    }
}

