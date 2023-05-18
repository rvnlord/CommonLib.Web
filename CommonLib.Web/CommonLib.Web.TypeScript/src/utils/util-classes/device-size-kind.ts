import "../../extensions/collections/map-extensions";

export default class DeviceSizeKind {
    private _deviceSize: string

    static XS = new DeviceSizeKind("XS");
    static SM = new DeviceSizeKind("SM");
    static MD = new DeviceSizeKind("MD");
    static LG = new DeviceSizeKind("LG");
    static XL = new DeviceSizeKind("XL");

    static DeviceSizes: Map<DeviceSizeKind, number> = new Map<DeviceSizeKind, number>([
        [DeviceSizeKind.XS, 0], 
        [DeviceSizeKind.SM, 576], 
        [DeviceSizeKind.MD, 768], 
        [DeviceSizeKind.LG, 992], 
        [DeviceSizeKind.XL, 1200]
    ]);

    private constructor(deviceSize: string) {
        this._deviceSize = deviceSize;
    }

    getMinWidth() { 
        const deviceWidth = DeviceSizeKind.DeviceSizes.getWithEqualsOrNull(this); 
        if (deviceWidth === null)
            throw new Error("Device Size not found");
        return deviceWidth;
    }

    getMaxWidthOrNull() {
        const currElIdx = DeviceSizeKind.DeviceSizes.indexOfKey(this);
        return currElIdx + 1 < DeviceSizeKind.DeviceSizes.count() ? DeviceSizeKind.DeviceSizes.elementAt_(currElIdx + 1).value - 0.02 : null;
    }

    toMediaQuery() {
        var minWidth = this.getMinWidth();
        var maxWidth = this.getMaxWidthOrNull();
        return !maxWidth
            ? `(min-width: ${minWidth}px)`
            : `(min-width: ${minWidth}px) and (max-width: ${maxWidth}px)`;
    }

    equals(other: DeviceSizeKind): boolean {
        return this._deviceSize.toLowerCase() === other._deviceSize.toLowerCase();
    }

    toString(): string {
        return this._deviceSize;
    }
}