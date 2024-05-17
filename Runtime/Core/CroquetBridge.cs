using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using WebSocketSharp;
using WebSocketSharp.Server;
using WebSocketSharp.Net;
using System.Runtime.InteropServices;
public class CroquetBridge : MonoBehaviour
{
    #region Public
    public CroquetSettings appProperties;

    [Header("Session Configuration")]
    public string appName;
    public string defaultSessionName = "ABCDE";
    public string launchViaMenuIntoScene = "";
    public CroquetDebugTypes croquetDebugLogging;
    public CroquetLogForwarding JSLogForwarding;

    [Header("Session State")]
    public string croquetSessionState = "stopped";
    public string sessionName = "";
    public string croquetViewId;
    public int croquetViewCount;
    public string croquetActiveScene;
    public string croquetActiveSceneState;
    public string unitySceneState = "dormant";

    [Header("Network Glitch Simulator")]
    public bool triggerGlitchNow = false;
    public float glitchDuration = 3.0f;
    public CroquetSystem[] croquetSystems = new CroquetSystem[0];

    #endregion

    #region PRIVATE
    private List<CroquetActorManifest> sceneDefinitionManifests = new List<CroquetActorManifest>();
    private List<string> sceneHarvestList = new List<string>();
    private Dictionary<string, List<string>> sceneDefinitionsByApp = new Dictionary<string, List<string>>();
    private static string bridgeState = "stopped";
    private bool waitingForCroquetSceneReset = false;
    private string requestedSceneLoad = "";
    private bool skipNextFrameUpdate = false;

    private static WebSocket clientSock = null;
    public class QueuedMessage
    {
        public long queueTime;
        public bool isBinary;
        public byte[] rawData;
        public string data;
    }

    static ConcurrentQueue<QueuedMessage> messageQueue = new ConcurrentQueue<QueuedMessage>();
    static long estimatedDateNowAtReflectorZero = -1;

    List<(string, string)> deferredMessages = new List<(string, string)>();
    LoadingProgressDisplay loadingProgressDisplay;
    public static CroquetBridge Instance { get; private set; }
    private CroquetRunner croquetRunner;
    private static Dictionary<string, List<(GameObject, Action<string>)>> croquetSubscriptions = new Dictionary<string, List<(GameObject, Action<string>)>>();
    private static Dictionary<GameObject, HashSet<string>> croquetSubscriptionsByGameObject = new Dictionary<GameObject, HashSet<string>>();

    Dictionary<string, bool> logOptions = new Dictionary<string, bool>();
    static string[] logCategories = new string[] { "info", "session", "diagnostics", "debug", "verbose" };
    Dictionary<string, bool> measureOptions = new Dictionary<string, bool>();
    static string[] measureCategories = new string[] { "update", "bundle", "geom" };

    int outMessageCount = 0;
    int outBundleCount = 0;
    int inBundleCount = 0;
    int inMessageCount = 0;
    long inBundleDelayMS = 0;
    long inProcessingMS = 0;
    float lastMessageDiagnostics;

    #endregion

    private void SetBridgeState(string state)
    {
        bridgeState = state;
        Log("session", $"bridge state: {bridgeState}");
    }

    private void SetUnitySceneState(string state, string sceneName)
    {
        unitySceneState = state;
    }


    // Import functions from the JavaScript library
    [DllImport("__Internal")]
    private static extern void CroquetBridge_Init();

    [DllImport("__Internal")]
    private static extern void CroquetBridge_Connect(string url);

    [DllImport("__Internal")]
    private static extern void CroquetBridge_Disconnect();

    [DllImport("__Internal")]
    private static extern void CroquetBridge_SendMessage(string message);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            Application.runInBackground = true;

            SetCSharpLogOptions("info,session");
            SetCSharpMeasureOptions("bundle");
            croquetRunner = gameObject.GetComponent<CroquetRunner>();

#if UNITY_WEBGL && !UNITY_EDITOR
            CroquetBridge_Init();
            CroquetBridge_Connect("ws://127.0.0.1:9000/Bridge");
#else
            StartWS();
#endif

            DontDestroyOnLoad(gameObject);
            croquetSystems = gameObject.GetComponents<CroquetSystem>();
            Croquet.Subscribe("croquet", "viewCount", HandleViewCount);
        }
    }    
    void Start()
    {
        Application.targetFrameRate = 60;
        LoadingProgressDisplay loadingObj = FindObjectOfType<LoadingProgressDisplay>();
        if (loadingObj != null)
        {
            DontDestroyOnLoad(loadingObj.gameObject);
            loadingProgressDisplay = loadingObj.GetComponent<LoadingProgressDisplay>();
            loadingProgressDisplay.Hide();
        }
    }

