using UnityEngine;
using UnityEditor;
using Multisynq;

[CustomEditor(typeof(SynqCommand_Mgr))]
public class SynqCommand_Mgr_Editor : Editor {
  
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SynqCommand_Mgr manager = (SynqCommand_Mgr)target;
    if (GUILayout.Button("Inject JS Plugin Code")) {
      WriteCode(manager);
    }
    if (GUILayout.Button("Select JS Plugins Folder")) {
      var plFldr = Mq_File.AppFolder().DeeperFolder("plugins").EnsureExists();
      if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
      else                            plFldr.SelectAndPing();
    }

  }

  private void WriteCode(SynqCommand_Mgr manager) {
    manager.WriteJsPluginCode();
    AssetDatabase.Refresh();
  }
}