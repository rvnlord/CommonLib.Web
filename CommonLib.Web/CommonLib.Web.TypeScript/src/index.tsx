import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.scss';
import App from './pages/shared/App';
import reportWebVitals from './reportWebVitals';
import { BrowserRouter } from "react-router-dom";
import { initializeLinq } from "linq-to-typescript";
import "./extensions/string-extensions";
import { suppressConsoleMessages } from './utils/console-utils';
import $ from "jquery";

initializeLinq();

//const test = ['log', 'warn', 'error', 'info', 'debug', 'group', 'groupCollapsed', 'groupEnd'];
//let t1 = test.toMyEnumerable().containsAllStrings(["log", "debug", "group"]);
//let t2 = test.toMyEnumerable().containsAllStrings(["log", "debug", "group", "lolz"]);
//let t3 = test.toMyEnumerable().any(s => s === "groupEnd");

suppressConsoleMessages(["activation failed", "KendoReact"], console);

//window.console.log("as");

//const evenNumbers = [1, 2, 3, 4, 5, 6, 7, 8, 9].where((x) => x % 2 === 0).toArray()

const root = ReactDOM.createRoot($("#root")[0] as HTMLElement);

root.render(
    <React.StrictMode>
        <BrowserRouter>
            <App />
        </BrowserRouter>
    </React.StrictMode>
);

reportWebVitals();
