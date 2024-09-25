using UnityEngine;
using UnityEditor;
using Multisynq;

[CustomEditor(typeof(SynqVar_Mgr))]
public class CroquetSynqVarMgrEditor : Editor {

  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    SynqVar_Mgr manager = (SynqVar_Mgr)target;
    if (GUILayout.Button("Inject JS Plugin Code")) {
      InjectCode(manager);
    }
    if (GUILayout.Button("Select JS Plugins Folder")) {
      var plFldr = Mq_File.AppFolder().DeeperFolder("plugins").EnsureExists();
      if (plFldr.FirstFile() != null) plFldr.FirstFile().SelectAndPing(true);
      else                            plFldr.SelectAndPing();
    }
  }

  private void InjectCode(SynqVar_Mgr manager) {
    manager.InjectJsPluginCode();
    AssetDatabase.Refresh();
  }
}