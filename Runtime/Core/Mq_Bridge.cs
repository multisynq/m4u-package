using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using WebSocketSharp.Server;
using WebSocketSharp.Net;
using System.Runtime.InteropServices;


namespace Multisynq {


/// <summary>
/// Croquet Bridge is the primary Component that should be attached to a Prefab in any scene that you want to support multiplayer.
/// </summary>
public class  Mq_Bridge : MonoBehaviour {
  public bool mq_Bridge;  // Helps tools resolve "missing Script" problems
  # region Public
  public Mq_Settings appProperties;

  [Header("Session Configuration")]
  [Tooltip("Single-token name (no spaces) for the Croquet app that synchronizes users in this scene.  The app's source code must appear in a directory with this name under 'Assets/MultisynqJS/'.")]
  public string appName;

  [Tooltip("Single-token name (no spaces) for the Croquet session if not otherwise specified at runtime through a menu.  Users entering the same named session of the same named Croquet app will find themselves playing together.")]
  public string defaultSessionName = "ABCDE";

  [Tooltip("If true, the Croquet session will ignore any differences between the code from build to build. This is useful for debugging, but can lead to unexpected behavior if builds are out of sync.")]
  public bool ignoreCodeDiffsForSession = true;

  [Tooltip("Optionally, the name of a scene in this project.  If blank, pressing Play in the current scene will immediately launch Croquet with the app name and session name specified above.  If a scene name is provided, the current scene is assumed to act as a menu for selecting the session name; once selected, Croquet is then launched to run the named scene.")]
  public string launchViaMenuIntoScene = "";

  [Tooltip("For debug use.  Causes Croquet to log the selected types of session information.  These logs will only be visible in the Unity console if you have enabled forwarding of the \"log\" category below.")]
  public CroquetDebugTypes croquetDebugLogging;

  [Tooltip("For debug use.  Specifies which categories of JavaScript console output are forwarded for echoing in the Unity console.")]
  public CroquetLogForwarding JSLogForwarding;

  [Header("Session State")]
  [Tooltip("The current state of the Croquet session for this view - one of: (stopped, requested, running)")]
  public string croquetSessionState = "stopped";

  [Tooltip("The Croquet session name requested by this view.")]
  public string sessionName = "";

  [Tooltip("This view's identifier, assigned by the session once running. Unique to each client in the session.")]
  public string croquetViewId;

  [Tooltip("How many views are currently connected to the session.")]
  public int croquetViewCount;

  [Tooltip("The scene currently being handled in the model.")]
  public string croquetActiveScene;

  [Tooltip("The state of scene readiness in the model - one of: (preload, loading, running)")]
  public string croquetActiveSceneState;

  [Tooltip("The state of scene readiness in this view - one of: (dormant, waitingToPrepare, preparing, ready, running)")]
  public string unitySceneState = "dormant";

  [Header("Network Glitch Simulator")]
  [Tooltip("For debug use. Check this to trigger a network glitch and see how the app performs.")]
  public bool triggerGlitchNow = false;

  [Tooltip("The duration of network glitch to trigger.")]
  public float glitchDuration = 3.0f;

  [Tooltip("The current active Croquet Systems that have registered with the Bridge.")]
  public Mq_System[] croquetSystems = new Mq_System[0];

  #endregion

  #region PRIVATE
  private List<Mq_ActorManifest> sceneDefinitionManifests = new List<Mq_ActorManifest>();

  private List<string> sceneHarvestList = new List<string>(); // joined pairs  sceneName:appName
  private Dictionary<string, List<string>> sceneDefinitionsByApp =
    new Dictionary<string, List<string>>(); // appName to list of scene definitions

  private static string bridgeState = "stopped"; // needJSBuild, waitingForJSBuild, foundJSBuild, waitingForConnection, waitingForSessionName, waitingForSession, started
  private bool waitingForCroquetSceneReset = false; // an editor sets this true until the Croquet session arrives in the scene we want to start with
  private string requestedSceneLoad = "";
  private bool skipNextFrameUpdate = false; // sometimes (e.g., on scene reload) you just need to chill for a frame

  HttpServer ws = null;
  WebSocketBehavior wsb = null; // not currently used
  static WebSocket clientSock = null;
  //static int sockMessagesReceived = 0;
  //static int sockMessagesSent = 0;
  public class QueuedMessage {
    public long queueTime;
    public bool isBinary;
    public byte[] rawData;
    public string data;
  }

  [DllImport("__Internal")]
  private static extern void SendMessageToJS(string message);

  // sending to JS through interop or through socket, as appropriate
  public void SendMessageToJavaScript(string message) {
    // if (message != "tick") Debug.Log($"SendMessageToJavaScript: {message}");
    if (INTEROP_BRIDGE) {
      if (!message.Contains("tick")) Debug.Log($"sending to JS: {message}");
      SendMessageToJS(message);
    } else {
      if (clientSock == null || clientSock.ReadyState != WebSocketState.Open) {
        Debug.LogWarning($"socket not ready to send: {message}");
      } else {
        // Debug.Log($"clientSock: {clientSock} | clientSock.ReadyState: {clientSock.ReadyState} | message: {message} readyState: {clientSock.ReadyState}");
        clientSock.Send(message);
      }
    }
  }


  [DllImport("__Internal")]
  private static extern void RegisterUnityReceiver();

  // entry point for a message received from JS through interop
  public void OnMessageReceivedFromJS(string messageData) {
    // $$$$ why the doubt about format?
    // Try to deserialize the message data as JSON
    MessageObject messageObject;
    try {
      messageObject = JsonUtility.FromJson<MessageObject>(messageData);
    }
    catch (ArgumentException) {
      // If JSON deserialization fails, it might be a base64 encoded string
      Debug.Log("Message data is not a valid JSON, attempting base64 decoding...");
      try {
        byte[] decodedBytes = Convert.FromBase64String(messageData);
        string decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        // Debug.Log("Decoded message data: " + decodedString);
        messageObject = JsonUtility.FromJson<MessageObject>(decodedString);
      }
      catch (Exception ex) {
        Debug.LogError("Failed to decode message data: " + ex.Message);
        return;
      }
    }

    if (messageObject != null) {
      EnqueueMessageFromInterop(messageObject);
    } else {
      Debug.LogError("MessageObject is null after deserialization.");
    }
  }
  [System.Serializable]
  private class MessageObject {
    public string message;
    public bool isBinary;
  }

  static void EnqueueMessageFromInterop(MessageObject messageObject) {
    QueuedMessage qm = new QueuedMessage();
    qm.queueTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    qm.isBinary = messageObject.isBinary;
    if (messageObject.isBinary) {
      byte[] binaryMessage = Convert.FromBase64String(messageObject.message);
      qm.rawData = binaryMessage;
    } else {
      qm.data = messageObject.message;
    }
    messageQueue.Enqueue(qm);
  }

  static ConcurrentQueue<QueuedMessage> messageQueue = new ConcurrentQueue<QueuedMessage>();
  static long estimatedDateNowAtReflectorZero = -1; // an impossible value

  List<(string, string)> deferredMessages = new List<(string, string)>(); // messages with (optionally) a throttleId for removing duplicates

  LoadingProgressDisplay loadingProgressDisplay;

  public static Mq_Bridge Instance { get; private set; }
  private Mq_Runner croquetRunner;

  private bool INTEROP_BRIDGE;

  private static Dictionary<string, List<(GameObject, Action<string>)>> croquetSubscriptions = new Dictionary<string, List<(GameObject, Action<string>)>>();
  private static Dictionary<GameObject, HashSet<string>> croquetSubscriptionsByGameObject =
    new Dictionary<GameObject, HashSet<string>>();

  // settings for logging and measuring (on JS-side performance log).  absence of an entry for a
  // category is taken as false.
  Dictionary<string, bool> logOptions = new Dictionary<string, bool>();
  static string[] logCategories = new string[] { "info", "session", "diagnostics", "debug", "verbose" };
  Dictionary<string, bool> measureOptions = new Dictionary<string, bool>();
  static string[] measureCategories = new string[] { "update", "bundle", "geom" };

