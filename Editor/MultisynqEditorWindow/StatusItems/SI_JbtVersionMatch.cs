using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Multisynq;

public class SI_JbtVersionMatch: StatusItem {

  Button OpenBuildPanel_Btn;
  Button Docs_Btn;
  // Button OpenBuildPanel_Btn;

  public SI_JbtVersionMatch(MultisynqBuildAssistantEW parent = null) : base(parent) {}

  override public void InitUI() {
    SetupVisElem("JbtVersionMatch_Img",         ref statusImage);
    SetupLabel(  "JbtVersionMatch_Message_Lbl", ref messageLabel);
    SetupButton( "OpenBuildPanel_Btn",          ref OpenBuildPanel_Btn, Clk_OpenBuildPanel);
    SetupButton( "JbtVersionMatch_Docs_Btn",    ref Docs_Btn, Clk_JbtVersionMatch_Docs, false);
    // SetupButton( "OpenBuildPanel_Btn",       ref OpenBuildPanel_Btn, Clk_OpenEditorBuildPanel);
  }
  override public void InitText() {
    string t_jsb  = "<b><color=#E5DB1C>JS Build</color></b>";
    StatusSetMgr.versionMatch = new StatusSet( messageLabel, statusImage,
      // (ready, warning, error, success, blank )
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
    // of (1) the tools in DotJsBuild and (2) the tools in Mq_Bridge
    // var installedToolsForDotJsBuild    = LastInstalled.LoadPath(Mq_Builder.installedToolsForDotJsBuild_Path);
    // var installedToolsForMq_Bridge = LastInstalled.LoadPath(Mq_Builder.installedToolsForMq_Bridge_Path);
    var installedToolsForDotJsBuild    = LastInstalled.LoadPath(Mq_Builder.JSToolsRecordInBuild);
    var installedToolsForMq_Bridge = LastInstalled.LoadPath(Mq_Builder.JSToolsRecordInEditor);
    bool allMatch = installedToolsForDotJsBuild.IsSameAs(installedToolsForMq_Bridge);

    StatusSetMgr.versionMatch.SetIsGood(allMatch);
    ShowVEs(OpenBuildPanel_Btn);
    if (allMatch) {
      Debug.Log("JSTools for Editor & Build match!!!");
    } else {
      Debug.Log( installedToolsForDotJsBuild.ReportDiffs(installedToolsForMq_Bridge, "Build", "Editor"));
    }
    // ShowVEs(ReinstallTools_Btn, OpenBuildPanel_Btn);
    return allMatch;
  }

  void Clk_OpenBuildPanel() { // VERSION MATCH - JS BUILD TOOLS  ------------- Click
    Logger.MethodHeader();
    EditorWindow.GetWindow<BuildPlayerWindow>().Show();
    EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where( s => s.enabled ).ToArray();
    if (scenes.Length == 0) {
      NotifyAndLogError("No scenes in Build Settings.\nAdd some scenes to build.");
      return;
    }
  }

  void Clk_JbtVersionMatch_Docs() {
    Logger.MethodHeaderAndOpenUrl();
    Application.OpenURL("https://multisynq.io/docs/unity/build_assistant-assistant_steps.html#js-build-tools-version-match");
  }

}
