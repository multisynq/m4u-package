using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_BuiltOutput: StatusItem {

  Button Save_Open_Scene_Btn;
  Button Goto_Build_Panel_Btn;
  Button Check_Building_Scenes_Btn;
  // Button BuiltOutput_Docs_Btn;
  bool skipCheckingThisSi = false;

  public SI_BuiltOutput(MultisynqBuildAssistantEW parent = null) : base(parent){}

  override public void InitUI() {
    SetupVisElem("BuiltOutput_Status_Img",     ref statusImage);
    SetupLabel(  "BuiltOutput_Message_Lbl",    ref messageLabel);
    SetupButton( "Save_Open_Scene_Btn",        ref Save_Open_Scene_Btn,       Clk_Save_Open_Scene);
    SetupButton( "Goto_Build_Panel_Btn",       ref Goto_Build_Panel_Btn,      Clk_Goto_Build_Panel);
    SetupButton( "Check_Building_Scenes_Btn",  ref Check_Building_Scenes_Btn, Clk_Check_Building_Scenes);
  }
  override public void InitText() {
    StatusSetMgr.builtOutput = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success, blank)
      $"Built output folders match the building scene list!",
      $"Compare output JS folders to Unity Build scene list with [ Check Building Scenes ] button.",
      $"Compare output JS folders to Unity Build scene list with [ Check Building Scenes ] button.",
      $"Built output folders match the building scene list! Well done!",
      "Built output status"
    );
    statusSet = StatusSetMgr.builtOutput;
  }
  
  override public bool Check() { // BUILT OUTPUT
    if (skipCheckingThisSi) return true; // <<<<<<<<<<
    bool sceneIsDirty = EditorSceneManager.GetActiveScene().isDirty;
    if (sceneIsDirty) {
      StatusSetMgr.builtOutput.success.Set();
      return false;
    } else {
      StatusSetMgr.builtOutput.warning.Set(); // best you can get is a warning
    }
    SetVEViz(!sceneIsDirty, Check_Building_Scenes_Btn);
    SetVEViz(sceneIsDirty, Goto_Build_Panel_Btn);
    ShowVEs(Goto_Build_Panel_Btn);
    return sceneIsDirty;
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
    StatusSetMgr.builtOutput.SetIsGood(isOk);
    if (isOk) NotifyAndLog("All scenes have CroquetBridge\n with appName set and\n app folder in StreamingAssets.");
    else      {
      NotifyAndLogError("Some scenes are missing CroquetBridge \nwith appName set or\n app folder in StreamingAssets.");
    }
    skipCheckingThisSi = true; // prevent double-checking
    edWin.CheckAllStatusForReady();
    skipCheckingThisSi = false;
  }

  
}
