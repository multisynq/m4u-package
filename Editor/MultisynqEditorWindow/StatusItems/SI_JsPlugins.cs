using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_JsPlugins: StatusItem {

  Button AddJsPlugins_Btn;
  Button GotoJsPlugins_Btn;
  Button JsPlugins_Docs_Btn;

  public SI_JsPlugins(MultisynqBuildAssistantEW parent = null) : base(parent) {
    
  }

  override public void InitUI() {
    SetupVisElem("JsPlugins_Status_Img",  ref statusImage);
    SetupLabel(  "JsPlugins_Message_Lbl", ref messageLabel);
    SetupButton( "AddJsPlugins_Btn",      ref AddJsPlugins_Btn,   Clk_AddJsPlugins_Btn);
    SetupButton( "GotoJsPlugins_Btn",     ref GotoJsPlugins_Btn,  Clk_GotoJsPlugins);
    SetupButton( "JsPlugins_Docs_Btn",    ref JsPlugins_Docs_Btn, Clk_JsPlugins_Docs);
  }

  override public void InitText() {
    string t_js  = "<b><color=#FFFF44>JS</color></b>";
    StatusSetMgr.jsPlugins = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success)
      $"All needed C#-to-{t_js}-Proxy-Plugins found!",
      $"Missing some C#-to-{t_js}-Proxy-Plugins!",
      $"Missing some C#-to-{t_js}-Proxy-Plugins!",
      $"All needed C#-to-{t_js}-Proxy-Plugins found!  Well done!",
      "C#-to-JS-Proxy-Plugins"
    );
    statusSet = StatusSetMgr.jsPlugins;
  }


  //-- Clicks - JS PLUGINS --------------------------------

  private void Clk_AddJsPlugins_Btn() { // JS PLUGINS  ------------- Click
    Logger.MethodHeader();
    JsCodeInjecting_MonoBehavior.DoAllNeededJsInjects();
    CqFile.AppFolder().DeeperFolder("plugins").SelectAndPing();
    edWin.CheckAllStatusForReady();
    Notify("Files Added.\nSelected on Project pane.");
  }

  private void Clk_GotoJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader();
    edWin.siSettings.GotoSettings();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_JsPlugins_Docs() {
    Logger.MethodHeader();
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  override public bool Check() { // API KEY
    var neededJsInjects = JsCodeInjecting_MonoBehavior.FindMissingJsPluginTypes();
    string report = string.Join(", ", neededJsInjects.Select(x => x.Name));
    var fldr = $"<color=#ff55ff>Assets/CroquetJS/{CqFile.GetAppNameForOpenScene()}/plugins/</color>";
    if (neededJsInjects.Length > 0) {
      Debug.Log($"| Missing <color=cyan>{neededJsInjects.Length}</color> JS Plugins: [ <color=white>{report}</color> ] in {fldr}");
      Debug.Log($"|    To Add Missing JS Plugin Files, in Menu:");
      Debug.Log($"|    <color=white>Croquet > Open Build Assistant Window > [Check If Ready], then [Add Missing JS Plugin Files]</color>");
    } else {
      Debug.Log($"All needed JS Plugins found in {fldr}");
    }
    bool amMissingPlugins = neededJsInjects.Length > 0;
    StatusSetMgr.jsPlugins.SetIsGood(!amMissingPlugins);
    SetVEViz(amMissingPlugins, AddJsPlugins_Btn);
    return amMissingPlugins;
  }

}

