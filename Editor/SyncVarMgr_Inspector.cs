using UnityEngine;
using UnityEditor;
using MultisynqNS;

[CustomEditor(typeof(SyncVar_Mgr))]
public class CroquetSyncVarMgrEditor : Editor {

  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SyncVar_Mgr manager = (SyncVar_Mgr)target;
    if (GUILayout.Button("Inject JS Plugin Code")) {
      InjectCode(manager);
    }
    if (GUILayout.Button("Select Plugins Folder")) {
      var plFldr = Mq_File.AppFolder().DeeperFolder("plugins").EnsureExists();
      if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
      else                            plFldr.SelectAndPing();
    }
  }

  private void InjectCode(SyncVar_Mgr manager) {
    manager.InjectJsPluginCode();
    AssetDatabase.Refresh();
  }
}