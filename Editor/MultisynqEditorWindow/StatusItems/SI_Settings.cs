using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_Settings: StatusItem {

  Button GotoSettings_Btn;
  Button SettingsCreate_Btn;
  Button GotoNodePath_Btn;
  Button GotoApiKey_Btn;
  Button Docs_Btn;

  public SI_Settings(MultisynqBuildAssistantEW parent = null) : base(parent){}

  override public void InitUI() {
    SetupVisElem("Settings_Status_Img",  ref statusImage);
    SetupLabel(  "Settings_Message_Lbl", ref messageLabel);
    SetupButton( "GotoSettings_Btn",     ref GotoSettings_Btn,   Clk_GotoSettings);
    SetupButton( "SettingsCreate_Btn",   ref SettingsCreate_Btn, Clk_SettingsCreate);
    SetupButton( "GotoNodePath_Btn",     ref GotoNodePath_Btn,   null);
    SetupButton( "GotoApiKey_Btn",       ref GotoApiKey_Btn,     null);
    SetupButton( "Settings_Docs_Btn",    ref Docs_Btn,           Clk_Settings_Docs, false);
  }
  override public void InitText() {
    StatusSetMgr.settings = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      $"Settings are ready to go!",
      $"Settings are set to defaults! Look for other red items below to fix this.",
      $"Settings asset is missing! Click <b>Create Settings</b> to make some.",
      $"Settings are configured!!! Well done!",
      "Settings status"
    );
    GotoSettings_Btn.SetEnabled(false);
    statusSet = StatusSetMgr.settings;
  }
  override public bool Check() { // SETTINGS
    var cqStgs = StatusSetMgr.FindProjectCqSettings();
    if (cqStgs == null) {
      GotoSettings_Btn.SetEnabled(false);
      ShowVEs(SettingsCreate_Btn);
      StatusSetMgr.settings.error.Set();
      return false;
    } else {
      GotoSettings_Btn.SetEnabled(true);
      StatusSetMgr.settings.success.Set();
      HideVEs(SettingsCreate_Btn);
      ShowVEs(GotoSettings_Btn);
      return true;
    }
  }
  
  private void Clk_GotoSettings() { // SETTINGS  ------------- Click
    Logger.MethodHeader();
    GotoSettings();
    Notify("Selected in Project.\nSee Inspector.");
  }


  private void Clk_SettingsCreate() { // SETTINGS  ------------- Click
    Logger.MethodHeader();
    // Mq_Settings in scene
    var cqStgs = Mq_Project.EnsureSettingsFile();
    StatusSetMgr.ready.SetIsGood(cqStgs != null);
    if (cqStgs == null) Debug.LogError("Could not find or create Mq_Settings file");
    GotoSettings();
    ShowVEs(GotoNodePath_Btn, GotoApiKey_Btn);
    Check();
    edWin.CheckAllStatusForReady();
  }

  public void GotoSettings() { // SETTINGS  ------------- Click
    // Select the file in Project pane of the Editor so it shows up in the Inspector
    var cqStgs = StatusSetMgr.FindProjectCqSettings();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create Mq_Settings file");
      return;
    } else {
      Notify("Selected in Project.\nSettings in Inspector.");
      Selection.activeObject = cqStgs;            // Select the settings you are using
      ProjectWindowUtil.ShowCreatedAsset(cqStgs); // Also show selection in Project pane
      EditorGUIUtility.PingObject(cqStgs);        // highlight in yellow
    }
  }

  private void Clk_Settings_Docs() {
    Logger.MethodHeaderAndOpenUrl(4);
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#settings");
  }

}
