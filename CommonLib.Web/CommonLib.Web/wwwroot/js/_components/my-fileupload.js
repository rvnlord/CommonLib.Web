/// <reference path="../../libs/libman/jquery/dist/jquery.js" />

import "../extensions.js";
import utils from "../utils.js";
import "../converters/base58converter.js";

export class FileUploadUtils {
    static _previewDataCache = {};

    static getFileIdFromFile(file) {
        return `${file.name.pathToName()}|${file.name.pathToExtension()}|${file.size}`;
    }

    static createFileId(name, extension, size) {
        return `${name}|${extension}|${size}`;
    }

    static cachePreviewDataForFile(guid, file, fileAsDataUrl) {
        const fileId = this.getFileIdFromFile(file);
        const fileUploads = this._previewDataCache;
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[fileId]) { fileUploads[guid].files[`${file.name}|${file.size}`] = {}; }

        fileUploads[guid].files[fileId].thumbnail = fileAsDataUrl;
        fileUploads[guid].files[fileId].name = file.name.split(".").first();
        fileUploads[guid].files[fileId].extension = file.name.contains(".") ? file.name.split(".").last() : "";
        fileUploads[guid].files[fileId].size = file.size;
        fileUploads[guid].files[fileId].file = file;
    }

    static getCachedPreviewDataForFile(guid, file) {
        const fileId = this.getFileIdFromFile(file);
        const fileUploads = this._previewDataCache;
        if (!fileUploads[guid]) { fileUploads[guid] = { guid: guid }; }
        if (!fileUploads[guid].files) { fileUploads[guid].files = {}; }
        if (!fileUploads[guid].files[fileId]) { fileUploads[guid].files[fileId] = {}; }

        return fileUploads[guid].files[fileId];
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

    static async getFileChunkAsync(guid, name, extension, totalSize, position, chunkSize) {
        const fileUploads = this._previewDataCache;
        const fileId = this.createFileId(name, extension, totalSize);
        const file = fileUploads[guid].files[fileId].file;
        return [...new Uint8Array(await file.slice(position, Math.min(position + chunkSize, totalSize)).arrayBuffer())];
    }
}

export async function blazor_FileUpload_GetFileChunk(guid, name, extension, totalSize, position, chunkSize) {
    return await FileUploadUtils.getFileChunkAsync(guid, name, extension, totalSize, position, chunkSize);
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
