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
    SetupButton( "JsPlugins_Docs_Btn",    ref JsPlugins_Docs_Btn, Clk_JsPlugins_Docs, false);
    SetupButton( "ForceJsPlugins_Btn",    ref ForceJsPlugins_Btn, Clk_ForceJsPlugins);
  }

  override public void InitText() {
    string t_js  = "<b><color=#FFFF44>JS</color></b>";
    StatusSetMgr.jsPlugins = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
      $"All needed C#-to-{t_js}-Plugins found!",
      $"Missing some C#-to-{t_js}-Plugins!",
      $"Missing some C#-to-{t_js}-Plugins!",
      $"All needed C#-to-{t_js}-Plugins found!  Well done!",
      "C#-to-JS-Plugins"
    );
    statusSet = StatusSetMgr.jsPlugins;
  }


  //-- Clicks - JS PLUGINS --------------------------------

  private void Clk_AddJsPlugins_Btn() { // JS PLUGINS  ------------- Click
    Logger.MethodHeader();
    JsPlugin_Writer.WriteMissingJsPlugins();
    // Update Asset DB
    AssetDatabase.Refresh();
    PingPlugins();
    edWin.CheckAllStatusForReady();
    Notify("JsPlugin files added.\nSelected on Project pane.");
  }
  private void PingPlugins() {
    var plFldr = Mq_File.AppPluginsFolder();
    var iopf   = plFldr.DeeperFile("indexOfPlugins.js");
    if (iopf != null) iopf.SelectAndPing(true);
    else {
      var ff = plFldr.FirstFile();
      if (ff != null) ff.SelectAndPing(true);
      else            plFldr.SelectAndPing();
    }
  }
  private void Clk_GotoJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader(4);
    // CqFile.AppFolder().DeeperFolder("plugins").EnsureExists().SelectAndPing();
    Mq_File.AppPluginsFolder().EnsureExists();
    PingPlugins();
    Notify("Selected in Project.\nSee Inspector.");
  }

  private void Clk_ForceJsPlugins() {  // JS PLUGINS  ------------- Click
    Logger.MethodHeader(4);
    if (EditorUtility.DisplayDialog(
      "Force JS Plugins?", 
      $"Are you sure you want to force JS Plugins?\nThis will overwrite JS Plugins in: \n\n{Mq_File.AppPluginsFolder().shortPath}", "Yes", "No"
    )) {
      Mq_File.AppPluginsFolder().EnsureExists();
      JsPlugin_Writer.WriteNeededJsPluginFiles();
      PingPlugins();
      Notify("Forced JS Plugins.");
    }
  }

  private void Clk_JsPlugins_Docs() {
    Logger.MethodHeader();
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#js-plugins");
  }

  override public bool Check() { // JS PLUGINS
    var         pluginRpt = JsPlugin_Writer.AnalyzeAllJsPlugins();
    bool needsSomePlugins = pluginRpt.neededTs.Count>0;
    var   hasPluginImport = JsPlugin_Writer.IndexJsHasPluginsImport(needsSomePlugins);
    bool amMissingPlugins = JsPlugin_Writer.LogJsPluginReport(pluginRpt, true);
    bool  puglinProblems  = amMissingPlugins || (needsSomePlugins && !hasPluginImport);
    if (puglinProblems) Debug.Log($"[SI_JsPlugins] amMissingPlugins: {amMissingPlugins}  needsSomePlugins: {needsSomePlugins}  hasPluginImport: {hasPluginImport}  puglinProblems: {puglinProblems}\npuglinProblems   = amMissingPlugins || (needsSomePlugins && !hasPluginImport)");

    StatusSetMgr.jsPlugins.SetIsGood(!puglinProblems);
    SetVEViz(puglinProblems, AddJsPlugins_Btn);
    ShowVEs(GotoJsPlugins_Btn, ForceJsPlugins_Btn);
    return !puglinProblems;
  }


}