  // TODO: Create Counter System in Metric Class
  // diagnostics counters
  int outMessageCount = 0;
  int outBundleCount = 0;
  int inBundleCount = 0;
  int inMessageCount = 0;
  long inBundleDelayMS = 0;
  long inProcessingMS = 0;
  float lastMessageDiagnostics; // realtimeSinceStartup

  #endregion

  private void SetBridgeState(string state) {
    bridgeState = state;
    Log("session", $"bridge state: {bridgeState}");
  }

  private void SetUnitySceneState(string state, string sceneName) {
    unitySceneState = state;
    // Log("session", $"scene state: {unitySceneState} (scene \"{sceneName}\")");
  }

  void Awake() {
    // Create Singleton Accessor
    // If there is an instance, and it's not me, delete myself.
    if (Instance != null && Instance != this) {
      Destroy(gameObject); // take responsibility for removing the whole object
    } else {
      Instance = this;

      INTEROP_BRIDGE = Application.platform == RuntimePlatform.WebGLPlayer && (!Application.isEditor);
      if (!INTEROP_BRIDGE) Debug.Log($"Not running in WebGL INTEROP_BRIDGE={INTEROP_BRIDGE}");

      Application.runInBackground = true;

      SetCSharpLogOptions("info,session");
      SetCSharpMeasureOptions("bundle"); // for now, just report handling of message batches from Croquet

      croquetRunner = gameObject.GetComponent<Mq_Runner>();

      #if UNITY_EDITOR
        string harvestScenes = Mq_Builder.HarvestSceneList;

        if (harvestScenes != "") {
          // this run has been triggered purely to harvest scenes.

          // the overall string is a comma-separated list of sceneName:appName strings
          sceneHarvestList = new List<string>(harvestScenes.Split(','));
          Mq_Builder.HarvestSceneList = ""; // clear immediately, in case something goes wrong
        } else {
          waitingForCroquetSceneReset = true; // when the session starts, we will demand a specific scene

          if (!croquetRunner.runOffline &&
            (appProperties.apiKey == "" || appProperties.apiKey == "PUT_YOUR_API_KEY_HERE")) {
            Debug.LogWarning(
              "No API key found in the Settings object; switching Croquet to run in Offline mode.");
            croquetRunner.runOffline = true;
          }

          SetBridgeState("needJSBuild");
        }
      #else
        SetBridgeState("foundJSBuild"); // assume that in a deployed app we always have a JS build
      #endif

      DontDestroyOnLoad(gameObject);
      croquetSystems = gameObject.GetComponents<Mq_System>().Where(system => system.enabled).ToArray();
      Debug.Log($"%mag%Systems: %wh%[%ye%{string.Join(", ", croquetSystems.Select(system => system.GetType().Name))}%wh%]".TagColors());
      Croquet.Subscribe("croquet", "viewCount", HandleViewCount);
    }
  }

  private void Start() {
    if (INTEROP_BRIDGE) {
      RegisterUnityReceiver();
    }
    // Frame cap
    Application.targetFrameRate = 60;

    LoadingProgressDisplay loadingObj = FindObjectOfType<LoadingProgressDisplay>();
    if (loadingObj != null) {
      DontDestroyOnLoad(loadingObj.gameObject);
      loadingProgressDisplay = loadingObj.GetComponent<LoadingProgressDisplay>();
      loadingProgressDisplay.Hide(); // until it's needed
    }
  }

#if UNITY_EDITOR
  private async void WaitForJSBuild() {
    bool haveTools = await Mq_Builder.EnsureJSToolsAvailable();
    bool haveBuild = Mq_Builder.EnsureJSBuildAvailableToPlay();
    bool success = haveTools && haveBuild;
    if (!success) {
      // report each flag's failure
      Debug.LogError( $"|>  STOPPING PLAY.  ==>  Have JS tools: {haveTools}, Have JS build: {haveBuild}.");
      Debug.Log(  "|> <color=#ff5555>TO FIX:</color> Use menu: <color=cyan>| Multisynq | Open Build Assistant |</color> <color=#55ff55>[ Check if Ready ]</color> to fix.");

      // error(s) will have already been reported
      EditorApplication.ExitPlaymode();
      return;
    }

    SetBridgeState("foundJSBuild"); // assume that in a deployed app we always have a JS build
  }
#endif

  public void SetSessionName(string newSessionName) {
    if (croquetRunner.runOffline) {
      sessionName = "offline";
      Debug.LogWarning("session name overridden for offline run");
    } else if (newSessionName == "") {
      sessionName = defaultSessionName;
      if (sessionName == "") {
        Debug.LogWarning("Attempt to start Croquet with a default sessionName, but no default has been set. Falling back to \"unnamed\".");
        sessionName = "unnamed";
      } else {
        Log("session", $"session name defaulted to {defaultSessionName}");
      }
    } else {
      sessionName = newSessionName;
      defaultSessionName = newSessionName;
      Log("session", $"session name set to {newSessionName}");
    }
  }

  private void ArrivedInGameScene(Scene currentScene) {
    // immediately deactivate all Croquet objects, but keep a record of those that were active
    // in case we're asked to provide a scene definition
    sceneDefinitionManifests.Clear();
    Mq_ActorManifest[] croquetObjects = FindObjectsOfType<Mq_ActorManifest>();
    foreach (Mq_ActorManifest manifest in croquetObjects) {
      GameObject go = manifest.gameObject;
      if (go.activeSelf) {
        sceneDefinitionManifests.Add(manifest);
        go.SetActive(false); // keep it around but invisible until we've read the manifest
      } else {
        Destroy(go); // not part of the definition; ditch it immediately
      }
    }

    // for now, the main thing we need to trigger is the loading of the scene-specific assets.
    // this is also a chance to capture subscriptions from early-awakening gameObjects in the scene.

    // if we're just harvesting, then once the assets are ready we can harvest the scene's definition.

    // if running the app, then once things are prepared we will tell Croquet that the scene is ready here.
    // if the scene is already running in Croquet, that information will be enough to trigger our
    // local bridge to create the ViewRoot, and hence the pawn manager that will tell us all the
    // pawns to make.
    // if the scene is *not* yet running in Croquet (in 'preload' state), we'll also send the
    // details to allow all clients to build the scene: the asset manifests, early subscriptions,
    // and the details of all pre-placed objects.  other clients may be sending the information
    // too; the first to ask gets permission.  the model on every client will initialise
    // itself with the scene's state, and then the client's view will wait for its Unity side
    // to be ready to load the scene; ours will immediately pass that test.
    foreach (Mq_System system in croquetSystems) {
      system.LoadedScene(currentScene.name);
    }
  }

  private void OnDestroy() {
    if (ws != null) {
      ws.Stop();
    }
  }

  public class Mq_BridgeWS : WebSocketBehavior {

    protected override void OnOpen() {
      Debug.Log("Mq_BridgeWS.OnOpen(): server socket opened");
      if (clientSock != null) {
        Debug.LogWarning("Rejecting attempt to connect second client");
        Context.WebSocket.Send(String.Join('\x01', new string[] { "log", "Rejecting attempt to connect second client" }));
        Context.WebSocket.Close(1011, "Rejecting duplicate connection");
        return;
      }

      // hint from https://github.com/sta/websocket-sharp/issues/236
      clientSock = Context.WebSocket;

      Instance.Log("session", "server socket opened");
    }

    protected override void OnMessage(MessageEventArgs e) {
      // bridge.Log("verbose", "received message in Unity: " + (e.IsBinary ? "binary" : e.Data));
      EnqueueMessageFromWS(e);
    }

    protected override void OnClose(CloseEventArgs e) {
      Instance.Log("session", System.String.Format("server socket closed {0}: {1}", e.Code, e.Reason));
    }
  }

