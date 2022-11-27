//import _ from "../../libs/libman/underscore/underscore-esm.js";

export default class JQueryConverter {
   static $toArray($selectors) {
        if ($selectors.length === 0) {
            return [];
        } else if ($selectors.length === 1) {
            return [$selectors];
        }
        return $selectors.toArray().map(el => $(el));
    }
}

