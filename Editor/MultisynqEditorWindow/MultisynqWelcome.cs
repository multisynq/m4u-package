using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TemporaryMenu {
  [MenuItem("Croquet/Copy UI to Cq package (TODO: DEL THIS)", false, 100)]
  private static void CopyUiToPackage() {
    string localUiFolder = "Assets/Scripts/Editor/UI_Only_MultisynqEditorWindow/";
    string packageFolder = "Packages/io.croquet.multiplayer/Editor/MultisynqEditorWindow/";
    string[] files = new string[] {
      // "MultisynqWelcome.cs",
      "MultisynqWelcome.uxml",
      // "MultisynqWelcome.uss",
      // "CroquetSettings.cs",
      // "CroquetSettings_Template.asset",
      // "Images/MultiSynq_Icon.png"
    };
    foreach (string file in files) {
      string src  = localUiFolder + file;
      string dest = packageFolder + file;
      Debug.Log("Copying " + src + " to " + dest);
      File.Copy(src, dest, true);
    }
    // Now reload that package
    AssetDatabase.Refresh();
  }
}

public class Colz {
  public Color green;
  public Color red;
  public Color yellow;
  public Color blue;
  public Color lime;
  public Color white;
  public Color grey;
  public Color c_node;

  public Colz() {
    green  = GetColor("#BFFFC5");
    red    = GetColor("#FFBFBF");
    yellow = GetColor("#FFFFBF");
    blue   = GetColor("#006AFF");
    lime   = GetColor("#00FF00");
    white  = GetColor("#FFFFFF");
    grey   = GetColor("#888888");
    c_node = GetColor("#417E37");
  }

  public static Color GetColor(string hex) {
    Color color;
    ColorUtility.TryParseHtmlString(hex, out color);
    return color;
  }
};

public class MultisynqWelcomeEW : EditorWindow {

  //=============================================================================
  public class Status {
    public string message;
    public string statusStr;
    public Color color;
    public Label label;
    public VisualElement img;
    public StatusSet statusSet;
    public void Set() {
      label.text = message;
      img.style.unityBackgroundImageTintColor = color;
      statusSet.status = statusStr;
    }
    public Status(string statusStr, Label label, VisualElement img, string message, Color color, StatusSet statusSet) {
      this.message = message;
      this.color = color;
      this.label = label;
      this.img = img;
      this.statusSet = statusSet;
      this.statusStr = statusStr;
    }
  }
  //=============================================================================
  public class StatusSet {
    public string status = "blank";
    public Status ready;
    public Status warning;
    public Status error;
    public Status success;
    public Status blank;
    public Label label;
    public VisualElement img;
    public bool IsOk() {
      return (status == "ready") || (status == "success");
    }
    public void SuccessToReady() {
      if (status == "success") {
        status = "ready";
        ready.Set();
      }
    }

    public StatusSet(Label label, VisualElement img, string _info, string _warning, string _error, string _success, string _blank) {
      ready   = new Status("ready",   label, img, _info,    colz.green,  this);
      warning = new Status("warning", label, img, _warning, colz.yellow, this);
      error   = new Status("error",   label, img, _error,   colz.red,    this);
      success = new Status("success", label, img, _success, colz.lime,   this);
      blank   = new Status("blank",   label, img, _blank,   colz.grey,   this);
    }
  }
  //=============================================================================
  static public class Statuses {
    static public StatusSet ready;
    static public StatusSet settings;
    static public StatusSet node;
    static public StatusSet key;
    static public StatusSet bridge;
    static public StatusSet bridgeHasSettings;
    static public StatusSet jsBuildTools;
    static public StatusSet jsBuild;

    static public void SuccessesToReady() {
      ready.SuccessToReady();
      settings.SuccessToReady();
      node.SuccessToReady();
      key.SuccessToReady();
      bridge.SuccessToReady();
      bridgeHasSettings.SuccessToReady();
      jsBuildTools.SuccessToReady();
      jsBuild.SuccessToReady();
    }
  }

  private static Colz colz;

  //=============================================================================
  double lastTime = 0;
  double deltaTime = 0;
  double countdown_ToConvertSuccesses = -1;

  //=============================================================================
  Button CheckIfReady_Btn; // CHECK IF READY

  VisualElement Ready_Status_Img; // ALL READY
  Label Ready_Message_Lbl;
  Button Awesome_Btn;        
  Button Top_Ready_Docs_Btn;

  VisualElement Settings_Status_Img; // SETTINGS
  Label Settings_Message_Lbl;
  Button GotoSettings_Btn;
  Button SettingsCreate_Btn;

