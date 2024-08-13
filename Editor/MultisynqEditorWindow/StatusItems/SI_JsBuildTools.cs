using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
      // (info, warning, error, success)
      "JS Build Tools are ready to go!",
      "JS Build Tools are missing",
      "JS Build Tools are missing! Click <b>Copy JS Build Tools</b> to get them.",
      "JS Build Tools installed!!! Well done!",
      "JS Build Tools status"
    );
    statusSet = StatusSetMgr.jsBuildTools;
  }

  override public bool Check() { // SETTINGS
    var cqJsNodeModulesFolder = CqFile.CroquetJS().DeeperFolder("node_modules");
    bool haveFolder = cqJsNodeModulesFolder.Exists();
    StatusSetMgr.jsBuildTools.SetIsGood(haveFolder);

    if (haveFolder) {
      ShowVEs(GotoJSBuildToolsFolder_Btn, edWin.siJsBuild.Build_JsNow_Btn);
      HideVEs(CopyJSBuildTools_Btn);
    } else {
      ShowVEs(CopyJSBuildTools_Btn);
      HideVEs(GotoJSBuildToolsFolder_Btn, edWin.siJsBuild.Build_JsNow_Btn);
      Debug.LogError($"JS Build Tools are missing from {cqJsNodeModulesFolder.shortPath}");
      Debug.LogError($"JS Build Tools are missing from {cqJsNodeModulesFolder.longPath}");
    }
    return haveFolder;
  }
  
  //-- JS BUILD TOOLS --------------------------------

  private async void Clk_CopyJSBuildTools() { // JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader();
    await CroquetBuilder.InstallJSTools();
    Check(); // recheck (this SI_JsBuildTools)
    edWin.CheckAllStatusForReady();
  }

  private void Clk_GotoJSBuildToolsFolder() { // JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader();
    var croquetJSFolder = CqFile.CroquetJS();
    if (croquetJSFolder.Exists()) {
      NotifyAndLog("Assets/CroquetJS/ \nfolder opened\nin Finder/Explorer.");
      EditorUtility.RevealInFinder(croquetJSFolder.longPath);
    } else {
      NotifyAndLogError("Could not find\nCroquetJS folder");
    }
  }

  private void Clk_JsBuildTools_Docs() {
    Logger.MethodHeaderAndOpenUrl();
  }

}
