using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_JsPlugins: StatusItem {

  Button AddJsPlugins_Btn;
  Button GotoJsPlugins_Btn;
  Button JsPlugins_Docs_Btn;

  public SI_JsPlugins(MultisynqBuildAssistantEW parent = null) : base(parent) { }

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
    JsCodeInjecting_MonoBehavior.InjectMissingJsPlugins();
    // Update Asset DB
    AssetDatabase.Refresh();
    
    CqFile.AppFolder().DeeperFolder("plugins").SelectAndPing();
    edWin.CheckAllStatusForReady();
    Notify("Files Added.\nSelected on Project pane.");
  }

  private void Clk_GotoJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader();
    // CqFile.AppFolder().DeeperFolder("plugins").EnsureExists().SelectAndPing();
    var plFldr = CqFile.AppFolder().DeeperFolder("plugins");
    if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
    else                            plFldr.SelectAndPing();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_JsPlugins_Docs() {
    Logger.MethodHeader();
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  override public bool Check() { // JS PLUGINS
    var pluginRpt = JsCodeInjecting_MonoBehavior.AnalyzeAllJsPlugins();
    //string report = string.Join(", ", missingJsInjects.Select(x => x.Name));
    // lambda function for report from list of types
    bool amMissingPlugins = JsCodeInjecting_MonoBehavior.LogJsPluginReport(pluginRpt);
    StatusSetMgr.jsPlugins.SetIsGood(!amMissingPlugins);
    SetVEViz(amMissingPlugins, AddJsPlugins_Btn);
    ShowVEs(GotoJsPlugins_Btn);
    return amMissingPlugins;
  }


}

