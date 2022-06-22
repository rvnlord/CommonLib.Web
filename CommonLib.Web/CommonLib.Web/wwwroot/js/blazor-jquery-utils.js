/// <reference path="../libs/libman/jquery/jquery.js" />
import "./extensions.js";
//import utils from "./utils.js";

function fixSelector(selector) {
    if (selector.occurances("my-guid='") === 1) {
        const guid = selector.between("my-guid='", "'");

        if ($(window).attr("my-guid") === guid) {
            selector = window;
        } else if ($(document).attr("my-guid") === guid) {
            selector = document;
        }
    }

    return selector;
}

window.BlazorJQueryUtils = {
    Query: (selector) => {
        let actualSelector = selector;
        if (selector === "window") {
            actualSelector = window;
        } else if (selector === "document") {
            actualSelector = document;
        }
        return $(actualSelector).toBlazorDomElementsCollection().jsonSerialize(selector);
    },

    GetAttr: (selector, attr) => { // with my-guids to check if they are not removed, get all attrs
        return $(fixSelector(selector)).attr(attr);
    },

    SetAttr: (selector, attr, value) => {
        $(fixSelector(selector)).attr(attr, value);
    },

    RemoveAttr: (selector, attr) => {
        $(fixSelector(selector)).removeAttr(attr);
    },

    AddClass: (selector, cls) => {
        $(fixSelector(selector)).addClass(cls);
    },

    RemoveClass: (selector, cls) => {
        $(fixSelector(selector)).removeClass(cls);
    },

    AddClassesAndGetAdded: (selector, cls) => {
        return $(fixSelector(selector)).addClassesAndGetAdded(cls);
    },

    RemoveClassesAndGetRemoved: (selector, cls) => {
        return $(fixSelector(selector)).removeClassesAndGetRemoved(cls);
    },

    GetCss: (selector, name) => {
        return $(fixSelector(selector)).css(name);
    },

    SetCss: (selector, css) => {
        $(fixSelector(selector)).css(css);
    },

    RemoveCss: (selector, ruleName) => {
        return $(fixSelector(selector)).css(ruleName, "").toBlazorDomElementsCollection().jsonSerialize(selector);
    },

    Closest: (selector, ancestorSelector) => {
        return $(fixSelector(selector)).parent().closest(ancestorSelector).toBlazorDomElementsCollection()
            .jsonSerialize(ancestorSelector);
    },

    Parents: (selector) => {
        return $(fixSelector(selector)).parents().toBlazorDomElementsCollection().jsonSerialize(selector);
    },

    ParentsUntil: (selector, parentsUntilSelector) => {
        return $(fixSelector(selector)).parentsUntil(parentsUntilSelector).toBlazorDomElementsCollection()
            .jsonSerialize(selector);
    },

    Parent: (selector) => {
        return $(fixSelector(selector)).parents().toBlazorDomElementsCollection().jsonSerialize(selector);
    },

    Children: (selector) => {
        return $(fixSelector(selector)).children().toBlazorDomElementsCollection().jsonSerialize(selector);
    },

    ChildrenBySelector: (selector, childrenSelector) => {
        return $(fixSelector(selector)).children(childrenSelector).toBlazorDomElementsCollection()
            .jsonSerialize(childrenSelector);
    },

    Find: (selector, findSelector) => {
        return $(fixSelector(selector)).find(findSelector).toBlazorDomElementsCollection().jsonSerialize(findSelector);
    },

    PrevAll: (selector, prevAllSelector) => {
        return $(fixSelector(selector)).prevAll(prevAllSelector).toBlazorDomElementsCollection()
            .jsonSerialize(prevAllSelector);
    },

    Width: (selector) => {
        return $(fixSelector(selector)).width();
    },

    OuterWidth: (selector) => {
        return $(fixSelector(selector)).outerWidth();
    },

    Height: (selector) => {
        return $(fixSelector(selector)).height();
    },

    OuterHeight: (selector) => {
        return $(fixSelector(selector)).outerHeight();
    },

    Filter: (selector, filterSelector) => {
        return $(fixSelector(selector)).filter(filterSelector).toBlazorDomElementsCollection()
            .jsonSerialize(filterSelector);
    },

    Not: (selector, notSelector) => {
        return $(fixSelector(selector)).not(notSelector).toBlazorDomElementsCollection().jsonSerialize(notSelector);
    },

    Is: (selector, isSelector) => {
        return $(fixSelector(selector)).is(isSelector);
    },

    ToggleClass: (selector, className) => {
        return $(fixSelector(selector)).toggleClass(className);
    }
};

$(document).ready(() => {

    $(window).on("resize", async () => { // on resize window
        await DotNet.invokeMethodAsync("CommonLib.Web", "OnWindowResizedAsync");
    });

    //var createChart = LightweightCharts.createChart;

});