#if UNITY_EDITOR
    private async void WaitForJSBuild()
    {
        bool success = await CroquetBuilder.EnsureJSToolsAvailable() && CroquetBuilder.EnsureJSBuildAvailableToPlay();
        if (!success)
        {
            EditorApplication.ExitPlaymode();
            return;
        }
        SetBridgeState("foundJSBuild");
    }
#endif

    public void SetSessionName(string newSessionName)
    {
        if (croquetRunner.runOffline)
        {
            sessionName = "offline";
            Debug.LogWarning("session name overridden for offline run");
        }
        else if (newSessionName == "")
        {
            sessionName = defaultSessionName;
            if (sessionName == "")
            {
                Debug.LogWarning("Attempt to start Croquet with a default sessionName, but no default has been set. Falling back to \"unnamed\".");
                sessionName = "unnamed";
            }
            else
            {
                Log("session", $"session name defaulted to {defaultSessionName}");
            }
        }
        else
        {
            sessionName = newSessionName;
            Log("session", $"session name set to {newSessionName}");
        }
    }

    private void ArrivedInGameScene(Scene currentScene)
    {
        sceneDefinitionManifests.Clear();
        CroquetActorManifest[] croquetObjects = FindObjectsByType<CroquetActorManifest>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (CroquetActorManifest manifest in croquetObjects)
        {
            GameObject go = manifest.gameObject;
            if (go.activeSelf)
            {
                sceneDefinitionManifests.Add(manifest);
                go.SetActive(false);
            }
            else
            {
                Destroy(go);
            }
        }

        foreach (CroquetSystem system in croquetSystems)
        {
            system.LoadedScene(currentScene.name);
        }
    }

    private void OnDestroy()
    {
        if (clientSock != null)
        {
            clientSock.Close();
        }
    }

    void StartWS()
    {
        int port = appProperties.preferredPort;
        int remainingTries = 9;
        bool goodPortFound = false;
        while (!goodPortFound && remainingTries > 0)
        {
            try
            {
                clientSock = new WebSocket($"ws://127.0.0.1:{port}/Bridge");
                clientSock.OnMessage += (sender, e) => HandleMessage(e.Data);
                clientSock.OnOpen += (sender, e) => {
                    SetJSLogForwarding(JSLogForwarding.ToString());
                    SetBridgeState("waitingForSessionName");
                    if (launchViaMenuIntoScene == "") SetSessionName("");
                };
                clientSock.OnClose += (sender, e) => {
                    Log("session", "server socket closed");
                };
                clientSock.Connect();
                goodPortFound = true;
            }
            catch (Exception e)
            {
                Debug.Log($"Port {port} is not available");
                Log("debug", $"Error on trying port {port}: {e}");
                port++;
                remainingTries--;
            }
        }

        if (!goodPortFound)
        {
            Debug.LogError("Cannot find an available port for the Croquet bridge");
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#endif
            return;
        }

        StartCoroutine(croquetRunner.StartCroquetConnection(port, appName, true, ""));
    }

    public void SendToCroquet(params string[] strings)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CroquetBridge_SendMessage(PackCroquetMessage(strings));
#else
        if (croquetSessionState != "running")
        {
            Debug.LogWarning($"attempt to send when Croquet session is not running: {string.Join(',', strings)}");
            return;
        }
        deferredMessages.Add(("", PackCroquetMessage(strings)));
#endif
    }

    public void SendToCroquetSync(params string[] strings)
    {
        SendToCroquet(strings);
    }

    public void SendThrottledToCroquet(string throttleId, params string[] strings)
    {
        int i = 0;
        int foundIndex = -1;
        foreach ((string throttle, string msg) entry in deferredMessages)
        {
            if (entry.throttle == throttleId)
            {
                foundIndex = i;
                break;
            }
            i++;
        }
        if (foundIndex != -1) deferredMessages.RemoveAt(i);
        deferredMessages.Add((throttleId, PackCroquetMessage(strings)));
    }

    public string PackCroquetMessage(string[] strings)
    {
        return String.Join('\x01', strings);
    }

    void SendDeferredMessages()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL-specific deferred message handling
