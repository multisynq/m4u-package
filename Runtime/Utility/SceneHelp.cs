using UnityEngine;
#if UNITY_EDITOR
  using UnityEditor;
#endif

static public class SceneHelp {

  static public T EnsureComp<T>(this GameObject gob) where T : Component {
    return gob.GetComponent<T>() ?? gob.AddComponent<T>();
  }
  
  static public string EnsureCompRpt<T>(GameObject gob) where T : Component {
    var comp = UnityEngine.Object.FindObjectOfType<T>();
    string name = typeof(T).Name;
    if (comp == null) {
      var go = (gob==null) ? new GameObject(name) : gob;
      go.AddComponent<T>();
      Debug.Log($"Created {name} Component in scene.", gob);
      return name+"\n";
    } else {
      Debug.Log($"{name} already exists in scene.");
      return "";
    }
  }

  static public T FindComp<T>() where T : Component{
    T component = null;
    string tName = typeof(T).Name;
    // First check for a T on the scene's Mq_Bridge
    var bridge = UnityEngine.Object.FindObjectOfType<T>();
    if (bridge != null) {
        component = bridge.GetComponent<T>();
        if (component != null) return component;
    }
    return component;
  }
  static public T FindCompInProject<T>() where T : Object {
    T component = null;
    string tName = typeof(T).Name;

    #if UNITY_EDITOR
      // Then look in the whole project for a file of T type
      string[] guids = AssetDatabase.FindAssets($"t:{tName}");
      guids = System.Array.FindAll(guids, guid => !AssetDatabase.GUIDToAssetPath(guid).Contains("Packages/"));
      if (guids.Length > 0) {
          component = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
          if (guids.Length > 1) {
              Debug.LogWarning($"Found more than one '{tName}' file. You should only have one.");
              // Print out path of all files found
              int i = 1;
              foreach (string guid in guids) {
                  string path = AssetDatabase.GUIDToAssetPath(guid);
                  var obj = AssetDatabase.LoadAssetAtPath<T>(path);
                  Debug.LogWarning($"{i++}. {path}", obj);// obj param will make them select it when user clicks each log
              }
          }
      }
    #endif
    return component;
  }

  static public T FindCompSceneThenProject<T>() where T : Component {
    T component = FindComp<T>();
    if (component == null) {
        component = FindCompInProject<T>();
    }
    return component;
  }

}