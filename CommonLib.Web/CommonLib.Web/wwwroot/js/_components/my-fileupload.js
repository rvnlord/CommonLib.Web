import "../extensions.js";
import utils from "../utils.js";
import "../converters/base58converter.js";

export class FileUploadUtils {
    static savePreviewDataToSession(guid, file, fileAsDataUrl) {
        const fileUploads = (sessionStorage.getItem("FileUploadsCache") || "{}").jsonDeserialize();
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[`${file.name}|${file.size}`]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }

        fileUploads[guid].files[`${file.name}|${file.size}`].thumbnail = fileAsDataUrl;
        fileUploads[guid].files[`${file.name}|${file.size}`].name = file.name.split(".").first();
        fileUploads[guid].files[`${file.name}|${file.size}`].extension = file.name.contains(".") ? file.name.split(".").last() : "";
        fileUploads[guid].files[`${file.name}|${file.size}`].size = file.size;
        
        sessionStorage.setItem("FileUploadsCache", fileUploads.jsonSerialize());
    }

    static getPreviewDataFromSession(guid, file) {
        const fileUploads = (sessionStorage.getItem("FileUploadsCache") || "{}").jsonDeserialize();
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[`${file.name}|${file.size}`]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }

        return fileUploads[guid].files[`${file.name}|${file.size}`];
    }

    //static async saveFilesToSessionAsync(guid, files) {
    //    const fileUploads = (sessionStorage.getItem("FileUploadsCache") || "{}").jsonDeserialize();
    //    if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
    //    if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }

    //    for (let file of files) {
    //        if (!fileUploads[guid].files[`${file.name}|${file.size}`]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }
    //        const fileData = await new FileReader().readAsByteArrayAsync(file);
    //        fileUploads[guid].files[`${file.name}|${file.size}`].fileData = fileData.toBase58String();
    //    }

    //    sessionStorage.setItem("FileUploadsCache", fileUploads.jsonSerialize());
    //}

    static async addFilesToUploadAsync($fileUploadDropContainer, files) {
        const $fileUploadThumbnailContainer = $fileUploadDropContainer.siblings(".my-fileupload-thumbnail-container").first();
        const $fileUpload = $fileUploadThumbnailContainer.closest(".my-fileupload");
        const guid = $fileUpload.guid();
        const file = files.last();
        let fileAsDataUrl = null;

        for (let file of files) {
            const sessionDataForFile = this.getPreviewDataFromSession(guid, file);
            const sessionFileNameWithExtension = (sessionDataForFile.name || "").toLowerCase() + (sessionDataForFile.extension ? "." : "") + (sessionDataForFile.extension || "").toLowerCase();
            if (sessionDataForFile && sessionFileNameWithExtension === file.name.toLowerCase() && sessionDataForFile.size === file.size) {
                files.remove(file);
            }
        }

        for (let i = 0; i < files.length; i++) {
            if (i === files.length - 1) {
                $fileUploadThumbnailContainer.empty();
                $fileUploadThumbnailContainer.removeCss("background-image");

                if (file.type.startsWith("image/") && file.size <= 52428800) {
                    fileAsDataUrl = await new FileReader().readAsDataURLAsync(file);
                    $fileUploadThumbnailContainer.css("background-image", `url('${fileAsDataUrl}#t=${new Date().getTime()}')`);           
                } else {
                    const fileIcon = await utils.getIconAsync("light", "file");
                    const $fileIcon = $(`<div class="my-icon">${fileIcon}</div>`);
                    const $svgMyIcon = $fileIcon.find("svg");
                    const vbDims = $svgMyIcon.attr("viewBox").split(" ");
                    const [,, vbWidth, vbHeight] = vbDims;
                    if (vbWidth >= vbHeight) { $svgMyIcon.css({"width": "100%", "height": "auto"}); } else { $svgMyIcon.css({"width": "auto", "height": "100%"}); }

                    $fileUploadThumbnailContainer.html($fileIcon);
                }
            }

            this.savePreviewDataToSession(guid, file, fileAsDataUrl);
        }

        const filesData = files.map(f => ({ 
            Name: f.name.split(".").first(), 
            Extension: f.name.contains(".") ? f.name.split(".").last() : "", 
            TotalSizeInBytes: f.size 
        }));

        await DotNet.invokeMethodAsync("CommonLib.Web", "AddFilesToUploadAsync", sessionStorage.getItem("SessionId"), guid, filesData);
    }
}

export async function blazor_FileeUpload_AfterRenderAsync() {
    return await FileUploadUtils.TestAsync();
}

$(document).ready(function () {
    $(document).on("dragenter dragleave dragover drop", function (e) {
        e.stopPropagation();
        e.preventDefault();
    });

    $(document).on("dragenter", ".my-fileupload-drop-container", function (e) {
        e.stopPropagation();
        e.preventDefault();
        $(e.currentTarget).css("border", "2px dotted #0B85A1");
    });

    $(document).on("dragleave", ".my-fileupload-drop-container", function (e) {
        e.stopPropagation();
        e.preventDefault();
        $(e.currentTarget).removeCss("border");
    });

    $(document).on("dragover", ".my-fileupload-drop-container:not([disabled])", function (e) {
        e.stopPropagation();
        e.preventDefault();
    });

    $(document).on("drop", ".my-fileupload-drop-container:not([disabled])", async function (e) {
        let files = Array.from(e.originalEvent.dataTransfer.files); // FileRTeader appears to beee changing 'e' data
        const $fileUploadDropContainer = $(e.currentTarget);
        $fileUploadDropContainer.removeCss("border");

        await FileUploadUtils.addFilesToUploadAsync($fileUploadDropContainer, files);
    });
});