#else
        if (clientSock == null || clientSock.ReadyState != WebSocketState.Open) return;

        if (deferredMessages.Count == 0)
        {
            clientSock.Send("tick");
            return;
        }

        outBundleCount++;
        outMessageCount += deferredMessages.Count;

        deferredMessages.Insert(0, ("", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()));
        List<string> messageContents = new List<string>();
        foreach ((string throttle, string msg) entry in deferredMessages)
        {
            messageContents.Add(entry.msg);
        }
        string[] msgs = messageContents.ToArray<string>();
        clientSock.Send(String.Join('\x02', msgs));
        deferredMessages.Clear();
#endif
    }

    void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL-specific update logic
#else
        if (skipNextFrameUpdate)
        {
            skipNextFrameUpdate = false;
            return;
        }

#if UNITY_EDITOR
        if (sceneHarvestList.Count > 0)
        {
            AdvanceHarvestStateWhenReady();
            return;
        }
#endif

        ProcessCroquetMessages();

        if (bridgeState != "started") AdvanceBridgeStateWhenReady();
        else if (croquetSessionState == "running")
        {
            AdvanceSceneStateIfNeeded();

            if (triggerGlitchNow)
            {
                triggerGlitchNow = false;
                int milliseconds = (int)(glitchDuration * 1000f);
                if (milliseconds > 0)
                {
                    SendToCroquetSync("simulateNetworkGlitch", milliseconds.ToString());
                }
            }
        }
#endif
    }

    void ProcessCroquetMessages()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL-specific message processing
#else
        long startMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        QueuedMessage qm;
        while (messageQueue.TryDequeue(out qm))
        {
            long nowWhenQueued = qm.queueTime;
            long nowWhenDequeued = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long queueDelay = nowWhenDequeued - nowWhenQueued;
            inBundleDelayMS += queueDelay;

            if (qm.isBinary)
            {
                byte[] rawData = qm.rawData;
                int sepPos = Array.IndexOf(rawData, (byte) 5);
                if (sepPos >= 1)
                {
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
                    Measure("geom", sendTime.ToString(), totalTime.ToString(), annotation);
                }
                continue;
            }

            string nextMessage = qm.data;
            string[] messages = nextMessage.Split('\x02');
            if (messages.Length > 1)
            {
                inBundleCount++;
                for (int i = 1; i < messages.Length; i++) ProcessCroquetMessage(messages[i]);

                long sendTime = long.Parse(messages[0]);
                long transmissionDelay = nowWhenQueued - sendTime;
                long nowAfterProcessing = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long processing = nowAfterProcessing - nowWhenDequeued;
                long totalTime = nowAfterProcessing - sendTime;
                string annotation = $"{messages.Length - 1} msgs in {nextMessage.Length} chars. sock={transmissionDelay}ms, queue={queueDelay}ms, process={processing}ms";
                Measure("bundle", sendTime.ToString(), totalTime.ToString(), annotation);
            }
            else
            {
                ProcessCroquetMessage(messages[0]);
            }
        }
        inProcessingMS += DateTimeOffset.Now.ToUnixTimeMilliseconds() - startMS;
#endif
    }
    bool IsSceneReadyToRun(string targetSceneName)
    {
        if (unitySceneState == "waitingToPrepare")
        {
            Scene unityActiveScene = SceneManager.GetActiveScene();
            if (unityActiveScene.name == targetSceneName && (requestedSceneLoad == "" || requestedSceneLoad == targetSceneName))
            {
                requestedSceneLoad = "";
                ArrivedInGameScene(unityActiveScene);
                SetUnitySceneState("preparing", unityActiveScene.name);
            }
        }

        if (unitySceneState == "preparing")
        {
            foreach (CroquetSystem system in croquetSystems)
            {
                if (!system.ReadyToRunScene(targetSceneName)) return false;
            }
            return true;
        }

        return false;
    }

    void AdvanceBridgeStateWhenReady()
    {
#if UNITY_EDITOR
        if (bridgeState == "needJSBuild")
        {
            SetBridgeState("waitingForJSBuild");
            WaitForJSBuild();
            return;
        }
#endif

        if (bridgeState == "foundJSBuild")
        {
            SetBridgeState("waitingForSocket");
            StartWS();
        }
        else if (bridgeState == "waitingForSocket" && clientSock != null)
        {
            SetJSLogForwarding(JSLogForwarding.ToString());
            SetBridgeState("waitingForSessionName");
            if (launchViaMenuIntoScene == "") SetSessionName("");
        }
        if (bridgeState == "waitingForSessionName" && sessionName != "")
        {
            SetBridgeState("waitingForSession");
            StartCroquetSession();
        }
    }

    void AdvanceSceneStateIfNeeded()
    {
        if (croquetActiveScene == "") return;

        if (unitySceneState == "ready" || unitySceneState == "running") return;

        Scene unityActiveScene = SceneManager.GetActiveScene();
        if (unityActiveScene.name != croquetActiveScene) return;

        if (IsSceneReadyToRun(croquetActiveScene))
        {
            SetUnitySceneState("ready", croquetActiveScene);
            TellCroquetWeAreReadyForScene();

            foreach (CroquetSystem system in croquetSystems)
            {
                system.ClearSceneBeforeRunning();
            }
        }
    }

