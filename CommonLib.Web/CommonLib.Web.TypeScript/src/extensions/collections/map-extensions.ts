import KeyValuePair from "../../utils/util-classes/key-value-pair";

declare global {
    interface Map<K, V> {
        elementAt_(index: number): KeyValuePair<K, V>;
        indexOfKey(key: K): number;
        toArray(): [K, V][];
        toKVPs(): KeyValuePair<K, V>[];
        toKeysArray(): K[];
        toValuesArray(): V[];
        getWithEqualsOrNull(key: K): V | null; // do not override `get` directly, it is used internally
    }
}

// eslint-disable-next-line
Map.prototype.elementAt_ = function <K, V>(index: number): KeyValuePair<K, V> {
    const entries = Array.from(this.entries()) as [K, V][];
    return entries[index].toKVP();
};

// eslint-disable-next-line
Map.prototype.indexOfKey = function <K, V>(key: K): number {
    const keys = Array.from(this.keys()) as K[];
    return keys.indexOf(key);
};

// eslint-disable-next-line
Map.prototype.toArray = function <K, V>(): [K, V][] {
    return Array.from(this.entries()) as [K, V][];
};

// eslint-disable-next-line
Map.prototype.toKVPs = function <K, V>(): KeyValuePair<K, V>[] {
    return (this as Map<K, V>).toArray().select(kvp => new KeyValuePair<K, V>(kvp[0], kvp[1])).toArray();
};

// eslint-disable-next-line
Map.prototype.toKeysArray = function <K, V>(): K[] {
    return (this as Map<K, V>).toKVPs().select(kvp => kvp.key).toArray();
};

// eslint-disable-next-line
Map.prototype.toValuesArray = function <K, V>(): V[] {
    return (this as Map<K, V>).toKVPs().select(kvp => kvp.value).toArray();
};

// eslint-disable-next-line
Map.prototype.getWithEqualsOrNull = function <K, V>(key: K): V | null {
    const kvps = (this as Map<K, V>).toKVPs();
    for (const { key: mapKey, value: mapValue } of kvps) {
        if ((key as any).equals && typeof (key as any).equals === "function") {
            if ((key as any).equals(mapKey)) {
                return mapValue;
            }
        } else if (key === mapKey) {
            return mapValue;
        }
    }
    return null;
};

export { }