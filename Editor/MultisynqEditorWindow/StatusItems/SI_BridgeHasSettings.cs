using UnityEngine;
using UnityEngine.UIElements;
using MultisynqNS;

public class SI_BridgeHasSettings: StatusItem {

  Button BridgeHasSettings_AutoConnect_Btn;
  Button BridgeHasSettings_Goto_Btn;
  Button BridgeHasSettings_Docs;

  public SI_BridgeHasSettings(MultisynqBuildAssistantEW parent = null) : base(parent) {
    
  }

  override public void InitUI() {
    SetupVisElem("BridgeHasSettings_Img",             ref statusImage);
    SetupLabel(  "BridgeHasSettings_Message_Lbl",     ref messageLabel);
    SetupButton( "BridgeHasSettings_AutoConnect_Btn", ref BridgeHasSettings_AutoConnect_Btn, Clk_BridgeHasSettings_AutoConnect);
    SetupButton( "BridgeHasSettings_Goto_Btn",        ref BridgeHasSettings_Goto_Btn,        Clk_BridgeHasSettings_Goto);
    SetupButton( "BridgeHasSettings_Docs_Btn",        ref BridgeHasSettings_Docs,            Clk_BridgeHasSettings_Docs);
  }

  override public void InitText() {
    StatusSetMgr.bridgeHasSettings = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      "Bridge has settings!",
      "Bridge is missing settings!",
      "Bridge is missing settings! Click <b>Auto Connect</b> to connect it.",
      "Bridge connected to settings!!! Well done!",
      "Bridge's Settings status"
    );
    statusSet = StatusSetMgr.bridgeHasSettings;
  }

  override public bool Check() { // SETTINGS
    var bridge = Object.FindObjectOfType<Mq_Bridge>();
    if (bridge==null) {
      StatusSetMgr.bridgeHasSettings.error.Set();
      HideVEs(BridgeHasSettings_AutoConnect_Btn, BridgeHasSettings_Goto_Btn);
      return false;
    }

    bool hasSettings = (bridge.appProperties != null);
    StatusSetMgr.bridgeHasSettings.SetIsGood(hasSettings);
    if (hasSettings) {
      ShowVEs(BridgeHasSettings_Goto_Btn);
      HideVEs(BridgeHasSettings_AutoConnect_Btn);
    } else ShowVEs(BridgeHasSettings_AutoConnect_Btn);
    return hasSettings;
  }

  //-- Clicks - BRIDGE HAS SETTINGS --------------------------------

  void Clk_BridgeHasSettings_AutoConnect() { // BRIDGE HAS SETTINGS  ------------- Click
    Logger.MethodHeader();
    var bridge = Object.FindObjectOfType<Mq_Bridge>();
    if (bridge == null) {
      NotifyAndLogError("Could not find Mq_Bridge in scene!");
      return;
    } else {
      var cqSettings = StatusSetMgr.FindProjectCqSettings();
      if (cqSettings == null) {
        NotifyAndLogError("Could not find Mq_Settings in project!");
        return;
      } else {
        bridge.appProperties = cqSettings;
        NotifyAndLog("Connected Mq_Bridge to Mq_Settings!");
        Check(); // recheck self (SI_BridgeHasSettings)
        edWin.CheckAllStatusForReady();
      }
    }
  }

  void Clk_BridgeHasSettings_Goto() { // BRIDGE HAS SETTINGS  ------------- Click
    Logger.MethodHeader();
    edWin.siSettings.GotoSettings();
  }

  void Clk_BridgeHasSettings_Docs() {
    Logger.MethodHeaderAndOpenUrl();
  }

}
