import "../extensions/collections/array-extensions";

let suppressedMessages: string[] = [];

export function suppressConsoleMessages(messages: string[], console: any): void {
    const consoleFunctions = ['log', 'warn', 'error', 'info', 'debug', 'group', 'groupCollapsed', 'groupEnd'];
    if (suppressedMessages.containsAllStrings(messages))
        return;

    for (let message of messages) {
        if (suppressedMessages.contains(message))
            return;

        suppressedMessages.push(message);
    }

    for (const func of consoleFunctions) {
        const originalFunction = (console as any)[func];
        (console as any)[func] = function (...args: string[]) {
            if (!args.any(a => {
                    if (typeof a === "object") {
                        a = Object.entries(a).map(kvp => kvp[0] + ": " + kvp[1]).joinAsString(",")
                    }
                    return a.containsAny(suppressedMessages);
                })) {
                originalFunction.apply(console, args);
            }
        };
    }
}
export function suppressConsoleMessage(message: string, console: any): void {
    suppressConsoleMessages([message], console);
}

