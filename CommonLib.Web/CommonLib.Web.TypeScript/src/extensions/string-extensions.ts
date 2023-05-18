import { IEnumerable } from "linq-to-typescript";

declare global { // arrow functions won't be able to access `this`, it will always be undefined
    interface String {
        contains(sub: string): boolean;
        containsAny(substrings: IEnumerable<string>): boolean;
        containsAll(substrings: IEnumerable<string>): boolean;
        isEmpty(): boolean;
        isEmptyOrWhiteSpace(): boolean;
    }
}

// eslint-disable-next-line
String.prototype.isEmpty = function () {
    const str = String(this) || null;
    return str === null || str.length < 1;
}

// eslint-disable-next-line
String.prototype.isEmptyOrWhiteSpace = function () {
    const str = String(this) || null;
    return str === null || str.trim().length < 1;
}

// eslint-disable-next-line
String.prototype.contains = function (sub: string) {
    const str = String(this) || null;
    if (sub === null || sub.isEmpty())
        throw new Error("substring value was empty");
    if (str === null || str.isEmpty())
        return false;

    if (sub.length > str.length) {
        return false;
    } else {
        return str.indexOf(sub, 0) !== -1;
    }
}

// eslint-disable-next-line
String.prototype.containsAny = function (substrings: IEnumerable<string>) {
    const str = String(this) || null;
    const arrSubstrings = substrings.toArray();
    if (substrings === null || !arrSubstrings.any())
        throw new Error("substrings value was empty");
    if (str === null || str.isEmpty())
        return false;

    return substrings.any(sub => str.contains(sub));
}

// eslint-disable-next-line
String.prototype.containsAll = function (substrings: IEnumerable<string>) {
    const str = String(this) || null;
    const arrSubstrings = substrings.toArray();
    if (substrings === null || !arrSubstrings.any())
        throw new Error("substrings value was empty");
    if (str === null || str.isEmpty())
        return false;

    return substrings.all(sub => str.contains(sub));
}

export { }