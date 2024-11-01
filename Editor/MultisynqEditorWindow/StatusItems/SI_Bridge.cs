using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;

public class SI_Bridge: StatusItem {

  Button GotoBridgeGob_Btn;
  Button CreateBridgeGob_Btn;
  Button Docs_Btn;

  public SI_Bridge(MultisynqBuildAssistantEW parent = null) : base(parent) {
    
  }

  override public void InitUI() {
    SetupVisElem("HaveBridge_Status_Img",  ref statusImage);
    SetupLabel(  "HaveBridge_Message_Lbl", ref messageLabel);
    SetupButton( "GotoBridgeGob_Btn",      ref GotoBridgeGob_Btn,   Clk_GotoBridgeGob);
    SetupButton( "CreateBridgeGob_Btn",    ref CreateBridgeGob_Btn, Clk_CreateBridgeGob);
    SetupButton( "HaveBridge_Docs_Btn",    ref Docs_Btn,            Clk_Bridge_Docs, false);
  }

  override public void InitText() {
    StatusSetMgr.bridge = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      "Bridge GameObject is ready to go!",
      "Bridge GameObject is missing!",
      "Bridge GameObject is missing in scene! Click <b>Create Bridge</b> to make one.",
      "Bridge Gob <color=#888888>(GameObject)</color> found!! Well done!",
      "Bridge GameObject status"
    );
    statusSet = StatusSetMgr.bridge;
  }

  override public bool Check() { // BRIDGE
    var bridge = Object.FindObjectOfType<Mq_Bridge>();
    bool fountIt = (bridge != null);
    StatusSetMgr.bridge.SetIsGood(fountIt);
    SetVEViz( fountIt, GotoBridgeGob_Btn  );
    SetVEViz(!fountIt, CreateBridgeGob_Btn);
    return fountIt;
  }

  //-- Clicks - BRIDGE --------------------------------

  private void Clk_GotoBridgeGob() { // BRIDGE  ------------- Click
    Logger.MethodHeader();
    var bridge = Object.FindObjectOfType<Mq_Bridge>();  // find ComponentType Mq_Bridge in scene
    if (bridge == null) NotifyAndLogError("Could not find\nMq_Bridge in scene!");
    else {
      Selection.activeGameObject = bridge.gameObject; // select in Hierachy
      EditorGUIUtility.PingObject(bridge.gameObject); // highlight in yellow for a sec
      NotifyAndLog("Selected in Hierarchy.\nSee Mq_Bridge in Inspector.");
    }
  }

  private void Clk_CreateBridgeGob() { // BRIDGE  ------------- Click
    Logger.MethodHeader();
    var bridge = Object.FindObjectOfType<Mq_Bridge>();
    if (bridge != null) {
      string msg = "Mq_Bridge already exists in scene";
      Notify(msg); Debug.LogError(msg);
    } else {
      var cbGob = new GameObject("Multisynq");
      var cb = cbGob.AddComponent<Mq_Bridge>();
      cbGob.AddComponent<Mq_Runner>();
      cbGob.AddComponent<Mq_Entity_System>();
      cbGob.AddComponent<Mq_Spatial_System>();
      cbGob.AddComponent<Mq_Material_System>();
      cbGob.AddComponent<Mq_FileReader>();
      var cqStgs = StatusSetMgr.FindProjectCqSettings();
      if (cqStgs != null) cb.appProperties = cqStgs;

      Selection.activeGameObject = cbGob;
      string msg = "Created Mq_Bridge\nGameObject in scene.\nSelected it.";
      Notify(msg); Debug.Log(msg);
    }
    Check(); // check self (bridge)
    edWin.siSettings.Check();    // Check_BridgeHasSettings();
    edWin.siSystems.Check(); // Check_HasCqSystems();
    edWin.CheckAllStatusForReady();
  }

  private void Clk_Bridge_Docs() {
    Logger.MethodHeaderAndOpenUrl();
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#bridge-gameobject");
  }

}
