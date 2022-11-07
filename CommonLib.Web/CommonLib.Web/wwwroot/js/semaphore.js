export default class Semaphore {
    _counter = 0;
    _waiting = [];
    _max;

    constructor(max) {
        this._max = max || 1;
    }

    take() {
        if (this._waiting.length > 0 && this._counter < this._max) {
            this._counter++;
            const promise = this._waiting.shift();
            promise.resolve();
        }
    }

    waitAsync() {
        if (this._counter < this._max) {
            this._counter++;
            return new Promise(resolve => resolve());
        } else {
            return new Promise((resolve, err) => {
                this._waiting.push({ resolve: resolve, err: err });
            });
        }
    }

    releaseAsync() {
        this._counter--;
        this.take();
    }

    purge = function() {
        const unresolved = this._waiting.length;

        for (let i = 0; i < unresolved; i++) {
            this._waiting[i].err("Task has been purged");
        }

        this._counter = 0;
        this._waiting = [];

        return unresolved;
    };
}