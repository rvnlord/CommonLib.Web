import React, { useEffect, useState } from 'react';
import DeviceSizeKind from '../../utils/util-classes/device-size-kind';
import './MyMediaQuery.scss';

type MediaQueryProps = {
    targetDeviceSizes?: DeviceSizeKind[];
    onChange: (deviceSize: DeviceSizeKind) => void;
    classes?: string[];
};

const MediaQuery: React.FC<MediaQueryProps> = ({ targetDeviceSizes, onChange, classes }) => {
    let [currentDeviceSize, setCurrentDeviceSize] = useState(DeviceSizeKind.XL);

    useEffect(() => {
        let deviceSizesWithQueries: Map<DeviceSizeKind, string> = new Map<DeviceSizeKind, string>();
        for (let deviceSize of targetDeviceSizes || DeviceSizeKind.DeviceSizes.toKeysArray()) {
            deviceSizesWithQueries.set(deviceSize, deviceSize.toMediaQuery());
        }

        const handleDeviceSizeChange = (e: MediaQueryListEvent, ds: DeviceSizeKind) => {
            if (e.matches) {
                setCurrentDeviceSize(ds);
                onChange(ds);
            }
        };

        const mediaQueryListeners: Map<DeviceSizeKind, MediaQueryList> = new Map<DeviceSizeKind, MediaQueryList>();
        for (let i = deviceSizesWithQueries.count() - 1; i >= 0; i--) {
            const { key: deviceName, value: mediaQuery } = deviceSizesWithQueries.elementAt_(i); // to avoid captured closure of "i"
            const query = window.matchMedia(mediaQuery);
            if (query.matches) {
                setCurrentDeviceSize(deviceName);
                onChange(deviceName);
            }

            query.addEventListener("change", e => handleDeviceSizeChange(e, deviceName));
            mediaQueryListeners.set(deviceName, query);
        }

        return () => {
            for (let { key: deviceName, value: query } of mediaQueryListeners.toKVPs()) {
                query.removeEventListener("change", e => handleDeviceSizeChange(e, deviceName));
            }
        };
    }, [targetDeviceSizes, onChange, currentDeviceSize]);

    const renderedClasses = ['my-media-query', ...(classes || [])].joinAsString(' ');

    return (<div device-size={currentDeviceSize.toString()} className={renderedClasses}></div>);
};

export default MediaQuery;