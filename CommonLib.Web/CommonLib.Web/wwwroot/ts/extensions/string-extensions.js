import { from as IEnumerable_from } from "linq-to-typescript";
// eslint-disable-next-line
String.prototype.trimMultilineTS = function (removeHTMLComments) {
    if (removeHTMLComments === void 0) { removeHTMLComments = true; }
    var str = this || null;
    if (!str) {
        throw new TypeError("trimMultiline() called on null or undefined");
    }
    var trimmed = IEnumerable_from(str.split(/\r?\n/)).select(function (line) { return line.trim(); }).toArray().join("").trim();
    if (removeHTMLComments) {
        trimmed = trimmed.replace(/<!--[\s\S]*?-->/g, "");
    }
    return trimmed;
};
//# sourceMappingURL=string-extensions.js.map