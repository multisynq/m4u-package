mergeInto(LibraryManager.library, {
    SendMessageToJS: function(messagePtr) {
        var message = UTF8ToString(messagePtr);

        if (typeof handleUnityMessage !== "undefined") {
            handleUnityMessage(message);
        } else {
            console.error("handleUnityMessage function not found.");
        }
    },
    
    RegisterUnityReceiver: function () {
        if (typeof window.BridgeToUnity !== 'undefined') {
            window.BridgeToUnity.registerUnityReceiver();
        }
    }
});
