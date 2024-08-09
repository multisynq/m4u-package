using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_BridgeHasSettings: StatusItem {

  Button BridgeHasSettings_AutoConnect_Btn;
  Button BridgeHasSettings_Goto_Btn;

    public SI_BridgeHasSettings(MultisynqBuildAssistantEW parent = null) : base(parent)
    {
    }

    override public void InitUI() {
    SetupVisElem("BridgeHasSettings_Img",             ref statusImage);
    SetupLabel(  "BridgeHasSettings_Message_Lbl",     ref messageLabel);
    SetupButton( "BridgeHasSettings_AutoConnect_Btn", ref BridgeHasSettings_AutoConnect_Btn, Clk_BridgeHasSettings_AutoConnect);
    SetupButton( "BridgeHasSettings_Goto_Btn",        ref BridgeHasSettings_Goto_Btn,        Clk_BridgeHasSettings_Goto);
  }

  override public void InitText() {
    MqWelcome_StatusSets.bridgeHasSettings = new StatusSet( messageLabel, statusImage,
      // ... info, warning, error, success)
      "Bridge has settings!",
      "Bridge is missing settings!",
      "Bridge is missing settings! Click <b>Auto Connect</b> to connect it.",
      "Bridge connected to settings!!! Well done!",
      "Bridge's Settings status"
    );
    statusSet = MqWelcome_StatusSets.bridgeHasSettings;
  }

  override public bool Check() { // SETTINGS
    var bridge = Object.FindObjectOfType<CroquetBridge>();
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

  //-- Clicks - BRIDGE HAS SETTINGS --------------------------------

  void Clk_BridgeHasSettings_AutoConnect() { // BRIDGE HAS SETTINGS  ------------- Click
    var bridge = Object.FindObjectOfType<CroquetBridge>();
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
        Check(); // recheck self (SI_BridgeHasSettings)
        edWin.CheckAllStatusForReady();
      }
    }
  }

  void Clk_BridgeHasSettings_Goto() { // BRIDGE HAS SETTINGS  ------------- Click
    edWin.siSettings.GotoSettings();
  }


}
