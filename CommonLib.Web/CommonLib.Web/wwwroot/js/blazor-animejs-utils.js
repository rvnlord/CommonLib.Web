/// <reference path="../libs/libman/jquery/jquery.js" />
/// <reference path="../libs/custom/@types/animejs/index.d.ts" />
import "./extensions.js";
//import utils from "./utils.js";

window.BlazorAnimeJsUtils = {
    AnimationStore: [],
    Began: (guid) => {
        const anim = window.BlazorAnimeJsUtils.AnimationStore[guid].anim;
        return anim.began;
    },
    Seek: (guid, duration) => {
        const anim = window.BlazorAnimeJsUtils.AnimationStore[guid].anim;
        anim.seek(duration);
    },
    Play: (guid) => {
        const anim = window.BlazorAnimeJsUtils.AnimationStore[guid].anim;
        anim.play();
    },
    CreateAnimation: (jsonAnimationOrTimeline, dotNetRef) => {
        const jAnimOrTimeLine = jsonAnimationOrTimeline.jsonDeserialize();

        if (jAnimOrTimeLine.type === "animation") {
            const anim = anime({
                duration: jAnimOrTimeLine.duration,
                targets: $(jAnimOrTimeLine.targetsSelector).toArray(),
                easing: jAnimOrTimeLine.easing,
                autoplay: jAnimOrTimeLine.autoplay,
                begin: () => jAnimOrTimeLine.beginMethodName ? dotNetRef.invokeMethodAsync(jAnimOrTimeLine.beginMethodName, jAnimOrTimeLine.guid) : null,
                complete: () => jAnimOrTimeLine.completeMethodName ? dotNetRef.invokeMethodAsync(jAnimOrTimeLine.completeMethodName, jAnimOrTimeLine.guid) : null,
                opacity: jAnimOrTimeLine.opacity,
                clipPath: jAnimOrTimeLine.clipPath,
                height: jAnimOrTimeLine.height
            });

            window.BlazorAnimeJsUtils.AnimationStore[jAnimOrTimeLine.guid] = {
                dotNetRef: dotNetRef,
                anim: anim
            };
        } else if (jAnimOrTimeLine.type === "timeline") {
            const timeline = anime.timeline({
                duration: jAnimOrTimeLine.duration
            });

            for (let jtlAnim of jAnimOrTimeLine.animations) {
                timeline.add({
                    duration: jtlAnim.duration,
                    targets: $(jtlAnim.targetsSelector).toArray(),
                    easing: jtlAnim.easing,
                    autoplay: jtlAnim.autoplay,
                    begin: () => jtlAnim.beginMethodName ? dotNetRef.invokeMethodAsync(jtlAnim.beginMethodName, jtlAnim.guid) : null,
                    complete: () => jtlAnim.completeMethodName ? dotNetRef.invokeMethodAsync(jtlAnim.completeMethodName, jtlAnim.guid) : null,
                    opacity: jtlAnim.opacity,
                    clipPath: jtlAnim.clipPath,
                    height: jtlAnim.height
                });

                window.BlazorAnimeJsUtils.AnimationStore[jtlAnim.guid] = {
                    dotNetRef: dotNetRef,
                    anim: timeline.children.last()
                };
            }

            window.BlazorAnimeJsUtils.AnimationStore[jAnimOrTimeLine.guid] = {
                dotNetRef: dotNetRef,
                anim: timeline
            };
        }

    }
};