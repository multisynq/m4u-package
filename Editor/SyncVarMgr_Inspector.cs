using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyncVar_Mgr))]
public class CroquetSyncVarMgrEditor : Editor {

  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncVar_Mgr manager = (SyncVar_Mgr)target;
    if (GUILayout.Button("Inject Code")) {
      InjectCode(manager);
    }
  }

  private void InjectCode(SyncVar_Mgr manager) {
    manager.InjectJsPluginCode();
  }
}