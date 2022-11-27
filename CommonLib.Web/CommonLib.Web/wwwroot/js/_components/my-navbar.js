/// <reference path="../../libs/libman/@types/jquery/index.d.ts" />
/// <reference path="../../libs/custom/@types/animejs/index.d.ts" />
/// <reference path="../../libs/libman/overlay-scrollbars/js/jquery.overlayscrollbars.d.ts" />
/// <reference path="../../libs/libman/overlay-scrollbars/js/overlayscrollbars.js" />

import * as OverlayScrollbars from "../../libs/libman/overlay-scrollbars/js/jquery.overlayscrollbars.js";
//import OverlayScrollbars from "../../libs/libman/overlay-scrollbars/js/overlayscrollbars.js";
import "../extensions.js";
import utils from "../utils.js";
import { NavLinkUtils } from "./my-nav-link.js";
import { NavBarUtils } from "../navbar-utils.js";

export async function blazor_NavBar_AfterFirstRender() {
    console.log("navbar first render");

    NavBarUtils.finishAndRemoveRunningAnims();
    
    var currentUrl = window.location.href.skipLastWhile(c => c === "/");
    if (currentUrl === utils.origin()) {
        currentUrl = currentUrl + "/Home/Index";
    }
    NavBarUtils.$ActiveNavLink = $(".my-navbar").first().find(".my-nav-link").$toArray().filter($nl => $nl.attr("href") && $nl.attr("href").equalsIgnoreCase(currentUrl)).first();
    NavBarUtils.setNavLinksActiveClasses(NavBarUtils.$ActiveNavLink, null);
}

export async function blazor_NavBar_AfterRender() {
    //const $navLinksToSetAsRendered = $(".my-navbar").find(".my-nav-item > .my-nav-link:not([rendered='true'])");
    //console.log(`navbar subsequent render, ${$navLinksToSetAsRendered.length} nav-links to be as rendered`);
    //NavBarUtils.finishAndRemoveRunningAnims();
    NavBarUtils.adjustToDeviceSize();
    NavBarUtils.setNavLinksActiveClasses(NavBarUtils.$ActiveNavLink, null);

    //$navLinksToSetAsRendered.attr("rendered", "true");
    //console.log(`navbar subsequent render, set ${$navLinksToSetAsRendered.length} nav-links as rendered`);
}

export function blazor_Layout_AfterRender_SetupNavbar() { // navLinkDotNetRefs
    console.log("navbar after layout render");
    //NavBarUtils.finishAndRemoveRunningAnims();

    // hide webassembly spinner

    $(".my-navbar").children(".spinner-container").remove();
    $(".my-navbar").css("overflow", "visible");

    // associate navlinks with the navigation logic

    //NavLinkUtils.setNavLinkDotNetRefs(navLinkDotNetRefs); // nav from JS will be checking nav link ids in C#

    // handle scrollbar

    NavBarUtils.ScrollBar = $(".my-page-scroll-container").first().addClass("os-host-flexbox").overlayScrollbars({
        className: "os-theme-dark",
        scrollbars : {
            clickScrolling : true
        },
        callbacks: {
            onScroll: function () {
                NavBarUtils.handleScrollBarChange();
            }
        }
    }).overlayScrollbars();

    NavBarUtils.adjustToDeviceSize(); // requires overlayscrollbar loaded (otherwise it would take default system scrollbar dimensions)
    const $stickyNavBars = $(".my-navbar.my-sticky").$toArray();
    for (let $nb of $stickyNavBars) {
        NavBarUtils.adjustNavbarMarginTop($nb); // requires overlayscrollbar loaded (otherwise it would take default system scrollbar dimensions)      
    }
    
    NavBarUtils.handleScrollBarChange();
}

export function blazor_NavBar_SetNavLinksActiveClasses() {
    NavBarUtils.setNavLinksActiveClasses(null, null);
}

