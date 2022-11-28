/// <reference path="wrapper.js" />

import Wrapper from "./wrapper.js";
import utils from "./utils.js";

export class NavBarUtils {
    static animsNavBar = [];
    static NavLinksInitCss = {};
    static NavBarInitCss = {};
    static SearchModalInitCss = {};
    static PositionInScrollableContainer;
    static ScrollBar;
    static PreviousWindowWidth;
    static $ActiveNavLink;

    static getSlideClipPath(show, dropClass, width, height, ignoreWindowSize) {
        if (show) {
            if (dropClass.endsWith("down") || !ignoreWindowSize && $(window).width() < 768) {
                return [
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} 0.00000001px, 0.00000001px 0.00000001px)`,
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`
                ];
            } else if (dropClass.endsWith("right")) {
                return [
                    `polygon(0.00000001px 0.00000001px, 0.00000001px 0.00000001px, 0.00000001px ${height}, 0.00000001px ${height})`,
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`
                ];
            } else if (dropClass.endsWith("left")) {
                return [
                    `polygon(${width} 0.00000001px, ${width} 0.00000001px, ${width} ${height}, ${width} ${height})`,
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`
                ];
            } else if (dropClass.endsWith("up")) {
                return [
                    `polygon(0.00000001px ${height}, ${width} ${height}, ${width} ${height}, 0.00000001px ${height})`,
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`
                ];
            }
        } else {
            if (dropClass.endsWith("down") || !ignoreWindowSize && $(window).width() < 768) {
                return [
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`,
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} 0.00000001px, 0.00000001px 0.00000001px)`
                ];
            } else if (dropClass.endsWith("right")) {
                return [
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`,
                    `polygon(0.00000001px 0.00000001px, 0.00000001px 0.00000001px, 0.00000001px ${height}, 0.00000001px ${height})`
                ];
            } else if (dropClass.endsWith("left")) {
                return [
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`,
                    `polygon(${width} 0.00000001px, ${width} 0.00000001px, ${width} ${height}, ${width} ${height})`
                ];
            } else if (dropClass.endsWith("up")) {
                return [
                    `polygon(0.00000001px 0.00000001px, ${width} 0.00000001px, ${width} ${height}, 0.00000001px ${height})`,
                    `polygon(0.00000001px ${height}, ${width} ${height}, ${width} ${height}, 0.00000001px ${height})`
                ];
            }
        }
    }

    static prepareNavMenu($navLink, dropClass) {
        const $navItem = $navLink.closest(".my-nav-item");
        const $navMenu = $navItem.children(".my-nav-menu").first();
        const $navMenuAncestors = $navItem.parentsUntil(".my-navbar").filter(".my-nav-menu");
        const $topMostNavItem = $navMenu.closest(".my-navbar > .my-nav-item");
        const $navMenuNavItems = $navMenu.children(".my-nav-item").$toArray();
        const $otherNavMenus = $navLink.parents(".my-navbar").first().find(".my-nav-menu").not($navMenu);

        const removedClasses = $navMenu.removeClassesAndGetRemoved("my-d-none");
        const addedClasses = $navMenu.addClassesAndGetAdded("my-d-block");

        $navMenu.parent().filter(".my-nav-item").css("z-index", 1);
        $otherNavMenus.parent().filter(".my-nav-item").removeCss("z-index");

        if (!$navMenu.attr("anim-height"))
            $navMenu.attr("anim-height", $navMenu.outerHeight() + "px");
        if (!$navMenu.attr("anim-width")) {
            if ($navMenuAncestors.length > 0) {
                $navMenu.attr("anim-width", ($(window).width() < 768
                    ? $navMenuAncestors.first().outerWidth()
                    : dropClass.endsWithAny("dropdown", "dropup")
                        ? Math.max($navMenu.outerWidth(), $navMenuAncestors.first().outerWidth())
                        : $navMenu.outerWidth()) + "px");
            } else {
                //$navMenu.attr("anim-width", Math.max($navMenu.outerWidth(), $navLink.outerWidth() + ($navItem.next().length === 0 || $navItem.next().is(".my-ml-auto") ? 0 : 1)) + "px");
                $navMenu.attr("anim-width", Math.max($navMenu.outerWidth(), $navLink.outerWidth() + ($navItem.next().length === 0 ? 0 : 1)) + "px"); // || $navItem.next().is(".my-ml-auto") not needed because I am painting right border as box shadow outside 
            }
        }

        if (!$topMostNavItem.attr("init-height"))
            $topMostNavItem.attr("init-height", $topMostNavItem.outerHeight() + "px");
        for (let $nmni of $navMenuNavItems) {
            if (!$nmni.attr("init-height"))
                $nmni.attr("init-height", $nmni.outerHeight() + "px");
        }

        const navMenuWidth = parseFloat($navMenu.attr("anim-width"));

        if (dropClass.endsWith("dropdown") || $(window).width() < 768) {
            let left = 0;
            let top = $navLink.outerHeight();
            let minWidth = $(window).width() < 768 ? "100%" : navMenuWidth + "px";

            if ($navMenuAncestors.length > 0) {
                const $navMenuParent = $navMenuAncestors.first();
                const navMenuParentBorderWidth = parseFloat($navMenuParent.css("border-top-width").split("px")[0]);
                const navMenuParentLeftPadding = parseFloat($navMenuParent.css("padding-left").split("px")[0]);

                left = -navMenuParentBorderWidth - navMenuParentLeftPadding;
                minWidth = $navMenuParent.outerWidth() + "px";
            }

            $navMenu.css({
                "min-width": minWidth,
                "left": left + "px",
                "top": top + "px"
            });
        } else if (dropClass.endsWith("dropright")) {
            const navLinkBorderLeftWidth = parseFloat($navItem.children(".my-nav-link").first().css("border-left-width"));
            const navLinkWidth = $navLink.outerWidth();

            let left = navLinkWidth - navLinkBorderLeftWidth;
            let top = 0;

            if ($navMenuAncestors.length > 0) {
                const $navMenuParent = $navMenuAncestors.first();
                const navMenuParentWidth = $navMenuParent.outerWidth();
                const navMenuParentBorderWidth = parseFloat($navMenuParent.css("border-top-width").split("px")[0]);
                const navMenuParentLeftPadding = parseFloat($navMenuParent.css("padding-left").split("px")[0]);
                const navMenuParentTopPadding = parseFloat($navMenuParent.css("padding-top").split("px")[0]);

                left = navMenuParentWidth - navMenuParentBorderWidth * 2 - navMenuParentLeftPadding;
                top = -navMenuParentTopPadding - navMenuParentBorderWidth;
            }

            $navMenu.css({
                "min-width": "0",
                "left": left + "px",
                "top": top + "px"
            });
        } else if (dropClass.endsWith("dropleft")) {
            const navLinkBorderLeftWidth = parseFloat($navItem.children(".my-nav-link").first().css("border-left-width"));

            let left = -navMenuWidth + navLinkBorderLeftWidth;
            let top = 0;

            if ($navMenuAncestors.length > 0) {
                const $navMenuParent = $navMenuAncestors.first();
                const navMenuParentBorderWidth = parseFloat($navMenuParent.css("border-top-width").split("px")[0]);
                const navMenuParentLeftPadding = parseFloat($navMenuParent.css("padding-left").split("px")[0]);
                const navMenuParentTopPadding = parseFloat($navMenuParent.css("padding-top").split("px")[0]);

                left = -navMenuWidth - navMenuParentLeftPadding;
                top = -navMenuParentTopPadding - navMenuParentBorderWidth;
            }

            $navMenu.css({
                "min-width": "0",
                "left": left + "px",
                "top": top + "px"
            });
        } else if (dropClass.endsWith("dropup")) {
            const navMenuHeight = parseFloat($navMenu.attr("anim-height"));

            let left = 0;
            let top = -navMenuHeight;
            let minWidth = $(window).width() < 768 ? "100%" : navMenuWidth + "px";

            if ($navMenuAncestors.length > 0) {
                const $navMenuParent = $navMenuAncestors.first();
                const navMenuParentBorderWidth = parseFloat($navMenuParent.css("border-top-width").split("px")[0]);
                const navMenuParentLeftPadding = parseFloat($navMenuParent.css("padding-left").split("px")[0]);

                left = -navMenuParentBorderWidth - navMenuParentLeftPadding;
                minWidth = $navMenuParent.outerWidth() + "px";
            }

            $navMenu.css({
                "min-width": minWidth,
                "left": left + "px",
                "top": top + "px"
            });
        }

        if (!$navMenu.attr("init-offset-top"))
            $navMenu.attr("init-offset-top", $navMenu.css("top"));

        if (addedClasses)
            $navMenu.removeClassesAndGetRemoved(addedClasses);
        if (removedClasses)
            $navMenu.addClassesAndGetAdded(removedClasses);
    }

    static finishAndRemoveRunningAnims() {
        for (let anim of NavBarUtils.animsNavBar) {
            if (anim.children > 0) {
                const timeline = anim.children;
                for (let cAnim of timeline) {
                    cAnim.seek(cAnim.duration);
                    //for (let target of cAnim.animatables.map(tbl => tbl.target))
                    //    anime.remove(target); // this will bug hovering
                }
            } else {
                anim.seek(anim.duration);
            }
        }
        NavBarUtils.animsNavBar.clear();
    }

    static createToggleNmAnim($navMenu, show, dropClass) {
        const height = $navMenu.attr("anim-height");
        const width = $navMenu.attr("anim-width");
        const navmenuNavitems = $navMenu.children(".my-nav-item").toArray();

        NavBarUtils.animsNavBar.unshift(show
            ? anime.timeline({
                duration: 500
            }).add({
                targets: $navMenu[0],
                clipPath: NavBarUtils.getSlideClipPath(show, dropClass, width, height),
                opacity: [0, 1],
                duration: 500,
                easing: "easeOutExpo",
                autoplay: false,
                begin: function () {
                    $navMenu.removeClass("my-d-none").addClass("my-d-block");
                },
                complete: function (anim) {
                    if (!anim.began) {
                        $navMenu.removeClass("my-d-none").addClass("my-d-block");
                    }
                    $navMenu.css("clip-path", "");
                }
            }).add({
                targets: navmenuNavitems,
                translateX: ["-100px", "0px"],
                opacity: [0, 1],
                delay: anime.stagger(500 / navmenuNavitems.length), // increase for each nav-item
                easing: "easeOutElastic",
                autoplay: false,
                complete: function () {
                    $navMenu.removeCss("opacity");
                }
            }, 0)
            : anime({
                targets: $navMenu[0],
                clipPath: NavBarUtils.getSlideClipPath(show, dropClass, width, height),
                opacity: [1, 0],
                duration: 500,
                easing: "easeOutCirc",
                autoplay: false,
                complete: function () {
                    $navMenu.removeClass("my-d-block").addClass("my-d-none");
                    $navMenu.css("clip-path", "");
                }
            }));

        if ($(window).width() < 768) {
            const $navItem = $navMenu.parents(".my-nav-item").first();
            const navItemHeight = parseFloat($navItem.attr("init-height"));
            const navMenuHeightFromNavItems = $navMenu.children(".my-nav-item").$toArray().map($ni => parseFloat($ni.attr("init-height"))).sum();

            const showHeight = navItemHeight + navMenuHeightFromNavItems + "px"; // TODO: w/o parenthesis?
            const hideHeight = navItemHeight + "px";

            NavBarUtils.animsNavBar.unshift(show ? anime({
                targets: $navItem[0],
                height: [hideHeight, showHeight],
                duration: 500,
                easing: "easeOutExpo",
                autoplay: false,
                complete: function () {
                    NavBarUtils.handleScrollBarChange();
                    //if ($navMenu.parents(".my-navbar").first().is(".glued")) {
                    //    NavBarUtils.ScrollBar.scroll({ y: 0 }); // this will raise the event and call handleScrollBarChange indirectly after scrolling to top
                    //}
                }
            }) : anime({
                targets: $navItem[0],
                height: [showHeight, hideHeight],
                duration: 590,
                easing: "easeOutCirc",
                autoplay: false,
                complete: function () {
                    NavBarUtils.handleScrollBarChange();
                }
            }));

            const $ancestorNavMenus = $navMenu.parents(".my-nav-menu").$toArray();
            for (let $nm of $ancestorNavMenus) {
                const $nmNavItem = $nm.parents(".my-nav-item").first();
                const nmNavItemHeight = parseFloat($nmNavItem.attr("init-height"));
                const nmHeight = $nm.children(".my-nav-item").$toArray().map($ni => parseFloat($ni.attr("init-height"))).sum();

                const $nmDescendantsExcludingNavMenu = $navMenu.parentsUntil($nm).filter(".my-nav-menu").$toArray();
                const nmDescendantsExcludingNavMenuHeight = $nmDescendantsExcludingNavMenu.map($dnm => $dnm.children(".my-nav-item").$toArray()
                    .map($ni => parseFloat($ni.attr("init-height"))).sum()).sum();

                const nmShowHeight = nmNavItemHeight + nmDescendantsExcludingNavMenuHeight + navMenuHeightFromNavItems + nmHeight + "px";
                const nmHideHeight = nmNavItemHeight + nmDescendantsExcludingNavMenuHeight + nmHeight + "px";

                NavBarUtils.animsNavBar.unshift(show ? anime({
                    targets: $nmNavItem[0],
                    height: [nmHideHeight, nmShowHeight],
                    duration: 500,
                    easing: "easeOutExpo",
                    autoplay: false
                }) : anime({
                    targets: $nmNavItem[0],
                    height: [nmShowHeight, nmHideHeight],
                    duration: 500,
                    easing: "easeOutCirc",
                    autoplay: false
                }));
            }
        }

    }

    static createHideOnmAnim($arrOtherNavMenusToHide) {
        for (let $onm of $arrOtherNavMenusToHide) {
            const height = $onm.attr("anim-height");
            const width = $onm.attr("anim-width");
            const dropClass = $onm.closest(".my-nav-item").attr("class").split(" ").find(c => c.includes("drop"));
            NavBarUtils.animsNavBar.unshift(anime({
                targets: $onm[0],
                clipPath: NavBarUtils.getSlideClipPath(false, dropClass, width, height),
                opacity: [1, 0],
                duration: 500,
                easing: "easeOutCirc",
                autoplay: false,
                complete: function () {
                    $onm.removeClass("my-d-block").addClass("my-d-none");
                    $onm.css("clip-path", "");
                }
            }));

            if ($(window).width() < 768) {
                const $navItem = $onm.parents(".my-nav-item").first();
                const navItemHeight = parseFloat($navItem.attr("init-height"));
                const navMenuHeightFromNavItems = $onm.children(".my-nav-item").$toArray().map($ni => parseFloat($ni.attr("init-height"))).sum();

                const showHeight = navItemHeight + navMenuHeightFromNavItems + "px";
                const hideHeight = navItemHeight + "px";

                NavBarUtils.animsNavBar.unshift(anime({
                    targets: $navItem[0],
                    height: [showHeight, hideHeight],
                    duration: 590,
                    easing: "easeOutCirc",
                    autoplay: false
                }));

                const $ancestorNavMenus = $onm.parents(".my-nav-menu").$toArray();
                const onmHeightFromNavitems = $onm.children(".my-nav-item").$toArray().map($ni => parseFloat($ni.attr("init-height"))).sum();
                for (let $nm of $ancestorNavMenus) {
                    const $nmNavItem = $nm.parents(".my-nav-item").first();
                    const nmNavItemHeight = parseFloat($nmNavItem.attr("init-height"));
                    const nmHeight = $nm.children(".my-nav-item").$toArray().map($ni => parseFloat($ni.attr("init-height"))).sum();

                    const $nmDescendantsExcludingNavMenu = $onm.parentsUntil($nm).filter(".my-nav-menu").$toArray();
                    const nmDescendantsExcludingNavMenuHeight = $nmDescendantsExcludingNavMenu.map($dnm => $dnm.children(".my-nav-item").$toArray()
                        .map($ni => parseFloat($ni.attr("init-height"))).sum()).sum();

                    const nmShowHeight = nmNavItemHeight + nmDescendantsExcludingNavMenuHeight + onmHeightFromNavitems + nmHeight + "px";
                    const nmHideHeight = nmNavItemHeight + nmDescendantsExcludingNavMenuHeight + nmHeight + "px";

                    NavBarUtils.animsNavBar.unshift(anime({
                        targets: $nmNavItem[0],
                        height: [nmShowHeight, nmHideHeight],
                        duration: 500,
                        easing: "easeOutCirc",
                        autoplay: false
                    }));
                }
            }
        }
    }

    static createToggleNmOcIconAnim($navLink, show) {
        const $openIcon = $(window).width() < 768 ? $navLink.find(".my-nav-link-open-icon-xs") : $navLink.find(".my-nav-link-open-icon");
        const $closeIcon = $(window).width() < 768 ? $navLink.find(".my-nav-link-close-icon-xs") : $navLink.find(".my-nav-link-close-icon");
        const $navLinkContent = $navLink.find(".my-nav-link-content");

        const $iconToHide = show ? $openIcon : $closeIcon;
        const $iconToShow = show ? $closeIcon : $openIcon;

        NavBarUtils.animsNavBar.unshift(anime.timeline({
            duration: 500
        }).add({
            targets: $iconToHide[0],
            opacity: [1, 0],
            duration: 250,
            easing: "easeInOutSine",
            autoplay: false,
            begin: function () {
                $navLink.css("width", $navLink.outerWidth() + "px");
                $navLinkContent.css("max-width", $navLinkContent.outerWidth() + "px");
            },
            complete: function (anim) {
                if (!anim.began) {
                    $navLink.css("width", $navLink.outerWidth() + "px");
                    $navLinkContent.css("max-width", $navLinkContent.outerWidth() + "px");
                }
                $iconToHide.removeClass("my-d-flex").addClass("my-d-none");
            }
        }).add({
            targets: $iconToShow[0],
            opacity: [0, 1],
            duration: 250,
            easing: "easeInOutSine",
            autoplay: false,
            begin: function () {
                $iconToShow.removeClass("my-d-none").addClass("my-d-flex");
            },
            complete: function (anim) {
                if (!anim.began) {
                    $iconToShow.removeClass("my-d-none").addClass("my-d-flex");
                }
                //$navLink.css("width", "");
                $navLinkContent.css("max-width", "");
            }
        }));
    }

    static createHideOnmOcIconAnim($arrOtherNavMenusToHide) {
        for (let $onm of $arrOtherNavMenusToHide) {
            const $onmNavItem = $onm.closest(".my-nav-item");
            const $onmNavLink = $onmNavItem.children(".my-nav-link").first();
            const $onmNavLinkContent = $onmNavLink.find(".my-nav-link-content");
            const $onmCloseIcon = $(window).width() < 768 ? $onmNavLink.find(".my-nav-link-close-icon-xs") : $onmNavLink.find(".my-nav-link-close-icon");
            const $onmOpenIcon = $(window).width() < 768 ? $onmNavLink.find(".my-nav-link-open-icon-xs") : $onmNavLink.find(".my-nav-link-open-icon");

            NavBarUtils.animsNavBar.unshift(anime.timeline({
                duration: 500
            }).add({
                targets: $onmCloseIcon[0],
                opacity: [1, 0],
                duration: 250,
                easing: "easeInOutSine",
                autoplay: false,
                begin: function () {
                    $onmNavLink.css("width", $onmNavLink.outerWidth() + "px");
                    $onmNavLinkContent.css("max-width", $onmNavLinkContent.outerWidth() + "px");
                },
                complete: function (anim) {
                    if (!anim.began) {
                        $onmNavLink.css("width", $onmNavLink.outerWidth() + "px");
                        $onmNavLinkContent.css("max-width", $onmNavLinkContent.outerWidth() + "px");
                    }
                    $onmCloseIcon.removeClass("my-d-flex").addClass("my-d-none");
                }
            }).add({
                targets: $onmOpenIcon[0],
                opacity: [0, 1],
                duration: 250,
                easing: "easeInOutSine",
                autoplay: false,
                begin: function () {
                    $onmOpenIcon.removeClass("my-d-none").addClass("my-d-flex");
                },
                complete: function (anim) {
                    if (!anim.began) {
                        $onmOpenIcon.removeClass("my-d-none").addClass("my-d-flex");
                    }
                    //$onmNavLink.css("width", "");
                    $onmNavLinkContent.css("max-width", "");
                }
            }));
        }

    }

    static adjustToDeviceSize() {
        const $arrNavBars = $(".my-navbar").$toArray();

        for (let $nb of $arrNavBars) {
            const $navMenus = $nb.find(".my-nav-menu").$toArray();
            for (let $nm of $navMenus) {
                NavBarUtils.setNavMenuToDeviceSize($nm);
                NavBarUtils.setNavLinkContentsLeftMargin($nm);
            }

            NavBarUtils.setNavBarToDeviceSize($nb); // relies on rearrange to read height correctly
            NavBarUtils.setNavLinkContentsLeftMarginForTopLevel($nb);
            NavBarUtils.removeIconsPaddingForNavLinksWithEmptyContent($nb);
            NavBarUtils.setRightBorderForLastLeftAlignedNavItem($nb);
            NavBarUtils.setNavBrandToDeviceSize($nb);
            NavBarUtils.adjustNavbarMarginTop($nb);
            NavBarUtils.rearrangeNavBarIfWrapping($nb);
            NavBarUtils.setSearchModalToDeviceSize($nb);
        }
    }

    static runAnims() {
        for (let anim of NavBarUtils.animsNavBar) {
            anim.play();
        }
    }

    static setNavLinksActiveClasses($clickedNavLinkOrUrl = null, $visibleNavMenu = null) {
        $clickedNavLinkOrUrl = $clickedNavLinkOrUrl || null;
        $visibleNavMenu = $visibleNavMenu || null;
        let $clickedNavLink;
        let desiredUrl;
        if (utils.isNull($clickedNavLinkOrUrl) || !$clickedNavLinkOrUrl.attr("href")) {
            $clickedNavLink = null;
            desiredUrl = Wrapper.$($(".my-navbar").find(".my-nav-link[href].active")).$toArray().distinctBy($nl => $nl.attr("href").toLowerCase()).singleOrNull().to$OrNull().attrOrNull("href").toLowerOrNull().unwrap() || window.location.href; // second case is when clicked nav-link is null and no links are currently active (i.e.: after logout from authorised page which after refresh is no longer on navbar)
        } else if (utils.is$($clickedNavLinkOrUrl)) {
            $clickedNavLink = $clickedNavLinkOrUrl;
            desiredUrl = $clickedNavLink.attr("href");
        } else if (utils.isString($clickedNavLinkOrUrl)) {
            $clickedNavLink = null;
            desiredUrl = $clickedNavLinkOrUrl;
        }

        if (!desiredUrl) {
            throw new Error("URL can't be empty");
        }

        NavBarUtils.$ActiveNavLink = $clickedNavLink;
        
        for (let $navBar of $(".my-navbar").$toArray()) {
            const nbNavLinksWithUrl = $navBar.find(".my-nav-link[href]");
            nbNavLinksWithUrl.removeClass("active");
            $navBar.find(".my-nav-item > .my-nav-link").removeClass("active-descendant");

            const $navLinksToActivate = Wrapper.$(nbNavLinksWithUrl).$toArray().where($nl => Wrapper.string($nl.attr("href")).equalsIgnoreCase(desiredUrl).unwrap());
            $navLinksToActivate.forEach($nl => $nl.addClass("active"));

            const $upperVisibleContainers = $visibleNavMenu
                ? $visibleNavMenu.parentsUntil($navBar).filter(".my-nav-menu").add($navBar).add($visibleNavMenu)
                : $navBar;
            const $upperVisibleNavLinks = $upperVisibleContainers.children(".my-nav-item").children(".my-nav-link").$toArray();
            const $visibleNavMenuNavLinks = $visibleNavMenu
                ? $visibleNavMenu.children(".my-nav-item").children(".my-nav-link").$toArray()
                : [];

            for (let $activeNavLink of $navBar.find(".my-nav-link.active").$toArray()) {
                const $anlAncestorNls = $activeNavLink.parentsUntil($navBar).filter(".my-nav-item").$toArray()
                    .map($ni => $ni.children(".my-nav-link").first()).skip(1);
                if ($anlAncestorNls.length === 0 || $visibleNavMenuNavLinks.contains($activeNavLink)) {
                    continue;
                }

                const activeAncestor = $anlAncestorNls.first($nl => $upperVisibleNavLinks.contains($nl));
                activeAncestor.addClass("active-descendant");
            }
        }
    }

    static createToggleNavBarForSmAnims($nb, show) {
        const closedHeight = NavBarUtils.NavBarInitCss["closed_height"];
        const openedHeight = NavBarUtils.NavBarInitCss["opened_height"];

        NavBarUtils.animsNavBar.push(show ? anime({
            targets: $nb[0],
            height: [closedHeight, openedHeight],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function () {
                $nb.css("height", closedHeight);
            },
            complete: function (anim) {
                if (!anim.began) {
                    $nb.css("height", closedHeight);
                } 
                $nb.css("height", "auto");

                NavBarUtils.handleScrollBarChange();
                //if ($nb.is(".glued")) {
                //    NavBarUtils.ScrollBar.scroll({ y: 0 });
                //}
            }
        }) : anime({
            targets: $nb[0],
            height: [openedHeight, closedHeight],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function () {
                $nb.css("height", openedHeight);
            },
            complete: function (anim) {
                if (!anim.began) {
                    $nb.css("height", openedHeight);
                }
                $nb.css("height", closedHeight);

                NavBarUtils.handleScrollBarChange();
            }
        }));
    }

    static createToggleSearchModalAnims($searchModal, showSearchModal) {
        const windowWidth = $(window).width();
        const $searchModalInputGroup = $searchModal.find(".my-text-input").parents(".my-input-group").first();
        const openedInputWidth = NavBarUtils.SearchModalInitCss["opened_input_width"];
        const openedInputHeight = NavBarUtils.SearchModalInitCss["opened_input_height"];
        const $searchNavItem = $searchModal.parents(".my-nav-item.my-search").first();
        const $searchNavLink = $searchNavItem.children(".my-nav-link").first();
        const $navItemsToBlur = windowWidth < 768 ? $searchNavItem.siblings(".my-toggler, .my-home, .my-brand, .my-login") : $searchNavItem.nextAll();
        const $iconToHide = $searchNavLink.children(".my-icon").first();
        const $iconToShow = $searchNavLink.next().filter(".my-icon").first();

        NavBarUtils.animsNavBar.push(showSearchModal ? anime({
            targets: $searchModal[0],
            opacity: [0, 1],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function () {
                $searchModal.removeClass("my-d-none").addClass("my-d-flex");
                $navItemsToBlur.css("filter", "blur(4px)"); // animating blur is too intensive
                $navItemsToBlur.css("clip-path", `polygon(0 0, 100% 0, 100% 100%, 0 100%)`); // clip-path would also cut off the outside box-shadow
            },
            complete: function (anim) {
                if (!anim.began) {
                    $searchModal.removeClass("my-d-none").addClass("my-d-flex");
                    $navItemsToBlur.css("filter", "blur(4px)");
                    $navItemsToBlur.css("clip-path", `polygon(0 0, 100% 0, 100% 100%, 0 100%)`);
                }
                document.getSelection().removeAllRanges();
            }
        }) : anime({
            targets: $searchModal[0],
            opacity: [1, 0],
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            begin: function () {
                $searchModal.removeClass("my-d-none").addClass("my-d-flex");
                $navItemsToBlur.removeCss("filter");
                $navItemsToBlur.removeCss("clip-path");
            },
            complete: function (anim) {
                if (!anim.began) {
                    $searchModal.removeClass("my-d-none").addClass("my-d-flex");
                    $navItemsToBlur.removeCss("filter");
                    $navItemsToBlur.removeCss("clip-path");
                }
                $searchModal.removeClass("my-d-flex").addClass("my-d-none");
                document.getSelection().removeAllRanges();
            }
        }));

        NavBarUtils.animsNavBar.push(showSearchModal ? anime({
            targets: $searchModalInputGroup[0],
            clipPath: windowWidth < 768
                ? NavBarUtils.getSlideClipPath(true, "left", openedInputWidth, openedInputHeight, true)
                : NavBarUtils.getSlideClipPath(true, "right", openedInputWidth, openedInputHeight, true),
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false,
            complete: function () {
                $searchModalInputGroup.removeCss("clip-path");
            }
        }) : anime({
            targets: $searchModalInputGroup[0],
            clipPath: windowWidth < 768
                ? NavBarUtils.getSlideClipPath(false, "left", openedInputWidth, openedInputHeight, true)
                : NavBarUtils.getSlideClipPath(false, "right", openedInputWidth, openedInputHeight, true),
            duration: 500,
            easing: "easeOutCubic",
            autoplay: false
        }));

        NavBarUtils.animsNavBar.push(anime.timeline({
            duration: 500
        }).add({
            targets: $iconToHide[0],
            opacity: [1, 0],
            duration: 250,
            easing: "easeInOutSine",
            autoplay: false,
            complete: function () {
                console.log("[\"mousedown\", \".my-nav-item > .my-nav-link\"] [$iconToHide[0]] opacity: [1, 0] complete");
                $iconToHide.removeCss("fill"); // since the icons are swapped the hidden one would retain hovered style and it would be kep[t even on swap back so the icon would be white and not gray as set in css
                $iconToShow.removeCss("fill");
                $iconToHide.addClass("my-d-none").removeClass("my-d-flex").insertAfter($searchNavLink);
            }
        }).add({
            targets: $iconToShow[0],
            opacity: [0, 1],
            duration: 250,
            easing: "easeInOutSine",
            autoplay: false,
            begin: function () {
                console.log("[\"mousedown\", \".my-nav-item > .my-nav-link\"] [$iconToShow[0]] opacity: [0, 1] begin");
                $iconToHide.removeCss("fill");
                $iconToShow.addClass("my-d-flex").removeClass("my-d-none").prependTo($searchNavLink);
                $iconToShow.removeCss("fill");
            },
            complete: function (anim) {
                console.log("[\"mousedown\", \".my-nav-item > .my-nav-link\"] [$iconToShow[0]] opacity: [0, 1] complete");
                $iconToHide.removeCss("fill");
                if (!anim.began) {
                    console.log("[$iconToShow[0]] ...but !anim.began");
                    $iconToShow.addClass("my-d-flex").removeClass("my-d-none").prependTo($searchNavLink);
                    $iconToShow.removeCss("fill");
                }
            }
        }));

        if (windowWidth < 768) {
            NavBarUtils.animsNavBar.push(showSearchModal ? anime({
                targets: $searchNavItem[0],
                translateX: [0, $searchNavItem.outerWidth().round().px()],
                duration: 500,
                easing: "easeOutCubic",
                autoplay: false
            }) : anime({
                targets: $searchNavItem[0],
                translateX: [$searchNavItem.outerWidth().round().px(), 0],
                duration: 500,
                easing: "easeOutCubic",
                autoplay: false,
                complete: function () {
                    $searchNavItem.removeCss("translateX");
                }
            }));

            NavBarUtils.animsNavBar.push(showSearchModal ? anime({
                targets: $searchModal[0],
                translateX: [$searchNavItem.outerWidth().round().px(), 0],
                duration: 500,
                easing: "easeOutCubic",
                autoplay: false
            }) : anime({
                targets: $searchModal[0],
                translateX: [0, $searchNavItem.outerWidth().round().px()],
                duration: 500,
                easing: "easeOutCubic",
                autoplay: false
            }));
        }
    }

    // private like methods
    static setNavMenuToDeviceSize($nm) {
        const $nmNavItem = $nm.closest(".my-nav-item");
        const $nmNavLink = $nmNavItem.children(".my-nav-link").first();
        const $nmCloseIcon = $nmNavLink.find(".my-nav-link-close-icon");
        const $nmOpenIcon = $nmNavLink.find(".my-nav-link-open-icon");
        const $nmCloseIconXs = $nmNavLink.find(".my-nav-link-close-icon-xs");
        const $nmOpenIconXs = $nmNavLink.find(".my-nav-link-open-icon-xs");
        const $topMostNavItem = $nm.closest(".my-navbar > .my-nav-item");
        const windowWidth = $(window).width();

        $nm.removeClass("shown").removeClass("my-d-block").addClass("my-d-none");  // hide all menus
        $nm.parents(".my-nav-item").first().removeCss("height");
        $nm.removeCss("clip-path");
        $nm.removeCss("min-width"); // it will break the anim calculations and the positioning because the menu will have the wrong size on changing from xs to higher if not reset

        $nm.removeAttr("anim-height"); // clear anim-height/width
        $nm.removeAttr("anim-width");
        $nm.removeAttr("init-offset-top");

        $topMostNavItem.css("height", "");

        if (windowWidth < 768) {
            $nmCloseIcon.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmOpenIcon.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmCloseIconXs.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmOpenIconXs.css("opacity", 1).removeClass("my-d-none").addClass("my-d-flex");

            $nm.css("max-width", "100%"); // max-width to 100% for xs
        } else {
            $nmCloseIconXs.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmOpenIconXs.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmCloseIcon.css("opacity", 0).removeClass("my-d-flex").addClass("my-d-none");
            $nmOpenIcon.css("opacity", 1).removeClass("my-d-none").addClass("my-d-flex");
            $nm.css("max-width", ""); // disable max width (it is restored during animation setup in positionMenus())
        }
    }

    static setNavLinkContentsLeftMargin($nm) {
        const $nmChildNavLinks = $nm.children(".my-nav-item").children(".my-nav-link");
        const menuContainsAtLeastOneIcon = $nmChildNavLinks.children(".my-nav-link-icon").length > 0;

        // add left margin to nav-link content to account for icons length and padding in other nav-links
        if (menuContainsAtLeastOneIcon) {
            const $arrNavLinkContentsWoIcons = $nmChildNavLinks.$toArray().filter($cnv => $cnv.children(".my-nav-link-icon").length === 0).map($cnv => $cnv.children(".my-nav-link-content").first());
            const $icon = $nmChildNavLinks.$toArray().filter($cnv => $cnv.children(".my-nav-link-icon").length === 1).map($cnv => $cnv.children(".my-nav-link-icon").first()).first();
            const iconWidth = parseFloat($icon.outerWidth());
            $arrNavLinkContentsWoIcons.arrayTo$().css("margin-left", iconWidth + "px");
        }
    }

    static setNavLinkContentsLeftMarginForTopLevel($nb) {
        const $nbTopNavItems = $nb.children(".my-nav-item");
        const $nbTopNavItemsWoutils = $nbTopNavItems.not(".my-toggler, .my-search, .my-login, .my-home");
        const $nbTopNavLinksWoUtils = $nbTopNavItemsWoutils.children(".my-nav-link");
        const navBarContainsAtLeastOneIcon = $nbTopNavLinksWoUtils.children(".my-nav-link-icon").length > 0;

        // Add left margin on sm device to navbar (top level) nav-item-contents without icons if at least one other nav-item have an icon
        if (navBarContainsAtLeastOneIcon) {
            const $arrNavLinkContentsWoIcons = $nbTopNavLinksWoUtils.$toArray()
                .filter($cnv => $cnv.children(".my-nav-link-icon").length === 0)
                .map($cnv => $cnv.children(".my-nav-link-content").first());
            const $icon = $nbTopNavLinksWoUtils.$toArray()
                .filter($cnv => $cnv.children(".my-nav-link-icon").length === 1)
                .map($cnv => $cnv.children(".my-nav-link-icon").first()).first();
            const iconWidth = parseFloat($icon.outerWidth());
            $arrNavLinkContentsWoIcons.arrayTo$().css("margin-left", $(window).width() < 768 ? iconWidth + "px" : "");
        }
    }

    static removeIconsPaddingForNavLinksWithEmptyContent($nb) {
        // Remove right padding for nav-link-icons without nav-link-content
        const $icons = $nb.find(".my-nav-link-icon").$toArray();
        for (let $icon of $icons) {
            const $navLinkContent = $icon.nextAll(".my-nav-link-content").first();
            if ($navLinkContent.text().trimMultiline()) {
                $icon.addClass("my-pr-10px");
            } else {
                $icon.removeClass("my-pr-10px");
            }
        }
    }

    static setNavBrandToDeviceSize($nb) {
        // Set Brand visibility depending on device size
        const $brand = $nb.find(".my-nav-brand").first();
        const $brandNavItem = $brand.parents(".my-nav-item.my-brand").first();
        const $brandMain = $brand.children(".my-nav-brand-main-image").first();
        const $brandAlt = $brand.children(".my-nav-brand-alt-image").first();
        const windowWidth = $(window).width();

        if (windowWidth < 1200) {
            $brandMain.removeClass("my-d-block").addClass("my-d-none");
            $brandAlt.removeClass("my-d-none").addClass("my-d-block");
        } else {
            $brandAlt.removeClass("my-d-block").addClass("my-d-none");
            $brandMain.removeClass("my-d-none").addClass("my-d-block");
        }

        // Position Nav Brand vertically
        const navItemHeight = $nb.children(".my-nav-item").$toArray().first($ni => $ni.outerHeight() > 0).outerHeight();
        if (windowWidth < 768) {
            const bgWidth = parseFloat($brandAlt.attr("original-width"));
            const bgHeight = parseFloat($brandAlt.attr("original-height"));
            const brandExpectedWidth = navItemHeight * bgWidth / bgHeight;
            const brandExpectedHeight = navItemHeight;

            $brandAlt.css("width", brandExpectedWidth.round() + "px");
            $brandAlt.css("height", brandExpectedHeight.round() + "px");
            $brand.css("width", brandExpectedWidth.round() + "px");
            $brand.css("height", brandExpectedHeight.round() + "px");
            $brandNavItem.css("width", brandExpectedWidth.round() + "px");
            $brandNavItem.css("height", brandExpectedHeight.round() + "px");

            $brandMain.removeCss("top");
            $brandAlt.removeCss("top");

            $brandNavItem.next().removeCss("margin-left");
        } else {
            const $brandImageVisible = $brandMain.visible() ? $brandMain : $brandAlt.visible() ? $brandAlt : null;

            const bgWidth = parseFloat($brandImageVisible.attr("original-width"));
            const bgHeight = parseFloat($brandImageVisible.attr("original-height"));
            const brandExpectedWidth = parseFloat($brandImageVisible.attr("expected-width")) || navItemHeight;
            const brandExpectedHeight = brandExpectedWidth * bgHeight / bgWidth;

            $brandImageVisible.css("width", brandExpectedWidth.round() + "px");
            $brandImageVisible.css("height", brandExpectedHeight.round() + "px");
            $brand.css("width", brandExpectedWidth.round() + "px");
            $brand.css("height", brandExpectedHeight.round() + "px");
            $brandNavItem.css("width", brandExpectedWidth.round() + "px");
            $brandNavItem.css("height", brandExpectedHeight.round() + "px");

            $brandNavItem.next().css("margin-left", brandExpectedWidth.round() + "px");
        }
    }

    static getNavBarHeight($nb) {
        const windowWidth = $(window).width();

        let nbHeight = $nb.find(".my-nav-item.my-toggler").first().outerHeight(); // get toggler height in order to always get init navbar height and not opened height

        const nbWidth = $nb.outerWidth();
        const $nbAllNavItems = $nb.children(".my-nav-item").not(".my-toggler");
        const $nbUtilNavItems = $nbAllNavItems.filter(".my-brand, .my-search, .my-home, .my-login");
        const $arrNbUtilNavItems = $nbUtilNavItems.$toArray();
        const $nbConcreteNavItems = $nbAllNavItems.not($nbUtilNavItems);
        const $arrNbConcreteNavItems = $nbConcreteNavItems.$toArray();
        const $navItemsTotalWidth = $nbAllNavItems.$toArray().sum($ni => $ni.outerWidth());

        if (windowWidth >= 768 && $navItemsTotalWidth.round() > nbWidth.round()) { // edge case, resizing window with navItems wrapping (happens before calling rearrangeNavItems() so the height is wrong but can't reorganize method order because they rely on each other)
            const niUtilsTotalWidth = $arrNbUtilNavItems.sum($ni => $ni.outerWidth());
            const niConcreteTotalWidth = $arrNbConcreteNavItems.sum($ni => $ni.outerWidth());
            const freeSpaceWidth = nbWidth - niUtilsTotalWidth;
            const rows = Math.ceil(niConcreteTotalWidth / freeSpaceWidth);

            nbHeight = nbHeight * rows;
        }

        return nbHeight;
    }

    static setNavBarToDeviceSize($nb) {
        // Hide NavBar For small devices and set opened and closed heights
        const windowWidth = $(window).width();
        const $toggler = $nb.find(".my-nav-item.my-toggler").first();
        const niHeight = $toggler.outerHeight();
        const concreteNavitemsNo = $toggler.siblings(".my-nav-item").not(".my-toggler, .my-home, .my-brand, .my-search, .my-login")
            .$toArray().sum($ni => $ni.outerHeight());
        if (windowWidth < 768) {
            $nb.removeClass("shown");
            if (!NavBarUtils.NavBarInitCss["opened_height"]) {
                NavBarUtils.NavBarInitCss["opened_height"] = niHeight + concreteNavitemsNo + "px";
            }
            if (!NavBarUtils.NavBarInitCss["closed_height"]) {
                NavBarUtils.NavBarInitCss["closed_height"] = $toggler.outerHeight() + "px";
            }
            $nb.css("height", NavBarUtils.NavBarInitCss["closed_height"]);
        } else {
            $nb.addClass("shown");
            $nb.removeCss("height");
        }

        // Set prompt and navbar Styles if navBar is sticky, same as on scroll change
        if (!$(".my-modal").is(".shown")) {
            this.setStickyNavBarStyles($nb);
        }
        
        $nb.find(".my-nav-item").children(".my-nav-link").removeCss("width"); // it is set in close/open icon animations and is not removed because it would break width of nav-items on menu close, if it wasnt set at all the nav-items would flicker on menu open close because of the icons being removed readded

        // Set Scrollbar dependent size
        const IsDeviceSizeChanging = !NavBarUtils.PreviousWindowWidth
            || windowWidth < 768 && NavBarUtils.PreviousWindowWidth >= 768
            || windowWidth >= 768 && NavBarUtils.PreviousWindowWidth < 768;
      
        if (IsDeviceSizeChanging && NavBarUtils.ScrollBar) {
            NavBarUtils.handleScrollBarChange();
        }

        NavBarUtils.PreviousWindowWidth = windowWidth;
    }

    static setStickyNavBarStyles($nb) {
        const $navItemBrand = $nb.children(".my-nav-item.my-brand").first();
        const $titlebar = $(".my-titlebar").first();
        const brandAdditionalMargin = parseFloat($navItemBrand.css("top")) < 0 ? -parseFloat($navItemBrand.css("top")) : 0 || 0;
        const titlebarHeight = $titlebar.outerHeight() || 0;
        const $promptMain = $("#promptMain");
        const nbHeight = this.getNavBarHeight($nb);

        const isAnyModalShown = $(".my-modal").is(".shown");
        if (!isAnyModalShown) {
            $promptMain.appendTo($(".my-navbar").first().parent());
            $promptMain.css("z-index", "9");
        }

        if ($nb.is(".glued")) {
            if (window.innerWidth >= 768) {
                $nb.css("width", $nb.parent().innerWidth() - 20 + "px");
            } else {
                $nb.removeCss("width");
            }

            if (!isAnyModalShown) {
                $promptMain.css("top", (brandAdditionalMargin + titlebarHeight + nbHeight).px());
            }
        } else {
            if (!isAnyModalShown) {
                $promptMain.css("top", nbHeight.px());
            }
        }
    }

    static setRightBorderForLastLeftAlignedNavItem($nb) {
        // Set right border for last left aligned nav-item
        const $nbTopNavItems = $nb.children(".my-nav-item");
        const $lastLeftAlignedNavItem = $nbTopNavItems.filter(".my-nav-item.my-ml-auto").prev(".my-nav-item");
        $lastLeftAlignedNavItem.addClass("my-last-left"); // device Width for the class is handled within css
    }

    static setSearchModalToDeviceSize($nb) {
        // Set initial values for search modal
        const windowWidth = $(window).width();
        const $searchModal = $nb.find(".my-nav-search-container").first();
        $searchModal.addClass("my-d-flex"); // temporarily
        const $searchNavItem = $nb.children(".my-nav-item.my-search").first();
        const $searchNavLink = $searchNavItem.children(".my-nav-link").first();
        const height = $nb.outerHeight();
        const $navItemsToBlur = windowWidth < 768 ? $searchNavItem.siblings(".my-toggler, .my-home, .my-brand, .my-login") : $searchNavItem.nextAll(".my-nav-item");
        const width = windowWidth < 768
            ? $nb.outerWidth() - $searchNavItem.outerWidth()
            : $nb.outerWidth() - $searchNavItem.prevAll(".my-nav-item").not(".my-toggler").addBack().$toArray().sum($ni => $ni.outerWidth(true));
        const $navSearchInput = $searchModal.find(".my-text-input").parents(".my-input-group").first();

        $searchModal.css("left", windowWidth < 768 ? (-$nb.outerWidth() + $searchNavItem.outerWidth()).round().px() : $searchNavItem.outerWidth().round().px());
        $searchModal.css("width", width.round().px());
        $searchModal.css("height", height.round().px());
        $searchModal.css("top", (-((height - $searchNavItem.outerHeight()) / 2)).round().px());

        const inputHeight = $navSearchInput.outerHeight(); // not earlier because $navSearchContainer will be outside of screen for sm or will have wrong width / height 
        const inputWidth = $navSearchInput.outerWidth();

        $navItemsToBlur.removeCss("filter");
        $navItemsToBlur.removeCss("clip-path");
        $searchNavItem.removeCss("transform");
        $searchModal.removeCss("transform");

        NavBarUtils.SearchModalInitCss["opened_width"] = width + "px";
        NavBarUtils.SearchModalInitCss["opened_height"] = height + "px";
        NavBarUtils.SearchModalInitCss["opened_input_width"] = inputWidth + "px";
        NavBarUtils.SearchModalInitCss["opened_input_height"] = inputHeight + "px";
        $searchModal.removeClass("shown my-d-flex").addClass("my-d-none");

        // Reset Search Nav Link Width and height
        $searchNavLink.css("width", $searchNavLink.outerWidth().round().px()); // prevents flickering in the split second when the icon is swapped for the other
        $searchNavLink.css("height", $searchNavLink.outerHeight().round().px());

        // Swap icons to the original order
        const $searchIconVisible = $searchNavLink.children(".my-icon").first();
        const $searchIconHidden = $searchNavLink.next().filter(".my-icon").first();

        if ($searchIconVisible.is(".my-close")) { // if icons are swapped (modal was opened on resize)
            $searchIconVisible.addClass("my-d-none").removeClass("my-d-flex").removeCss("opacity").insertAfter($searchNavLink);
            $searchIconHidden.addClass("my-d-flex").removeClass("my-d-none").removeCss("opacity").prependTo($searchNavLink);
        }
    }

    static rearrangeNavBarIfWrapping($nb) {
        const $nbAllNavItems = $nb.children(".my-nav-item").not(".my-toggler");
        const $nbUtilNavItems = $nbAllNavItems.filter(".my-brand, .my-search, .my-home, .my-login");
        const $arrNbUtilNavItems = $nbUtilNavItems.$toArray();
        const $nbConcreteNavItems = $nbAllNavItems.not($nbUtilNavItems);
        const $arrNbConcreteNavItems = $nbConcreteNavItems.$toArray();
        const windowWidth = $(window).width();

        //$nb.removeCss("height"); // reset first
        for (let $niUtil of $arrNbUtilNavItems) {
            $niUtil.removeCss("position");
            $niUtil.removeCss("left");
            $niUtil.removeCss("right");
            $niUtil.removeCss("top");
        }

        for (let $niConcrete of $arrNbConcreteNavItems) {
            $niConcrete.removeCss("position");
            $niConcrete.removeCss("left");
            $niConcrete.removeCss("top");
            $niConcrete.removeCss("width");
            $niConcrete.removeCss("height");
        }

        const nbWidth = $nb.outerWidth();
        const $navItemsTotalWidth = $nbAllNavItems.$toArray().sum($ni => $ni.outerWidth());
        const niHeight = $nbConcreteNavItems.first().outerHeight();
        let nbExpectedHeight = niHeight;

        if (windowWidth >= 768 && $navItemsTotalWidth.round() > nbWidth.round()) {
            const niUtilsTotalWidth = $arrNbUtilNavItems.sum($ni => $ni.outerWidth());
            const niConcreteTotalWidth = $arrNbConcreteNavItems.sum($ni => $ni.outerWidth());
            const freeSpaceWidth = nbWidth - niUtilsTotalWidth;
            const rows = Math.ceil(niConcreteTotalWidth / freeSpaceWidth);
            nbExpectedHeight = rows * niHeight;
            const itemsNo = $arrNbConcreteNavItems.length;
            const cols = Math.ceil(itemsNo / rows);
            const newItemWidth = freeSpaceWidth / cols;
            const baseLeft = $nbConcreteNavItems.first().prevAll().filter($nbAllNavItems).$toArray()
                .sum($ni => $ni.outerWidth());

            $nb.css("height", nbExpectedHeight.round().px());

            for (let $niUtil of $arrNbUtilNavItems) {
                const topForNiUtilMiddleAlignement = -((niHeight - nbExpectedHeight) / 2);

                $niUtil.removeCss("margin-left");
                $niUtil.css("position", "absolute");
                if ($niUtil.is(".my-ml-auto")) { // right aligned
                    const nextNiUtilWidth = $niUtil.nextAll().filter($nbAllNavItems).$toArray().sum($ni => $ni.outerWidth());
                    $niUtil.css("right", nextNiUtilWidth.round().px());
                } else {
                    const prevNiUtilWidth = $niUtil.prevAll().filter($nbAllNavItems).$toArray().sum($ni => $ni.outerWidth());
                    $niUtil.css("left", prevNiUtilWidth.round().px());
                }

                $niUtil.css("top", topForNiUtilMiddleAlignement.round(1).px());
            }

            let i = 0;
            for (let row = 0; row < rows; row++) {
                for (let col = 0; col < cols; col++) {
                    if (i >= itemsNo) continue;
                    let $currItem = $arrNbConcreteNavItems[i++];
                    $currItem.css("position", "absolute");
                    $currItem.css("left", (baseLeft + col * newItemWidth).round().px());
                    $currItem.css("top", (row * niHeight).round().px());
                    $currItem.css("width", newItemWidth.round().px());
                    $currItem.css("height", niHeight.round().px());
                    $currItem.children(".my-nav-link").first().css("width", "100%");
                }
                if (i >= itemsNo) continue;
            }
        } else { // in all other cases brand has to be positioned vertically anyway
            const $navItemBrand = $nbUtilNavItems.filter(".my-brand").first();
            $navItemBrand.css("top", (-(($navItemBrand.outerHeight() - niHeight) / 2)).round(1).px());
        }

        $(".my-page-container").css("margin-top", nbExpectedHeight.px());
    }

    static handleScrollBarChange(forceReapply = false) {
        if (!NavBarUtils.ScrollBar) {
            NavBarUtils.ScrollBar = $("*").overlayScrollbars().filter(s => s !== undefined).first();
        }
        const scrollInfo = NavBarUtils.ScrollBar.scroll();
        const scrollY = scrollInfo.position.y;
        const $stickyNavBars = $(".my-navbar.my-sticky").$toArray();
        const $banner = $(".my-page-main-image").first();
        const bannerHeight = $banner.outerHeight();
        const $promptMain = $("#promptMain");
        const $modals = $(".my-modal");
        const isAnyModalShown = $modals.is(".shown");
        const windowWidth = $(window).width();

        for (let $nb of $stickyNavBars) {
            const $navContainer = $nb.parents(".my-nav-container").first();
            const $pageContainer = $navContainer.next(".my-page-container");
            const $brand = $nb.find(".my-nav-brand").first();
            const $brandMain = $brand.children(".my-nav-brand-main-image").first();
            const $brandAlt = $brand.children(".my-nav-brand-alt-image").first();
            const navItemHeight = $nb.children(".my-nav-item").$toArray().first($ni => $ni.outerHeight() > 0).outerHeight();
            const $titlebar = $(".my-titlebar").first();
            const brandExpectedHeight = windowWidth < 768 ? navItemHeight : (parseFloat((windowWidth < 1200 ? $brandAlt : $brandMain).attr("expected-width")) || navItemHeight) * parseFloat((windowWidth < 1200 ? $brandAlt : $brandMain).attr("original-height")) / parseFloat((windowWidth < 1200 ? $brandAlt : $brandMain).attr("original-width"));
            const brandAdditionalMargin = (((brandExpectedHeight - navItemHeight) / 2)).round(1) || 0;
            //const brandAdditionalMargin = parseFloat($navItemBrand.css("top")) < 0 ? -parseFloat($navItemBrand.css("top")) : 0 || 0; // can't be this because it is set in rearrange method which is not executed yet at this point

            const titlebarHeight = $titlebar.outerHeight() || 0;
            const nbHeight = window.innerWidth < 768 
                ? $nb.find(".my-nav-item.my-toggler").first().outerHeight()
                : $nb.outerHeight(); // get toggler height in order to always get init navbar height and not opened height, taking navbar height is safe here unlike on resize where rearrange hasn't been called yet

            if (!$navContainer.next().is($pageContainer)) {
                $navContainer.insertBefore($pageContainer);
            }

            if (($nb.is(".glued") || forceReapply) && scrollY <= bannerHeight - brandAdditionalMargin && window.innerWidth >= 768) {
                $nb.removeCss("margin-top");
                $nb.removeClass("glued");
                $promptMain.removeClass("glued");
                if (window.innerWidth >= 768) {
                     $nb.removeCss("width");
                }

                if (isAnyModalShown) {
                    $promptMain.prependTo("body");
                    $promptMain.css("z-index", "101");
                    $promptMain.css("top", "0");
                } else {
                    $promptMain.appendTo($(".my-navbar").first().parent());
                    $promptMain.css("z-index", "9");
                    $promptMain.css("top", nbHeight.px());
                }
            } else if ((!$nb.is(".glued") || forceReapply) && scrollY > bannerHeight - brandAdditionalMargin || window.innerWidth < 768) {
                $nb.addClass("glued");
                $promptMain.addClass("glued");
                if (brandAdditionalMargin > 0 || titlebarHeight > 0) {
                    $nb.css("margin-top", (brandAdditionalMargin + titlebarHeight).px());
                } else {
                    $nb.removeCss("margin-top");
                }

                if (window.innerWidth >= 768) {
                    $nb.css("width", $nb.parent().innerWidth() - 20 + "px");
                }

                if (isAnyModalShown) {
                    $promptMain.prependTo("body");
                    $promptMain.css("z-index", "101");
                    $promptMain.css("top", "0");
                } else {
                    $promptMain.appendTo($(".my-navbar").first().parent());
                    $promptMain.css("z-index", "9");
                    $promptMain.css("top", (brandAdditionalMargin + titlebarHeight + nbHeight).px());
                }
            }

        }
    }

    static adjustNavbarMarginTop($nb) {
        //const $banner = $(".my-page-main-image").first();
        //const bannerWidth = $banner.outerWidth(); // styles with css instead
        //const pageContainerWidth = $nb.parents(".my-nav-container").first().next(".my-page-container").outerWidth();
        //const nbLeftMargin = parseFloat($nb.css("margin-left"));
        //const nbRightMargin = parseFloat($nb.css("margin-right"));
        //$nb.css("width", (bannerWidth || pageContainerWidth - nbLeftMargin - nbRightMargin).px());

        const $navItemBrand = $nb.children(".my-nav-item.my-brand").first();
        const brandAdditionalMargin = parseFloat($navItemBrand.css("top")) < 0 ? -parseFloat($navItemBrand.css("top")) : 0 || 0;
        const $titlebar = $(".my-titlebar").first();
        const titlebarHeight = $titlebar.outerHeight() || 0;
        if ($nb.is(".my-sticky.glued")) {
            $nb.css("margin-top", (brandAdditionalMargin + titlebarHeight).px());
        } else {
            $nb.removeCss("margin-top");
        }
    }
}