  void StartWS() {
    // TODO: could try this workaround (effectively disabling Nagel), as suggested at
    // https://github.com/sta/websocket-sharp/issues/327
    //var listener = typeof(WebSocketServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ws) as System.Net.Sockets.TcpListener;
    //listener.Server.NoDelay = true;
    // ...but I don't know how to apply this now that we're using HttpServer (plus WS service)
    // rather than WebSocketServer

    if (launchViaMenuIntoScene == "") SetLoadingStage(0.25f, "Connecting...");

    Log("session", "building WS Server on open port");
    int port = appProperties.preferredPort;
    int remainingTries = 9;
    bool goodPortFound = false;
    while (!goodPortFound && remainingTries > 0) {
      HttpServer wsAttempt = null;
      try {
        Debug.Log($"StartWS(): Attempting to start WS server on port {port}");
        wsAttempt = new HttpServer(port);
        wsAttempt.AddWebSocketService<Mq_BridgeWS>("/Bridge", s => wsb = s);
        wsAttempt.KeepClean = false; // see comment in https://github.com/sta/websocket-sharp/issues/43
        wsAttempt.DocumentRootPath = Application.streamingAssetsPath; // set now, before Start()

        wsAttempt.Start();

        goodPortFound = true;
        ws = wsAttempt;
      }
      catch (Exception e) {
        Debug.Log($"Port {port} is not available");
        Log("debug", $"Error on trying port {port}: {e}");

        port++;
        remainingTries--;
        wsAttempt.Stop();
      }
    }

    if (!goodPortFound) {
      Debug.LogError("Cannot find an available port for the Croquet bridge");
#if UNITY_EDITOR
      EditorApplication.ExitPlaymode();
#endif
      return;
    }

    ws.OnHead += OnHeadHandler;
    ws.OnGet += OnGetHandler;

    Log("session", $"started HTTP/WS Server on port {port}");

    string pathToNode = "";
    bool forceToUseNodeJS = croquetRunner.forceToUseNodeJS;
    bool useNodeJS = forceToUseNodeJS; // default

    #if UNITY_EDITOR_OSX
      pathToNode = appProperties.pathToNode; // if needed
    #elif UNITY_EDITOR_WIN
      // in Windows editor, use Node unless user has set debugUsingExternalSession and has *not* set forceToUseNodeJS
      pathToNode = Mq_Builder.NodeExeInPackage; // if needed
      useNodeJS = !(croquetRunner.debugUsingExternalSession && !forceToUseNodeJS);
    #elif UNITY_STANDALONE_WIN || UNITY_WSA
      pathToNode = Mq_Builder.NodeExeInBuild;
      useNodeJS = true;
    #else
      // some form of non-Windows standalone.  Node is not available.
      useNodeJS = false;
    #endif
    // Log these: port, appName, useNodeJS, pathToNode
    Debug.Log($"croquetRunner.StartCroquetConnection( port:{port}, appName:{appName}, useNodeJS:{useNodeJS}, pathToNode:{pathToNode})");
    StartCoroutine(croquetRunner.StartCroquetConnection(port, appName, useNodeJS, pathToNode));
  }

  void OnHeadHandler(object sender, HttpRequestEventArgs e) {
    // extremely simple response.  always sets a ContentLength64 of zero (because otherwise
    // Chrome complains ERR_EMPTY_RESPONSE if there's no body of that length).  sets status
    // to 200 for a file that is present, and 204 for one that is not found.
    var req = e.Request;
    var res = e.Response;

    var path = req.Url.LocalPath;
    if (path == "/") path += "index.html";

    bool success = TryToGetFile(e, path, out byte[] contents);
    res.ContentLength64 = 0;
    res.StatusCode = success ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NoContent;
  }

  void OnGetHandler(object sender, HttpRequestEventArgs e) {
    var req = e.Request;
    var res = e.Response;

    var path = req.Url.LocalPath;
    if (path == "/") path += "index.html";

    bool success = TryToGetFile(e, path, out byte[] contents);
    if (success) {
      if (path.EndsWith(".html")) {
        res.ContentType = "text/html";
        res.ContentEncoding = Encoding.UTF8;
      } else if (path.EndsWith(".js")) {
        res.ContentType = "application/javascript";
        res.ContentEncoding = Encoding.UTF8;
      } else if (path.EndsWith(".wasm")) {
        res.ContentType = "application/wasm";
      }

      res.ContentLength64 = contents.LongLength;

      res.Close(contents, true);
    } else {
      res.StatusCode = (int)HttpStatusCode.NotFound; // whatever the error
      // res.Close();  no need; will be done for us
    }
  }

  bool TryToGetFile(HttpRequestEventArgs e, string path, out byte[] contents) {
    bool success;

    #if UNITY_ANDROID && !UNITY_EDITOR
      string src = Application.streamingAssetsPath + path;
      // Debug.Log("attempting to fetch " + src);
      var unityWebRequest = UnityWebRequest.Get(src);
      unityWebRequest.SendWebRequest();
      // until we figure out a way to incorporate an await or yield without
      // accidentally losing the HttpRequest along the way, using a busy-wait
      // is blunt but appears to get the job done.
      // note: "[isDone] will return true both when the UnityWebRequest
      // finishes successfully, or when it encounters a system error."
      while (!unityWebRequest.isDone) { }
      if (unityWebRequest.result != UnityWebRequest.Result.Success) {
        if (unityWebRequest.error != null) UnityEngine.Debug.Log(src + ": " + unityWebRequest.error);
        contents = new byte[0];
        success = false;
      } else {
        contents = unityWebRequest.downloadHandler.data; // binary
        success = true;
      }
      unityWebRequest.Dispose();
    #else
      success = e.TryReadFile(path, out contents);
    #endif

    return success;
  }

  // WebSocket messages come in on a separate thread.  Put each message on a queue to be
  // read by the main thread.
  // static because called from a class that doesn't know about this instance.
  static void EnqueueMessageFromWS(MessageEventArgs e) {
    // Add a time so we can tell how long it sits in the queue
    QueuedMessage qm = new QueuedMessage();
    qm.queueTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    qm.isBinary = e.IsBinary;
    if (e.IsBinary) {
      qm.rawData = e.RawData;
    } else {
      qm.data = e.Data;
    }
    messageQueue.Enqueue(qm);
  }

  void StartCroquetSession() {
    Debug.Log("StartCroquetSession()");
    SetLoadingStage(0.5f, "Starting...");

    string debugLogTypes = croquetDebugLogging.ToString();
    // issue a warning if Croquet debug logging is enabled when not using an
    // external browser
    if (!croquetRunner.debugUsingExternalSession && debugLogTypes != "") {
      Debug.LogWarning($"Croquet debug logging is set to \"{debugLogTypes}\"");
    }

    string debugFlags = debugLogTypes; // unless...
    if (croquetRunner.runOffline) {
      // @@ minor hack: Croquet treats "offline" as just another debug flag.  since in Unity
      // the option appears in the UI separately from the logging flags, add it in here.
      debugFlags = debugFlags == "" ? "offline" : $"{debugFlags},offline";
    }

    // manualStart can only be used in the editor
    #if UNITY_EDITOR
      bool manualStart = (croquetRunner.manualStart && croquetRunner.showWebview);
    #else
      bool manualStart = false;
    #endif

    ReadyForSessionProps props = new ReadyForSessionProps() {
      apiKey = appProperties.apiKey,
      appId = appProperties.appPrefix + "." + appName,
      appName = appName,
      packageVersion = Mq_Builder.FindJSToolsRecord().packageVersion, // uses different lookups in editor and in a build (and a potentially length async fetch on WebGL)
      sessionName = sessionName,
      debugFlags = debugFlags,
      isEditor = Application.isEditor,
      manualStart = manualStart,
    };

    string propsJson = JsonUtility.ToJson(props);
    string[] command = new string[] {
      "readyForSession",
      propsJson
    };

    // send the message directly (bypassing the deferred-message queue)
    string msg = String.Join('\x01', command);
    SendMessageToJavaScript(msg);

    croquetSessionState = "requested";
  }

  [Serializable]
  class ReadyForSessionProps {
    public string apiKey;
    public string appId;
    public string appName;
    public string packageVersion;
    public string sessionName;
    public string debugFlags;
    public bool isEditor;
    public bool manualStart;
  }


  public void SendToCroquet(params string[] strings) {
    // Debug.Log($"==== [3.1] SendToCroquet: {string.Join(',', strings)}" + $" | croquetSessionState: {croquetSessionState}");
    if (croquetSessionState != "running") {
      Debug.LogWarning($"attempt to send when Croquet session is not running: {string.Join(',', strings)}");
      return;
    }
    // Debug.Log($"==== [3.2] SendToCroquet: {string.Join(',', strings)}");
    deferredMessages.Add(("", PackCroquetMessage(strings)));
  }

