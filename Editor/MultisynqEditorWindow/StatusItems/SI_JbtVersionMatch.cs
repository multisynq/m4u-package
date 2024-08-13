using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_JbtVersionMatch: StatusItem {


  Button ReinstallTools_Btn;
  // Button OpenBuildPanel_Btn;

    public SI_JbtVersionMatch(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("JbtVersionMatch_Img",         ref statusImage);
    SetupLabel(  "JbtVersionMatch_Message_Lbl", ref messageLabel);
    SetupButton( "BuildJsNow2_Btn",             ref ReinstallTools_Btn, Clk_BuildJsNow);
    // SetupButton( "OpenBuildPanel_Btn",       ref OpenBuildPanel_Btn, Clk_OpenEditorBuildPanel);
  }
  override public void InitText() {
    string t_jsb  = "<b><color=#E5DB1C>JS Build</color></b>";
    StatusSetMgr.versionMatch = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success, blank)
      $"Versions of {t_jsb} Tools and Built output match!",
      $"Versions of {t_jsb} Tools and Built output do not match",
      $"Versions of {t_jsb} Tools and Built output do not match!\n<b>Make a new or first build!</b>",
      $"Versions of {t_jsb} Tools and Built output match!!! Well done!",
      "Version Match status"
    );
    statusSet = StatusSetMgr.versionMatch;
  }

  override public bool Check() { 
    // load the two ".last-installed-tools" files to compare versions and Tools levels
    // of (1) the tools in DotJsBuild and (2) the tools in CroquetBridge
    // var installedToolsForDotJsBuild    = LastInstalled.LoadPath(CroquetBuilder.installedToolsForDotJsBuild_Path);
    // var installedToolsForCroquetBridge = LastInstalled.LoadPath(CroquetBuilder.installedToolsForCroquetBridge_Path);
    var installedToolsForDotJsBuild    = LastInstalled.LoadPath(CroquetBuilder.JSToolsRecordInBuild);
    var installedToolsForCroquetBridge = LastInstalled.LoadPath(CroquetBuilder.JSToolsRecordInEditor);
    bool allMatch = installedToolsForDotJsBuild.IsSameAs(installedToolsForCroquetBridge);

    StatusSetMgr.versionMatch.SetIsGood(allMatch);
    if (allMatch) {
      Debug.Log("JSTools for Editor & Build match!!!");
    } else {
      Debug.LogError( installedToolsForDotJsBuild.ReportDiffs(installedToolsForCroquetBridge, "Build", "Editor"));
      ShowVEs(ReinstallTools_Btn);
    }
    // ShowVEs(ReinstallTools_Btn, OpenBuildPanel_Btn);
    return allMatch;
  }

  void Clk_OpenEditorBuildPanel() { // Open Build - JS BUILD TOOLS  ------------- Click
    EditorWindow.GetWindow<BuildPlayerWindow>().Show();
    EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where( s => s.enabled ).ToArray();
    if (scenes.Length == 0) {
      NotifyAndLogError("No scenes in Build Settings.\nAdd some scenes to build.");
      return;
    }
  }
  void Clk_BuildJsNow() { // VERSION MATCH - JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader();
    edWin.siJsBuild.Clk_Build_JsNow();
    Check();
    edWin.CheckAllStatusForReady();
  }
}
