import "../extensions.js";

export class FileUploadUtils {
    static async TestAsync() {
        return null;
    }
}

export async function blazor_FileeUpload_AfterRenderAsync() {
    return await FileUploadUtils.TestAsync();
}

$(document).ready(function() {

});

