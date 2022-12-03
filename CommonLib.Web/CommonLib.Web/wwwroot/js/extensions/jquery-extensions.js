//import _ from "../libs/libman/underscore/underscore-esm.js";
import JQueryConverter from "../converters/jquery-converter.js";

export default class JQueryExtensions {
    static attrOrNull($selectors, attrName) {
        return !$selectors ? null : $selectors.attr(attrName) || null;
    }

    static isHovered($selectors, cursorX, cursorY) { // TODO: This would prevent some 'mouseleave' events from being triggered, using ':hover' instead atm
        const $arrSelectors = JQueryConverter.$toArray($selectors);
        for (let $selector of $arrSelectors) {
            const offset = $selector[0].getBoundingClientRect();
            if (cursorX >= offset.left && cursorX <= offset.right && cursorY >= offset.top && cursorY <= offset.bottom) {
                return true;
            }
        }

        return false;
    }
}