  public void SendToCroquetSync(params string[] strings) {
    // Aug 2023: now that we check for deferred messages every 20ms, this is currently identical to SendToCroquet()
    SendToCroquet(strings);
  }

  public void SendThrottledToCroquet(string throttleId, params string[] strings) {
    // this simply replaces any message with the same throttleId that is waiting to be sent -
    // thus it only "throttles" to our frequency of processing deferred messages (currently 50Hz),
    int i = 0;
    int foundIndex = -1;
    foreach ((string throttle, string msg) entry in deferredMessages) {
      if (entry.throttle == throttleId) {
        foundIndex = i;
        break;
      }

      i++;
    }
    if (foundIndex != -1) deferredMessages.RemoveAt(i);

    deferredMessages.Add((throttleId, PackCroquetMessage(strings)));
  }

  public string PackCroquetMessage(string[] strings) {
    return String.Join('\x01', strings);
  }

  void SendDeferredMessages() {
    // we expect this to be called 50 times per second.

    if (!INTEROP_BRIDGE &&
      (clientSock == null || clientSock.ReadyState != WebSocketState.Open)) return;

    if (deferredMessages.Count == 0) {
      SendMessageToJavaScript("tick");
      return;
    }

    outBundleCount++;
    outMessageCount += deferredMessages.Count;

    // preface every bundle with the current time
    deferredMessages.Insert(0, ("", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()));
    List<string> messageContents = new List<string>(); // strip out the throttle info
    foreach ((string throttle, string msg) entry in deferredMessages) {
      messageContents.Add(entry.msg);
    }

    string joinedMsgs = String.Join('\x02', messageContents.ToArray());
    // Debug.Log($"==== [4] SendDeferredMessages: {joinedMsgs.Replace("\x03", "|").Replace("\x01", "|").Replace("\x02", "  |  ")}");
    SendMessageToJavaScript(joinedMsgs);

    deferredMessages.Clear();
  }


  void Update() {
    if (skipNextFrameUpdate) {
      skipNextFrameUpdate = false;
      return;
    }

#if UNITY_EDITOR
  if (sceneHarvestList.Count > 0) {
    AdvanceHarvestStateWhenReady();
    return;
  }
#endif

    ProcessCroquetMessages();

    if      (bridgeState         != "started") AdvanceBridgeStateWhenReady();
    else if (croquetSessionState == "running") {
      AdvanceSceneStateIfNeeded();

      // things to check periodically while the session is supposedly in full flow
      if (triggerGlitchNow) {
        // @@ need to debounce this
        triggerGlitchNow = false; // cancel the request

        int milliseconds = (int)(glitchDuration * 1000f);
        if (milliseconds > 0) {
          SendToCroquetSync("simulateNetworkGlitch", milliseconds.ToString());
        }
      }
    }
  }
  bool IsSceneReadyToRun(string targetSceneName) {
    if (unitySceneState == "waitingToPrepare") {
      Scene unityActiveScene = SceneManager.GetActiveScene();
      if (unityActiveScene.name == targetSceneName && (requestedSceneLoad == "" || requestedSceneLoad == targetSceneName)) {
        requestedSceneLoad = "";
        ArrivedInGameScene(unityActiveScene);
        SetUnitySceneState("preparing", unityActiveScene.name);
      }

      // ok to drop through
    }

    if (unitySceneState == "preparing") {
      foreach (Mq_System system in croquetSystems) {
        if (!system.ReadyToRunScene(targetSceneName)) return false;
      }

      return true;
    }

    return false;
  }

  void AdvanceBridgeStateWhenReady() {
    // go through the asynchronous steps involved in starting the bridge
    #if UNITY_EDITOR
      if (bridgeState == "needJSBuild") {
        SetBridgeState("waitingForJSBuild");
        WaitForJSBuild();
        return;
      }
    #endif

    if (bridgeState == "foundJSBuild") {
      SetBridgeState("waitingForConnection");
      Debug.Log($"==== [1] bridgeState==waitingForConnection | WebSocket is {( (clientSock == null)? "awaiting OnOpen()" : "<color=#44ff44>OPEN!</color>" )}");
      if (INTEROP_BRIDGE) {
        // nothing special to do; connection is already available
      } else {
        StartWS(); // will move on when socket has been set up
      }
    } else if (bridgeState == "waitingForConnection") {
      if (!INTEROP_BRIDGE && clientSock == null) return; // not ready yet

      // configure which logs are forwarded
      SetJSLogForwarding(JSLogForwarding.ToString());

      SetBridgeState("waitingForSessionName");

      // if we're not waiting for a menu to launch the session, set the session name immediately
      if (launchViaMenuIntoScene == "") SetSessionName(""); // use the default name
    }

    // allow to drop through (although we would catch a non-empty sessionName on the next update anyway).
    // only proceed if the JSToolsRecord is available - which in WebGL can take a
    // second or two.
    if (bridgeState == "waitingForSessionName") {
      Debug.Log($"sessionName: {sessionName}, JSToolsRecordReady: {Mq_Builder.JSToolsRecordReady()}");
      if (sessionName != "" && Mq_Builder.JSToolsRecordReady()) {
        SetBridgeState("waitingForSession");
        Debug.Log($"Call StartCroquetSession()");
        StartCroquetSession();
      }
    }
  }

  void AdvanceSceneStateIfNeeded() {
    if (croquetActiveScene == "") return; // nothing to do yet

    if (unitySceneState == "ready" || unitySceneState == "running") return; // everything's where it should be

    Scene unityActiveScene = SceneManager.GetActiveScene();
    if (unityActiveScene.name != croquetActiveScene) return; // not in the right scene yet

    // HandleSceneStateUpdated is responsible for ensuring that the scene that is currently active
    // in the Croquet session is loaded in this view.
    // here we take care of nudging that scene through any needed initialisation (which for
    // now mainly involves pre-loading any prefabs that are associated with the scene).  once ready,
    // we tell the Croquet session - with a full scene definition (including object placements)
    // if Croquet is in "preload" scene state, but otherwise just with the prefab details.
    if (IsSceneReadyToRun(croquetActiveScene)) {
      // Debug.Log($"ready to run scene \"{croquetActiveScene}\"");
      SetUnitySceneState("ready", croquetActiveScene);
      TellCroquetWeAreReadyForScene();

      foreach (Mq_System system in croquetSystems) {
        system.ClearSceneBeforeRunning();
      }
    }
  }

  #if UNITY_EDITOR
    void AdvanceHarvestStateWhenReady() {
      string sceneAndApp = sceneHarvestList[0];
      string sceneToHarvest = sceneAndApp.Split(':')[0];

      if (unitySceneState == "dormant") {
        EnsureUnityActiveScene(sceneToHarvest, false, false);
      }
      else if (IsSceneReadyToRun(sceneToHarvest)) {
        string appName = sceneAndApp.Split(':')[1];
        HarvestSceneDefinition(sceneToHarvest, appName);

        SetUnitySceneState("dormant", ""); // ready for next harvest scene, if any

        sceneHarvestList.RemoveAt(0);
        if (sceneHarvestList.Count == 0) {
          // all harvested
          WriteAllSceneDefinitions();
          EditorApplication.ExitPlaymode();
        }
      }
    }

    void HarvestSceneDefinition(string sceneName, string appName) {
      // the scene is ready.  get its definition.
      Log("session", $"ready to harvest \"{appName}\" scene \"{sceneName}\"");

      List<string> sceneStrings = new List<string>() {
        EarlySubscriptionTopicsAsString(),
        Mq_Entity_System.Instance.assetManifestString
      };
      sceneStrings.AddRange(GetSceneObjectStrings());
      string sceneFullString = string.Join('\x01', sceneStrings.ToArray());

      // in the list we interleave scene name and scene definition, for convenience of parsing the assembled file
      if (!sceneDefinitionsByApp.ContainsKey(appName)) sceneDefinitionsByApp[appName] = new List<string>();
      if (sceneFullString.Length > 0) {
        sceneDefinitionsByApp[appName].AddRange(new[] { sceneName, sceneFullString });
        // Log("session",$"definition of {sceneFullString.Length} chars for app {appName}");
      }
    }

    void WriteAllSceneDefinitions() {
      foreach (KeyValuePair<string, List<string>> appScenes in sceneDefinitionsByApp) {
        string app = appScenes.Key;
        string filePath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "MultisynqJS", app, "scene-definitions.txt"));
        List<string> sceneDefs = appScenes.Value;
        if (sceneDefs.Count > 0) {
          string appDefinitions = string.Join('\x02', sceneDefs.ToArray());
          File.WriteAllText(filePath, appDefinitions);

          // check that the write itself succeeded
          string definitionContents = File.ReadAllText(filePath).Trim(); // will throw if no file
          if (definitionContents == appDefinitions) {
            Debug.Log($"wrote definitions for app \"{app}\": {appDefinitions.Length:N0} chars in {filePath}");
          }
          else {
            Debug.LogError($"failed to write definitions for app \"{app}\"");
          }
        }
        else {
          // the app is mentioned in some scene(s), but no scene offered any definition
          // (i.e., no subscriptions, no manifests, and no object placements)
          if (File.Exists(filePath)) {
            Debug.Log($"removing previous scene-definition file for app \"{app}\"");
            File.Delete(filePath);
          }
        }
      }