#if UNITY_EDITOR
    void AdvanceHarvestStateWhenReady()
    {
        string sceneAndApp = sceneHarvestList[0];
        string sceneToHarvest = sceneAndApp.Split(':')[0];

        if (unitySceneState == "dormant")
        {
            EnsureUnityActiveScene(sceneToHarvest, false, false);
        }
        else if (IsSceneReadyToRun(sceneToHarvest))
        {
            string appName = sceneAndApp.Split(':')[1];
            HarvestSceneDefinition(sceneToHarvest, appName);

            SetUnitySceneState("dormant", "");
            sceneHarvestList.RemoveAt(0);
            if (sceneHarvestList.Count == 0)
            {
                WriteAllSceneDefinitions();
                EditorApplication.ExitPlaymode();
            }
        }
    }

    void HarvestSceneDefinition(string sceneName, string appName)
    {
        Log("session", $"ready to harvest \"{appName}\" scene \"{sceneName}\"");

        List<string> sceneStrings = new List<string>() {
            EarlySubscriptionTopicsAsString(),
            CroquetEntitySystem.Instance.assetManifestString
        };
        sceneStrings.AddRange(GetSceneObjectStrings());
        string sceneFullString = string.Join('\x01', sceneStrings.ToArray());

        if (!sceneDefinitionsByApp.ContainsKey(appName)) sceneDefinitionsByApp[appName] = new List<string>();
        if (sceneFullString.Length > 0)
        {
            sceneDefinitionsByApp[appName].AddRange(new[] { sceneName, sceneFullString });
        }
    }

    void WriteAllSceneDefinitions()
    {
        foreach(KeyValuePair<string, List<string>> appScenes in sceneDefinitionsByApp)
        {
            string app = appScenes.Key;
            string filePath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS", app, "scene-definitions.txt"));
            List<string> sceneDefs = appScenes.Value;
            if (sceneDefs.Count > 0)
            {
                string appDefinitions = string.Join('\x02', sceneDefs.ToArray());
                File.WriteAllText(filePath, appDefinitions);

                string definitionContents = File.ReadAllText(filePath).Trim();
                if (definitionContents == appDefinitions)
                {
                    Debug.Log($"wrote definitions for app \"{app}\": {appDefinitions.Length:N0} chars in {filePath}");
                }
                else
                {
                    Debug.LogError($"failed to write definitions for app \"{app}\"");
                }
            }
            else
            {
                if (File.Exists(filePath))
                {
                    Debug.Log($"removing previous scene-definition file for app \"{app}\"");
                    File.Delete(filePath);
                }
            }
        }
        sceneDefinitionsByApp.Clear();
    }
