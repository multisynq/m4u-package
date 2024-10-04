using UnityEngine;
using UnityEditor;
using Multisynq;

[CustomEditor(typeof(SynqVar_Mgr))]
public class SynqVar_Mgr_Editor : Editor {

  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SynqVar_Mgr manager = (SynqVar_Mgr)target;
    if (GUILayout.Button("Inject JS Plugin Code")) {
      WriteCode(manager);
    }
    if (GUILayout.Button("Select JS Plugins Folder")) {
      var plFldr = Mq_File.AppFolder().DeeperFolder("plugins").EnsureExists();
      if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
      else                            plFldr.SelectAndPing();
    }
  }

  private void WriteCode(SynqVar_Mgr manager) {
    manager.WriteMyJsPluginFile();
    AssetDatabase.Refresh();
  }
}