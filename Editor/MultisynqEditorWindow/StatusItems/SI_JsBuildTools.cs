using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;


public class SI_JsBuildTools: StatusItem {

  Button CopyJSBuildTools_Btn;
  Button GotoJSBuildToolsFolder_Btn;
  Button Docs_Btn;

  public SI_JsBuildTools(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("JSBuildTools_Img",           ref statusImage);
    SetupLabel(  "JSBuildTools_Message_Lbl",   ref messageLabel);
    SetupButton( "CopyJSBuildTools_Btn",       ref CopyJSBuildTools_Btn,       Clk_CopyJSBuildTools);
    SetupButton( "GotoJSBuildToolsFolder_Btn", ref GotoJSBuildToolsFolder_Btn, Clk_GotoJSBuildToolsFolder);
    SetupButton( "JSBuildTools_Docs_Btn",      ref Docs_Btn,                   Clk_JsBuildTools_Docs);
  }

  override public void InitText() {
    StatusSetMgr.jsBuildTools = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      "JS Build Tools are ready to go!",
      "JS Build Tools are missing",
      "JS Build Tools are missing! Click <b>Copy JS Build Tools</b> to get them.",
      "JS Build Tools installed!!! Well done!",
      "JS Build Tools status"
    );
    statusSet = StatusSetMgr.jsBuildTools;
  }

  override public bool Check() { // SETTINGS
    bool haveBuildTools = true;
    var rootDir = Mq_File.RootFolder();
    haveBuildTools &= rootDir.DeeperFolder("node_modules").Exists();
    haveBuildTools &= rootDir.DeeperFile(  "package.json").Exists();
    haveBuildTools &= Mq_File.MultisynqJS().Exists();
    haveBuildTools &= Mq_File.MultisynqJS().DeeperFile("package.json").Exists();
    haveBuildTools &= Mq_File.MultisynqJS().DeeperFile("package-lock.json").Exists();
    haveBuildTools &= Mq_File.StreamingAssets_Dir().DeeperFile("build/Release/node_datachannel.node").Exists();

    StatusSetMgr.jsBuildTools.SetIsGood(haveBuildTools);

    if (haveBuildTools) {
      ShowVEs(GotoJSBuildToolsFolder_Btn, edWin.siJsBuild.Build_JsNow_Btn);
      HideVEs(CopyJSBuildTools_Btn);
    } else {
      ShowVEs(CopyJSBuildTools_Btn);
      HideVEs(GotoJSBuildToolsFolder_Btn, edWin.siJsBuild.Build_JsNow_Btn);
    }
    return haveBuildTools;
  }
  
  //-- JS BUILD TOOLS --------------------------------

  private async void Clk_CopyJSBuildTools() { // JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader(6);
    await Mq_Builder.InstallJSTools();
    Check(); // recheck (this SI_JsBuildTools)
    edWin.CheckAllStatusForReady();
  }

  private void Clk_GotoJSBuildToolsFolder() { // JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader(4);
    var mqJSFolder = Mq_File.MultisynqJS();
    if (mqJSFolder.Exists()) {
      NotifyAndLog("Assets/MultisynqJS/ \nfolder opened\nin Finder/Explorer.");
      EditorUtility.RevealInFinder(mqJSFolder.longPath);
    } else {
      NotifyAndLogError("Could not find\nMultisynqJS folder");
    }
  }

  private void Clk_JsBuildTools_Docs() {
    Logger.MethodHeaderAndOpenUrl();
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#js-build-tools");
  }

}
