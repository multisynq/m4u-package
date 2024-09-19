using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;

public class SI_JsBuild: StatusItem {

  public Button Build_JsNow_Btn;
  Button ToggleJSBuild_Btn;
  Button GotoBuiltOutput_Btn;
  Button Docs_Btn;

  public SI_JsBuild(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("JSBuild_Status_Img",   ref statusImage);
    SetupLabel(  "JSBuild_Message_Lbl",  ref messageLabel);
    SetupButton( "ToggleJSBuild_Btn",    ref ToggleJSBuild_Btn,    Clk_ToggleJSBuild); // Start JS Build Watcher
    SetupButton( "Build_JsNow_Btn",      ref Build_JsNow_Btn,      Clk_Build_JsNow);
    SetupButton( "GotoBuiltOutput_Btn",  ref GotoBuiltOutput_Btn,  Clk_GotoBuiltOutput);
    SetupButton( "JSBuild_Docs_Btn",     ref Docs_Btn,             Clk_JsBuild_Docs);
  }

  override public void InitText() {
    StatusSetMgr.jsBuild = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      $"Output JS was built!",
      $"Output JS missing.",
      $"Output JS not found. Need to Build JS.",
      $"Output JS was built! Well done!",
      "JS Build status"
    );
    statusSet = StatusSetMgr.jsBuild;
  }

  override public bool Check() { // SETTINGS
    bool haveBuiltOutput = Mq_File.StreamingAssetsAppFolder().Exists();
    StatusSetMgr.jsBuild.SetIsGood(haveBuiltOutput);
    if (!haveBuiltOutput) ShowVEs(Build_JsNow_Btn);
    ShowVEs(GotoBuiltOutput_Btn);
    return haveBuiltOutput;
  }

  //-- Clicks - JS BUILD --------------------------------
  async void Clk_ToggleJSBuild() { // JS BUILD  ------------- Click
    Logger.MethodHeader();
    if (ToggleJSBuild_Btn.text == "Start JS Build Watcher") {
      bool success = await Mq_Builder.EnsureJSToolsAvailable();
      if (!success) return;
      Mq_Builder.StartBuild(true); // true => start watcher
      Debug.Log("Started JS Build Watcher");
      ToggleJSBuild_Btn.text = "Stop JS Build Watcher";
    } else {
      ToggleJSBuild_Btn.text = "Start JS Build Watcher";
      Mq_Builder.StopWatcher();
    }
  }

  async public void Clk_Build_JsNow() { // JS BUILD  ------------- Click
    Logger.MethodHeader();
    Debug.Log("Building JS...");
    bool success = await Mq_Builder.EnsureJSToolsAvailable();
    if (!success) {
      var msg = "JS Build Tools are missing!!!\nCannot build.";
      Debug.LogError(msg);
      EditorUtility.DisplayDialog("Missing JS Tools", msg, "OK");
      return;
    }
    Mq_Builder.StartBuild(false); // false => no watcher
    AssetDatabase.Refresh();
    Mq_File.StreamingAssetsAppFolder().SelectAndPing(false);
    Check(); // recheck (this SI_JsBuild)
  }

  void Clk_GotoBuiltOutput() { // JS BUILD  ------------- Click
    Logger.MethodHeader();
    var boF = Mq_File.StreamingAssetsAppFolder();
    if (!boF.Exists()) {
      NotifyAndLogError("Could not find\nJS Build output folder");
      var ft = new FolderThing(Application.streamingAssetsPath);
      ft.SelectAndPing(false);
      return;
    }
    boF.SelectAndPing();
    EditorApplication.delayCall += ()=>{
      var ixdF = Mq_File.AppStreamingAssetsOutputFolder().DeeperFile("index.html");
      if (ixdF.Exists()) ixdF.SelectAndPing(false);
      else {
        NotifyAndLogWarning("Could not find\nindex.html in\nJS Build output folder");
      }
    };
  }

  void Clk_JsBuild_Docs() {
    Logger.MethodHeaderAndOpenUrl();
  }
}
