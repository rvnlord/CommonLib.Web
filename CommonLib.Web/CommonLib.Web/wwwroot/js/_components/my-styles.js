/// <reference path="../../libs/libman/jquery/dist/jquery.js" />

import "../extensions.js";

class StylesUtils {
    static getRenderedStyles() {
        let strStyles = $("style").$toArray().map($s => $s[0].outerHTML).joinAsString("\n");
        return strStyles;
    }

    static setPostProcessedStyles(stylesStr) {
        $("head").remove("style").append(`<style>${stylesStr}</style>`); // remove has to be in blazor because the components will be recreated otherwise
    }
}

export function blazor_Layout_AfterRender_GetRenderedStyles() {
    return StylesUtils.getRenderedStyles();
}

export function blazor_Layout_AfterRender_SetPostProcessedStyles(strStyles) {
    StylesUtils.setPostProcessedStyles(strStyles);
}

$(document).ready(function() {

});