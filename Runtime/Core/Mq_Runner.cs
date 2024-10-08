using System;
using System.Collections;
using UnityEngine;
using Unity.Jobs;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace Multisynq {


public class Mq_Runner : MonoBehaviour {
  #region Public
    [Tooltip("For debug use.  If selected, Croquet session startup will wait for user initiation using an external web browser or Node JS command.")]
    public bool debugUsingExternalSession;

    [Tooltip("Whether to force Croquet to run with Node JS, rather than in a WebView.  Windows does not support WebView, so on Windows Node JS is used by default. All other platforms default to WebView.")]
    public bool forceToUseNodeJS = false;

    [Tooltip("For debug use, when access to the Croquet Reflector Infrastructure is not available.  Run messaging locally, supporting only a single user.")]
    public bool runOffline;

    // Not available to most users, so hide for now.
    // [Tooltip("Provide an IP Address for a local Croquet reflector instance.")]
    [HideInInspector] public string localReflector;

    #if UNITY_EDITOR_WIN
      [HideInInspector] public bool showWebview;
      [HideInInspector] public bool manualStart;
    #else
      [Tooltip("For debug use.  If selected, and running with a WebView, the view will appear at bottom-left on the desktop as a small featureless window.  The window's right-click menu provides access to JavaScript debug tools.")]
      public bool showWebview;
      [Tooltip("Require developer to press a button in the WebView page to start the Croquet session.")]
      public bool manualStart;
    #endif
  #endregion

  #region Private

    private static string appSourcePath; // app's own folder under StreamingAssets
    private static string nodeExecPath = ""; // provided by Mq_Bridge

  #endregion

  struct CroquetNodeProcess : IJob {
    public int port;

    private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
      // this is currently not used
      if (!String.IsNullOrEmpty(outLine.Data)) {
        Debug.LogWarning("Node.js stderr: " + outLine.Data);
      }
    }

    public void Execute() {
      // start the child process
      Process croquetProcess = new Process();

      // redirect the output stream of the child process.
      croquetProcess.StartInfo.UseShellExecute = false;
      croquetProcess.StartInfo.RedirectStandardOutput = false;
      croquetProcess.StartInfo.RedirectStandardError = false;
      croquetProcess.StartInfo.CreateNoWindow = true;

      string nodeEntry = "node-main.js";

      croquetProcess.StartInfo.FileName = nodeExecPath;
      croquetProcess.StartInfo.Arguments = $"{nodeEntry} {port}";

      // croquetProcess.OutputDataReceived += OutputHandler;
      // croquetProcess.ErrorDataReceived += OutputHandler;
      croquetProcess.EnableRaisingEvents = true;

      croquetProcess.StartInfo.WorkingDirectory = appSourcePath;

      int exitCode = -1;

      try {
        croquetProcess.Start();
        // croquetProcess.BeginOutputReadLine();
        // croquetProcess.BeginErrorReadLine();

        //UnityEngine.Debug.Log("Process id: " + croquetProcess.Id.ToString());

        croquetProcess.WaitForExit();
      }
      catch (Exception e) {
        UnityEngine.Debug.LogError("Run error: " + e.ToString()); // or throw new Exception
      }
      finally {
        exitCode = croquetProcess.ExitCode;
        UnityEngine.Debug.Log("Croquet process exit code: " + exitCode.ToString());

        croquetProcess.Dispose();
        croquetProcess = null;
      }
    }
  }

  public IEnumerator StartCroquetConnection(int port, string appName, bool useNodeJS, string pathToNode) {
    // this is invoked from Mq_Bridge.StartWS (i.e., never when running in WebGL)
    appSourcePath = Path.Combine(Application.streamingAssetsPath, appName);

    // options for running Croquet code (given lack of webview support on Windows):
    //
    //   editor on MacOS:
    //     a. webview
    //     b. user-launched browser
    //     c. nodeJS (using path from settings object)
    //     d. user-launched nodeJS
    //
    //   editor on Windows:
    //     e. nodeJS (using node.exe from package; forced when "user launched" is false)
    //     f. user-launched nodeJS
    //     g. user-launched browser
    //
    //   deployed standalone on anything other than Windows:
    //     h. webview
    //
    //   deployed standalone on Windows:
    //     i. nodeJS (using node.exe copied into StreamingAssets)

    // figure out the web url, whatever is going to happen
    // Use the port number determined by the bridge

    // TODO: Remove appName from webURL as it is not needed. We also need to change the server.

    // if appName has any characters that are not alphanumeric _ or - then we need to through an error\
    // We don't want to allow spaces either
    if (!Regex.IsMatch(appName, @"^[a-zA-Z0-9._-]+$")) {
      Debug.LogError("Invalid appName. appName can only contain alphanumeric words separated by \".\", \"_\", \"-\" ]");
      yield break;
    }

    string webURL = $"http://localhost:{port}/{appName}/index.html";
    // string escapedAppName = Uri.EscapeDataString(appName);
    // string webURL = $"http://localhost:{port}/{escapedAppName}/index.html";

    localReflector = PlayerPrefs.GetString("sessionIP", "");
    if (localReflector != "") {
      webURL += $"?reflector=ws://{localReflector}/reflector&files=http://{localReflector}/files/";
    }

    // only compile with WebViewObject on non-Windows platforms
    #if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
      Debug.Log($"CroquetRunner.StartCqConnection(): Running on non-Windows platform useNodeJS={useNodeJS} && debugUsingExternalSession={debugUsingExternalSession}");
      if (!useNodeJS && !debugUsingExternalSession) {
        // cases (a), (h)
        #if UNITY_WEBGL
          Debug.Log("CroquetRunner.StartCqConnection(): Using WebViewObject_WebGLTolerant");
          WebViewObject_WebGLTolerant webViewObject = (new GameObject("WebViewObject_WebGLTolerant")).AddComponent<WebViewObject_WebGLTolerant>();
        #else
          Debug.Log("CroquetRunner.StartCqConnection(): Using WebViewObject");
          WebViewObject webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        #endif

        Debug.Log("CroquetRunner.StartCqConnection(): WebViewObject created");
        DontDestroyOnLoad(webViewObject.gameObject);
        webViewObject.Init(
          separated: showWebview,
          enableWKWebView: true,

          cb:      (msg) => { TimedLog(string.Format("CallFromJS[{0}]", msg)); },
          err:     (msg) => { TimedLog(string.Format("CallOnError[{0}]", msg)); },
          httpErr: (msg) => { TimedLog(string.Format("CallOnHttpError[{0}]", msg)); },
          started: (msg) => { /* TimedLog(string.Format("CallOnStarted[{0}]", msg)); */ },
          hooked:  (msg) => { TimedLog(string.Format("CallOnHooked[{0}]", msg)); },
          ld:      (msg) => {
            // TimedLog(string.Format("CallOnLoaded[{0}]", msg));
            webViewObject.EvaluateJS(@"
              console.log('webViewObject.EvaluateJS()');
              // make a blue bg div
              var div = document.createElement('div');
              div.style.position = 'absolute';
              div.style.left = '0px';
              div.style.top = '0px';
              div.style.width = '100%';
              div.style.height = '100%';
              div.style.backgroundColor = 'blue';
              if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                window.Unity = {
                  call: function(msg) {
                    window.webkit.messageHandlers.unityControl.postMessage(msg);
                  }
                }
              } else {
                window.Unity = {
                  call: function(msg) {
                    window.location = 'unity:' + msg;
                  }
                }
              }
            ");
            webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
          }
        );
        #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
          webViewObject.bitmapRefreshCycle = 1;
        #endif

        //webViewObject.SetMargins(-5, -5, Screen.width - 8, Screen.height - 8);
        //webViewObject.SetMargins(5, 5, (int)(Screen.width * 0.6f), (int)(Screen.height * 0.6f));
        webViewObject.SetMargins(Screen.width - 3, Screen.height - 3, -100, -100);
        webViewObject.SetVisibility(showWebview);

        // webViewObject.SetTextZoom(100);  // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410

        TimedLog("invoke LoadURL on " + webURL);
        webViewObject.LoadURL(webURL);
      }
    #else // running in Windows
      if (!debugUsingExternalSession) useNodeJS = true; // force node unless user explicitly wants an external browser
    #endif

    if (!useNodeJS && debugUsingExternalSession) {
      // cases (b), (g)
      TimedLog("ready for browser to load from <a href=\""+$"{webURL}\">{webURL}</a>");
    }

    if (useNodeJS) {
      if (!debugUsingExternalSession) {
        // cases (c), (e), (i)
        nodeExecPath = pathToNode;

        var job = new CroquetNodeProcess() {
          port = port
        };
        JobHandle jobHandle = job.Schedule();
      } else {
        // cases (d), (f)
        TimedLog($"ready to run '{pathToNode} node-main.js {port}' in {appSourcePath}\n" +
                 $"    cd {appSourcePath}\n" +
                 $"    {pathToNode} node-main.js {port}");
      }
    }

    yield break;
  }

  void TimedLog(string msg) {
    UnityEngine.Debug.Log($"{System.DateTimeOffset.Now.ToUnixTimeMilliseconds() % 100000}: {msg}");
  }

}

} // namespace MultisynqNS