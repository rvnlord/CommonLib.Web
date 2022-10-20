import "../extensions.js";
import utils from "../utils.js";
import "../converters/base58converter.js";

export class FileUploadUtils {
    static _previewDataCache = {};

    static cachePreviewDataForFile(guid, file, fileAsDataUrl) {
        const fileUploads = this._previewDataCache;
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[`${file.name}|${file.size}`]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }

        fileUploads[guid].files[`${file.name}|${file.size}`].thumbnail = fileAsDataUrl;
        fileUploads[guid].files[`${file.name}|${file.size}`].name = file.name.split(".").first();
        fileUploads[guid].files[`${file.name}|${file.size}`].extension = file.name.contains(".") ? file.name.split(".").last() : "";
        fileUploads[guid].files[`${file.name}|${file.size}`].size = file.size;
    }

    static getCachedPreviewDataForFile(guid, file) {
        const fileUploads = this._previewDataCache;
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[`${file.name}|${file.size}`]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }

        return fileUploads[guid].files[`${file.name}|${file.size}`];
    }

    static async addFilesToUploadAsync($fileUploadDropContainer, files) {
        const $fileUploadThumbnailContainer = $fileUploadDropContainer.siblings(".my-fileupload-thumbnail-container").first();
        const $fileUpload = $fileUploadThumbnailContainer.closest(".my-fileupload");
        const guid = $fileUpload.guid();
        let fileAsDataUrl = null;

        for (let i = files.length - 1; i >= 0; i--) { // reversed iteration due to removing files
            const file = files[i];
            const previewDataForFile = this.getCachedPreviewDataForFile(guid, file);
            const cachedFileNameWithExtension = (previewDataForFile.name || "").toLowerCase() + (previewDataForFile.extension ? "." : "") + (previewDataForFile.extension || "").toLowerCase();
            if (previewDataForFile && cachedFileNameWithExtension === file.name.toLowerCase() && previewDataForFile.size === file.size) {
                files.remove(file);
            }
        }

        for (let i = 0; i < files.length; i++) {
            const file = files[i];
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

            this.cachePreviewDataForFile(guid, file, fileAsDataUrl);
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

    $(document).on("click", ".my-fileupload-btn-choose-file-container > button", async function (e) {
        const $fileUpload = $(e.currentTarget).closest(".my-fileupload");
        const $hiddenFileInput = $fileUpload.find("input[type='file'].my-fileupload-hidden-file-input").first();
        $hiddenFileInput.click();
    });

    $(document).on("change", "input[type='file'].my-fileupload-hidden-file-input", async function (e) {
        let files = Array.from($(e.currentTarget)[0].files);
        const $fileUploadDropContainer = $(e.currentTarget).closest(".my-fileupload-drop-container:not([disabled])");
        await FileUploadUtils.addFilesToUploadAsync($fileUploadDropContainer, files);
    });
});
