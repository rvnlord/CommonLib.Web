import KeyValuePair from "../../utils/util-classes/key-value-pair";
import { MyEnumerable } from "./ienumerable-extensions";

declare global { // arrow functions won't be able to access `this`, it will always be undefined
    interface Array<T> {
        containsAllStrings(substrings: string[]): boolean;
        toMyEnumerable(): MyEnumerable<T>;
        joinAsString(separator: string): string;
        toKVP<K, V>(): KeyValuePair<K, V>
    }
}

// eslint-disable-next-line
Array.prototype.containsAllStrings = function (substrings: string[]): boolean {
    const arr = (this as string[]) || null;
    if (substrings === null || !substrings.any())
        throw new Error("substrings value was empty");
    if (arr === null || arr.length === 0)
        return false;

    const arr1 = arr.distinct().toArray();
    const arr2 = substrings.distinct().toArray();
    return arr1.intersect(arr2).count() === arr2.length;
}

// eslint-disable-next-line
Array.prototype.toMyEnumerable = function <T>(): MyEnumerable<T> {
    const arr = (this as T[]) || null;
    if (arr === null || arr.length === 0)
        return new MyEnumerable([]);

    return new MyEnumerable<T>(arr);
}

// eslint-disable-next-line
Array.prototype.joinAsString = function (separator = "") {
    if (!this.every(el => typeof el === "string" || el instanceof String))
        throw new Error("some array elements are not of type \"string\"");
    const arr = this as string[];
    let joinedString = "";
    for (let i = 0; i < arr.length; i++) {
        joinedString += arr[i];
        if (i !== arr.length - 1) {
            joinedString += separator;
        }
    }
    return joinedString;
}

// eslint-disable-next-line
Array.prototype.toKVP = function <K, V>(): KeyValuePair<K, V> {
    if (this.length !== 2)
        throw new Error("the array has more than 2 elements");
    return new KeyValuePair<K, V>(this[0] as K, this[1] as V);
}

export { }