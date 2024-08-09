using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_ApiKey: StatusItem {

  Button SignUpApi_Btn;
  Button GotoApiKey_Btn;
  Button ApiKey_Docs_Btn;

    public SI_ApiKey(MultisynqBuildAssistantEW parent = null) : base(parent)
    {
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
    MqWelcome_StatusSets.apiKey = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success)
      $"The {t_key} is ready to go!",
      $"The {t_key} is not set",
      $"Let's get you a free {t_key}. It's easy.",
      $"The {t_key} is configured!!! Well done!",
      "API Key status"
    );
    statusSet = MqWelcome_StatusSets.apiKey;
  }


  //-- Clicks - API KEY --------------------------------

  private void Clk_SignUpApi() { // API KEY  ------------- Click
    edWin.siSettings.GotoSettings();
    Application.OpenURL("https://croquet.io/account/");
  }

  private void Clk_EnterApiKey() {  // API KEY  ------------- Click
    edWin.siSettings.GotoSettings();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_ApiKey_Docs() {
    Application.OpenURL("https://croquet.io/account/");
    // Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  override public bool Check() { // API KEY
    var cqStgs = CqFile.FindProjectCqSettings();
    if (cqStgs == null)  return false;
    ShowVEs(GotoApiKey_Btn, SignUpApi_Btn);

    // curl -s -X GET -H "X-Croquet-Auth: 1_s77e6tyzkx5m3yryb9305sqxhkdmz65y69oy5s8e" -H "X-Croquet-App: io.croquet.vdom.ploma" -H "X-Croquet-Id: persistentId" -H "X-Croquet-Version: 1.1.0" -H "X-Croquet-Path: https://croquet.io" 'https://api.croquet.io/sign/join?meta=login'
    var apiKey = cqStgs.apiKey;
    bool apiKeyIsGood = (apiKey != null && apiKey != "<go get one at multisynq.io>" && apiKey.Length > 0);
    MqWelcome_StatusSets.apiKey.SetIsGood(apiKeyIsGood);
    return apiKeyIsGood;
  }
  


}

