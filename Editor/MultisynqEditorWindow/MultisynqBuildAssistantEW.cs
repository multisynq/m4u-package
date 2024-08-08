using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

//------------------ ||||||||||||||||||||||||| ----------------------------------
public partial class MultisynqBuildAssistantEW : EditorWindow {

  public static HandyColors colz;

  //=============================================================================
  double lastTime = 0;
  double deltaTime = 0;
  double countdown_ToConvertSuccesses = -1;
  //=============================================================================

  //==== UI Refs ================================================================

  Button CheckIfReady_Btn; // CHECK IF READY

  VisualElement Ready_Status_Img; // ALL READY
  Label Ready_Message_Lbl;
  Button Awesome_Btn;
  Button Top_Ready_Docs_Btn;

  VisualElement HasCqSys_Img; // HAS CQ SYSTEMS
  Label HasCqSys_Message_Lbl;
  Button AddCqSys_Btn;
  Button ListMissingCqSys_Btn;

  VisualElement BridgeHasSettings_Img; // BRIDGE HAS SETTINGS
  Label BridgeHasSettings_Message_Lbl;
  Button BridgeHasSettings_AutoConnect_Btn;
  Button BridgeHasSettings_Goto_Btn;

  VisualElement JSBuildTools_Img; // JS BUILD TOOLS
  Label JSBuildTools_Message_Lbl;
  Button CopyJSBuildTools_Btn;
  Button GotoJSBuildToolsFolder_Btn;

  VisualElement HasAppJs_Status_Img; // HAS APP JS
  Label HasAppJs_Message_Lbl;
  Button SetAppName_Btn;
  Button MakeAppJsFile_Btn;
  Button HasAppJs_Docs_Btn;
  Button GotoAppJsFile_Btn;
  Button GotoAppJsFolder_Btn;

  VisualElement JbtVersionMatch_Img; // VERSION MATCH - JS BUILD TOOLS
  Label JbtVersionMatch_Message_Lbl;
  Button ReinstallTools_Btn;
  // Button GotoJSBuildToolsFolder_Btn;
  Button OpenBuildPanel_Btn;

  VisualElement JSBuild_Status_Img; // JS BUILD
  Label JSBuild_Message_Lbl;
  Button Build_JsNow_Btn;
  Button ToggleJSBuild_Btn;
  Button GotoBuiltOutput_Btn;

  VisualElement BuiltOutput_Status_Img; // BUILD OUTPUT
  Label BuiltOutput_Message_Lbl;
  Button Save_Open_Scene_Btn;
  Button Goto_Build_Panel_Btn;
  // Button BuiltOutput_Docs_Btn;
  Button Check_Building_Scenes_Btn;

  List<Button> allButtons = new();

  //====== Singleton ============================================================
  static private MultisynqBuildAssistantEW _Instance;
  static public MultisynqBuildAssistantEW Instance {
    get {
      if (_Instance == null)_Instance = GetWindow<MultisynqBuildAssistantEW>();
      return _Instance;
    }
    private set{}
  }

  //====== Menu ============================================================
  [MenuItem("Multisynq/Open Multisynq Build Assistant Window...",priority=10)]
  [MenuItem("Window/Multisynq/Open Build Assistant...",priority=1000)]
  public static void ShowMultisynqWelcome_MenuMethod() {
    var icon = AssetDatabase.LoadAssetAtPath<Texture>(CqFile.ewFolder + "Images/MultiSynq_Icon.png");
    // referencing the Instance property will create the window if it doesn't exist
    Instance.titleContent = new GUIContent("Multisynq Build Assistant", icon);
  }
  void OnDestroy() {
    _Instance = null;
  }

  //====== EditowWindow Init (auto-called when Shown) ==================================
  public void CreateGUI() {
    // Import UXML
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CqFile.ewFolder + "MultisynqBuildAssistant_UI.uxml");
    var labelFromUXML = visualTree.Instantiate();
    rootVisualElement.Add(labelFromUXML);

    // A stylesheet can be added to a VisualElement.
    // The style will be applied to the VisualElement and all of its children.
    // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CustomEditor.uss");
    // rootVisualElement.styleSheets.Add(styleSheet);

