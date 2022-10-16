import "../extensions.js";

export class FileUploadUtils {
    static async TestAsync() {
        return null;
    }

    static setFilePreviewImage() {

    }

    static addFilesToUploadQueue() {

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

    $(document).on("dragover", ".my-fileupload-drop-container", function (e) {
        e.stopPropagation();
        e.preventDefault();
    });

    $(document).on("drop", ".my-fileupload-drop-container", function (e) {
        $(e.currentTarget).removeCss("border");
        FileUploadUtils.setFilePreviewImage();
        FileUploadUtils.addFilesToUploadQueue();
    });
});
