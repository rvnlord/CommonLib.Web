/// <reference path="../../libs/libman/jquery/dist/jquery.js" />

import _ from "../../libs/libman/underscore/underscore-esm.js";
import utils from "../utils.js";
import "./utf8converter.js";

const _alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

Object.defineProperty(Array.prototype, "toBase58String", {
    value: function () {
        const arrBytes = this;
        let d = [];
        var s = "";
        var j = 0;
        for (let i = 0; i < arrBytes.length; i++)
        {
            j = 0;
            let c = arrBytes[i];
            if (c === 0 && (s.length ^ i) === 0) 
                s += '1';

            while (j < d.length || c !== 0)
            {
                let n = j >= d.length ? -1 : d[j];                  
                n = n > 0 ? n * 256 + c : c;     
                c = n / 58 | 0;             
                d[j] = n % 58;
                j++;
            }
        }

        while (j-- > 0)
            s += _alphabet[d[j]];

        return s;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "Base58ToByteArray", {
    value: function () {
        const base58Str = this;
        let d = [];  
        let b = [];
        let j = 0;
        for (let i = 0; i < base58Str.length; i++) 
        {
            j = 0;
            var c = _alphabet.indexOf(base58Str[i]);
            if (c < 0)
                throw new Error("invalid character");
            if (c === 0 && (b.length ^ i) === 0) 
                b.Add(0);

            while (j < d.length || c !== 0) 
            {
                var n = j >= d.length ? -1 : d[j];    
                n = n > 0 ? n * 58 + c : c;
                c = n >> 8;
                d[j] = n % 256;
                j++;
            }
        }

        while (j-- > 0)
            b.push(d[j]);

        return b;
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "utf8ToBase58", {
    value: function () {
        const utf8str = this;
        utf8str.utf8ToByteArray().toBase58String();
    },
    writable: true,
    configurable: true
});

Object.defineProperty(String.prototype, "Base58ToUTF8", {
    value: function () {
        const base58 = this;
        base58.base58ToByteArray().toUTF8String();
    },
    writable: true,
    configurable: true
});