      sceneDefinitionsByApp.Clear();
    }
  #endif // UNITY_EDITOR

  void TellCroquetWeAreReadyForScene() {
    if (croquetActiveSceneState == "preload") SendDefineScene();
    else SendReadyForScene();

    sceneDefinitionManifests.Clear(); // no longer needed
  }

  void SendDefineScene() {
    if (Mq_Entity_System.Instance==null) return;
    
    // Debug.Log($"sending defineScene for {SceneManager.GetActiveScene().name}");
    // args to the command across the bridge are
    //   scene name - if different from model's existing scene, init will always be accepted
    //   earlySubscriptionTopics
    //   assetManifests
    //   object string 1
    //   object string 2
    //   etc
    List<string> commandStrings = new List<string>() {
      "defineScene",
      SceneManager.GetActiveScene().name,
      EarlySubscriptionTopicsAsString(),
      Mq_Entity_System.Instance.assetManifestString
    };

    commandStrings.AddRange(GetSceneObjectStrings());

    // send the message directly (bypassing the deferred-message queue)
    string msg = String.Join('\x01', commandStrings.ToArray());
    SendMessageToJavaScript(msg);
  }

  List<string> GetSceneObjectStrings() {
    // return a definition string for each pre-included object that has a CroquetActorManifest and
    // is active in the editor
    List<string> definitionStrings = new List<string>();

    Dictionary<string, string> abbreviations = new Dictionary<string, string>();
    int objectCount = 0;
    int uncondensedLength = 0;
    int condensedLength = 0;
    foreach (Mq_ActorManifest manifest in sceneDefinitionManifests) {
      objectCount++;

      // the properties for actor.create() are sent as a string prop1:val1|prop2:val2...
      List<string> initStrings = new List<string>();
      initStrings.Add($"ACTOR:{manifest.defaultActorClass}");
      initStrings.Add($"type:{manifest.pawnType}");
      GameObject go = manifest.gameObject;
      foreach (Mq_System system in croquetSystems) {
        initStrings.AddRange(system.InitializationStringsForObject(go));
      }

      List<string> convertedStrings = new List<string>();
      foreach (string pair in initStrings) {
        uncondensedLength += pair.Length + 1; // assume a separator
        if (!abbreviations.ContainsKey(pair)) {
          abbreviations.Add(pair, $"${abbreviations.Count}");
          convertedStrings.Add(pair); // first and last time
        } else {
          convertedStrings.Add(abbreviations[pair]);
        }
      }
      string oneObject = String.Join('|', convertedStrings.ToArray());
      condensedLength += oneObject.Length;
      definitionStrings.Add(oneObject);

      Destroy(go); // now that we have what we need
    }

    if (objectCount == 0) {
      Log("session", $"no pre-placed objects found");
    }
    else {
      Log("session", $"{objectCount:N0} scene objects provided {uncondensedLength:N0} chars, encoded as {condensedLength:N0}");
    }

    return definitionStrings;
  }

  void SendReadyForScene() {
    // Debug.Log($"sending readyToRunScene for {SceneManager.GetActiveScene().name}");

    string sceneName = SceneManager.GetActiveScene().name;
    string[] command = new string[] {
      "readyToRunScene",
      sceneName
    };

    // send the message directly (bypassing the deferred-message queue)
    string msg = String.Join('\x01', command);
    SendMessageToJavaScript(msg);
  }

  void FixedUpdate() {
    long start = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // in case we'll be reporting to Croquet

    SendDeferredMessages();

    long duration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start;
    if (duration == 0) duration++;
    if (croquetSessionState == "running") {
      Measure("update", start.ToString(), duration.ToString());

      float now = Time.realtimeSinceStartup;
      if (now - lastMessageDiagnostics > 1f) {
        if (inBundleCount > 0 || inMessageCount > 0) {
          Log("diagnostics", $"from Croquet: {inMessageCount} messages with {inBundleCount} bundles ({Mathf.Round((float)inBundleDelayMS / inBundleCount)}ms avg delay) handled in {inProcessingMS}ms");
        }

        //Log("diagnostics", $"to Croquet: {outMessageCount} messages with {outBundleCount} bundles");
        lastMessageDiagnostics = now;
        inBundleCount = 0;
        inMessageCount = 0;
        inBundleDelayMS = 0; // long
        inProcessingMS = 0; // long
        outBundleCount = outMessageCount = 0;
      }
    }
  }

  void ProcessCroquetMessages() {
    long startMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    QueuedMessage qm;
    while (messageQueue.TryDequeue(out qm)) {
      long nowWhenQueued = qm.queueTime; // unixTimeMilliseconds
      long nowWhenDequeued = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      long queueDelay = nowWhenDequeued - nowWhenQueued;
      inBundleDelayMS += queueDelay;

      if (qm.isBinary) {
        byte[] rawData = qm.rawData;
        int sepPos = Array.IndexOf(rawData, (byte)5);
        // Debug.Log(BitConverter.ToString(rawData));
        if (sepPos >= 1) {
          byte[] timeAndCmdBytes = new byte[sepPos];
          Array.Copy(rawData, timeAndCmdBytes, sepPos);
          string[] strings = System.Text.Encoding.UTF8.GetString(timeAndCmdBytes).Split('\x02');
          string command = strings[1];
          ProcessCroquetMessage(command, rawData, sepPos + 1);

          long sendTime = long.Parse(strings[0]);
          long transmissionDelay = nowWhenQueued - sendTime;
          long nowAfterProcessing = DateTimeOffset.Now.ToUnixTimeMilliseconds();
          long processing = nowAfterProcessing - nowWhenDequeued;
          long totalTime = nowAfterProcessing - sendTime;
          string annotation = $"{rawData.Length - sepPos - 1} bytes. sock={transmissionDelay}ms, queue={queueDelay}ms, process={processing}ms";
          Measure("geom", sendTime.ToString(), totalTime.ToString(), annotation); // @@ assumed to be geometry
        }
        continue;
      }

      string nextMessage = qm.data;
      string[] messages = nextMessage.Split('\x02');
      if (messages.Length > 1) {
        // bundle of messages
        inBundleCount++;

        for (int i = 1; i < messages.Length; i++) ProcessCroquetMessage(messages[i]);

        // to measure message-processing performance, we gather
        //  JS now() when message was sent
        //  transmission delay (time until read and queued by C#)
        //  queue delay (time between queuing and dequeuing)
        //  processing time (time between dequeuing and completion)
        long sendTime = long.Parse(messages[0]); // first entry is just the JS Date.now() when sent
        long transmissionDelay = nowWhenQueued - sendTime;
        long nowAfterProcessing = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        long processing = nowAfterProcessing - nowWhenDequeued;
        long totalTime = nowAfterProcessing - sendTime;
        string annotation = $"{messages.Length - 1} msgs in {nextMessage.Length} chars. sock={transmissionDelay}ms, queue={queueDelay}ms, process={processing}ms";
        Measure("bundle", sendTime.ToString(), totalTime.ToString(), annotation);
      } else {
        // single message
        ProcessCroquetMessage(messages[0]);
      }
    }
    inProcessingMS += DateTimeOffset.Now.ToUnixTimeMilliseconds() - startMS;
  }

