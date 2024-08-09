using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

//------------------ ||||||||||||||||||||||||| ----------------------------------
public partial class MultisynqBuildAssistantEW : EditorWindow {

  //=============================================================================
  public double countdown_ToConvertSuccesses = -1;
  double lastTime = 0;
  double deltaTime = 0;

  Button CheckIfReady_Btn; // CHECK IF READY

  //==== Status Items (SI_) =====================================================
  public SI_ReadyTotal        siReadyTotal;
  public SI_Settings          siSettings;
  public SI_Node              siNode;
  public SI_ApiKey            siApiKey;
  public SI_Bridge            siBridge;
  public SI_Systems           siSystems;
  public SI_BridgeHasSettings siBridgeHasSettings;
  public SI_JsBuildTools      siJsBuildTools;
  public SI_HasAppJs          siHasAppJs;
  public SI_JsBuild           siJsBuild;
  public SI_JbtVersionMatch   siJbtVersionMatch;
  public SI_BuiltOutput       siBuiltOutput;

  //====== EditowWindow Init (auto-called when Shown) ==================================
  public void CreateGUI() {

    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CqFile.ewFolder + "MultisynqBuildAssistant_UI.uxml");
    var labelFromUXML = visualTree.Instantiate();
    rootVisualElement.Add(labelFromUXML);

    SetupUI();
    StatusSet.InitTextures();
    StatusItem.AllStatusSetsToBlank();
  }

  //=============================================================================

  private void SetupUI() {
    StatusItem.ClearStaticLists();
    siReadyTotal        = new SI_ReadyTotal(this);
    siReadyTotal.SetupButton("CheckIfReady_Btn", ref CheckIfReady_Btn, Clk_CheckIfReady);
    siSettings          = new SI_Settings(this);
    siNode              = new SI_Node(this);
    siApiKey            = new SI_ApiKey(this);
    siBridge            = new SI_Bridge(this);
    siSystems           = new SI_Systems(this);
    siBridgeHasSettings = new SI_BridgeHasSettings(this);
    siJsBuildTools      = new SI_JsBuildTools(this);
    siHasAppJs          = new SI_HasAppJs(this);
    siJsBuild           = new SI_JsBuild(this);
    siJbtVersionMatch   = new SI_JbtVersionMatch(this);
    siBuiltOutput       = new SI_BuiltOutput(this);    

    StatusItem.HideMostButtons();
  }

  //=============================================================================

  //-- Clicks - CHECK READINESS --------------------------------
  private void Clk_CheckIfReady() { // CHECK READINESS  ------------- Click
    Debug.Log($"<color=#006AFF>============= [ <color=#0196FF>Check If Ready</color> ] =============</color>");
    CheckAllStatusForReady();
  }

  //=============================================================================
  public void CheckAllStatusForReady() {
    bool allRdy = true;
    // NEVER: allRdy &= Statuses.ready.IsOk() // NEVER want this
    allRdy &= siSettings.Check();
    allRdy &= siNode.Check();
    allRdy &= siApiKey.Check();
    allRdy &= siBridge.Check();
    allRdy &= siSystems.Check();
    allRdy &= siBridgeHasSettings.Check();
    allRdy &= siJsBuildTools.Check();
    allRdy &= siHasAppJs.Check();
    allRdy &= siJbtVersionMatch.Check();
    allRdy &= siJsBuild.Check();
    allRdy &= siBuiltOutput.Check();
    siNode.NodePathsToDropdownAndCheck();

    siReadyTotal.AllAreReady(allRdy);
  }

  //=============================================================================
  void Update() {
    Update_DeltaTime();
    Update_CountdownAndMessage(ref countdown_ToConvertSuccesses, siReadyTotal.messageLabel, StatusSetMgr.ready, true);
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
        StatusItem.AllSuccessesToReady(); // <=======
      }
    }
  }

  //====== Singleton ============================================================
  static private MultisynqBuildAssistantEW _Instance;
  static public MultisynqBuildAssistantEW Instance {
    get {
      if (_Instance == null)_Instance = GetWindow<MultisynqBuildAssistantEW>(); // <-- this will auto-create the window
      return _Instance;
    }
    private set{}
  }
  void OnDestroy() {
    _Instance = null;
  }

  //====== Open _this_ Editor Window from   >>>> MENU <<<<    =============================
  [MenuItem("Croquet/Open Croquet Build Assistant Window...",priority=0)]
  [MenuItem("Window/Croquet/Open Build Assistant...",priority=1000)]
  public static void ShowMultisynqWelcome_MenuMethod() {
    if (_Instance != null) _Instance.Close(); // First destroy the old one...
    var icon = AssetDatabase.LoadAssetAtPath<Texture>(CqFile.ewFolder + "Images/MultiSynq_Icon.png");
    Instance.titleContent = new GUIContent("Croquet Build Assistant", icon); // Referencing the Instance property will auto-create the window
  }

  [UnityEditor.Callbacks.DidReloadScripts]
  private static void OnScriptsReloaded() { // detect recompile and reopen
    if (_Instance != null) ShowMultisynqWelcome_MenuMethod();
  }
}
