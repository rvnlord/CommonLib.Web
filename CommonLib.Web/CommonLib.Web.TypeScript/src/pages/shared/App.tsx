import React from 'react';
import './App.scss';
import { Routes, Route } from "react-router-dom";
import Layout from './Layout';
import HomeIndex from '../home/HomeIndex';
import NoMatch from './NoMatch';
import TestIndex from '../test/TestIndex';

function App() {
    return (
        <div className="App">
            <h1>CommonLib.Web.TypeScript</h1>

            <Routes>
                <Route path="/" element={<Layout />}>
                    <Route index element={<HomeIndex />} />
                    <Route path="test" element={<TestIndex />} />
                    <Route path="*" element={<NoMatch />} />
                </Route>
            </Routes>
        </div>
    );
}

export default App;