    // Custom init for our elements
    colz = new();
    SetupUI();
    SetupStatuses();
  }
  
  public SI_Settings siSettings;
  public SI_Node     siNode;
  public SI_ApiKey   siApiKey;
  public SI_Bridge   siBridge;
  public SI_Systems  siSystems;

  //=============================================================================

  private void SetupUI() {
    // CHECK READINESS
    SetupButton("CheckIfReady_Btn",      ref CheckIfReady_Btn,   Clk_CheckIfReady);
    // READY
    SetupVisElem("Ready_Status_Img",     ref Ready_Status_Img);
    SetupLabel(  "Ready_Message_Lbl",    ref Ready_Message_Lbl);
    SetupButton( "Awesome_Btn",          ref Awesome_Btn,        Clk_BeAwesome);
    SetupButton( "Top_Ready_Docs_Btn",   ref Top_Ready_Docs_Btn, Clk_Top_Ready_Docs);
    // SETTINGS
    siSettings = new SI_Settings(this);
    // NODE
    siNode     = new SI_Node(this);
    // API KEY
    siApiKey   = new SI_ApiKey(this);
    // BRIDGE
    siBridge   = new SI_Bridge(this);
    // HAS CQ SYSTEMS
    siSystems  = new SI_Systems(this);
    // BRIDGE HAS SETTINGS
    SetupVisElem("BridgeHasSettings_Img",             ref BridgeHasSettings_Img);
    SetupLabel(  "BridgeHasSettings_Message_Lbl",     ref BridgeHasSettings_Message_Lbl);
    SetupButton( "BridgeHasSettings_AutoConnect_Btn", ref BridgeHasSettings_AutoConnect_Btn, Clk_BridgeHasSettings_AutoConnect);
    SetupButton( "BridgeHasSettings_Goto_Btn",        ref BridgeHasSettings_Goto_Btn,        Clk_BridgeHasSettings_Goto);
    // JS BUILD TOOLS
    SetupVisElem("JSBuildTools_Img",                  ref JSBuildTools_Img);
    SetupLabel(  "JSBuildTools_Message_Lbl",          ref JSBuildTools_Message_Lbl);
    SetupButton( "CopyJSBuildTools_Btn",              ref CopyJSBuildTools_Btn,              Clk_CopyJSBuildTools);
    SetupButton( "GotoJSBuildToolsFolder_Btn",        ref GotoJSBuildToolsFolder_Btn,        Clk_GotoJSBuildToolsFolder);

    // HAS APP JS
    SetupVisElem("HasAppJs_Status_Img",               ref HasAppJs_Status_Img);
    SetupLabel(  "HasAppJs_Message_Lbl",              ref HasAppJs_Message_Lbl);
    SetupButton( "SetAppName_Btn",                    ref SetAppName_Btn,                    Clk_SetAppName);
    SetupButton( "MakeAppJsFile_Btn",                 ref MakeAppJsFile_Btn,                 Clk_MakeAppJsFile);
    SetupButton( "HasAppJs_Docs_Btn",                 ref HasAppJs_Docs_Btn,                 Clk_HasAppJs_Docs);
    SetupButton( "GotoAppJsFile_Btn",                 ref GotoAppJsFile_Btn,                 Clk_GotoAppJsFile);
    SetupButton( "GotoAppJsFolder_Btn",               ref GotoAppJsFolder_Btn,               Clk_GotoAppJsFolder);

    // JS BUILD
    SetupVisElem("JSBuild_Status_Img",                ref JSBuild_Status_Img);
    SetupLabel(  "JSBuild_Message_Lbl",               ref JSBuild_Message_Lbl);
    SetupButton( "ToggleJSBuild_Btn",                 ref ToggleJSBuild_Btn,                 Clk_ToggleJSBuild); // Start JS Build Watcher
    SetupButton( "Build_JsNow_Btn",                   ref Build_JsNow_Btn,                   Clk_Build_JsNow);
    SetupButton( "GotoBuiltOutput_Btn",               ref GotoBuiltOutput_Btn,               Clk_GotoBuiltOutput);

    // VERSION MATCH - JS BUILD TOOLS
    SetupUI_VersionMatch();
    // SetupVisElem("JbtVersionMatch_Img",               ref JbtVersionMatch_Img);
    // SetupLabel(  "JbtVersionMatch_Message_Lbl",       ref JbtVersionMatch_Message_Lbl);
    // SetupButton( "ReinstallTools_Btn",                ref ReinstallTools_Btn,                Clk_ReinstallTools);
    // SetupButton( "OpenBuildPanel_Btn",                ref OpenBuildPanel_Btn,                Clk_OpenEditorBuildPanel);

    // BUILT OUTPUT
    SetupUI_BuiltOutput();
    // SetupVisElem("BuiltOutput_Status_Img",            ref BuiltOutput_Status_Img);
    // SetupLabel(  "BuiltOutput_Message_Lbl",           ref BuiltOutput_Message_Lbl);
    // SetupButton( "Save_Open_Scene_Btn",               ref Save_Open_Scene_Btn,               Clk_Save_Open_Scene);
    // SetupButton( "Goto_Build_Panel_Btn",              ref Goto_Build_Panel_Btn,              Clk_Goto_Build_Panel);
    // SetupButton( "Check_Building_Scenes_Btn",         ref Check_Building_Scenes_Btn,         Clk_Check_Building_Scenes);

    //-----
    // Hide most buttons
    HideMostButtons();
  }

  private void HideMostButtons() {
    string[] whitelisted = {
      "CheckIfReady_Btn",
      "_Docs_",
    };
    foreach (Button button in allButtons) {
      bool isWhitelisted = whitelisted.Any(button.name.Contains);
      SetVEViz(isWhitelisted, button);
    }
    // HideVEs(Node_Dropdown);
  }

  private void SetupStatuses() {
    string t_synq = "<b><color=#006AFF>Synq</color></b>";
    string t_jsb  = "<b><color=#E5DB1C>JS Build</color></b>";

    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] {CqFile.img_root});

    foreach (string guid in guids) {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      StyleBackground newStyle = new StyleBackground(AssetDatabase.LoadAssetAtPath<Sprite>(path));
      if (path.Contains("Checkmark")) {
        StatusSet.readyImgStyle = newStyle;
        StatusSet.successImgStyle = newStyle;
      }
      if (path.Contains("Warning"))  { StatusSet.warningImgStyle = newStyle; }
      if (path.Contains("Multiply")) { StatusSet.errorImgStyle   = newStyle; }
    }

    // var hideTheseGrp = rootVisualElement.Query<VisualElement>("HideThese_Grp").First();
    // hideTheseGrp.style.display = DisplayStyle.None;

    MqWelcome_StatusSets.ready = new StatusSet( Ready_Message_Lbl, Ready_Status_Img,
      // (info, warning, error, success)
      $"You are <b><size=+1><color=#77ff77>Ready to </color>{t_synq}</b></size><color=#888>      All green lights below.",
      $"Warn 00000",
      $"Look below to fix what's not ready...",
      $"W00t!!! You are ready to {t_synq}!", // displays for 5 seconds, then switches to the .ready message
      "Press   Check If Ready   above"
    );

    MqWelcome_StatusSets.bridgeHasSettings = new StatusSet( BridgeHasSettings_Message_Lbl, BridgeHasSettings_Img,
      // ... info, warning, error, success)
      "Bridge has settings!",
      "Bridge is missing settings!",
      "Bridge is missing settings! Click <b>Auto Connect</b> to connect it.",
      "Bridge connected to settings!!! Well done!",
      "Bridge's Settings status"
    );
    MqWelcome_StatusSets.jsBuildTools = new StatusSet( JSBuildTools_Message_Lbl, JSBuildTools_Img,
      // (info, warning, error, success)
      "JS Build Tools are ready to go!",
      "JS Build Tools are missing",
      "JS Build Tools are missing! Click <b>Copy JS Build Tools</b> to get them.",
      "JS Build Tools installed!!! Well done!",
      "JS Build Tools status"
    );
    MqWelcome_StatusSets.hasAppJs = new StatusSet( HasAppJs_Message_Lbl, HasAppJs_Status_Img,
      // (info, warning, error, success)
      "Input JS: index.js for AppName is ready to go!",
      "Input JS: index.js for AppName is missing",
      "Input JS: index.js for AppName is missing!",
      "Input JS: index.js for AppName found! Well done!",
      "Input JS: index.js for AppName status"
    );
    MqWelcome_StatusSets.jsBuild = new StatusSet( JSBuild_Message_Lbl, JSBuild_Status_Img,
      // (info, warning, error, success, blank)
      $"Output JS was built!",
      $"Output JS missing.",
      $"Output JS not found. Need to Build JS.",
      $"Output JS was built! Well done!",
      "JS Build status"
    );
    MqWelcome_StatusSets.versionMatch = new StatusSet( JbtVersionMatch_Message_Lbl, JbtVersionMatch_Img,
      // (info, warning, error, success, blank)
      $"Versions of {t_jsb} Tools and Built output match!",
      $"Versions of {t_jsb} Tools and Built output do not match",
      $"Versions of {t_jsb} Tools and Built output do not match!\n<b>Make a new or first build!</b>",
      $"Versions of {t_jsb} Tools and Built output match!!! Well done!",
      "Version Match status"
    );
    MqWelcome_StatusSets.builtOutput = new StatusSet( BuiltOutput_Message_Lbl, BuiltOutput_Status_Img,
      // (info, warning, error, success, blank)
      $"Built output folders match the building scene list!",
      $"Compare output JS folders to Unity Build scene list with [ Check Building Scenes ] button.",
      $"Compare output JS folders to Unity Build scene list with [ Check Building Scenes ] button.",
      $"Built output folders match the building scene list! Well done!",
      "Built output status"
    );
    MqWelcome_StatusSets.AllStatusSetsToBlank();
  }
  //=============================================================================
  //=============================================================================
  static bool doCheckBuiltOutput = true;

  //-- Clicks - CHECK READINESS --------------------------------
  private void Clk_CheckIfReady() { // CHECK READINESS  ------------- Click
    Debug.Log($"<color=#006AFF>============= [ <color=#0196FF>Check If Ready</color> ] =============</color>");
    bool allRdy = true;
    allRdy &= siSettings.Check();
    allRdy &= siNode.Check();
    allRdy &= siApiKey.Check();
    allRdy &= siBridge.Check();
    allRdy &= siSystems.Check();
    allRdy &= Check_BridgeHasSettings();
    allRdy &= Check_JS_BuildTools();
    allRdy &= Check_HasAppJs();
    allRdy &= Check_ToolsVersionMatch();
    allRdy &= Check_JS_Build();
    if (doCheckBuiltOutput) allRdy &= Check_BuiltOutput();
    //-----
    if (allRdy) AllAreReady();
    else        AllAreReady(false);
    siNode.NodePathsToDropdownAndCheck();
  }


  //-- Clicks - READY --------------------------------

  private void Clk_BeAwesome() { // READY  ------------- Click
    Debug.Log("Be Awesome!!!!");
    // Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ"); // Copilot thinks you should go to this url. You know you want to. =]
    Application.OpenURL("https://giphy.com/search/everything-is-awesome");
  }

  private void Clk_Top_Ready_Docs() { // READY  ------------- Click
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }



  //-- Clicks - BRIDGE HAS SETTINGS --------------------------------

  void Clk_BridgeHasSettings_AutoConnect() { // BRIDGE HAS SETTINGS  ------------- Click
    var bridge = FindObjectOfType<CroquetBridge>();
    if (bridge == null) {
      NotifyAndLogError("Could not find CroquetBridge in scene!");
      return;
    } else {
      var cqSettings = CqFile.FindProjectCqSettings();
      if (cqSettings == null) {
        NotifyAndLogError("Could not find CroquetSettings in project!");
        return;
      } else {
        bridge.appProperties = cqSettings;
        NotifyAndLog("Connected CroquetBridge to CroquetSettings!");
        Check_BridgeHasSettings();
        CheckAllStatusForReady();
      }
    }
  }

  void Clk_BridgeHasSettings_Goto() { // BRIDGE HAS SETTINGS  ------------- Click
    siSettings.GotoSettings();
  }

  //-- Clicks - JS BUILD --------------------------------
  async void Clk_ToggleJSBuild() { // JS BUILD  ------------- Click
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

  async void Clk_Build_JsNow() { // JS BUILD  ------------- Click
    Debug.Log("Building JS...");
    bool success = await CroquetBuilder.EnsureJSToolsAvailable();
    if (!success) {
      var msg = "JS Build Tools are missing!!!\nCannot build.";
      Debug.LogError(msg);
      EditorUtility.DisplayDialog("Missing JS Tools", msg, "OK");
      return;
    }
    CroquetBuilder.StartBuild(false); // false => no watcher
    AssetDatabase.Refresh();
    CqFile.StreamingAssetsAppFolder().SelectAndPing(false);
    Check_JS_Build();
  }

  void Clk_GotoBuiltOutput() { // JS BUILD  ------------- Click
    var boF = CqFile.StreamingAssetsAppFolder();
    if (!boF.Exists()) {
      NotifyAndLogError("Could not find\nJS Build output folder");
      var ft = new FolderThing(Application.streamingAssetsPath);
      ft.SelectAndPing(false);
      return;
    }
    boF.SelectAndPing();
    EditorApplication.delayCall += ()=>{
      var ixdF = CqFile.AppStreamingAssetsOutputFolder().DeeperFile("index.html");
      if (ixdF.Exists()) ixdF.SelectAndPing(false);
      else {
        NotifyAndLogWarning("Could not find\nindex.html in\nJS Build output folder");
      }
    };
  }

  //-- JS BUILD TOOLS --------------------------------

  private async void Clk_CopyJSBuildTools() { // JS BUILD TOOLS  ------------- Click
    await CroquetBuilder.InstallJSTools();
    Check_JS_BuildTools();
    CheckAllStatusForReady();
  }

  private void Clk_GotoJSBuildToolsFolder() { // JS BUILD TOOLS  ------------- Click
    string croquetJSFolder = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS"));
    string jsBuildFolder   = Path.GetFullPath(Path.Combine(croquetJSFolder, ".js-build"));
    if (!Directory.Exists(jsBuildFolder)) {
      NotifyAndLogError("Could not find\nJS Build Tools folder");
      return;
    }
    NotifyAndLog("CroquetJS/.js-build \nfolder opened\nin Finder/Explorer.");
    EditorUtility.RevealInFinder(jsBuildFolder);
  }

  //-- HAS APP JS --------------------------------

  private void Clk_SetAppName() { // HAS APP JS  ------------- Click
    var cqBridge = FindObjectOfType<CroquetBridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find CroquetBridge in scene!");
      return;
    } else {
      // direct user to enter an appName into the CroquetBridge field for appName
      // select the CroquetBridge in the scene
      Selection.activeGameObject = cqBridge.gameObject;
      EditorGUIUtility.PingObject(cqBridge.gameObject);
      // in 100 ms, show a dialog to the user
      EditorApplication.delayCall += ()=>{
        EditorUtility.DisplayDialog(
          "Set App Name",
          "Enter a name for your app into the CroquetBridge's Session Configuration field \n\nApp Name\n( appName )\n \n\nWe will select it for you, so check the Inspector.",
          "Ok"
        );
      };
    }
  }
  private void Clk_MakeAppJsFile() {
    string fromDir = CqFile.StarterTemplateFolder().longPath;
    string toDir   = CqFile.AppFolder(true).longPath; // here true means log no error if missing
    CroquetBuilder.CopyDirectory(fromDir, toDir);
    AssetDatabase.Refresh();
    Check_HasAppJs();
    CheckAllStatusForReady();
  }
  private void Clk_HasAppJs_Docs() {}

  private void Clk_GotoAppJsFolder() {
    CqFile.AppFolder().DeeperFile("index.js").SelectAndPing();
  }

  private void Clk_GotoAppJsFile() {
    CqFile.AppFolder().DeeperFile("index.js").SelectAndPing();
  }

  //-- Check - BUILT OUTPUT --------------------------------
  private bool Check_BuiltOutput() {
    bool sceneIsDirty = EditorSceneManager.GetActiveScene().isDirty;
    if (sceneIsDirty) {
      MqWelcome_StatusSets.builtOutput.success.Set();
      return false;
    } else {
      MqWelcome_StatusSets.builtOutput.warning.Set(); // best you can get is a warning
    }
    SetVEViz(!sceneIsDirty, Check_Building_Scenes_Btn);
    SetVEViz(sceneIsDirty, Goto_Build_Panel_Btn);
    ShowVEs(Goto_Build_Panel_Btn);
    return sceneIsDirty;
  }

  void SetupUI_BuiltOutput() {
    SetupVisElem("BuiltOutput_Status_Img",            ref BuiltOutput_Status_Img);
    SetupLabel(  "BuiltOutput_Message_Lbl",           ref BuiltOutput_Message_Lbl);
    SetupButton( "Save_Open_Scene_Btn",               ref Save_Open_Scene_Btn,               Clk_Save_Open_Scene);
    SetupButton( "Goto_Build_Panel_Btn",              ref Goto_Build_Panel_Btn,              Clk_Goto_Build_Panel);
    SetupButton( "Check_Building_Scenes_Btn",         ref Check_Building_Scenes_Btn,         Clk_Check_Building_Scenes);
  }

  //-- Clicks - BUILT OUTPUT --------------------------------
  void Clk_Save_Open_Scene() { // Save Open Scene  -  BUILT OUTPUT  ------------- Click
    EditorSceneManager.SaveScene( EditorSceneManager.GetActiveScene() );
  }

  void Clk_Goto_Build_Panel() { // Goto Build  -  BUILT OUTPUT  ------------- Click
    EditorWindow.GetWindow<BuildPlayerWindow>().Show();
  }

  void Clk_Check_Building_Scenes() { // Check -  BUILT OUTPUT  ------------- Click
    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
    var isOk = CqFile.AllScenesHaveBridgeWithAppNameSet();
    MqWelcome_StatusSets.builtOutput.SetIsGood(isOk);
    if (isOk) NotifyAndLog("All scenes have CroquetBridge\n with appName set and\n app folder in StreamingAssets.");
    else      {
      NotifyAndLogError("Some scenes are missing CroquetBridge \nwith appName set or\n app folder in StreamingAssets.");
    }
    doCheckBuiltOutput = false;
    CheckAllStatusForReady();
    doCheckBuiltOutput = true;
  }

  //-- VERSION MATCH - JS BUILD TOOLS --------------------------------
  void SetupUI_VersionMatch() {
    SetupVisElem("JbtVersionMatch_Img",               ref JbtVersionMatch_Img);
    SetupLabel(  "JbtVersionMatch_Message_Lbl",       ref JbtVersionMatch_Message_Lbl);
    SetupButton( "ReinstallTools_Btn",                ref ReinstallTools_Btn,                Clk_ReinstallTools);
    SetupButton( "OpenBuildPanel_Btn",                ref OpenBuildPanel_Btn,                Clk_OpenEditorBuildPanel);
  }
  private bool Check_ToolsVersionMatch() {
    // load the two ".last-installed-tools" files to compare versions and Tools levels
    // of (1) the tools in DotJsBuild and (2) the tools in CroquetBridge
    // var installedToolsForDotJsBuild    = LastInstalled.LoadPath(CroquetBuilder.installedToolsForDotJsBuild_Path);
    // var installedToolsForCroquetBridge = LastInstalled.LoadPath(CroquetBuilder.installedToolsForCroquetBridge_Path);
    var installedToolsForDotJsBuild    = LastInstalled.LoadPath(CroquetBuilder.JSToolsRecordInBuild);
    var installedToolsForCroquetBridge = LastInstalled.LoadPath(CroquetBuilder.JSToolsRecordInEditor);
    bool allMatch = installedToolsForDotJsBuild.IsSameAs(installedToolsForCroquetBridge);

    MqWelcome_StatusSets.versionMatch.SetIsGood(allMatch);
    if (allMatch) {
      Debug.Log("JSTools for Editor & Build match!!!");
    } else {
      Debug.LogError( installedToolsForDotJsBuild.ReportDiffs(installedToolsForCroquetBridge) );
      ShowVEs(ReinstallTools_Btn);
    }
    ShowVEs(ReinstallTools_Btn, OpenBuildPanel_Btn);
    return allMatch;
  }

  void Clk_OpenEditorBuildPanel() { // Open Build - JS BUILD TOOLS  ------------- Click
    EditorWindow.GetWindow<BuildPlayerWindow>().Show();
    EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where( s => s.enabled ).ToArray();
    if (scenes.Length == 0) {
      NotifyAndLogError("No scenes in Build Settings.\nAdd some scenes to build.");
      return;
    }
  }
  void Clk_ReinstallTools() { // VERSION MATCH - JS BUILD TOOLS  ------------- Click
    CroquetMenu.InstallJSTools();
    Check_ToolsVersionMatch();
    CheckAllStatusForReady();
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
    if (label == null) Debug.LogError("Could not find label: " + labelName);
  }

  private void SetupVisElem(string visElemName, ref VisualElement visElem) {
    visElem = rootVisualElement.Query<VisualElement>(visElemName).First();
    if (visElem == null) Debug.LogError("Could not find VisualElement: " + visElemName);
  }

  //=============================================================================

  static public void NotifyAndLog(string msg, float seconds = 4) {
    Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.Log(msg.Replace("\n", " "));
  }
  static public void NotifyAndLogError(string msg, float seconds = 4) {
    Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.LogError(msg.Replace("\n", " "));
  }
  static public void NotifyAndLogWarning(string msg, float seconds = 4) {
    Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.LogWarning(msg.Replace("\n", " "));
  }

  static public void Notify(string msg, float seconds = 4) {
    Instance.ShowNotification(new GUIContent(msg), seconds);
  }

  //=============================================================================
  // private CroquetSettings FindProjectCqSettings() {
  //   CroquetSettings cqSettings = null;
  //   // First check for a CroquetSettings on the scene's CroquetBridge
  //   var bridge = SceneHelp.FindComp<CroquetBridge>();
  //   if (bridge != null && bridge.appProperties != null) {
  //     return bridge.appProperties; // appProperties is a CroquetSettings
  //   }
  //   // Then look in all project folders for a file of CroquetSettings type
  //   cqSettings = SceneHelp.FindCompInProject<CroquetSettings>();
  //   if (cqSettings == null) {
  //     Debug.LogWarning("Could not find CroquetSettings.asset in your Assets folders.");
  //     MqWelcome_StatusSets.settings.error.Set();
  //     MqWelcome_StatusSets.node.error.Set();
  //     MqWelcome_StatusSets.apiKey.error.Set();
  //     MqWelcome_StatusSets.ready.error.Set();
  //   }

  //   return cqSettings;
  // }

  // private CroquetSettings CopyDefaultSettingsFile() {
  //   // string path = ewFolder + "resources/CroquetSettings_Template.asset";
  //   // croquet-for-unity-package/Prefabs/CroquetSettings_Template.asset
  //   string path = CqFile.CqSettingsTemplateFile().shortPath;
  //   CqFile.EnsureAssetsFolder("Croquet");
  //   Debug.Log($"Copying from '{path}' to '{CqFile.cqSettingsAssetOutputPath}'");
  //   bool sourceFileExists = File.Exists(path);
  //   bool targFolderExists = Directory.Exists(Path.GetDirectoryName(CqFile.cqSettingsAssetOutputPath));
  //   AssetDatabase.CopyAsset(path, CqFile.cqSettingsAssetOutputPath);
  //   bool copiedFileExists = File.Exists(CqFile.cqSettingsAssetOutputPath);
  //   Debug.Log($"Source file exists: {sourceFileExists}  Target folder exists: {targFolderExists}, Copied file exists: {copiedFileExists}");
  //   return AssetDatabase.LoadAssetAtPath<CroquetSettings>(CqFile.cqSettingsAssetOutputPath);
  // }

  // private CroquetSettings EnsureSettingsFile() {
  //   CroquetSettings cqSettings = FindProjectCqSettings();
  //   // If not, copy file from ./resources/CroquetSettings_Template.asset
  //   // into Assets/Settings/CroquetSettings.asset
  //   if (cqSettings == null) cqSettings = CopyDefaultSettingsFile();
  //   return cqSettings;
  // }

  //=============================================================================

  private void AllAreReady(bool really = true) {
    if (really) {
      MqWelcome_StatusSets.ready.success.Set();
      ShowVEs(Awesome_Btn);
      countdown_ToConvertSuccesses = 3f;
    } else {
      MqWelcome_StatusSets.ready.error.Set();
      HideVEs(Awesome_Btn);
    }
  }

  //=============================================================================

  public void CheckAllStatusForReady() {
    bool allRdy = true;
    // NEVER: allRdy &= Statuses.ready.IsOk() // NEVER want this
    allRdy &= MqWelcome_StatusSets.settings.IsOk();
    allRdy &= MqWelcome_StatusSets.node.IsOk();
    allRdy &= MqWelcome_StatusSets.apiKey.IsOk();
    allRdy &= MqWelcome_StatusSets.bridge.IsOk();
    allRdy &= MqWelcome_StatusSets.jsBuildTools.IsOk();
    allRdy &= MqWelcome_StatusSets.versionMatch.IsOk();
    allRdy &= MqWelcome_StatusSets.jsBuild.IsOk();
    if (allRdy) AllAreReady();
    else        AllAreReady(false);
  }
  //=============================================================================


  private bool Check_BridgeHasSettings() {
    var bridge = FindObjectOfType<CroquetBridge>();
    if (bridge==null) {
      MqWelcome_StatusSets.bridgeHasSettings.error.Set();
      HideVEs(BridgeHasSettings_AutoConnect_Btn, BridgeHasSettings_Goto_Btn);
      return false;
    }

    bool hasSettings = (bridge.appProperties != null);
    MqWelcome_StatusSets.bridgeHasSettings.SetIsGood(hasSettings);
    if (hasSettings) {
      ShowVEs(BridgeHasSettings_Goto_Btn);
      HideVEs(BridgeHasSettings_AutoConnect_Btn);
    } else ShowVEs(BridgeHasSettings_AutoConnect_Btn);
    return hasSettings;
  }

  private bool Check_JS_BuildTools() {
    var jsBuildNmFolder = CqFile.CroquetJS().DeeperFolder("node_modules");
    bool havejsBuildFolder = jsBuildNmFolder.Exists();
    MqWelcome_StatusSets.jsBuildTools.SetIsGood(havejsBuildFolder);

    if (havejsBuildFolder) {
      ShowVEs(GotoJSBuildToolsFolder_Btn, Build_JsNow_Btn);
      HideVEs(CopyJSBuildTools_Btn);
    } else {
      ShowVEs(CopyJSBuildTools_Btn);
      HideVEs(GotoJSBuildToolsFolder_Btn, Build_JsNow_Btn);
      Debug.LogError($"JS Build Tools are missing from {jsBuildNmFolder.shortPath}");
      Debug.LogError($"JS Build Tools are missing from {jsBuildNmFolder.longPath}");
    }
    return havejsBuildFolder;
  }

  private bool Check_HasAppJs() {
    var cqBridge = FindObjectOfType<CroquetBridge>();
    string appName = cqBridge?.appName;
    if (appName==null || appName=="") {
      MqWelcome_StatusSets.hasAppJs.error.Set();
      ShowVEs(SetAppName_Btn);
      HideVEs(MakeAppJsFile_Btn, GotoAppJsFile_Btn, GotoAppJsFolder_Btn);
      return false;
    }
    var appJsFile      = CqFile.AppIndexJs();
    bool haveAppJsFile = appJsFile.Exists(); // file should be in Assets/CroquetJS/<appName>/index.js
    MqWelcome_StatusSets.hasAppJs.SetIsGood(haveAppJsFile);
    HideVEs(SetAppName_Btn);
    SetVEViz( haveAppJsFile, GotoAppJsFile_Btn, GotoAppJsFolder_Btn );
    SetVEViz(!haveAppJsFile, MakeAppJsFile_Btn);

    return haveAppJsFile;
  }

  private void SetVEViz(bool seen, params VisualElement[] ves) {
    foreach (var ve in ves) ve.style.visibility = seen ? Visibility.Visible : Visibility.Hidden;
  }
  private void ShowVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(true, ve);
  }

  private void HideVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(false, ve);
  }

  private bool Check_JS_Build() {
    bool haveBuiltOutput = CqFile.StreamingAssetsAppFolder().Exists();
    MqWelcome_StatusSets.jsBuild.SetIsGood(haveBuiltOutput);
    if (!haveBuiltOutput) ShowVEs(Build_JsNow_Btn);
    ShowVEs(GotoBuiltOutput_Btn);
    return haveBuiltOutput;
  }

  //=============================================================================



  //=============================================================================
  void Update() {
    Update_DeltaTime();
    Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Ready_Message_Lbl, MqWelcome_StatusSets.ready, true);
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Node_Message_Lbl,     Statuses.node    );
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Key_Message_Lbl,      Statuses.apiKey  );
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, JSBuild_Message_Lbl,  Statuses.jsBuild );
    // Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, Settings_Message_Lbl, Statuses.settings);
  }
  void Update_DeltaTime()  {
    if (lastTime == 0) lastTime = EditorApplication.timeSinceStartup;
    deltaTime = EditorApplication.timeSinceStartup - lastTime;
    lastTime  = EditorApplication.timeSinceStartup;
  }
  void Update_CountdownAndMessage(ref double countdownSeconds, Label messageField, StatusSet status, bool showTimer = false) {

    if (countdownSeconds > 0f) {
      countdownSeconds -= deltaTime;
      if (showTimer) messageField.text = status.success.message + "   <b>" + countdownSeconds.ToString("0.0") + "</b>";
      if (countdownSeconds <= 0f) {
        countdownSeconds = -1;
        messageField.text = status.success.message;
        MqWelcome_StatusSets.SuccessesToReady(); // <=======
      }
    }
  }
}