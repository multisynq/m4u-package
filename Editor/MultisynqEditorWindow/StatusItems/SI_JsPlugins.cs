using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;

public class SI_JsPlugins: StatusItem {

  Button AddJsPlugins_Btn;
  Button GotoJsPlugins_Btn;
  Button JsPlugins_Docs_Btn;
  Button ForceJsPlugins_Btn;

  public SI_JsPlugins(MultisynqBuildAssistantEW parent = null) : base(parent) { }

  override public void InitUI() {
    SetupVisElem("JsPlugins_Status_Img",  ref statusImage);
    SetupLabel(  "JsPlugins_Message_Lbl", ref messageLabel);
    SetupButton( "AddJsPlugins_Btn",      ref AddJsPlugins_Btn,   Clk_AddJsPlugins_Btn);
    SetupButton( "GotoJsPlugins_Btn",     ref GotoJsPlugins_Btn,  Clk_GotoJsPlugins);
    SetupButton( "JsPlugins_Docs_Btn",    ref JsPlugins_Docs_Btn, Clk_JsPlugins_Docs);
    SetupButton( "ForceJsPlugins_Btn",    ref ForceJsPlugins_Btn, Clk_ForceJsPlugins);
  }

  override public void InitText() {
    string t_js  = "<b><color=#FFFF44>JS</color></b>";
    StatusSetMgr.jsPlugins = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
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
    JsPlugin_Behaviour.WriteMissingJsPlugins();
    // Update Asset DB
    AssetDatabase.Refresh();
    
    Mq_File.AppFolder().DeeperFolder("plugins").SelectAndPing();
    edWin.CheckAllStatusForReady();
    Notify("Files Added.\nSelected on Project pane.");
  }

  private void Clk_GotoJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader(4);
    // CqFile.AppFolder().DeeperFolder("plugins").EnsureExists().SelectAndPing();
    var plFldr = Mq_File.AppFolder().DeeperFolder("plugins");
    if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
    else                            plFldr.SelectAndPing();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_ForceJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader(4);
    if (EditorUtility.DisplayDialog(
      "Force JS Plugins?", 
      $"Are you sure you want to force JS Plugins?\nThis will overwrite JS Plugins in: \n\n{Mq_File.AppPluginsFolder().shortPath}", "Yes", "No"
    )) {
      JsPlugin_Behaviour.WriteAllJsPlugins();
      Notify("Forced JS Plugins.");
    }
  }

  private void Clk_JsPlugins_Docs() {
    Logger.MethodHeader();
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  override public bool Check() { // JS PLUGINS
    var pluginRpt = JsPlugin_Behaviour.AnalyzeAllJsPlugins();
    //string report = string.Join(", ", missingJsInjects.Select(x => x.Name));
    // lambda function for report from list of types
    bool amMissingPlugins = JsPlugin_Behaviour.LogJsPluginReport(pluginRpt);
    StatusSetMgr.jsPlugins.SetIsGood(!amMissingPlugins);
    SetVEViz(amMissingPlugins, AddJsPlugins_Btn);
    ShowVEs(GotoJsPlugins_Btn, ForceJsPlugins_Btn);
    return amMissingPlugins;
  }


}

