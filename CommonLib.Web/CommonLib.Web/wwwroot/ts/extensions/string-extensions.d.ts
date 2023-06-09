declare global {
    interface String {
        trimMultilineTS(removeHTMLComments: boolean): string;
    }
}
export {};
