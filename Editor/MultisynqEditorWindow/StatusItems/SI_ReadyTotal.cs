using UnityEngine;
using UnityEngine.UIElements;

public class SI_ReadyTotal: StatusItem {

  Button Awesome_Btn;
  Button Top_Ready_Docs_Btn;

  public SI_ReadyTotal(MultisynqBuildAssistantEW parent = null) : base(parent){}

  override public void InitUI() {
    SetupVisElem("Ready_Status_Img",     ref statusImage);
    SetupLabel(  "Ready_Message_Lbl",    ref messageLabel);
    SetupButton( "Awesome_Btn",          ref Awesome_Btn,        Clk_BeAwesome);
    SetupButton( "Top_Ready_Docs_Btn",   ref Top_Ready_Docs_Btn, Clk_Top_Ready_Docs);    
  }

  override public void InitText() {
    string t_synq = "<b><color=#006AFF>Synq</color></b>";
    StatusSetMgr.ready = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success)
      $"You are <b><size=+1><color=#77ff77>Ready to </color>{t_synq}</b></size><color=#888>      All green lights below.",
      $"Warn 00000",
      $"Look below to fix what's not ready...",
      $"W00t!!! You are ready to {t_synq}!", // displays for 5 seconds, then switches to the .ready message
      "Press   Check If Ready   above"
    );
    statusSet = StatusSetMgr.ready;
  }

  override public bool Check() { // SETTINGS
    // the real check is in MultisynqBuildAssistantEW.CheckAllStatusForReady()
    return true;
  }

  //-- Clicks - READY --------------------------------

  void Clk_BeAwesome() { // READY  ------------- Click
    Debug.Log("Be Awesome!!!!");
    // Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ"); // Copilot thinks you should go to this url. You know you want to. =]
    Application.OpenURL("https://giphy.com/search/everything-is-awesome");
  }

  void Clk_Top_Ready_Docs() { // READY  ------------- Click
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }


  public void AllAreReady(bool really = true) {
    if (really) {
      StatusSetMgr.ready.success.Set();
      ShowVEs(Awesome_Btn);
      MultisynqBuildAssistantEW.Instance.countdown_ToConvertSuccesses = 3f;
    } else {
      StatusSetMgr.ready.error.Set();
      HideVEs(Awesome_Btn);
    }
  }
}
