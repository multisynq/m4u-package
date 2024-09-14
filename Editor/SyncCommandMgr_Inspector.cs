using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyncCommand_Mgr))]
public class SyncCommandMgrEditor : Editor {
  
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncCommand_Mgr manager = (SyncCommand_Mgr)target;
    if (GUILayout.Button("Inject JS Plugin Code")) {
      InjectCode(manager);
    }
    if (GUILayout.Button("Select Plugins Folder")) {
      var plFldr = CqFile.AppFolder().DeeperFolder("plugins").EnsureExists();
      if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
      else                            plFldr.SelectAndPing();
    }

  }

  private void InjectCode(SyncCommand_Mgr manager) {
    manager.InjectJsPluginCode();
    AssetDatabase.Refresh();
  }
}