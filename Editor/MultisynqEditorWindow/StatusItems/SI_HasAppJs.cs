using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;

public class SI_HasAppJs: StatusItem {

  Button SetAppName_Btn;
  Button MakeAppJsFile_Btn;
  Button HasAppJs_Docs_Btn;
  Button GotoAppJsFile_Btn;
  Button GotoAppJsFolder_Btn;

  public SI_HasAppJs(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("HasAppJs_Status_Img",  ref statusImage);
    SetupLabel(  "HasAppJs_Message_Lbl", ref messageLabel);
    SetupButton( "SetAppName_Btn",       ref SetAppName_Btn,      Clk_SetAppName);
    SetupButton( "MakeAppJsFile_Btn",    ref MakeAppJsFile_Btn,   Clk_MakeAppJsFile);
    SetupButton( "HasAppJs_Docs_Btn",    ref HasAppJs_Docs_Btn,   Clk_HasAppJs_Docs);
    SetupButton( "GotoAppJsFile_Btn",    ref GotoAppJsFile_Btn,   Clk_GotoAppJsFile);
    SetupButton( "GotoAppJsFolder_Btn",  ref GotoAppJsFolder_Btn, Clk_GotoAppJsFolder);
  }

  override public void InitText() {
    StatusSetMgr.hasAppJs = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      "Input JS: index.js for AppName is ready to go!",
      "Input JS: index.js for AppName is missing",
      "Input JS: index.js for AppName is missing!",
      "Input JS: index.js for AppName found! Well done!",
      "Input JS: index.js for AppName status"
    );
    statusSet = StatusSetMgr.hasAppJs;
  }

  override public bool Check() { // SETTINGS
    var cqBridge = Object.FindObjectOfType<Mq_Bridge>();

    string appName = cqBridge?.appName;
    if (appName==null || appName=="") {
      StatusSetMgr.hasAppJs.error.Set();
      ShowVEs(SetAppName_Btn);
      HideVEs(MakeAppJsFile_Btn, GotoAppJsFile_Btn, GotoAppJsFolder_Btn);
      return false;
    }
    var appJsFile      = Mq_File.AppIndexJs();
    bool haveAppJsFile = appJsFile.Exists(); // file should be in Assets/MultisynqJS/<appName>/index.js
    StatusSetMgr.hasAppJs.SetIsGood(haveAppJsFile);
    HideVEs(SetAppName_Btn);
    SetVEViz( haveAppJsFile, GotoAppJsFile_Btn, GotoAppJsFolder_Btn );
    SetVEViz(!haveAppJsFile, MakeAppJsFile_Btn);

    return haveAppJsFile;
  }

  private void Clk_SetAppName() { // HAS APP JS  ------------- Click
    Logger.MethodHeader();
    var cqBridge = Object.FindObjectOfType<Mq_Bridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find Mq_Bridge in scene!");
      return;
    } else {
      // direct user to enter an appName into the Mq_Bridge field for appName
      // select the Mq_Bridge in the scene
      Selection.activeGameObject = cqBridge.gameObject;
      EditorGUIUtility.PingObject(cqBridge.gameObject);
      // in 100 ms, show a dialog to the user
      EditorApplication.delayCall += ()=>{
        EditorUtility.DisplayDialog(
          "Set App Name",
          "Enter a name for your app into the Mq_Bridge's Session Configuration field \n\nApp Name\n( appName )\n \n\nWe will select it for you, so check the Inspector.",
          "Ok"
        );
      };
    }
  }

  private void Clk_MakeAppJsFile() { // HAS APP JS  ------------- Click
    Logger.MethodHeader();
    Notify("Please wait. \n\nImporting node_modules\nand copying starter\ntemplate files...");
    if (Mq_File.AppPluginsFolder().Exists()) {
      var indexJs = Mq_File.AppFolder(true).DeeperFile("index.js");
      if (indexJs.Exists()) {
        if (JsPlugin_Behaviour.CheckIndexJsForPluginsImport()) {
          Notify("index.js already exists and is ready to go!");
          return;
        } else {
          var msg = "index.js already exists, \nbut is missing the import \nstatement for the plugins folder.";
          Notify(msg);
          // dialog ask
          if (EditorUtility.DisplayDialog(
            "Add Import Statement?",
            $"{indexJs.shortPath} already exists, \nbut is MISSING needed \nJsPlugin imports.\nExisting code kept, but commented out for you to merge.\n\nReplace code there to fix?",
            "Yes", "No"
          )) {
            JsPlugin_Writer.PrependPluginCodeAndWrapExistingCodeInCommentMarkers();
          }
        }
      }
    }
    string fromDir = Mq_File.StarterTemplateFolder().longPath;
    string toDir   = Mq_File.AppFolder(true).longPath; // here true means log no error if missing
    Mq_Builder.CopyDirectory(fromDir, toDir);
    AssetDatabase.Refresh();
    Check(); // recheck (this SI_HasAppJs)
    edWin.CheckAllStatusForReady();
  }

  private void Clk_HasAppJs_Docs() {// HAS APP JS  ------------- Click
    Logger.MethodHeaderAndOpenUrl();
  }

  private void Clk_GotoAppJsFolder() {// HAS APP JS  ------------- Click
    Logger.MethodHeader();
    Mq_File.AppFolder().DeeperFile("index.js").SelectAndPing();
  }

  private void Clk_GotoAppJsFile() {// HAS APP JS  ------------- Click
    Logger.MethodHeader();
    Mq_File.AppFolder().DeeperFile("index.js").SelectAndPing();
  }

}
