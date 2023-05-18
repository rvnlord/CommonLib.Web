import { IEnumerable } from "linq-to-typescript";

class MyEnumerable<T> {
    private _en: IEnumerable<T>;

    constructor(en: IEnumerable<T>) {
        this._en = en;
        return new Proxy(this, {
            get(target, prop, receiver) {
                if (typeof prop === "string" && typeof target._en[prop as keyof IEnumerable<T>] === "function") {
                    return (target._en[prop as keyof IEnumerable<T>] as any).bind(target._en);
                }
                return Reflect.get(target, prop, receiver);
            },
        }) as MyEnumerable<T> & IEnumerable<T>;
    }

    containsAllStrings(substrings: IEnumerable<string>): boolean {
        const arr = this._en.toArray() as string[] || null;
        if (substrings === null || !substrings.any())
            throw new Error("substrings value was empty");
        if (arr === null || !arr.length)
            return false;

        const arr1 = arr.distinct().toArray();
        const arr2 = substrings.distinct().toArray();
        return arr1.intersect(arr2).count() === arr2.length;
    }
}

interface MyEnumerable<T> extends IEnumerable<T> {
    //containsAllStrings<T>(substrings: IEnumerable<string>): boolean;
}

export { MyEnumerable };