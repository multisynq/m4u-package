using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_JsBuild: StatusItem {

  
  public Button Build_JsNow_Btn;
  Button ToggleJSBuild_Btn;
  Button GotoBuiltOutput_Btn;

  public SI_JsBuild(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("JSBuild_Status_Img",  ref statusImage);
    SetupLabel(  "JSBuild_Message_Lbl", ref messageLabel);
    SetupButton( "ToggleJSBuild_Btn",   ref ToggleJSBuild_Btn,    Clk_ToggleJSBuild); // Start JS Build Watcher
    SetupButton( "Build_JsNow_Btn",     ref Build_JsNow_Btn,      Clk_Build_JsNow);
    SetupButton( "GotoBuiltOutput_Btn", ref GotoBuiltOutput_Btn,  Clk_GotoBuiltOutput);
  }

  override public void InitText() {
    StatusSetMgr.jsBuild = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success, blank)
      $"Output JS was built!",
      $"Output JS missing.",
      $"Output JS not found. Need to Build JS.",
      $"Output JS was built! Well done!",
      "JS Build status"
    );
    statusSet = StatusSetMgr.jsBuild;
  }

  override public bool Check() { // SETTINGS
    bool haveBuiltOutput = CqFile.StreamingAssetsAppFolder().Exists();
    StatusSetMgr.jsBuild.SetIsGood(haveBuiltOutput);
    if (!haveBuiltOutput) ShowVEs(Build_JsNow_Btn);
    ShowVEs(GotoBuiltOutput_Btn);
    return haveBuiltOutput;
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
    Check(); // recheck (this SI_JsBuild)
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
}
