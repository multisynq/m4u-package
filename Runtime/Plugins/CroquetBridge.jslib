mergeInto(LibraryManager.library, {
    SendMessageToJS: function (msg) {
        var message = UTF8ToString(msg);
        if (typeof window.BridgeToUnity !== 'undefined') {
            window.BridgeToUnity.handleMessageFromUnity(message);
        }
    },
    
    RegisterUnityReceiver: function () {
        if (typeof window.BridgeToUnity !== 'undefined') {
            window.BridgeToUnity.registerUnityReceiver();
        }
    }
});
