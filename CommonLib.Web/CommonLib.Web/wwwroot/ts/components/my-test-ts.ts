import $ from "jquery";
import "../extensions/string-extensions"

export async function blazor_TestTS_AfterFirstRender(testStr: string) {
    const trimmedTestStr =  testStr.trimMultilineTS();
    $(".my-navbar").append(trimmedTestStr); 
}