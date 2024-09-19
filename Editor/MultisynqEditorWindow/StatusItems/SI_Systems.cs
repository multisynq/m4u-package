using UnityEngine;
using UnityEngine.UIElements;
using MultisynqNS;

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
      // (ready, warning, error, success, blank )
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
    var cqBridge = Object.FindObjectOfType<Mq_Bridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find Mq_Bridge in scene!");
      return;
    } else {
      var cqGob = cqBridge.gameObject;
      string rpt = "";
      rpt += SceneHelp.EnsureCompRpt<Mq_Runner>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<Mq_Entity_System>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<Mq_Spatial_System>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<Mq_Material_System>(cqGob);
      rpt += SceneHelp.EnsureCompRpt<Mq_FileReader>(cqGob);
      if (rpt == "") NotifyAndLog("All Croquet Systems are present in Mq_Bridge GameObject.");
      else           NotifyAndLog("Added:\n"+rpt);
    }
    Check(); // recheck self (Cq Systems)
    edWin.CheckAllStatusForReady();
  }
  //--------------------------------------------------------------------------------
  (string,string) MissingSystemsRpt() {
    string critRpt = "";
    critRpt += (Object.FindObjectOfType<Mq_Runner>()         == null) ? "CroquetRunner\n"         : "";
    critRpt += (Object.FindObjectOfType<Mq_FileReader>()     == null) ? "Mq_FileReader\n"     : "";
    critRpt += (Object.FindObjectOfType<Mq_Entity_System>()   == null) ? "Mq_Entity_System\n"   : "";
    critRpt += (Object.FindObjectOfType<Mq_Spatial_System>()  == null) ? "Mq_Spatial_System\n"  : "";
    string optRpt = "";
    optRpt += (Object.FindObjectOfType<Mq_Material_System>() == null) ? "Mq_Material_System\n" : "";
    return (critRpt, optRpt);
  }

  void Clk_ListMissingCqSys() { // HAS CQ SYSTEMS  ------------- Click
    Logger.MethodHeader();
    var cqBridge = Object.FindObjectOfType<Mq_Bridge>();
    if (cqBridge == null) {
      NotifyAndLogError("Could not find Mq_Bridge in scene!");
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
