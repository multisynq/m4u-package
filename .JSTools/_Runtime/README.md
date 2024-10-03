# MultisynqJS Runtime Files

Croquet needs a JavaScript engine to execute JS code, and its supporting JS code. This folder contains both.

## Platforms

Depending on the host OS and build configuration, Croquet can use one of three ways to execute JavaScript:

### WebView

This is the preferred option. It uses the platform's own JS runtime via a WebView. This currently works on MacOS, iOS, and Android builds.

C# creates a Web Server and a WebView. The WebView loads `WebView/webview.html` from the Web Server. The HTML file loads the Croquet JS code. The JS code opens a WebSocket connection to the Web Server and thus establishes a communication channel between C# and JS.

The build process copies `webview.html` and a bundle of all the JS source code to `Assets/StreamingAssets` to be deployed with the app.

### Node

On Windows, we launch a Web Server and a NodeJS process to execute JS code. The Croquet JS code opens a WebSocket connection to the Web Server and thus establishes a communication channel between C# and JS.

The build process copies `node.exe` and a bundle of all the JS source code to `Assets/StreamingAssets`.

### WebGL

On WebGL builds, the Web Browser provides the JS execution context. It essentially loads `WebGL/index.html` which executes both the C# code that has been compiled to WASM, and the Croquet JS code. Since both run in the same JS context, they communicate directly, rather than via a WebSocket.

The build process copies this folder and a bundle of all the JS source code into `Assets/WebGLTemplates/MultisynqLoader`, from which the regular Unity WebGL build continues.