$(document).ready(function () {

    $(document).on("mouseleave", ".my-navbar", async function () {
        // ultimately mouseleaving any nav-link, item, menu will end here, use it!
    });

    $(document).on("mousedown", ".my-nav-item.my-dropdown > .my-nav-link, .my-nav-item.my-dropright > .my-nav-link, .my-nav-item.my-dropup > .my-nav-link, .my-nav-item.my-dropleft > .my-nav-link", async function (e) {
        const $navLink = $(this);
        const $nb = $navLink.parents(".my-navbar").first();
        const $searchContainer = $nb.find(".my-nav-search-container").first();
        const $modals = $(".my-modal");
        const isRendered = ($navLink.attr("rendered") || "false").toBool();
        const isDisabled = $navLink.is(".disabled") || $navLink.attr("disabled") === "disabled";

        if (e.which !== 1 || !$nb.is(".shown")) { // prevents clicking on ddl if navbar is hiding
            return;
        }
        if ($searchContainer.is(".shown") || $modals.is(".shown")) { // is implies any
            return;
        }
        if (!isRendered) {
            console.log("nav-link is not rendered yet");
            return;
        }
        if (isDisabled) {
            console.log("nav-link is disabled");
            return;
        }

        const $navItem = $navLink.closest(`.my-nav-item`);
        const $navMenu = $navItem.children(".my-nav-menu").first();
        const $navMenuAncestors = $navItem.parentsUntil(".my-navbar").filter(".my-nav-menu");
        const $otherNavMenus = $navLink.closest(".my-navbar").find(".my-nav-menu").not($navMenu).not($navMenuAncestors);
        const $arrOtherNavMenusToHide = $otherNavMenus.filter(".shown").toArray().map(nm => $(nm));
        const dropClass = $navItem.attr("class").split(" ").find(c => c.includes("drop"));
        const show = !$navMenu.is(".shown");
        $navMenu.toggleClass("shown");
        $otherNavMenus.removeClass("shown");

        NavBarUtils.finishAndRemoveRunningAnims();
        NavBarUtils.prepareNavMenu($navLink, dropClass);
        NavBarUtils.createToggleNmAnim($navMenu, show, dropClass);
        NavBarUtils.createHideOnmAnim($arrOtherNavMenusToHide);
        NavBarUtils.createToggleNmOcIconAnim($navLink, show);
        NavBarUtils.createHideOnmOcIconAnim($arrOtherNavMenusToHide);
        NavBarUtils.runAnims();
        NavBarUtils.setNavLinksActiveClasses(NavBarUtils.$ActiveNavLink, show ? $navMenu : $navMenuAncestors.first() || null);
    });

    $(document).on("mousedown", ".my-nav-item > .my-nav-link", async function(e) { // hide nav-menus on making valid selection
        e.preventDefault();
        const $clickedNavLink = $(this);
        const $clickedNavItem = $clickedNavLink.parents(".my-nav-item").first();
        const isRendered = ($clickedNavLink.attr("rendered") || "false").toBool();
        const isDisabled = $clickedNavLink.is(".disabled") || $clickedNavLink.attr("disabled") === "disabled";

        if (e.which !== 1 || $clickedNavLink.parents(`.my-nav-item`).first().classes().some(c => c.startsWith("my-drop"))) {
            return;
        }

        if (!isRendered) {
            console.log("nav-link is not rendered yet");
            return;
        }

        if (isDisabled) {
            console.log("nav-link is disabled");
            return;
        }

        if (!$clickedNavItem.is(".my-toggler, .my-brand, .my-search, .my-login")) {
            await NavLinkUtils.navigateAsync($clickedNavLink.guid());
        }

        const $allShownNavMenus = $clickedNavLink.parents(".my-navbar").find(".my-nav-menu.shown");
        const $arrAllShownNavMenus = $allShownNavMenus.$toArray();
        const $nb = $clickedNavLink.parents(".my-navbar").first();
        const showNavBar = !$nb.is(".shown");
        const $searchContainer = $nb.find(".my-nav-search-container").first();
        const $modals = $(".my-modal");

        $allShownNavMenus.removeClass("shown");

        console.log("[\"mousedown\", \".my-nav-item > .my-nav-link\"] NavBarUtils.finishAndRemoveRunningAnims()");
        NavBarUtils.finishAndRemoveRunningAnims();
        NavBarUtils.createHideOnmAnim($arrAllShownNavMenus);
        NavBarUtils.createHideOnmOcIconAnim($arrAllShownNavMenus);

        if (!$searchContainer.is(".shown") && !$modals.is(".shown") && ($clickedNavItem.is(".my-nav-item.my-toggler") || !showNavBar && $(window).width() < 768)) { // toggle if clicked item is toggler, or hide if it is home or an actual link, do nothing if searchmodal is opened
            $nb.toggleClass("shown");
            NavBarUtils.createToggleNavBarForSmAnims($nb, showNavBar);
        }

        const showSearchContainer = !$searchContainer.is(".shown");
        if ($clickedNavItem.is(".my-nav-item.my-search") && !$modals.is(".shown")) {
            $searchContainer.toggleClass("shown");
            NavBarUtils.createToggleSearchModalAnims($searchContainer, showSearchContainer);
        }

        console.log("[\"mousedown\", \".my-nav-item > .my-nav-link\"] NavBarUtils.runAnims()");
        NavBarUtils.runAnims();
        NavBarUtils.setNavLinksActiveClasses($clickedNavLink, null);
    });

    $(document).on("mousedown", "*:not(.my-nav-item) > a.my-nav-link", async function (e) { // 'a' is enabled, 'div' is disabled, no .disabled class for navlinks
        e.preventDefault();
        const $clickedNavLink = $(this);
        const isDisabled = $clickedNavLink.is(".disabled") || $clickedNavLink.attr("disabled") === "disabled";

        if (e.which !== 1 || isDisabled) {
            return;
        }

        await NavLinkUtils.navigateAsync($clickedNavLink.guid());
        NavBarUtils.setNavLinksActiveClasses(null, null);
    });

    $(document).on("mousedown", "body", function (e) { // hide nav-menus on clicking sth else
        if ($(e.target).is(".my-navbar") || $(e.target).parents(".my-navbar").length || e.which !== 1) {
            return; // return if user didn't click navbar or its descendant with left button or if there are no shown nav-menus
        }

        NavBarUtils.finishAndRemoveRunningAnims();

        const windowWidth = $(window).width();
        const $navBars = $(".my-navbar").$toArray();

        for (let $nb of $navBars) {
            const $searchModal = $nb.find(".my-nav-search-container").first();

            if ($nb.find(".my-nav-menu.shown").length > 0) {
                const $allShownNavMenus = $nb.find(".my-nav-menu.shown");
                const $arrAllShownNavMenus = $allShownNavMenus.$toArray();
                $allShownNavMenus.removeClass("shown");

                NavBarUtils.createHideOnmAnim($arrAllShownNavMenus);
                NavBarUtils.createHideOnmOcIconAnim($arrAllShownNavMenus);
            }

            if (windowWidth < 768 && $nb.is(".shown")) {
                $nb.removeClass("shown");
                NavBarUtils.createToggleNavBarForSmAnims($nb, false);
            }

            if ($searchModal.is(".shown")) {
                $searchModal.toggleClass("shown");
                NavBarUtils.createToggleSearchModalAnims($searchModal, false);
            }
        }

        NavBarUtils.runAnims();
        NavBarUtils.setNavLinksActiveClasses();
    });

    $(document).on("mouseenter", ".my-nav-item > .my-nav-link", async function () { // [rendered='true']:not([disabled])
        const navLink = this;
        const $navLink = $(navLink);
        const $navItem = $navLink.parents(".my-nav-item").first();
        const navLinkContent = $navLink.children(".my-nav-link-content")[0];
        // it specifically can't be `$navlink.children("div").children("svg").children("path").toArray();` because search icon for instance is in different navLink but in the same NavItem
        const navLinkIcons = $navLink.parent(".my-nav-item").find("div > svg > path").parent().parent().toArray().filter(ni => $(ni).classes().length > 1 && ($(ni).parent().is($navLink) || $(ni).parent().is($navLink.parent()))).map(ni => $(ni).find("path")[0]); // all 4 icons: magnifying glass, x and the same for xs displays need to be animated simulatanously because we don't know which one user ends up needing (and it might stay white if we don't animate it) 
        const runningAnims = anime.running.filter(a => a.animatables.map(tbl => tbl.target).containsAny([navLink, navLinkContent, ...navLinkIcons]));
        const $nb = $navLink.parents(".my-navbar").first();
        const $searchContainer = $nb.find(".my-nav-search-container").first();
        const $modals = $(".my-modal");
        const isRendered = ($navLink.attr("rendered") || "false").toBool();
        const isDisabled = $navLink.is(".disabled") || $navLink.attr("disabled") === "disabled";

        for (let anim of runningAnims) {
            anim.seek(anim.duration);
            NavBarUtils.animsNavBar.remove(anim);
        }

        if (!isRendered || isDisabled) {
            $navLink.mouseleave();
            return;
        }

        if (!$nb.is(".shown") && !$navItem.is(".my-toggler, .my-home, .my-brand, .my-search, .my-login")) { // prevents hovering ddl if navbar is hiding
            $navLink.mouseleave();
            return;
        }
        if ($searchContainer.is(".shown") && !$navLink.parent().hasClass("my-search") || $modals.is(".shown")) {// is implies any
            $navLink.mouseleave();
            return;
        }

        if ($navLink.is(".hovered")) return; // hover can happen only once, otherwise it would take init css from already hovered item and animation would be broken
        $navLink.addClass("hovered");

        const origBoxShadow = $navLink.css("box-shadow") === "none" ? "0 0 0.00000001px 0.00000001px rgb(255, 255, 255)" : $navLink.css("box-shadow");
        const origBoxShadowSplit = origBoxShadow.split(/ (?![^(]*\))/);
        if (origBoxShadowSplit[0].startsWith("rgb")) {
            origBoxShadowSplit.push(origBoxShadowSplit.shift());
        }

        const nlBoxShadow = origBoxShadowSplit.map((s, i) => {
            const parsed = parseFloat(s);
            if (!parsed.isNumber())
                return s;
            if (parsed !== 0 || i < 2) // 0 0 6px 2px white, first two should remain 0
                return `${parseFloat(s).toString().removeScientificNotation()}px`;
            return `${parseFloat(s).toString().removeScientificNotation()}.00000001px`;
        }).join(" ");

        NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")] = {
            "box-shadow": nlBoxShadow,
            "nav-link-content_color": $(navLinkContent).css("color"),
            "nav-link-icons_fill": $(navLinkIcons).first().css("fill"),
            "nav-link_was-active": $navLink.is(".active")
        };

        const activeClasses = $navLink.classes().filter(c => c.startsWith("active"));
        if (activeClasses.length > 0) {
            NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")]["background-image-active"] = $navLink.css("background-image");
            $navLink.removeClass(activeClasses.join(" "));
            NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")]["background-image-inactive"] =
                $navLink.css("background-image") === "none"
                    ? "linear-gradient(rgba(0, 0, 0, 0), rgba(0, 0, 139, 0))"
                    : $navLink.css("background-image");
            $navLink.addClass(activeClasses.join(" "));
        } else {
            NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")]["background-image-inactive"] =
                $navLink.css("background-image") === "none"
                    ? "linear-gradient(rgba(0, 0, 0, 0), rgba(0, 0, 139, 0))"
                    : $navLink.css("background-image");
            $navLink.addClass("active");
            NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")]["background-image-active"] = $navLink.css("background-image");
            $navLink.removeClass("active");
        }

        const initCss = NavBarUtils.NavLinksInitCss[$navLink.attr("my-guid")];

        NavBarUtils.animsNavBar.push(anime({
            targets: navLink,
            //boxShadow: [initCss["box-shadow"], "0 0 6px 2px rgb(255, 255, 255)"],
            backgroundImage: [
                $navLink.is(".active") ? initCss["background-image-active"] : initCss["background-image-inactive"],
                "linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 139, 0.5))"
            ],
            duration: 500,
            easing: "easeInOutSine"
        }));

        NavBarUtils.animsNavBar.push(anime({
            targets: navLinkContent,
            color: [initCss["nav-link-content_color"], "rgb(255, 255, 255)"],
            duration: 500,
            easing: "easeInOutSine"
        }));

        NavBarUtils.animsNavBar.push(anime({
            targets: navLinkIcons,
            fill: [initCss["nav-link-icons_fill"], "rgb(255, 255, 255)"],
            duration: 500,
            easing: "easeInOutSine"
        }));

    });

    $(document).on("mouseleave", ".my-nav-item > .my-nav-link", async function () {
        const navLink = this;
        const $navlink = $(navLink);
        const navLinkContent = $navlink.children(".my-nav-link-content")[0];
        // it specifically can't be `$navlink.children("div").children("svg").children("path").toArray();` because search icon for instance is in different navLink but in the same NavItem
        const navLinkIcons = $navlink.parent(".my-nav-item").find("div > svg > path").parent().parent().toArray().filter(ni => $(ni).classes().length > 1 && ($(ni).parent().is($navlink) || $(ni).parent().is($navlink.parent()))).map(ni => $(ni).find("path")[0]); // all 4 icons: magnifying glass, x and the same for xs displays need to be animated simulatanously because we don't know which one user ends up needing (and it might stay white if we don't animate it) 
        const runningAnims = anime.running.filter(a => a.animatables.map(tbl => tbl.target).containsAny([navLink, navLinkContent, ...navLinkIcons]));

        //console.log("[\"mouseleave\", \".my-nav-item > .my-nav-link\"] let anim of runningAnims, seek, remove");
        for (let anim of runningAnims) {
            anim.seek(anim.duration);
            NavBarUtils.animsNavBar.remove(anim);
        }

        if (!$navlink.is(".hovered")) return; // hover can happen only once, otherwise it would take init css from already hovered item and animation would be broken
        $navlink.removeClass("hovered");

        const initCss = NavBarUtils.NavLinksInitCss[$navlink.attr("my-guid")];

        //console.log("[\"mouseleave\", \".my-nav-item > .my-nav-link\"] let anim of runningAnims, create, run anims");
        NavBarUtils.animsNavBar.push(anime({
            targets: navLink,
            //boxShadow: ["0 0 6px 2px rgb(255, 255, 255)", initCss["box-shadow"]],
            backgroundImage: [
                "linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 139, 0.5))",
                $navlink.is(".active, .active-descendant") ? initCss["background-image-active"] : initCss["background-image-inactive"]
            ],
            duration: 500,
            easing: "easeInOutSine",
            complete: function () {
                $navlink.removeCss("background-image");
                $navlink.removeCss("box-shadow");
            }
        }));

        NavBarUtils.animsNavBar.push(anime({
            targets: navLinkContent,
            color: ["rgb(255, 255, 255)", initCss["nav-link-content_color"]],
            duration: 500,
            easing: "easeInOutSine",
            complete: function () {
                $(navLinkContent).removeCss("color");
            }
        }));

        NavBarUtils.animsNavBar.push(anime({
            targets: navLinkIcons,
            fill: ["rgb(255, 255, 255)", initCss["nav-link-icons_fill"]],
            duration: 500,
            easing: "easeInOutSine",
            begin: function () {
                console.log(`["mouseleave", ".my-nav-item > .my-nav-link"] [navLinkIcons] fill: ["rgb(255, 255, 255)", "${initCss["nav-link-icons_fill"]}"] began`);
            },
            complete: function () {
                console.log(`["mouseleave", ".my-nav-item > .my-nav-link"] [navLinkIcons] fill: ["rgb(255, 255, 255)", "${initCss["nav-link-icons_fill"]}"] complete`);
                $(navLinkIcons).removeCss("fill");
            }
        }));
    });

    $(window).on("resize", () => { // on resize window
        NavBarUtils.finishAndRemoveRunningAnims(); // stop all animations
        NavBarUtils.adjustToDeviceSize();
        NavBarUtils.setNavLinksActiveClasses(NavBarUtils.$ActiveNavLink, null);
        NavBarUtils.handleScrollBarChange(true);
    });

});
