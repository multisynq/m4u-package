using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_ApiKey: StatusItem {

  Button SignUpApi_Btn;
  Button GotoApiKey_Btn;
  Button ApiKey_Docs_Btn;

    public SI_ApiKey(MultisynqBuildAssistantEW parent = null) : base(parent) {
      
    }

    override public void InitUI() {
    SetupVisElem("ApiKey_Status_Img",  ref statusImage);
    SetupLabel(  "ApiKey_Message_Lbl", ref messageLabel);
    SetupButton( "SignUpApi_Btn",      ref SignUpApi_Btn,   Clk_SignUpApi);
    SetupButton( "GotoApiKey_Btn",     ref GotoApiKey_Btn,  Clk_EnterApiKey);
    SetupButton( "ApiKey_Docs_Btn",    ref ApiKey_Docs_Btn, Clk_ApiKey_Docs);
  }

  override public void InitText() {
    string t_key  = "<b><color=#006AFF>API Key</color></b>";
    StatusSetMgr.apiKey = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      $"The {t_key} is ready to go!",
      $"The {t_key} is not set",
      $"Let's get you a free {t_key}. It's easy.",
      $"The {t_key} is configured!!! Well done!",
      "API Key status"
    );
    statusSet = StatusSetMgr.apiKey;
  }


  //-- Clicks - API KEY --------------------------------

  private void Clk_SignUpApi() { // API KEY  ------------- Click
    Logger.MethodHeader();
    edWin.siSettings.GotoSettings();
    Application.OpenURL("https://croquet.io/account/");
  }

  private void Clk_EnterApiKey() {  // API KEY  ------------- Click
    Logger.MethodHeader();
    edWin.siSettings.GotoSettings();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_ApiKey_Docs() {
    Logger.MethodHeader();
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#api-key");
  }

  override public bool Check() { // API KEY
    var cqStgs = StatusSetMgr.FindProjectCqSettings();
    if (cqStgs == null)  return false;
    ShowVEs(GotoApiKey_Btn, SignUpApi_Btn);
    // check a key:
    // curl -s -X GET -H "X-Croquet-Auth: 1_s77e6tyzkx5m3yryb9305sqxhkdmz65y69oy5s8e" -H "X-Croquet-App: io.croquet.vdom.ploma" -H "X-Croquet-Id: persistentId" -H "X-Croquet-Version: 1.1.0" -H "X-Croquet-Path: https://croquet.io" 'https://api.croquet.io/sign/join?meta=login'
    var apiKey = cqStgs.apiKey;
    bool apiKeyIsGood = (apiKey != null && apiKey != "<go get one at multisynq.io>" && apiKey.Length > 0);
    StatusSetMgr.apiKey.SetIsGood(apiKeyIsGood);
    return apiKeyIsGood;
  }

}