  /// <summary>
  /// Croquet String Message
  /// </summary>
  /// <param name="msg"></param>
  void ProcessCroquetMessage(string msg) {
    // a command message is an array of strings separated by \x01, of which the first is the command.
    // @@ splitting with ReadOnlySpan<char> would be a lot more heap-efficient.
    string[] strings = msg.Split('\x01');
    string command = strings[0]; // or a single piece of text, for logging
    string[] args = strings[1..];
    Log("verbose", command + ": " + String.Join(", ", args));

    if (command == "croquetPub") {
      ProcessCroquetPublish(args);
      return;
    }

    bool messageWasProcessed = false;

    foreach (Mq_System system in croquetSystems) {
      if (system.KnownCommands.Contains(command)) {
        system.ProcessCommand(command, args);
        messageWasProcessed = true;
      }
    }

    if      (command == "logFromJS")         HandleLogFromJS(args);
    else if (command == "croquetPing")       HandleCroquetPing(args[0]);
    else if (command == "setLogOptions")     SetCSharpLogOptions(args[0]);  //OUT:LOGGER
    else if (command == "setMeasureOptions") SetCSharpMeasureOptions(args[0]);//OUT:METRICS
    else if (command == "joinProgress")      HandleSessionJoinProgress(args[0]);
    else if (command == "sessionRunning")    HandleSessionRunning(args[0]);
    else if (command == "sceneStateUpdated") HandleSceneStateUpdated(args);
    else if (command == "sceneRunning")      HandleSceneRunning(args[0]);
    else if (command == "tearDownScene")     HandleSceneTeardown();
    else if (command == "tearDownSession")   HandleSessionTeardown(args[0]);
    else if (command == "croquetTime")       HandleCroquetReflectorTime(args[0]);
    else if (!messageWasProcessed) {
      // not a known command; maybe just text for logging
      Log("info", "Unhandled Command From Croquet: " + msg);
    }

    inMessageCount++;
  }

  /// <summary>
  /// Croquet Byte Message
  /// </summary>
  /// <param name="command"></param>
  /// <param name="data"></param>
  /// <param name="startIndex"></param>
  void ProcessCroquetMessage(string command, byte[] data, int startIndex) {
    // Debug.Log("ProcessCroquetMessage: " + command);
    foreach (Mq_System system in croquetSystems) {
      if (system.KnownCommands.Contains(command)) {
        system.ProcessCommand(command, data, startIndex);
        return;
      }
    }
  }

  public static void SubscribeToCroquetEvent(string scope, string eventName, Action<string> handler) {
    string topic = scope + ":" + eventName;
    if (!croquetSubscriptions.ContainsKey(topic)) {
      croquetSubscriptions[topic] = new List<(GameObject, Action<string>)>();
      if (Instance != null && Instance.unitySceneState == "running") {
        Instance.SendToCroquet("registerForEventTopic", topic); // subscribe to this topic
      }
    }
    croquetSubscriptions[topic].Add((null, handler));
  }

  public static void ListenForCroquetEvent(GameObject subscriber, string scope, string eventName, Action<string> handler) {
    // if this has been invoked before the object has its croquetActorId,
    // the scope will be an empty string.  in that case we still record the subscription,
    // but expect that FixUpEarlyListens will be invoked shortly to replace the
    // subscription with the correct (actor id) scope.

    string topic = scope + ":" + eventName;
    if (!croquetSubscriptions.ContainsKey(topic)) {
      croquetSubscriptions[topic] = new List<(GameObject, Action<string>)>();
    }

    if (!croquetSubscriptionsByGameObject.ContainsKey(subscriber)) {
      croquetSubscriptionsByGameObject[subscriber] = new HashSet<string>();
    }
    croquetSubscriptionsByGameObject[subscriber].Add(topic);

    croquetSubscriptions[topic].Add((subscriber, handler));
  }

  private string EarlySubscriptionTopicsAsString() {
    // gameObjects and scripts that start up before the Croquet view has been built are
    // allowed to request subscriptions to Croquet events.  when the bridge connection is
    // first made, we gather all existing subscriptions that have a null subscriber (i.e.,
    // are not pawn-specific Listens) and tell Croquet to be ready to send those events as
    // soon as the session starts.
    HashSet<string> topics = new HashSet<string>();
    foreach (string topic in croquetSubscriptions.Keys) {
      List<(GameObject, Action<string>)> subscriptions = croquetSubscriptions[topic];
      foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions) {
        if (sub.gameObject == null) {
          topics.Add(topic);
        }
      }
    }

