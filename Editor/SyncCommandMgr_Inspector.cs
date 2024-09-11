using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyncCommandMgr))]
public class SyncCommandMgrEditor : Editor {
  
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncCommandMgr manager = (SyncCommandMgr)target;
    if (GUILayout.Button("Inject Code")) {
      InjectCode(manager);
    }
  }

  private void InjectCode(SyncCommandMgr manager) {
    manager.OnInjectJsPluginCode();
  }
}