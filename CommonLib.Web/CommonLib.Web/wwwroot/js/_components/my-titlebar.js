$(document).ready(function() {
    $("body").on("mousemove", function(e) {
        const $titlebar = $(".my-titlebar").first();
        if ($titlebar.length === 0) {
            return;
        }
        const titleBarOffset = $titlebar.offset();
        const mouseRelX = e.pageX - titleBarOffset.left;
        const mouseRelY = e.pageY - titleBarOffset.top;
        const titleBarHeight = $titlebar.outerHeight();
        const titleBarWidth = $titlebar.outerWidth();

        if (mouseRelX >= 0 && mouseRelX <= titleBarWidth && mouseRelY >= 0 && mouseRelY <= titleBarHeight) {
            if (!$titlebar.hasClass("hover")) {
                $titlebar.addClass("hover");
                //console.log(`titlebar hovered (${mouseRelX}, ${mouseRelY})`);
            }
        } else {
            if ($titlebar.hasClass("hover")) {
                $titlebar.removeClass("hover");
                //console.log(`titlebar unhovered (${mouseRelX}, ${mouseRelY})`);
            }
        }
    });
});