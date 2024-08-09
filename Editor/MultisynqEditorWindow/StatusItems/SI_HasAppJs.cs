using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_HasAppJs: StatusItem {

  Button SetAppName_Btn;
  Button MakeAppJsFile_Btn;
  Button HasAppJs_Docs_Btn;
  Button GotoAppJsFile_Btn;
  Button GotoAppJsFolder_Btn;

  public SI_HasAppJs(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    //Debug.Log("SI_HasAppJs.InitUI()");

    SetupVisElem("HasAppJs_Status_Img",               ref statusImage);
    SetupLabel(  "HasAppJs_Message_Lbl",              ref messageLabel);
    SetupButton( "SetAppName_Btn",                    ref SetAppName_Btn,                    Clk_SetAppName);
    SetupButton( "MakeAppJsFile_Btn",                 ref MakeAppJsFile_Btn,                 Clk_MakeAppJsFile);
    SetupButton( "HasAppJs_Docs_Btn",                 ref HasAppJs_Docs_Btn,                 Clk_HasAppJs_Docs);
    SetupButton( "GotoAppJsFile_Btn",                 ref GotoAppJsFile_Btn,                 Clk_GotoAppJsFile);
    SetupButton( "GotoAppJsFolder_Btn",               ref GotoAppJsFolder_Btn,               Clk_GotoAppJsFolder);
  }

  override public void InitText() {
    //Debug.Log("SI_HasAppJs.InitText()");
    MqWelcome_StatusSets.hasAppJs = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success)
      "Input JS: index.js for AppName is ready to go!",
      "Input JS: index.js for AppName is missing",
      "Input JS: index.js for AppName is missing!",
      "Input JS: index.js for AppName found! Well done!",
      "Input JS: index.js for AppName status"
    );
    statusSet = MqWelcome_StatusSets.hasAppJs;
  }

  override public bool Check() { // SETTINGS
    //Debug.Log("SI_HasAppJs.Check()");
    var cqBridge = Object.FindObjectOfType<CroquetBridge>();

    string appName = cqBridge?.appName;
    if (appName==null || appName=="") {
      MqWelcome_StatusSets.hasAppJs.error.Set();
      ShowVEs(SetAppName_Btn);
      HideVEs(MakeAppJsFile_Btn, GotoAppJsFile_Btn, GotoAppJsFolder_Btn);
      return false;
    }
    var appJsFile      = CqFile.AppIndexJs();
    bool haveAppJsFile = appJsFile.Exists(); // file should be in Assets/CroquetJS/<appName>/index.js
    MqWelcome_StatusSets.hasAppJs.SetIsGood(haveAppJsFile);
    HideVEs(SetAppName_Btn);
    SetVEViz( haveAppJsFile, GotoAppJsFile_Btn, GotoAppJsFolder_Btn );
    SetVEViz(!haveAppJsFile, MakeAppJsFile_Btn);

    return haveAppJsFile;
  }

  private void Clk_SetAppName() { // HAS APP JS  ------------- Click
    var cqBridge = Object.FindObjectOfType<CroquetBridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find CroquetBridge in scene!");
      return;
    } else {
      // direct user to enter an appName into the CroquetBridge field for appName
      // select the CroquetBridge in the scene
      Selection.activeGameObject = cqBridge.gameObject;
      EditorGUIUtility.PingObject(cqBridge.gameObject);
      // in 100 ms, show a dialog to the user
      EditorApplication.delayCall += ()=>{
        EditorUtility.DisplayDialog(
          "Set App Name",
          "Enter a name for your app into the CroquetBridge's Session Configuration field \n\nApp Name\n( appName )\n \n\nWe will select it for you, so check the Inspector.",
          "Ok"
        );
      };
    }
  }
  private void Clk_MakeAppJsFile() { // HAS APP JS  ------------- Click
    string fromDir = CqFile.StarterTemplateFolder().longPath;
    string toDir   = CqFile.AppFolder(true).longPath; // here true means log no error if missing
    CroquetBuilder.CopyDirectory(fromDir, toDir);
    AssetDatabase.Refresh();
    Check(); // recheck (this SI_HasAppJs)
    edWin.CheckAllStatusForReady();
  }
  private void Clk_HasAppJs_Docs() {// HAS APP JS  ------------- Click

  }

  private void Clk_GotoAppJsFolder() {// HAS APP JS  ------------- Click
    CqFile.AppFolder().DeeperFile("index.js").SelectAndPing();
  }

  private void Clk_GotoAppJsFile() {// HAS APP JS  ------------- Click
    CqFile.AppFolder().DeeperFile("index.js").SelectAndPing();
  }
}
