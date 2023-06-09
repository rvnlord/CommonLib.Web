import { from as IEnumerable_from } from "linq-to-typescript";

declare global { // arrow functions won't be able to access `this`, it will always be undefined
    interface String {
        trimMultilineTS(removeHTMLComments?: boolean): string;
    }
}

// eslint-disable-next-line
String.prototype.trimMultilineTS = function (removeHTMLComments: boolean = true): string {
    const str = this as string || null;
    if (!str) {
        throw new TypeError("trimMultiline() called on null or undefined");
    }

    let trimmed = IEnumerable_from(str.split(/\r?\n/)).select(line => line.trim()).toArray().join("").trim();
    if (removeHTMLComments) {
        trimmed = trimmed.replace(/<!--[\s\S]*?-->/g, "");
    }

    return trimmed;
}

export { }