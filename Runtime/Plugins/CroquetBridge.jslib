mergeInto(LibraryManager.library, {
    SendMessageToJS: function(messagePtr) {
        var message = UTF8ToString(messagePtr);
        console.log("Message from Unity:", message);

        if (typeof handleUnityMessage !== "undefined") {
            handleUnityMessage(message);
        } else {
            console.error("handleUnityMessage function not found.");
        }
    }
});
