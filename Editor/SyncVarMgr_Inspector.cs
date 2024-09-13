using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyncVarMgr))]
public class CroquetSyncVarMgrEditor : Editor {

  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncVarMgr manager = (SyncVarMgr)target;
    if (GUILayout.Button("Inject Code")) {
      InjectCode(manager);
    }
  }

  private void InjectCode(SyncVarMgr manager) {
    manager.InjectJsPluginCode();
  }
}