#endif

    void TellCroquetWeAreReadyForScene()
    {
        if (croquetActiveSceneState == "preload") SendDefineScene();
        else SendReadyForScene();
        sceneDefinitionManifests.Clear();
    }

    void SendDefineScene()
    {
        List<string> commandStrings = new List<string>() {
            "defineScene",
            SceneManager.GetActiveScene().name,
            EarlySubscriptionTopicsAsString(),
            CroquetEntitySystem.Instance.assetManifestString
        };
        commandStrings.AddRange(GetSceneObjectStrings());
        string msg = String.Join('\x01', commandStrings.ToArray());
        clientSock.Send(msg);
    }

    List<string> GetSceneObjectStrings()
    {
        List<string> definitionStrings = new List<string>();

        Dictionary<string, string> abbreviations = new Dictionary<string, string>();
        int objectCount = 0;
        int uncondensedLength = 0;
        int condensedLength = 0;
        foreach (CroquetActorManifest manifest in sceneDefinitionManifests)
        {
            objectCount++;
            List<string> initStrings = new List<string>();
            initStrings.Add($"ACTOR:{manifest.defaultActorClass}");
            initStrings.Add($"type:{manifest.pawnType}");
            GameObject go = manifest.gameObject;
            foreach (CroquetSystem system in croquetSystems)
            {
                initStrings.AddRange(system.InitializationStringsForObject(go));
            }

            List<string> convertedStrings = new List<string>();
            foreach (string pair in initStrings)
            {
                uncondensedLength += pair.Length + 1;
                if (!abbreviations.ContainsKey(pair))
                {
                    abbreviations.Add(pair, $"${abbreviations.Count}");
                    convertedStrings.Add(pair);
                }
                else
                {
                    convertedStrings.Add(abbreviations[pair]);
                }
            }
            string oneObject = String.Join('|', convertedStrings.ToArray());
            condensedLength += oneObject.Length;
            definitionStrings.Add(oneObject);
            Destroy(go);
        }

        if (objectCount == 0)
        {
            Log("session", $"no pre-placed objects found");
        }
        else
        {
            Log("session", $"{objectCount:N0} scene objects provided {uncondensedLength:N0} chars, encoded as {condensedLength:N0}");
        }

        return definitionStrings;
    }

    void SendReadyForScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string[] command = new string[] { "readyToRunScene", sceneName };
        string msg = String.Join('\x01', command);
        clientSock.Send(msg);
    }

    void FixedUpdate()
    {
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        SendDeferredMessages();

        long duration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start;
        if (duration == 0) duration++;
        if (croquetSessionState == "running")
        {
            Measure("update", start.ToString(), duration.ToString());

            float now = Time.realtimeSinceStartup;
            if (now - lastMessageDiagnostics > 1f)
            {
                if (inBundleCount > 0 || inMessageCount > 0)
                {
                    Log("diagnostics", $"from Croquet: {inMessageCount} messages with {inBundleCount} bundles ({Mathf.Round((float)inBundleDelayMS / inBundleCount)}ms avg delay) handled in {inProcessingMS}ms");
                }
                lastMessageDiagnostics = now;
                inBundleCount = 0;
                inMessageCount = 0;
                inBundleDelayMS = 0;
                inProcessingMS = 0;
                outBundleCount = outMessageCount = 0;
            }
        }
    }

    void ProcessCroquetMessage(string msg)
    {
        string[] strings = msg.Split('\x01');
        string command = strings[0];
        string[] args = strings[1..];
        Log("verbose", command + ": " + String.Join(", ", args));

        if (command == "croquetPub")
        {
            ProcessCroquetPublish(args);
            return;
        }

        bool messageWasProcessed = false;

        foreach (CroquetSystem system in croquetSystems)
        {
            if (system.KnownCommands.Contains(command))
            {
                system.ProcessCommand(command, args);
                messageWasProcessed = true;
            }
        }

        if (command == "logFromJS") HandleLogFromJS(args);
        else if (command == "croquetPing") HandleCroquetPing(args[0]);
        else if (command == "setLogOptions") SetCSharpLogOptions(args[0]);
        else if (command == "setMeasureOptions") SetCSharpMeasureOptions(args[0]);
        else if (command == "joinProgress") HandleSessionJoinProgress(args[0]);
        else if (command == "sessionRunning") HandleSessionRunning(args[0]);
        else if (command == "sceneStateUpdated") HandleSceneStateUpdated(args);
        else if (command == "sceneRunning") HandleSceneRunning(args[0]);
        else if (command == "tearDownScene") HandleSceneTeardown();
        else if (command == "tearDownSession") HandleSessionTeardown(args[0]);
        else if (command == "croquetTime") HandleCroquetReflectorTime(args[0]);
        else if (!messageWasProcessed)
        {
            Log("info", "Unhandled Command From Croquet: " + msg);
        }
        inMessageCount++;
    }

    void ProcessCroquetMessage(string command, byte[] data, int startIndex)
    {
        foreach (CroquetSystem system in croquetSystems)
        {
            if (system.KnownCommands.Contains(command))
            {
                system.ProcessCommand(command, data, startIndex);
                return;
            }
        }
    }

    public static void SubscribeToCroquetEvent(string scope, string eventName, Action<string> handler)
    {
        string topic = scope + ":" + eventName;
        if (!croquetSubscriptions.ContainsKey(topic))
        {
            croquetSubscriptions[topic] = new List<(GameObject, Action<string>)>();
            if (Instance != null && Instance.unitySceneState == "running")
            {
                Instance.SendToCroquet("registerForEventTopic", topic);
            }
        }
        croquetSubscriptions[topic].Add((null, handler));
    }

    public static void ListenForCroquetEvent(GameObject subscriber, string scope, string eventName, Action<string> handler)
    {
        string topic = scope + ":" + eventName;
        if (!croquetSubscriptions.ContainsKey(topic))
        {
            croquetSubscriptions[topic] = new List<(GameObject, Action<string>)>();
        }

        if (!croquetSubscriptionsByGameObject.ContainsKey(subscriber))
        {
            croquetSubscriptionsByGameObject[subscriber] = new HashSet<string>();
        }
        croquetSubscriptionsByGameObject[subscriber].Add(topic);

        croquetSubscriptions[topic].Add((subscriber, handler));
    }

    private string EarlySubscriptionTopicsAsString()
    {
        HashSet<string> topics = new HashSet<string>();
        foreach (string topic in croquetSubscriptions.Keys)
        {
            List<(GameObject, Action<string>)> subscriptions = croquetSubscriptions[topic];
            foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions)
            {
                if (sub.gameObject == null)
                {
                    topics.Add(topic);
                }
            }
        }

        string joinedTopics = "";
        if (topics.Count > 0)
        {
            joinedTopics = string.Join(',', topics.ToArray());
        }
        return joinedTopics;
    }

    public static void UnsubscribeFromCroquetEvent(GameObject gameObject, string scope, string eventName, Action<string> forwarder)
    {
        string topic = scope + ":" + eventName;
        if (croquetSubscriptions.ContainsKey(topic))
        {
            int remainingSubscriptionsForSameObject = 0;
            (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
            foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions)
            {
                if (sub.handler.Equals(forwarder))
                {
                    croquetSubscriptions[topic].Remove(sub);
                    if (croquetSubscriptions[topic].Count == 0)
                    {
                        Debug.Log($"removed last subscription for {topic}");
                        croquetSubscriptions.Remove(topic);
                        if (Instance != null && Instance.unitySceneState == "running")
                        {
                            Instance.SendToCroquet("unregisterEventTopic", topic);
                        }
                    }
                }
                else if (gameObject != null && sub.gameObject.Equals(gameObject))
                {
                    remainingSubscriptionsForSameObject++;
                }
            }

            if (gameObject != null && remainingSubscriptionsForSameObject == 0)
            {
                Debug.Log($"removed {topic} from object's topic list");
                croquetSubscriptionsByGameObject[gameObject].Remove(topic);
            }
        }
    }

    public static void UnsubscribeFromCroquetEvent(string scope, string eventName, Action<string> forwarder)
    {
        UnsubscribeFromCroquetEvent(null, scope, eventName, forwarder);
    }

    public void FixUpEarlyListens(GameObject subscriber, string croquetActorId)
    {
        if (croquetSubscriptionsByGameObject.ContainsKey(subscriber))
        {
            string[] allTopics = croquetSubscriptionsByGameObject[subscriber].ToArray();
            foreach (string topic in allTopics)
            {
                if (topic.StartsWith(':'))
                {
                    (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
                    foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions)
                    {
                        if (sub.gameObject == subscriber)
                        {
                            string eventName = topic.Split(':')[1];
                            ListenForCroquetEvent(subscriber, croquetActorId, eventName, sub.handler);
                            croquetSubscriptions[topic].Remove(sub);
                        }
                    }
                    croquetSubscriptionsByGameObject[subscriber].Remove(topic);
                }
            }
        }
    }

    public void RemoveCroquetSubscriptionsFor(GameObject subscriber)
    {
        if (croquetSubscriptionsByGameObject.ContainsKey(subscriber))
        {
            foreach (string topic in croquetSubscriptionsByGameObject[subscriber])
            {
                (GameObject, Action<string>)[] subscriptions = croquetSubscriptions[topic].ToArray();
                foreach ((GameObject gameObject, Action<string> handler) sub in subscriptions)
                {
                    if (sub.gameObject == subscriber)
                    {
                        croquetSubscriptions[topic].Remove(sub);
                        if (croquetSubscriptions[topic].Count == 0)
                        {
                            croquetSubscriptions.Remove(topic);
                            if (unitySceneState == "running")
                            {
                                SendToCroquet("unregisterEventTopic", topic);
                            }
                        }
                    }
                }
            }
            croquetSubscriptionsByGameObject.Remove(subscriber);
        }
    }

    void ProcessCroquetPublish(string[] args)
    {
        string scope = args[0];
        string eventName = args[1];
        string argString = args.Length > 2 ? args[2] : "";
        string topic = $"{scope}:{eventName}";
        if (croquetSubscriptions.ContainsKey(topic))
        {
            foreach ((GameObject gameObject, Action<string> handler) sub in croquetSubscriptions[topic].ToArray())
            {
                sub.handler(argString);
            }
        }
    }

    void HandleCroquetPing(string time)
    {
        Log("diagnostics", "PING");
        SendToCroquet("unityPong", time);
    }

    void HandleCroquetReflectorTime(string time)
    {
        long newEstimate = long.Parse(time);
        if (estimatedDateNowAtReflectorZero == -1) estimatedDateNowAtReflectorZero = newEstimate;
        else
        {
            long oldEstimate = estimatedDateNowAtReflectorZero;
            int ratio = 50;
            estimatedDateNowAtReflectorZero = (ratio * newEstimate + (100 - ratio) * estimatedDateNowAtReflectorZero) / 100;
            if (Math.Abs(estimatedDateNowAtReflectorZero - oldEstimate) > 10)
            {
                Debug.Log($"CROQUET TIME CHANGE: {estimatedDateNowAtReflectorZero - oldEstimate}ms");
            }
        }
    }

    public float CroquetSessionTime()
    {
        if (estimatedDateNowAtReflectorZero == -1) return -1f;
        return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - estimatedDateNowAtReflectorZero) / 1000f;
    }

    void HandleLogFromJS(string[] args)
    {
        string type = args[0];
        string logText = args[1];
        switch (type)
        {
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

    void HandleSessionJoinProgress(string ratio)
    {
        if (unitySceneState == "running") return;
        SetLoadingProgress(float.Parse(ratio));
    }

    void HandleSessionRunning(string viewId)
    {
        croquetViewId = viewId;
        Log("session", "Croquet session running!");
        SetBridgeState("started");
        croquetSessionState = "running";
        lastMessageDiagnostics = Time.realtimeSinceStartup;
        estimatedDateNowAtReflectorZero = -1;

        if (waitingForCroquetSceneReset)
        {
            string startupSceneName = launchViaMenuIntoScene == "" ? SceneManager.GetActiveScene().name : launchViaMenuIntoScene;
            Croquet.RequestToLoadScene(startupSceneName, true, true);
        }
    }

    void HandleSceneStateUpdated(string[] args)
    {
        croquetActiveScene = args[0];
        croquetActiveSceneState = args[1];
        Log("session", $"Croquet scene \"{croquetActiveScene}\", state \"{croquetActiveSceneState}\"");

        Scene unityActiveScene = SceneManager.GetActiveScene();
        string startupSceneName = launchViaMenuIntoScene == "" ? unityActiveScene.name : launchViaMenuIntoScene;

        if (waitingForCroquetSceneReset)
        {
            if (croquetActiveScene == startupSceneName && croquetActiveSceneState == "preload")
            {
                waitingForCroquetSceneReset = false;
                EnsureUnityActiveScene(croquetActiveScene, false, false);
            }
            return;
        }

        if (croquetActiveScene == "")
        {
            Debug.Log($"No initial scene name set; requesting to load scene \"{startupSceneName}\"");
            Croquet.RequestToLoadScene(startupSceneName, false);
            return;
        }

        if (croquetActiveScene != unityActiveScene.name)
        {
            EnsureUnityActiveScene(croquetActiveScene, false, true);
        }
        else if (croquetActiveSceneState == "preload")
        {
            CleanUpSceneAndSystems();
            EnsureUnityActiveScene(croquetActiveScene, true, true);
        }
        else if (unitySceneState == "dormant")
        {
            SetUnitySceneState("waitingToPrepare", croquetActiveScene);
        }
    }

    public void RequestToLoadScene(string sceneName, bool forceReload, bool forceRebuild)
    {
        string[] cmdAndArgs = { "requestToLoadScene", sceneName, forceReload.ToString(), forceRebuild.ToString() };
        SendToCroquet(cmdAndArgs);
    }

    void EnsureUnityActiveScene(string targetSceneName, bool forceReload, bool showLoadProgress)
    {
        Scene unityActiveScene = SceneManager.GetActiveScene();
        if (forceReload || (unityActiveScene.name != targetSceneName && requestedSceneLoad != targetSceneName))
        {
            string switchMsg = forceReload ? "forced reload of" : "switch to";
            Debug.Log($"{switchMsg} scene {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
            requestedSceneLoad = targetSceneName;
            skipNextFrameUpdate = true;

            if (showLoadProgress && loadingProgressDisplay != null && !loadingProgressDisplay.gameObject.activeSelf)
            {
                SetLoadingStage(1.0f, "Loading...");
            }
        }
        SetUnitySceneState("waitingToPrepare", targetSceneName);
    }

    void HandleSceneRunning(string sceneName)
    {
        Log("session", $"Croquet view for scene {sceneName} running");
        if (loadingProgressDisplay != null) loadingProgressDisplay.Hide();
        SetUnitySceneState("running", sceneName);
    }

    void HandleSceneTeardown()
    {
        Log("session", "Croquet scene teardown");
        CleanUpSceneAndSystems();
    }

    void CleanUpSceneAndSystems()
    {
        deferredMessages.Clear();
        SetUnitySceneState("dormant", "");
        requestedSceneLoad = "";
        foreach (CroquetSystem system in croquetSystems)
        {
            system.TearDownScene();
        }
    }

    void HandleSessionTeardown(string postTeardownScene)
    {
        string postTeardownMsg = postTeardownScene == "" ? "" : $" (and jump to {postTeardownScene})";
        Log("session", $"Croquet session teardown{postTeardownMsg}");
        CleanUpSceneAndSystems();
        croquetSessionState = "stopped";
        foreach (CroquetSystem system in croquetSystems)
        {
            system.TearDownSession();
        }

        croquetViewId = "";
        croquetActiveScene = "";
        croquetActiveSceneState = "";

        if (postTeardownScene != "")
        {
            sessionName = "";
            SetBridgeState("waitingForSessionName");
            int buildIndex = int.Parse(postTeardownScene);
            SceneManager.LoadScene(buildIndex);
        }
        else
        {
            if (loadingProgressDisplay && !loadingProgressDisplay.gameObject.activeSelf)
            {
                SetLoadingStage(0.5f, "Reconnecting...");
            }
        }
    }

    void HandleViewCount(float viewCount)
    {
        croquetViewCount = (int)viewCount;
    }

    void SetCSharpLogOptions(string options)
    {
        string[] wanted = options.Split(',');
        foreach (string cat in logCategories)
        {
            logOptions[cat] = wanted.Contains(cat);
        }
        logOptions["routeToCroquet"] = wanted.Contains("routeToCroquet");
    }

    void SetCSharpMeasureOptions(string options)
    {
        string[] wanted = options.Split(',');
        foreach (string cat in measureCategories)
        {
            measureOptions[cat] = wanted.Contains(cat);
        }
    }

    void SetJSLogForwarding(string optionString)
    {
        string[] cmdAndArgs = { "setJSLogForwarding", optionString, croquetRunner.debugUsingExternalSession.ToString() };
        string msg = String.Join('\x01', cmdAndArgs);
        clientSock.Send(msg);
    }

    void SetLoadingStage(float ratio, string msg)
    {
        if (loadingProgressDisplay == null) return;
        loadingProgressDisplay.Show();
        loadingProgressDisplay.SetProgress(ratio, msg);
    }

    void SetLoadingProgress(float loadRatio)
    {
        if (loadingProgressDisplay == null) return;
        float barRatio = loadRatio * 0.5f + 0.5f;
        loadingProgressDisplay.Show();
        loadingProgressDisplay.SetProgress(barRatio, $"Loading... ({loadRatio * 100:#0.0}%)");
    }

    public void Log(string category, string msg)
    {
        bool loggable;
        if (logOptions.TryGetValue(category, out loggable) && loggable)
        {
            string logString = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds() % 100000}: {msg}";
            if (logOptions.TryGetValue("routeToCroquet", out loggable) && loggable)
            {
                SendToCroquet("log", logString);
            }
            else
            {
                Debug.Log(logString);
            }
        }
    }

    void Measure(params string[] strings)
    {
        string category = strings[0];
        bool loggable;
        if (measureOptions.TryGetValue(category, out loggable) && loggable)
        {
            string[] cmdString = { "measure" };
            string[] cmdAndArgs = cmdString.Concat(strings).ToArray();
            SendToCroquet(cmdAndArgs);
        }
    }
}

[System.Serializable]
public class CroquetDebugTypes
{
    public bool session;
    public bool messages;
    public bool sends;
    public bool snapshot;
    public bool data;
    public bool hashing;
    public bool subscribe;
    public bool classes;
    public bool ticks;

    public override string ToString()
    {
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

        return string.Join(',', flags.ToArray());
    }
}

[System.Serializable]
public class CroquetLogForwarding
{
    public bool log = false;
    public bool warn = true;
    public bool error = true;

    public override string ToString()
    {
        List<string> flags = new List<string>();
        if (log) flags.Add("log");
        if (warn) flags.Add("warn");
        if (error) flags.Add("error");

        return string.Join(',', flags.ToArray());
    }
}