  VisualElement Node_Status_Img; // NODE
  Label Node_Message_Lbl;
  Button TryAuto_Btn;
  Button GotoNodePath_Btn;

  VisualElement Key_Status_Img; // API KEY
  Label Key_Message_Lbl;
  Button SignUpApi_Btn;      
  Button GotoApiKey_Btn;
  Button ApiKey_Docs_Btn;

  VisualElement HaveBridge_Status_Img; // BRIDGE
  Label HaveBridge_Message_Lbl;
  Button GotoBridgeGob_Btn;
  Button CreateBridgeGob_Btn;

  VisualElement BridgeHasSettings_Img; // BRIDGE HAS SETTINGS
  Label BridgeHasSettings_Message_Lbl;
  Button BridgeHasSettings_AutoConnect_Btn;
  Button BridgeHasSettings_Goto_Btn;

  VisualElement JSBuild_Status_Img; // JS BUILD
  Label JSBuild_Message_Lbl;
  Button Build_JsNow_Btn;
  Button ToggleJSBuild_Btn;

  VisualElement JSBuildTools_Img; // JS BUILD TOOLS
  Label JSBuildTools_Message_Lbl;
  Button CopyJSBuildTools_Btn;
  Button GotoJSBuildToolsFolder_Btn;

  List<Button> allButtons = new();

  //=============================================================================

  // private const string ewFolder = "Assets/Scripts/Editor/MultisynqEditorWindow/";
  private const string ewFolder = "Packages/io.croquet.multiplayer/Editor/MultisynqEditorWindow/";
  private const string cqSettingsAssetOutputPath = "Assets/Settings/CroquetSettings_XXXXXXXX.asset";

  //=============================================================================

  [MenuItem("Window/==Multisynq Welcome (from Package!)")]
  public static void ShowMultisynqWelcome() {
    var ceWindow = GetWindow<MultisynqWelcomeEW>();
    // Assets/Scripts/Editor/MultisynqEditorWindow/Images/MultiSynq_Icon.png
    var icon = AssetDatabase.LoadAssetAtPath<Texture>(ewFolder + "Images/MultiSynq_Icon.png");
    ceWindow.titleContent = new GUIContent("Multisynq Welcome", icon);
  }

  void AllStatusToBlank() {
    Statuses.ready.blank.Set();
    Statuses.settings.blank.Set();
    Statuses.node.blank.Set();
    Statuses.key.blank.Set();
    Statuses.bridge.blank.Set();
    Statuses.bridgeHasSettings.blank.Set();
    Statuses.jsBuildTools.blank.Set();
    Statuses.jsBuild.blank.Set();
  }

  public void CreateGUI() {
    colz = new();

    // Each editor window contains a root VisualElement object
    var root = rootVisualElement;

    // Import UXML
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ewFolder + "MultisynqWelcome.uxml");
    var labelFromUXML = visualTree.Instantiate();
    root.Add(labelFromUXML);

    // A stylesheet can be added to a VisualElement.
    // The style will be applied to the VisualElement and all of its children.
    // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CustomEditor.uss");
    // root.styleSheets.Add(styleSheet);