    string joinedTopics = "";
    if (topics.Count > 0) {
      // Debug.Log($"sending {topics.Count} early-subscription topics");
      joinedTopics = string.Join(',', topics.ToArray());
    }
    return joinedTopics;
  }

  public static void UnsubscribeFromCroquetEvent(GameObject gameObject, string scope, string eventName,
    Action<string> forwarder) {
    // gameObject will be null for non-Listen subscriptions.
    // if gameObject is *not* null, we need to check whether the removal of this subscription
    // means that the topic can be removed from the list being listened to by this object.
    // that will be the case as long as there aren't subscriptions for the same gameObject and
    // same topic but with different handlers.
    string topic = scope + ":" + eventName;
    if (croquetSubscriptions.ContainsKey(topic)) {
      int remainingSubscriptionsForSameObject = 0;
      (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
      foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions) {
        if (sub.handler.Equals(forwarder)) {
          croquetSubscriptions[topic].Remove(sub);
          if (croquetSubscriptions[topic].Count == 0) {
            // no remaining subscriptions for this topic at all
            Debug.Log($"removed last subscription for {topic}");
            croquetSubscriptions.Remove(topic);
            if (Instance != null && Instance.unitySceneState == "running") {
              Instance.SendToCroquet("unregisterEventTopic", topic);
            }
          }
        } else if (gameObject != null && sub.gameObject.Equals(gameObject)) {
          remainingSubscriptionsForSameObject++;
        }
      }

      if (gameObject != null && remainingSubscriptionsForSameObject == 0) {
        Debug.Log($"removed {topic} from object's topic list");
        croquetSubscriptionsByGameObject[gameObject].Remove(topic);
      }
    }
  }

  public static void UnsubscribeFromCroquetEvent(string scope, string eventName, Action<string> forwarder) {
    UnsubscribeFromCroquetEvent(null, scope, eventName, forwarder);
  }

  public void FixUpEarlyListens(GameObject subscriber, string croquetActorId) {
    // in principle we could also use this as the time to send Say() events that were sent
    // before the actor id was known.  for now, those will just have been sent with
    // empty scopes (and therefore presumably ignored).
    if (croquetSubscriptionsByGameObject.ContainsKey(subscriber)) {
      // Debug.Log($"removing all subscriptions for {gameObject}");
      string[] allTopics = croquetSubscriptionsByGameObject[subscriber].ToArray(); // take a copy
      foreach (string topic in allTopics) {
        if (topic.StartsWith(':')) {
          // found a topic that was supposed to be a Listen.
          // go through and find the relevant subscriptions for this gameObject,
          // remove them, and make new subscriptions using the right scope.
          (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
          foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions) {
            if (sub.gameObject == subscriber) {
              string eventName = topic.Split(':')[1];
              // Debug.Log($"fixing up subscription to {eventName}");
              ListenForCroquetEvent(subscriber, croquetActorId, eventName, sub.handler);

              // then remove the dummy subscription
              croquetSubscriptions[topic].Remove(sub);
            }
          }

          // now remove the dummy topic from the subs by game object
          croquetSubscriptionsByGameObject[subscriber].Remove(topic);
        }
      }
    }
  }

  public void RemoveCroquetSubscriptionsFor(GameObject subscriber) {
    if (croquetSubscriptionsByGameObject.ContainsKey(subscriber)) {
      // Debug.Log($"removing all subscriptions for {gameObject}");
      foreach (string topic in croquetSubscriptionsByGameObject[subscriber]) {
        (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
        foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions) {
          if (sub.gameObject == subscriber) {
            croquetSubscriptions[topic].Remove(sub);
            if (croquetSubscriptions[topic].Count == 0) {
              // Debug.Log($"removed last subscription for {topic}");
              croquetSubscriptions.Remove(topic);
              if (unitySceneState == "running") {
                // don't even try to send if this is happening as part of a teardown
                SendToCroquet("unregisterEventTopic", topic);
              }
            }
          }
        }
      }

      croquetSubscriptionsByGameObject.Remove(subscriber);
    }
  }

  // might be useful at some point
  // void SimulateCroquetPublish(params string[] args)
  // {
  //     ProcessCroquetPublish(args);
  // }

  void ProcessCroquetPublish(string[] args) {
    // args are
    //   - scope
    //   - eventName
    //   - [optional]: arguments, encoded as a single string

    string scope = args[0];
    string eventName = args[1];
    string argString = args.Length > 2 ? args[2] : "";
    string topic = $"{scope}:{eventName}";
    if (croquetSubscriptions.ContainsKey(topic)) {
      foreach ((GameObject gameObject, Action<string> handler) sub in croquetSubscriptions[topic].ToArray()) { // take copy in case some mutating happens
        sub.handler(argString);
      }
    }
  }

  void HandleCroquetPing(string time) {
    Log("diagnostics", "PING");
    SendToCroquet("unityPong", time);
  }

  void HandleCroquetReflectorTime(string time) {
    // this code assumes that JS and C# share system time (Date.now and
    // DateTimeOffset.Now.ToUnixTimeMilliseconds).
    // these messages are sent once per second.
    long newEstimate = long.Parse(time);
    if (estimatedDateNowAtReflectorZero == -1) estimatedDateNowAtReflectorZero = newEstimate;
    else {
      long oldEstimate = estimatedDateNowAtReflectorZero;
      int ratio = 50; // weight (percent) for the incoming value
      estimatedDateNowAtReflectorZero =
        (ratio * newEstimate + (100 - ratio) * estimatedDateNowAtReflectorZero) / 100;
      if (Math.Abs(estimatedDateNowAtReflectorZero - oldEstimate) > 10) {
        Debug.Log($"CROQUET TIME CHANGE: {estimatedDateNowAtReflectorZero - oldEstimate}ms");
      }
    }
  }

  public float CroquetSessionTime() {
    if (estimatedDateNowAtReflectorZero == -1) return -1f;

    return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - estimatedDateNowAtReflectorZero) / 1000f;
  }

  void HandleLogFromJS(string[] args) {
    // args[0] is log type (log,warn,error)
    // args[1] is a best-effort single-string concatenation of whatever values were logged
    string type = args[0];
    string logText = args[1];
    switch (type) {
      case "log":
        Debug.Log("JS log: " + logText);
        break;
      case "warn":
        Debug.LogWarning("JS warning: " + logText);
        break;
      case "error":
        Debug.LogError("JS error: " + logText);
        break;
    }
  }

  void HandleSessionJoinProgress(string ratio) {
    if (unitySceneState == "running") return; // loading has finished; this is probably just a delayed message

    SetLoadingProgress(float.Parse(ratio));
  }

  void HandleSessionRunning(string viewId) {
    // this is dispatched from the Croquet session's PreloadingViewRoot constructor, telling us which
    // viewId we have in the session.
    // it will be sent the first time through, and also on recovery from a Croquet network glitch.
    croquetViewId = viewId;
    Debug.Log("==== [?????] Croquet session running!");
    Log("session", "Croquet session running!");
    SetBridgeState("started");
    croquetSessionState = "running";
    lastMessageDiagnostics = Time.realtimeSinceStartup;
    estimatedDateNowAtReflectorZero = -1; // reset, to accept first value from new view

    // when starting in a Unity editor, we ask Croquet to abandon whatever scene it had running
    // and (re)load whichever scene this view wants to play first.  the Croquet InitializationManager
    // will force a rebuild, and will expect this view to provide it (because this is the latest
    // editor to join the session).
    if (waitingForCroquetSceneReset) {
      string startupSceneName = launchViaMenuIntoScene == ""
        ? SceneManager.GetActiveScene().name
        : launchViaMenuIntoScene;
      Croquet.RequestToLoadScene(startupSceneName, true, true);
    }
  }

  void HandleSceneStateUpdated(string[] args) {
    // args are [activeScene, activeSceneState]

    // this is sent by the PreloadingViewRoot, under the following circs:
    // - on the view root's initial construction, forwarding the InitializationManager's current state (which could
    //   be anything, including presence of no scene at all when the session first starts)
    // - on every update in scene name, or of scene state (preload, loading, running)

    // the range of situations that this method deals with:
    //
    // a. this view has just started running in an editor, and we're waiting for the session to reset itself
    //    - we will have demanded (above) a reload and rebuild for our preferred startup scene.  we have
    //      nothing to do until Croquet tells us it's ready to preload (i.e., rebuild) that scene.
    // b. the Croquet session has no active scene
    //    - this view should propose to the Croquet session that it load our preferred startup scene
    // c. the Croquet session reports that a scene other than the current one is active
    //    - tell Unity to enter the named scene.  when we arrive there, we'll start preparing to run it
    // d. the Croquet session reports a return to "preload" for the scene we're already in
    //    - force a teardown of the scene, then reload it so we can provide a scene definition
    // e. none of the above
    //    - there is evidently an active scene in Croquet, and this view is in that scene.
    //      if unitySceneState is dormant, nudge it to waitingToPrepare so we can proceed.

    croquetActiveScene = args[0];
    croquetActiveSceneState = args[1];
    Log("session", $"Croquet scene \"{croquetActiveScene}\", state \"{croquetActiveSceneState}\"");

    Scene unityActiveScene = SceneManager.GetActiveScene();
    string startupSceneName = launchViaMenuIntoScene == ""
      ? unityActiveScene.name
      : launchViaMenuIntoScene;

    if (waitingForCroquetSceneReset) {
      // (a)
      // on session startup in a Unity editor, the first scene load is triggered in
      // HandleSessionRunning.  when the Croquet session reveals that it has arrived at preload
      // for that scene (whatever it was doing before), we can start preparing the scene here.
      // if the user later moves on to other scenes, and perhaps even comes back to this one,
      // we use the normal state-change handling below.
      if (croquetActiveScene == startupSceneName && croquetActiveSceneState == "preload") {
        waitingForCroquetSceneReset = false;
        EnsureUnityActiveScene(croquetActiveScene, false, false);
      }
      return; // nothing more to do here
    }

    if (croquetActiveScene == "") {
      // (b)
      // Croquet doesn't have an active scene, so propose loading our preferred startup scene.
      // note that once the Croquet session has started any scene, the active scene can change
      // but is never reset to "".
      Debug.Log($"No initial scene name set; requesting to load scene \"{startupSceneName}\"");
      Croquet.RequestToLoadScene(startupSceneName, false);
      return; // nothing more to do here
    }

    if (croquetActiveScene != unityActiveScene.name) {
      // (c)
      // Croquet is running a scene that we're not currently in.  head over there.
      EnsureUnityActiveScene(croquetActiveScene, false, true);
    } else if (croquetActiveSceneState == "preload") {
      // (d)
      // this view is in the scene that Croquet is running, but Croquet has done a full reset -
      // perhaps because an in-progress load attempt failed.
      // "preload" - rather than "loading", which we would see fleetingly on a normal scene reset -
      // implies that we need to force the scene to be reloaded (so we can gather a full scene
      // definition to offer to the session).
      // because in this situation the scene's Croquet view root might never have been built,
      // we won't necessarily have been sent a tearDownScene that would have cleared out any
      // Croquet system data already gathered from the scene.  so do that cleanup first.
      CleanUpSceneAndSystems();
      EnsureUnityActiveScene(croquetActiveScene, true, true);
    } else if (unitySceneState == "dormant") {
      // (e)
      // the Croquet session has an active scene, and we're currently in it, but for some reason
      // (e.g., due to a session glitch) our unity scene state was reset to dormant.  since we
      // know that there is a scene awaiting us, let AdvanceSceneStateIfNeeded get to work.
      SetUnitySceneState("waitingToPrepare", croquetActiveScene);
    }
  }

  public void RequestToLoadScene(string sceneName, bool forceReload, bool forceRebuild) {
    Debug.Log($"==== [2] Request to load scene {sceneName} (forceReload={forceReload}, forceRebuild={forceRebuild})");
    // ask Croquet to switch scene
    string[] cmdAndArgs = {
      "requestToLoadScene",
      sceneName,
      forceReload.ToString(),
      forceRebuild.ToString()
    };
    SendToCroquet(cmdAndArgs);
  }

  void EnsureUnityActiveScene(string targetSceneName, bool forceReload, bool showLoadProgress) {
    Scene unityActiveScene = SceneManager.GetActiveScene();
    if (forceReload || (unityActiveScene.name != targetSceneName && requestedSceneLoad != targetSceneName)) {
      string switchMsg = forceReload ? "forced reload of" : "switch to";
      Debug.Log($"{switchMsg} scene {targetSceneName}");

      SceneManager.LoadScene(targetSceneName);
      requestedSceneLoad = targetSceneName; // don't ask to load again
      skipNextFrameUpdate = true; // in case same scene is being loaded, make sure we don't check its "arrival" too soon

      if (showLoadProgress && loadingProgressDisplay != null && !loadingProgressDisplay.gameObject.activeSelf) {
        // we won't have a chance to set different loading stages.  run smoothly from 0 to 1,
        // and we'll probably have arrived.
        SetLoadingStage(1.0f, "Loading...");
      }
    }

    SetUnitySceneState("waitingToPrepare", targetSceneName);
  }

  void HandleSceneRunning(string sceneName) {
    // triggered by the startup of a GameRootView
    Log("session", $"Croquet view for scene {sceneName} running");
    if (loadingProgressDisplay != null) loadingProgressDisplay.Hide();
    SetUnitySceneState("running", sceneName); // we're off!
  }

  void HandleSceneTeardown() {
    // this is triggered by the PreloadingViewRoot when it destroys the game's running viewRoot as part
    // of a scene switch
    Log("session", "Croquet scene teardown");
    CleanUpSceneAndSystems();
  }

  void CleanUpSceneAndSystems() {
    // clear out the state of the current scene
    deferredMessages.Clear();
    SetUnitySceneState("dormant", ""); // ready to load the next scene, whichever it is
    requestedSceneLoad = "";
    foreach (Mq_System system in croquetSystems) {
      system.TearDownScene();
    }
  }

  void HandleSessionTeardown(string postTeardownScene) {
    // this is triggered by the disappearance (temporary or otherwise) of the Croquet session,
    // or the processing of a "shutdown" command sent from here.  in the latter case, we'll have
    // specified which scene is to be loaded locally in order to stay in the game.  typically a
    // menu scene.
    string postTeardownMsg = postTeardownScene == "" ? "" : $" (and jump to {postTeardownScene})";
    Log("session", $"Croquet session teardown{postTeardownMsg}");
    CleanUpSceneAndSystems(); // if there wasn't a scene viewroot yet, this won't have happened
    croquetSessionState = "stopped"; // suppresses sending of any further messages over the bridge
    foreach (Mq_System system in croquetSystems) {
      system.TearDownSession();
    }

    croquetViewId = "";
    croquetActiveScene = ""; // wait for session to resume and tell us the scene
    croquetActiveSceneState = "";

    if (postTeardownScene != "") {
      sessionName = ""; // the session has really gone
      SetBridgeState("waitingForSessionName"); // but the bridge presumably hasn't, so we're not back to square one

      int buildIndex = int.Parse(postTeardownScene);
      SceneManager.LoadScene(buildIndex);
    } else {
      // probably a glitch in the Croquet network connection.  should resume shortly.
      if (loadingProgressDisplay && !loadingProgressDisplay.gameObject.activeSelf) {
        SetLoadingStage(0.5f, "Reconnecting...");
      }
    }
  }

  void HandleViewCount(float viewCount) {
    croquetViewCount = (int)viewCount;
  }

  // OUT: Logger Util
  void SetCSharpLogOptions(string options) {
    // logs that the Croquet side wants the C# side to send.
    // arg is a comma-separated list of the log categories to show
    string[] wanted = options.Split(',');
    foreach (string cat in logCategories) {
      logOptions[cat] = wanted.Contains(cat);
    }

    // and display options
    logOptions["routeToCroquet"] = wanted.Contains("routeToCroquet");
  }

  // OUT: Metrics system util
  void SetCSharpMeasureOptions(string options) {
    // arg is a comma-separated list of the measure categories (currently
    // available are bundle,geom,update) to send to Croquet to appear as
    // marks in a Chrome performance plot.

    string[] wanted = options.Split(',');
    foreach (string cat in measureCategories) {
      measureOptions[cat] = wanted.Contains(cat);
    }
  }

  void SetJSLogForwarding(string optionString) {
    // first arg is a comma-separated list of the log types (log,warn,error) that we want
    // the JS side to send for logging here
    // second is a stringified boolean of debugUsingExternalSession
    string[] cmdAndArgs = { "setJSLogForwarding", optionString, croquetRunner.debugUsingExternalSession.ToString() };

    // send the message directly (bypassing the deferred-message queue), because this can
    // be sent regardless of whether a session is running
    string msg = String.Join('\x01', cmdAndArgs);
    SendMessageToJavaScript(msg);
  }

  void SetLoadingStage(float ratio, string msg) {
    if (loadingProgressDisplay == null) return;

    loadingProgressDisplay.Show(); // make sure it's visible
    loadingProgressDisplay.SetProgress(ratio, msg);
  }

  void SetLoadingProgress(float loadRatio) {
    if (loadingProgressDisplay == null) return;

    // fast-forward progress 0=>1 is mapped onto bar 50=>100%
    float barRatio = loadRatio * 0.5f + 0.5f;
    loadingProgressDisplay.Show(); // make sure it's visible (especially on a reload)
    loadingProgressDisplay.SetProgress(barRatio, $"Loading... ({loadRatio * 100:#0.0}%)");
  }

  // OUT: Logging System
  public void Log(string category, string msg) {
    bool loggable;
    if (logOptions.TryGetValue(category, out loggable) && loggable) {
      string logString = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds() % 100000}: {msg}";
      if (logOptions.TryGetValue("routeToCroquet", out loggable) && loggable) {
        SendToCroquet("log", logString);
      } else {
        Debug.Log(logString);
      }
    }
  }

  // OUT metrics system
  void Measure(params string[] strings) {
    string category = strings[0];
    bool loggable;
    if (measureOptions.TryGetValue(category, out loggable) && loggable) {
      string[] cmdString = { "measure" };
      string[] cmdAndArgs = cmdString.Concat(strings).ToArray();
      SendToCroquet(cmdAndArgs);
    }
  }

}


[System.Serializable]
public class CroquetDebugTypes {
  public bool session;
  public bool messages;
  public bool sends;
  public bool snapshot;
  public bool data;
  public bool hashing;
  public bool subscribe;
  public bool classes;
  public bool ticks;
  public bool write;

  public override string ToString() {
    List<string> flags = new List<string>();
    if (session) flags.Add("session");
    if (messages) flags.Add("messages");
    if (sends) flags.Add("sends");
    if (snapshot) flags.Add("snapshot");
    if (data) flags.Add("data");
    if (hashing) flags.Add("hashing");
    if (subscribe) flags.Add("subscribe");
    if (classes) flags.Add("classes");
    if (ticks) flags.Add("ticks");
    if (write) flags.Add("write");
    
    return string.Join(',', flags.ToArray());
  }
}

[System.Serializable]
public class CroquetLogForwarding {
  public bool log = false;
  public bool warn = true;
  public bool error = true;

  public override string ToString() {
    List<string> flags = new List<string>();
    if (log) flags.Add("log");
    if (warn) flags.Add("warn");
    if (error) flags.Add("error");

    return string.Join(',', flags.ToArray());
  }
}

}