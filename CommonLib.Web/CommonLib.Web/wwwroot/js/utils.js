export default class utils {
    static guid = () => {
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0;
            const v = c === "x" ? r : r & 0x3 | 0x8;
            return v.toString(16);
        });
    }

    static startsWithAny = (str, substrs) => {
        for (let substr of substrs) {
            if (str.startsWith(substr)) {
                return true;
            }
        }
        return false;
    }

    static endsWithAny = (str, substrs) => {
        for (let substr of substrs) {
            if (str.endsWith(substr)) {
                return true;
            }
        }
        return false;
    }

    static iif = (condition, functions) => {
        for (let func of functions) {
            if (condition()) {
                func();
            } else {
                return;
            }
        }
    }

    static wait = (ms) => {
        const start = Date.now();
        let now = start;
        while (now - start < ms)
            now = Date.now();
    }

    static waitAsync = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    static waitUntilAsync = (condition, ms) => new Promise(async resolve => {
        ms = ms || 25;
        while (!condition())
            await this.waitAsync(25);
        return resolve();
    });

    static origin = () => {
        return window.location.protocol + "//" + window.location.hostname + (window.location.port ? `:${window.location.port}` : "");
    }
    
    static getRandomInt = (min, max) => {
        min = Math.ceil(min);
        max = Math.floor(max);
        return Math.floor(Math.random() * (max - min) + min); //The maximum is exclusive and the minimum is inclusive
    }

}