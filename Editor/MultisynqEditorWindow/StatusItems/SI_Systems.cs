using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_Systems: StatusItem {

  Button AddCqSys_Btn;        
  Button ListMissingCqSys_Btn;
  Button Systems_Docs_Btn;

  public SI_Systems(MultisynqBuildAssistantEW parent = null) : base(parent) {
    
  }

  override public void InitUI() {
    SetupVisElem("HasCqSys_Img",         ref statusImage);
    SetupLabel(  "HasCqSys_Message_Lbl", ref messageLabel);
    SetupButton( "AddCqSys_Btn",         ref AddCqSys_Btn,         Clk_AddCqSys);
    SetupButton( "ListMissingCqSys_Btn", ref ListMissingCqSys_Btn, Clk_ListMissingCqSys);
    SetupButton( "HasCqSys_Docs_Btn",    ref Systems_Docs_Btn,     Clk_Systems_Docs);
  }

  override public void InitText() {
    StatusSetMgr.hasCqSys = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success, blank)
      "Croquet Systems are ready to go!",
      "Croquet Systems are missing",
      "Croquet Systems are missing! Click <b>Add Croquet Systems</b> to get them.",
      "Croquet Systems installed!!! Well done!",
      "Croquet Systems status"
    );
    statusSet = StatusSetMgr.hasCqSys;
  }

  override public bool Check() { // SYSTEMS
    (string critRpt, string optRpt) = MissingSystemsRpt();
    bool noneMissing = (critRpt + optRpt == "");
    bool critMissing = (critRpt != "");

    if (noneMissing) {
      HideVEs( AddCqSys_Btn, ListMissingCqSys_Btn );
      StatusSetMgr.hasCqSys.success.Set();
    } else {
      ShowVEs( AddCqSys_Btn, ListMissingCqSys_Btn );
      if (critMissing) {
        StatusSetMgr.hasCqSys.error.Set();
        Debug.LogError("Missing Critical Croquet Systems:\n" + critRpt);
      } else {
        StatusSetMgr.hasCqSys.warning.Set();
        Debug.LogWarning("Missing Optional Croquet Systems:\n" + optRpt);
      }
    }
    return noneMissing;
  }

  //-- Clicks - HAS CROQUET SYSTEMS --------------------------------
  void Clk_AddCqSys() { // HAS CQ SYSTEMS  ------------- Click
    Logger.MethodHeader();
    var cqBridge = Object.FindObjectOfType<CroquetBridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find CroquetBridge in scene!");
      return;
    } else {
      var cqGob = cqBridge.gameObject;
      string rpt = "";
      rpt += SceneHelp.EnsureCompRpt<CroquetRunner>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<CroquetEntitySystem>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<CroquetSpatialSystem>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<CroquetMaterialSystem>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<CroquetFileReader>(cqGob);
      if (rpt == "") NotifyAndLog("All Croquet Systems are present in CroquetBridge GameObject.");
      else           NotifyAndLog("Added:\n"+rpt);
    }
    Check(); // recheck self (Cq Systems)
    edWin.CheckAllStatusForReady();
  }
  //--------------------------------------------------------------------------------
  (string,string) MissingSystemsRpt() {
    string critRpt = "";
    critRpt += (Object.FindObjectOfType<CroquetRunner>()         == null) ? "CroquetRunner\n"         : "";
    critRpt += (Object.FindObjectOfType<CroquetFileReader>()     == null) ? "CroquetFileReader\n"     : "";
    critRpt += (Object.FindObjectOfType<CroquetEntitySystem>()   == null) ? "CroquetEntitySystem\n"   : "";
    critRpt += (Object.FindObjectOfType<CroquetSpatialSystem>()  == null) ? "CroquetSpatialSystem\n"  : "";
    string optRpt = "";
    optRpt += (Object.FindObjectOfType<CroquetMaterialSystem>() == null) ? "CroquetMaterialSystem\n" : "";
    return (critRpt, optRpt);
  }

  void Clk_ListMissingCqSys() { // HAS CQ SYSTEMS  ------------- Click
    Logger.MethodHeader();
    var cqBridge = Object.FindObjectOfType<CroquetBridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find CroquetBridge in scene!");
      return;
    } else {
      (string critRpt, string optRpt) = MissingSystemsRpt();
      if (critRpt + optRpt == "") NotifyAndLog("All Croquet Systems present.");
      else {
        if      (critRpt != "") NotifyAndLogError(  "Missing Critical:\n"+critRpt);
        else if (optRpt  != "") NotifyAndLogWarning("Missing Optional:\n"+optRpt);
      }
    }
  }

  private void Clk_Systems_Docs() {
    Logger.MethodHeaderAndOpenUrl();
  }

}
