using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyncCommand_Mgr))]
public class SyncCommandMgrEditor : Editor {
  
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncCommand_Mgr manager = (SyncCommand_Mgr)target;
    if (GUILayout.Button("Inject Code")) {
      InjectCode(manager);
    }
  }

  private void InjectCode(SyncCommand_Mgr manager) {
    manager.InjectJsPluginCode();
  }
}