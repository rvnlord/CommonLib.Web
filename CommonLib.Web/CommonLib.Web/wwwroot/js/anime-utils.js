/// <reference path="../libs/libman/@types/jquery/index.d.ts" />
/// <reference path="../libs/libman/@types/animejs/index.d.ts" />

import utils from "./utils.js";
import "./extensions.js";

export default class AnimeUtils {
    static getRunningAnimations($targets) {
        let runAll = false;
        if (!$targets) {
            runAll = true;
            //throw new Error("No arguments specified");
        } else {
            if ($targets instanceof jQuery) {
                $targets = $targets.$toArray();
            }

            if (!Array.isArray($targets) || $targets.length <= 0) {
                throw new Error("Specified argument must be a JQuery object or an array of them");
            }

            if (!$targets.every(t => t instanceof jQuery)) {
                throw new Error("All args must mee JQuery objects");
            }
        }

        const runningAnims = runAll ? anime.running : anime.running.filter(a => a.animatables.map(tbl => tbl.target).containsAny($targets.map($t => $t[0])));
        return runningAnims;
    }

    static getAnimationsForTargetsFromPool($targets, allAnimations) {
        if (!$targets) {
            throw new Error("No '$targets' specified");
        } else {
            if ($targets instanceof jQuery) {
                $targets = $targets.$toArray();
            }

            if (!Array.isArray($targets) || $targets.length <= 0) {
                throw new Error("Specified argument must be a JQuery object or an array of them");
            }

            if (!$targets.every(t => t instanceof jQuery)) {
                throw new Error("All args must mee JQuery objects");
            }
        }

        allAnimations = allAnimations || [];
        if (!allAnimations.any())
            return [];

        const animations = allAnimations.filter(a => a.animatables.map(tbl => tbl.target).containsAny($targets.map($t => $t[0])));
        return animations;
    }

    static finishRunningAnimations($targets) {
        const runningAnims = this.getRunningAnimations($targets);
        this.finishAnimations(runningAnims);
    }

    static finishAnimations(animations) {
        for (let anim of animations) {
            if (anim.children > 0) {
                const timeline = anim.children;
                for (let cAnim of timeline) {
                    cAnim.seek(cAnim.duration);
                }
            } else {
                anim.seek(anim.duration);
            }
        }
        animations.clear();
    }

    static runAnimations(anims) {
        if (!anims || !Array.isArray(anims) || anims.length <= 0) {
            throw new Error("No arguments specified");
        }
        if (!anims.every(a => AnimeUtils.isAnimationOrTimeLine(a))) {
            throw new Error("All args must mee AnimeJs objects");
        }
        for (let anim of anims) {
            anim.play();
        }
    }

    static async runAnimationsAndWaitUntilAllStarted(anims) {
        this.runAnimations(anims);
        await utils.waitUntilAsync(() => anims.every(a => a.began || a.completed)); // test: const t = anims.map(a => `a.began: ${a.began} || a.completed: ${a.completed}`);
    }

    static async runAndAwaitAnimationsAsync(anims) {
        if (AnimeUtils.isAnimationOrTimeLine(anims)) {
            anims = [ anims ];
        }
        if (!anims || !Array.isArray(anims) || anims.length <= 0) {
            throw new Error("No arguments specified");
        }
        if (!anims.every(a => AnimeUtils.isAnimationOrTimeLine(a))) {
            throw new Error("All args must mee AnimeJs objects");
        }
        for (let anim of anims) {
            anim.play();
        }
        while (anims.some(a => !a.completed)) {
            await utils.waitAsync(100);
        }
        return anims;
    }

    static isAnimationOrTimeLine(anim) {
        return Object.keys(anim).sequenceEqual([
            "update", "begin", "loopBegin", "changeBegin", "change", "changeComplete", "loopComplete", "complete",
            "loop", "direction", "autoplay", "timelineOffset", "id", "children", "animatables", "animations",
            "duration", "delay", "endDelay", "finished", "reset", "set", "tick", "seek", "pause", "play", "reverse",
            "restart", "passThrough", "currentTime", "progress", "paused", "began", "loopBegan", "changeBegan",
            "completed", "changeCompleted", "reversePlayback", "reversed", "remaining"
        ]);
    }
}