import React from 'react';
import MyMediaQuery from '../../components/media-query/MyMediaQuery';
import logo from '../../content/logo.svg';
import DeviceSizeKind from '../../utils/util-classes/device-size-kind';
import $ from "jquery";

function mediaQuery_Changed(ds: DeviceSizeKind) {
    $("#mqMessage").html(ds.toString());
}

function HomeIndex() {
    return (
        <div>
            <h2>Home</h2>
            <div className="my-media-query-test-container">
                <MyMediaQuery onChange={d => mediaQuery_Changed(d)} />
                <div id={"mqMessage"}>@MediaQueryMessage</div>
            </div>
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo" />
                <p>
                    Edit <code>src/App.tsx</code> and save to reload.
                </p>
                <a
                    className="App-link"
                    href="https://reactjs.org"
                    target="_blank"
                    rel="noopener noreferrer"
                >
                    Learn React test
                </a>
            </header>
        </div>
    );
}

export default HomeIndex;