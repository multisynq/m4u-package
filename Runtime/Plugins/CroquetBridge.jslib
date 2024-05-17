mergeInto(LibraryManager.library, {
    CroquetBridge_Init: function() {
        if (typeof WebSocket === 'undefined') {
            console.error('WebSocket is not supported in this environment.');
            return;
        }
        
        window.CroquetBridge = {
            ws: null,
            sendMessage: function(message) {
                if (this.ws && this.ws.readyState === WebSocket.OPEN) {
                    this.ws.send(message);
                } else {
                    console.warn('WebSocket is not connected.');
                }
            },
            onMessage: function(event) {
                var message = event.data;
                ccall('OnCroquetMessage', 'void', ['string'], [message]);
            },
            connect: function(url) {
                this.ws = new WebSocket(url);
                this.ws.onmessage = this.onMessage;
                this.ws.onopen = function() {
                    console.log('WebSocket connection established.');
                };
                this.ws.onclose = function() {
                    console.log('WebSocket connection closed.');
                };
                this.ws.onerror = function(error) {
                    console.error('WebSocket error: ' + error);
                };
            },
            disconnect: function() {
                if (this.ws) {
                    this.ws.close();
                    this.ws = null;
                }
            }
        };
    },
    CroquetBridge_Connect: function(url) {
        var jsUrl = UTF8ToString(url);
        window.CroquetBridge.connect(jsUrl);
    },
    CroquetBridge_Disconnect: function() {
        window.CroquetBridge.disconnect();
    },
    CroquetBridge_SendMessage: function(message) {
        var jsMessage = UTF8ToString(message);
        window.CroquetBridge.sendMessage(jsMessage);
    }
});
