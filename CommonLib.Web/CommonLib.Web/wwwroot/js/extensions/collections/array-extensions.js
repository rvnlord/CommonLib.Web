import _ from "../../../libs/libman/underscore/underscore-esm.js";
import utils from "../../utils.js";
import Wrapper from "../../wrapper.js";

export default class ArrayExtensions {
    static singleOrNull(array, selector = x => x) {
        const arr = array.filter(selector);
        if (!Array.isArray(arr))
            throw new Error("Not an array");
        if (arr.length > 1)
            throw new Error("Array contains more thaan one element");
        if (arr.length < 1)
            return null;
        return arr[0] ? arr[0] : null;
    }

    static select(array, selector) {
        return array.map(selector);
    }

    static where(array, filter) {
        return array.filter(filter);
    }

    static distinctBy(array, selector = x => x) {
        return Wrapper.array(array).groupBy(selector).select(kvp => kvp.items[0]).unwrap();
    }

    static forEach(array, action) {
        for (let el of array) {
            action(el);
        }
        return array;
    }

    static groupBy(array, keySelector = (x, i) => i, elementSelector = (x) => x, resultSelector = (key, items) => ({ key, items: items.toArray() })) {
        return utils.groupBy(array, keySelector, elementSelector, resultSelector);
    }

    static last(array) {
        return array[array.length - 1];
    }

    static skipLast(array, n) {
        return array.slice(0, -n);
    }

    static joinAsString(array, joinChar) {
        return array.join(joinChar);
    }
}