    SetupUI();
    SetupStatuses();
    AllStatusToBlank();
    // // Ready_Status_Img blue!
  }
  
  //=============================================================================
  //=============================================================================
  
  private void SetupUI() {
    // CHECK READINESS
    SetupButton("CheckIfReady_Btn", ref CheckIfReady_Btn, Clk_CheckIfReady);
    // READY
    SetupVisElem("Ready_Status_Img",  ref Ready_Status_Img);
    SetupLabel("Ready_Message_Lbl",   ref Ready_Message_Lbl); 
    SetupButton("Awesome_Btn",        ref Awesome_Btn,        Clk_BeAwesome);
    SetupButton("Top_Ready_Docs_Btn", ref Top_Ready_Docs_Btn, Clk_Top_Ready_Docs);
    // SETTINGS
    SetupVisElem("Settings_Status_Img", ref Settings_Status_Img);
    SetupLabel("Settings_Message_Lbl",  ref Settings_Message_Lbl);
    SetupButton("GotoSettings_Btn",     ref GotoSettings_Btn,   Clk_GotoSettings);
    SetupButton("SettingsCreate_Btn",   ref SettingsCreate_Btn, Clk_SettingsCreate);
    // NODE
    SetupVisElem("Node_Status_Img", ref Node_Status_Img);
    SetupLabel("Node_Message_Lbl",  ref Node_Message_Lbl);
    SetupButton("GotoNodePath_Btn", ref GotoNodePath_Btn, Clk_GotoNodePath);
    SetupButton("TryAuto_Btn",      ref TryAuto_Btn,      Clk_AutoSetupNode);
    // API KEY
    SetupVisElem("Key_Status_Img",  ref Key_Status_Img);
    SetupLabel("Key_Message_Lbl",   ref Key_Message_Lbl);
    SetupButton("SignUpApi_Btn",    ref SignUpApi_Btn,   Clk_SignUpApi);
    SetupButton("GotoApiKey_Btn",   ref GotoApiKey_Btn,  Clk_EnterApiKey);
    SetupButton("ApiKey_Docs_Btn",  ref ApiKey_Docs_Btn, Clk_ApiKey_Docs);
    // BRIDGE
    SetupVisElem("HaveBridge_Status_Img", ref HaveBridge_Status_Img);
    SetupLabel("HaveBridge_Message_Lbl",  ref HaveBridge_Message_Lbl);
    SetupButton("GotoBridgeGob_Btn",      ref GotoBridgeGob_Btn,   Clk_GotoBridgeGob);
    SetupButton("CreateBridgeGob_Btn",    ref CreateBridgeGob_Btn, Clk_CreateBridgeGob);
    // BRIDGE HAS STEEINGS
    SetupVisElem("BridgeHasSettings_Img",             ref BridgeHasSettings_Img);
    SetupLabel( "BridgeHasSettings_Message_Lbl",      ref BridgeHasSettings_Message_Lbl);
    SetupButton( "BridgeHasSettings_AutoConnect_Btn", ref BridgeHasSettings_AutoConnect_Btn, Clk_BridgeHasSettings_AutoConnect);
    SetupButton( "BridgeHasSettings_Goto_Btn",        ref BridgeHasSettings_Goto_Btn,        Clk_BridgeHasSettings_Goto);
    // JS BUILD TOOLS
    SetupVisElem("JSBuildTools_Img",          ref JSBuildTools_Img);
    SetupLabel("JSBuildTools_Message_Lbl",    ref JSBuildTools_Message_Lbl);
    SetupButton("CopyJSBuildTools_Btn",       ref CopyJSBuildTools_Btn,       Clk_CopyJSBuildTools);
    SetupButton("GotoJSBuildToolsFolder_Btn", ref GotoJSBuildToolsFolder_Btn, Clk_GotoJSBuildToolsFolder);
    // JS BUILD
    SetupVisElem("JSBuild_Status_Img", ref JSBuild_Status_Img);
    SetupLabel("JSBuild_Message_Lbl",  ref JSBuild_Message_Lbl);
    SetupButton("ToggleJSBuild_Btn",   ref ToggleJSBuild_Btn, Clk_ToggleJSBuild); // Start JS Build Watcher
    SetupButton("Build_JsNow_Btn",     ref Build_JsNow_Btn,   Clk_Build_JsNow);
    //-----
    // Hide most buttons
    HideMostButtons();
  }

  private void HideMostButtons() {
    foreach (Button button in allButtons) {
      string[] whitelisted = {
        "CheckIfReady_Btn",
        "_Docs_",
        // "GotoSettings_Btn",
      };
      if (whitelisted.Any(button.name.Contains)) {
        button.style.visibility = Visibility.Visible;
      } else {
        button.style.visibility = Visibility.Hidden;
      }
    }
  }

  private void SetupStatuses() {
    string t_synq = "<b><color=#006AFF>Synq</color></b>";
    string t_key  = "<b><color=#006AFF>API Key</color></b>";
    string t_node = "<b><color=#417E37>Node</color></b>";
    string t_jsb  = "<b><color=#E5DB1C>JS Build</color></b>";

    Statuses.ready = new StatusSet( Ready_Message_Lbl, Ready_Status_Img,
      // (info, warning, error, success)
      $"You are <b><size=+1><color=#77ff77>Ready to </color>{t_synq}</b></size><color=#888>      All green lights below.",
      $"Warn 00000",
      $"Look below to fix what's not ready...",
      $"W00t!!! You are ready to {t_synq}!", // displays for 5 seconds, then switches to the .ready message
      "Press   Check If Ready   above"
    );
    Statuses.settings = new StatusSet( Settings_Message_Lbl, Settings_Status_Img,
      // ... info, warning, error, success)
      $"Settings are ready to go!",
      $"Settings are set to defaults! Look for other red items below to fix this.",
      $"Settings asset is missing! Click <b>Create Settings</b> to make some.",
      $"Settings are configured!!! Well done!",
      "< Settings status >"
    );
    GotoSettings_Btn.SetEnabled(false);

    Statuses.node = new StatusSet( Node_Message_Lbl, Node_Status_Img,
      // (info, warning, error, success)
      $"{t_node} is ready to go!",
      $"{t_node} is not running",
      $"{t_node} needs your help getting set up.",
      $"{t_node} path configured!!! Well done!",
      "< Node status >"
    );
    Statuses.key = new StatusSet( Key_Message_Lbl, Key_Status_Img,
      // ... info, warning, error, success)
      $"The {t_key} is ready to go!",
      $"The {t_key} is not set",
      $"Let's get you a free {t_key}. It's easy.",
      $"The {t_key} is configured!!! Well done!",
      "< API Key status >"
    );
    Statuses.bridge = new StatusSet( HaveBridge_Message_Lbl, HaveBridge_Status_Img,
      // ... info, warning, error, success)
      "Bridge GameObject is ready to go!",
      "Bridge GameObject is missing!",
      "Bridge GameObject is missing in scene! Click <b>Create Bridge</b> to make one.",
      "Bridge Gob <color=#888888>(GameObject)</color> found!! Well done!",
      "< Bridge GameObject status >"
    );
    Statuses.bridgeHasSettings = new StatusSet( BridgeHasSettings_Message_Lbl, BridgeHasSettings_Img,
      // ... info, warning, error, success)
      "Bridge has settings!",
      "Bridge is missing settings!",
      "Bridge is missing settings! Click <b>Auto Connect</b> to connect it.",
      "Bridge connected to settings!!! Well done!",
      "< Bridge's Settings status >"
    );
    Statuses.jsBuild = new StatusSet( JSBuild_Message_Lbl, JSBuild_Status_Img,
      // ... info, warning, error, success)
      $"{t_jsb} is ready to go!",
      $"{t_jsb} is not ready",
      $"{t_jsb} needs your help getting set up.",
      $"{t_jsb} path configured!!! Well done!",
      "< JS Build status >"
    );
    Statuses.jsBuildTools = new StatusSet( JSBuildTools_Message_Lbl, JSBuildTools_Img,
      // ... info, warning, error, success)
      "JS Build Tools are ready to go!",
      "JS Build Tools are missing",
      "JS Build Tools are missing! Click <b>Copy JS Build Tools</b> to get them.",
      "JS Build Tools installed!!! Well done!",
      "< JS Build Tools status >"
    );
  }

  //=============================================================================
  //=============================================================================

  private void Clk_BeAwesome() {
    Debug.Log("Be Awesome!!!!");
    // Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ"); // Copilot thinks you should go to this url. You know you want to. =]
    Application.OpenURL("https://giphy.com/search/everything-is-awesome");
  }

  private void Clk_GotoNodePath() {
    Clk_GotoSetting();
    // notify message
    var msg = @"
See Inspector.

Croquet Settings
with node path
selected in Project.
";
    ShowNotification(new GUIContent(msg), 4);
  }

  private void Clk_GotoSetting() {
    // Select the file in Project pane of the Editor so it shows up in the Inspector
    var cqStgs = EnsureSettingsFile();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create CroquetSettings file");
      return;
    } else {
      Debug.Log("Found CroquetSettings file");
      Selection.activeObject = cqStgs;
    }
  }

  private void Clk_AutoSetupNode() {
    Debug.Log("Auto Setup Node!");
    if (!Application.isEditor) {
      Debug.LogError("This feature is only available in the Editor.");
      return;
    }

    string nodeVer;

    switch (Application.platform) {
      case RuntimePlatform.OSXEditor:
        Debug.Log("OSX Editor Detected");

        // string path = "/bin/bash";
        // string args = "-c '\"/Users/imac/PathToThe/Application.app/Contents/MacOS/Application\" \"$0\" \"$1\" \"$2\"' \"argument_or_Path_1\" \"argument_or_Path_2\" \"argument_or_Path_3\"";
        // string args = "-c '\"/Users/imac/PathToThe/Application.app/Contents/MacOS/Application\" \"$0\" \"$1\" \"$2\"' \"argument_or_Path_1\" \"argument_or_Path_2\" \"argument_or_Path_3\"";


        // Debug.Log(RunShell(path, args));
        
        // Debug.Log("which node => " + RunShell(@"bash", @"-c ""which node"""));
        // Debug.Log("echo $PATH => " + RunShell("bash", @"-c ""echo $PATH"""));

        // Debug.Log("which node => " + RunShell("bash", @"-c ""which node"""));
        // Debug.Log( "which node => " + RunShell("/bin/bash", "echo $PATH", 2, true) );
        // Debug.Log("which node => " + RunShell("bash", @"-c ""which node"""));
        // Debug.Log( "which node => " + RunShell("/bin/bash", "which node") );

        
        // nodeVer = GetNodeVersion("/usr/local/bin/node", "-v");
        var cqStgs = FindProjectCqSettings();
        cqStgs.pathToNode = "/usr/local/bin/node";
        Check_Node();
        break;
      case RuntimePlatform.WindowsEditor:
        Debug.Log("Windows Editor Detected");
        nodeVer = GetNodeVersion("cmd.exe", $"/c runwebpack.bat ");
        break;
      // case RuntimePlatform.LinuxEditor:
      // Debug.Log("Linux Editor Detected");
      // break;
      default:
        Debug.LogError("Unsupported platform: " + Application.platform);
        break;
    }
    CheckAllStatusForReady();
  }

  private void Clk_SignUpApi() {
    Clk_GotoSetting();
    Application.OpenURL("https://croquet.io/account/");
  }

  private async void Clk_ToggleJSBuild() {
    if (ToggleJSBuild_Btn.text == "Start JS Build Watcher") {
      bool success = await CroquetBuilder.EnsureJSToolsAvailable();
      if (!success) return;
      CroquetBuilder.StartBuild(true); // true => start watcher
      Debug.Log("Started JS Build Watcher");
      ToggleJSBuild_Btn.text = "Stop JS Build Watcher";
    } else {
      ToggleJSBuild_Btn.text = "Start JS Build Watcher";
      CroquetBuilder.StopWatcher();
    }
  }

  private void Clk_Top_Ready_Docs() {
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  private void Clk_CheckIfReady() {
    bool allRdy = true;
    allRdy &= Check_Settings(); 
    allRdy &= Check_Node();
    allRdy &= Check_ApiKey();
    allRdy &= Check_BridgeComponent();
    allRdy &= Check_BridgeHasSettings();
    allRdy &= Check_JS_BuildTools();
    allRdy &= Check_JS_Build();
    //-----
    if (allRdy) AllAreReady();
    else        AllAreReady(false);
  }

  private void NotifyAndLog(string msg, float seconds = 4) {
    ShowNotification(new GUIContent(msg), seconds);
    Debug.Log(msg);
  }
  private void Notify(string msg, float seconds = 4) {
    ShowNotification(new GUIContent(msg), seconds);
  }

  private void Clk_GotoSettings() {
    Clk_GotoSetting();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_SettingsCreate() {
    // CroquetSettings in scene
    var cqStgs = EnsureSettingsFile();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create CroquetSettings file");
      Statuses.ready.error.Set();
    } else {
      Statuses.ready.success.Set();
    }
    Clk_GotoSetting();
    // make buttons for goto Node path and API key visible
    GotoNodePath_Btn.style.visibility = Visibility.Visible;
    GotoApiKey_Btn.style.visibility   = Visibility.Visible;
    TryAuto_Btn.style.visibility = Visibility.Visible;
    Check_Settings();
    CheckAllStatusForReady();
  }

  private void Clk_EnterApiKey() {
    Clk_GotoSetting();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private async void Clk_Build_JsNow() {
    Debug.Log("Building JS...");
    bool success = await CroquetBuilder.EnsureJSToolsAvailable();
    if (!success) {
      var msg = @"
JS Build Tools are missing!!! 
Cannot build. 
";
      Debug.LogError(msg);
      EditorUtility.DisplayDialog("Missing JS Tools", msg, "OK");
      return;
    }
    CroquetBuilder.StartBuild(false); // false => no watcher
  }

  private void Clk_ApiKey_Docs() {
    Application.OpenURL("https://croquet.io/account/");
    // Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  private void Clk_GotoBridgeGob() {
    // find ComponentType CroquetBridge in scene
    var bridge = FindObjectOfType<CroquetBridge>();
    if (bridge == null) {
      string msg = "Could not find\nCroquetBridge in scene!";
      Notify(msg); Debug.LogError(msg);
    } else {
      Selection.activeGameObject = bridge.gameObject; // select in Hierachy
      string msg = "CroquetBridge\nselected in\nHierarchy.";
      Notify(msg); Debug.Log(msg); 
    }
  }
  
  private void Clk_CreateBridgeGob() {
    var bridge = FindObjectOfType<CroquetBridge>();
    if (bridge != null) {
      string msg = "CroquetBridge already exists in scene";
      Notify(msg); Debug.LogError(msg);
    } else {
      var cbGob = new GameObject("CroquetBridge");
      cbGob.AddComponent<CroquetBridge>();
      cbGob.AddComponent<CroquetRunner>();
      cbGob.AddComponent<CroquetEntitySystem>();
      cbGob.AddComponent<CroquetSpatialSystem>();

      Selection.activeGameObject = cbGob;
      string msg = "Created CroquetBridge\nGameObject in scene.\nSelected it.";
      Notify(msg); Debug.Log(msg); 
      Check_BridgeComponent();
      CheckAllStatusForReady();
    }
    Check_BridgeComponent();
    Check_BridgeHasSettings();
    CheckAllStatusForReady();
  }

  void Clk_BridgeHasSettings_AutoConnect() {
    var bridge = FindObjectOfType<CroquetBridge>();
    if (bridge == null) {
      NotifyAndLog("Could not find CroquetBridge in scene!");
      return;
    } else {
      var cqSettings = FindProjectCqSettings();
      if (cqSettings == null) {
        NotifyAndLog("Could not find CroquetSettings in project!");
        return;
      } else {
        bridge.appProperties = cqSettings;
        NotifyAndLog("Connected CroquetBridge to CroquetSettings!");
        Check_BridgeHasSettings();
        CheckAllStatusForReady();
      }
    }
  }

  void Clk_BridgeHasSettings_Goto() {
    Clk_GotoBridgeGob();
  }
  private async void Clk_CopyJSBuildTools() {
    await CroquetBuilder.InstallJSTools();
    Check_JS_BuildTools();
    CheckAllStatusForReady();
  }

  private void Clk_GotoJSBuildToolsFolder() {
    string croquetJSFolder = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS"));
    string jsBuildFolder   = Path.GetFullPath(Path.Combine(croquetJSFolder, ".js-build"));
    if (!Directory.Exists(jsBuildFolder)) {
      string msg = "Could not find JS Build Tools folder";
      Debug.LogError(msg);
      ShowNotification(new GUIContent(msg), 4);
      return;
    }
    string msg2 = "The CroquetJS/.js-build folder opened in Finder/Explorer.";
    Debug.Log(msg2);
    ShowNotification(new GUIContent(msg2), 4);
    EditorUtility.RevealInFinder(jsBuildFolder);
  }

  //=============================================================================
  //=============================================================================

  private void SetupButton(string buttonName, ref Button button, Action buttonAction) {
    button = rootVisualElement.Query<Button>(buttonName).First();
    if (button == null) {
      Debug.LogError("Could not find button: " + buttonName);
      return;
    }
    button.clicked += buttonAction;
    allButtons.Add(button);
  }

  private void SetupLabel(string labelName, ref Label label) {
    label = rootVisualElement.Query<Label>(labelName).First();
    if (label == null) {
      Debug.LogError("Could not find label: " + labelName);
    }
  }

  private void SetupVisElem(string visElemName, ref VisualElement visElem) {
    visElem = rootVisualElement.Query<VisualElement>(visElemName).First();
    if (visElem == null) {
      Debug.LogError("Could not find VisualElement: " + visElemName);
    }
  }

  //=============================================================================

  private CroquetSettings CopyDefaultSettingsFile() {
    string path = ewFolder + "resources/CroquetSettings_Template.asset";
    AssetDatabase.CopyAsset(path, cqSettingsAssetOutputPath);
    return AssetDatabase.LoadAssetAtPath<CroquetSettings>(cqSettingsAssetOutputPath);
  }

  private CroquetSettings FindProjectCqSettings() {
    CroquetSettings cqSettings = null;
      // Check if the file is there
    string[] guids = AssetDatabase.FindAssets("t:CroquetSettings");
    guids = Array.FindAll(guids, guid => !AssetDatabase.GUIDToAssetPath(guid).Contains("Packages/"));

    if (guids.Length > 0) {
      cqSettings = AssetDatabase.LoadAssetAtPath<CroquetSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
      if (guids.Length > 1) {
        Debug.LogWarning("Found more than one CroquetSettings file. You should only have one.");
        // Print out path of all files found
        int i = 1;
        foreach (string guid in guids) {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          // Make them select when click each log
          Debug.LogWarning( i++ + ". " + path, AssetDatabase.LoadAssetAtPath<CroquetSettings>(path));
        }
      }
    }
    if (cqSettings == null) {
      Debug.LogWarning("Could not find CroquetSettings.asset in your Assets folders.");
      Statuses.settings.error.Set();
      Statuses.node.error.Set();
      Statuses.key.error.Set();
      Statuses.ready.error.Set();
    }
    return cqSettings;
  }

  private CroquetSettings EnsureSettingsFile() {
    CroquetSettings cqSettings = FindProjectCqSettings();
    // If not, copy file from ./resources/CroquetSettings_Template.asset
    // into Assets/Settings/CroquetSettings.asset
    if (cqSettings == null) {
      cqSettings = CopyDefaultSettingsFile();
    }
    return cqSettings;
  }
  
  //=============================================================================

  private void AllAreReady(bool really = true) {
    if (really) {
      Statuses.ready.success.Set();
      Awesome_Btn.style.visibility = Visibility.Visible;
      countdown_ToConvertSuccesses = 3f;
    } else {
      Statuses.ready.error.Set();
      Awesome_Btn.style.visibility = Visibility.Hidden;
    }
  }

  //=============================================================================

  private void CheckAllStatusForReady() {
    bool allRdy = true;
    // NEVER: allRdy &= Statuses.ready.IsOk() // NEVER want this
    allRdy &= Statuses.settings.IsOk();
    allRdy &= Statuses.node.IsOk();
    allRdy &= Statuses.key.IsOk();
    allRdy &= Statuses.bridge.IsOk();
    allRdy &= Statuses.jsBuildTools.IsOk();
    allRdy &= Statuses.jsBuild.IsOk();
    if (allRdy) AllAreReady();      
    else        AllAreReady(false);
  }

  private bool Check_Settings() {
    var cqStgs = FindProjectCqSettings();
    if (cqStgs == null) {
      GotoSettings_Btn.SetEnabled(false);
      SettingsCreate_Btn.style.visibility = Visibility.Visible;
      Statuses.settings.error.Set();
      return false;
    } else {
      GotoSettings_Btn.SetEnabled(true);
      Statuses.settings.success.Set();
      SettingsCreate_Btn.style.visibility = Visibility.Hidden;
      GotoSettings_Btn.style.visibility   = Visibility.Visible;
      return true;
    }
  }

  private bool Check_Node() {
    var cqStgs = FindProjectCqSettings();
    if (cqStgs == null) {
      Statuses.node.error.Set();
      // hide AutoSetup button
      TryAuto_Btn.style.visibility = Visibility.Hidden;
      return false;
    }
    TryAuto_Btn.style.visibility = Visibility.Visible;
    string nodePath = cqStgs.pathToNode;
    string nodeVer = TryNodePath(nodePath);
    if (nodeVer == null) {
      Statuses.node.error.Set();
      return false;
    } else {
      Statuses.node.success.Set();
      TryAuto_Btn.style.visibility      = Visibility.Hidden;
      GotoNodePath_Btn.style.visibility = Visibility.Visible;
      return true;
    }
  }

  private bool Check_ApiKey() {
    bool ok = false;
    var cqStgs = FindProjectCqSettings();

    if (cqStgs != null) {
      GotoApiKey_Btn.style.visibility = Visibility.Visible;
      SignUpApi_Btn.style.visibility  = Visibility.Visible;
      var apiKey = cqStgs.apiKey;
      if (apiKey == null || apiKey == "<go get one at multisynq.io>" || apiKey.Length < 1) {
        // curl -s -X GET -H "X-Croquet-Auth: 1_s77e6tyzkx5m3yryb9305sqxhkdmz65y69oy5s8e" -H "X-Croquet-App: io.croquet.vdom.ploma" -H "X-Croquet-Id: persistentId" -H "X-Croquet-Version: 1.1.0" -H "X-Croquet-Path: https://croquet.io" 'https://api.croquet.io/sign/join?meta=login'
        Statuses.key.error.Set();
      } else {
        Statuses.key.success.Set();
        ok = true;
      }
    }
    return ok;
  }

  private bool Check_JS_BuildTools() {
    string croquetJSFolder = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS"));
    string jsBuildFolder   = Path.GetFullPath(Path.Combine(croquetJSFolder, ".js-build"));
    bool havejsBuildFolder = Directory.Exists(jsBuildFolder);
    if (!havejsBuildFolder) {
      Statuses.jsBuildTools.error.Set();
      CopyJSBuildTools_Btn.style.visibility = Visibility.Visible;
      Build_JsNow_Btn.style.visibility = Visibility.Hidden;
    } else {
      Statuses.jsBuildTools.success.Set();
      CopyJSBuildTools_Btn.style.visibility = Visibility.Hidden;
      GotoJSBuildToolsFolder_Btn.style.visibility = Visibility.Visible;
      Build_JsNow_Btn.style.visibility = Visibility.Visible;
    }
    return havejsBuildFolder;
  }

  private bool Check_JS_Build() {
    // if (havejsBuiltJsCode) {
      Statuses.jsBuild.success.Set();
    // } else {
      Statuses.jsBuild.error.Set();
      Build_JsNow_Btn.style.visibility = Visibility.Visible;
    // }
    // return havejsBuildFolder;
    return false;
  }

  private bool Check_BridgeComponent() {
    var bridge = FindObjectOfType<CroquetBridge>();
    bool fountIt = (bridge != null);
    if (!fountIt) {
      Statuses.bridge.error.Set();
      CreateBridgeGob_Btn.style.visibility = Visibility.Visible;
    } else {
      Statuses.bridge.success.Set();
      GotoBridgeGob_Btn.style.visibility   = Visibility.Visible;
      CreateBridgeGob_Btn.style.visibility = Visibility.Hidden;
    }
    return fountIt;
  }

  private bool Check_BridgeHasSettings() {
    var bridge = FindObjectOfType<CroquetBridge>();
    bool foundIt = (bridge != null);
    if (!foundIt) {
      Statuses.bridgeHasSettings.error.Set();
      BridgeHasSettings_AutoConnect_Btn.style.visibility = Visibility.Hidden;
      BridgeHasSettings_Goto_Btn.style.visibility = Visibility.Hidden;
    } else {
      if (bridge.appProperties == null) {
        Statuses.bridgeHasSettings.error.Set();
        BridgeHasSettings_AutoConnect_Btn.style.visibility = Visibility.Visible;
        foundIt = false;
      } else {
        Statuses.bridgeHasSettings.success.Set();
        BridgeHasSettings_AutoConnect_Btn.style.visibility = Visibility.Hidden;
        BridgeHasSettings_Goto_Btn.style.visibility = Visibility.Visible;
      }
    }
    return foundIt;
  }

  //=============================================================================  
  private string TryNodePath(string nodePath) {
    if (!File.Exists(nodePath)) {
      Debug.LogError("Could not find node path file: " + nodePath);
      return null;
    } else {
      return GetNodeVersion(nodePath, "-v");
    }
  }
  private string GetNodeVersion(string executable = "", string arguments = "") {
    string output = RunShell(executable, arguments);
    string[] stdoutLines = output.Split('\n');

    // If output is a string that starts with v and then has a number,
    // then it's the version number, otherwise its an error
    foreach (string line in stdoutLines) {
      if (line.StartsWith("v") && float.TryParse(line.Substring(1,3), out float version)) {
        Debug.Log("Node version: " + line);
        return line;
      }
    }
    Debug.LogError("Node not found");
    return null;
  }

  private string RunShell(string executable = "", string arguments = "", int logLevel = 2, bool shellExec = false) {
    System.Diagnostics.Process pcs = new();
    pcs.StartInfo.UseShellExecute = shellExec;
    pcs.StartInfo.RedirectStandardOutput = true;
    pcs.StartInfo.RedirectStandardError = true;
    pcs.StartInfo.CreateNoWindow = true;
    pcs.StartInfo.WorkingDirectory = Path.GetFullPath(ewFolder);
    pcs.StartInfo.FileName = executable;
    pcs.StartInfo.Arguments = arguments;
    pcs.StartInfo.UserName = "root";
    pcs.Start();

    string output = pcs.StandardOutput.ReadToEnd();
    string errors = pcs.StandardError.ReadToEnd();
    pcs.WaitForExit();

    if (output.Length > 0 && logLevel > 1) Debug.Log("RunShell().output = '" + output + "'");
    if (errors.Length > 0 && logLevel > 0) Debug.LogError("RunShell().errors = '" + errors + "'");

    return output;
  }

  //=============================================================================  
  void Update() {
    Update_DeltaTime();
    Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Ready_Message_Lbl, Statuses.ready, true);
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Node_Message_Lbl, Statuses.node);
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Key_Message_Lbl, Statuses.key);
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, JSBuild_Message_Lbl, Statuses.jsBuild);
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Settings_Message_Lbl, Statuses.settings);
  }
  void Update_DeltaTime()  {
    if (lastTime == 0) lastTime = EditorApplication.timeSinceStartup;
    deltaTime = EditorApplication.timeSinceStartup - lastTime;
    lastTime = EditorApplication.timeSinceStartup;
  }
  void Update_CountdownAndMessage(ref double countdownSeconds, Label messageField, StatusSet status, bool showTimer = false) {

    if (countdownSeconds > 0f) {
      countdownSeconds -= deltaTime;
      if (showTimer) messageField.text = status.success.message + "   <b>" + countdownSeconds.ToString("0.0") + "</b>";
      if (countdownSeconds <= 0f) {
        countdownSeconds = -1;
        messageField.text = status.success.message;
        Statuses.SuccessesToReady(); // <=======
      }
    }
  }
}