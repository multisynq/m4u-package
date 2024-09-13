using UnityEngine;

#if UNITY_EDITOR
  using System.Text.RegularExpressions;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;
#endif

//=================== ||||||||||||||||||||||||||| =========================
abstract public class JsCodeInjectingMonoBehavior : MonoBehaviour {

  static public string logPrefix = "[ <color=yellow>Js</color><color=cyan>CodeInject</color> ]";
  static bool dbg = true;

  abstract public string JsPluginFileName();
  abstract public string JsPluginCode();

  #if UNITY_EDITOR
    static public Dictionary<System.Type, string[]> codeMatchPatternsByJsInjectorsNeeded = new() {
      { typeof(SyncVarMgr), new[] { @"\[SyncVar\]" } },
      { typeof(SyncCommandMgr), new[] { @"\[SyncCommand\]", @"\[SyncRPC\]" } }
      // Add more patterns here as needed
    };
  #endif
  #if !UNITY_EDITOR
    virtual public void InjectJsPluginCode() { }
  #endif

  //----------------- ||||| ----------------------------------------
  virtual public void Start() {
    #if UNITY_EDITOR
      CheckIfMyJsCodeIsPresent();
    #endif
  }

  #if UNITY_EDITOR

    //----------------- |||||||||||||||||| ----------------------------------------
    virtual public void InjectJsPluginCode() { // you can override this to do more complex stuff, but it's base is a good default
      if (dbg)  Debug.Log($"{logPrefix} <color=white>BASE</color>   virtual public void OnInjectJsPluginCode()");

      var modelClassPath = CqFile.AppFolder().DeeperFile(JsPluginFileName());
      if (modelClassPath.Exists()) {
        Debug.Log($"{logPrefix} '{modelClassPath.shortPath}' already present at '{modelClassPath.longPath}'");
      } else {
        if (dbg)  Debug.LogWarning($"{logPrefix} Needed JS code added. Writing new file '{modelClassPath.shortPath}'");
        string jsCode = JsPluginCode().LessIndent();
        modelClassPath.WriteAllText(jsCode, true); // true = create needed folders
      }
    }

    //--------- |||||||||||||||||||||||| ----------------------------------------
    public void CheckIfMyJsCodeIsPresent() {
      var modelClassPath = CqFile.AppFolder().DeeperFile(JsPluginFileName());
      if (modelClassPath.Exists()) {
        Debug.Log($"{logPrefix} '{JsPluginFileName()}' already present at '{modelClassPath.longPath}'");
      } else {
        modelClassPath.SelectAndPing();
        Debug.LogError($"   v");
        Debug.LogError($"   v");
        Debug.LogError($"   v");
        Debug.LogError($"MISSING JS FILE {JsPluginFileName()} for {this.GetType().Name}.cs");
        Debug.LogError($"   ^");
        Debug.LogError($"   ^");
        Debug.LogError($"   ^");
        EditorApplication.isPlaying = false;
      }
    }
    //---------------- |||||||||||||||||||| ----------------------------------------
    static public void DoAllNeededJsInjects() {
      foreach (var jsInjector in FindNeededJsInjects()) {
        jsInjector.InjectJsPluginCode();
      }
    }
    //----------------------------------------- ||||||||||||||||||| ----------------------------------------
    static public JsCodeInjectingMonoBehavior[] FindNeededJsInjects() {
      List<JsCodeInjectingMonoBehavior> neededJsInjects = new();

      // Find all SyncedBehaviour subclasses in the scene
      var syncedBehaviours = GameObject.FindObjectsOfType<SyncedBehaviour>(true);

      foreach (var behaviour in syncedBehaviours) {
        var behaviourType = behaviour.GetType();
        var script = MonoScript.FromMonoBehaviour(behaviour);
        if (script != null) {
          string scriptPath = AssetDatabase.GetAssetPath(script);
          string scriptContent = System.IO.File.ReadAllText(scriptPath);

          foreach (var entry in codeMatchPatternsByJsInjectorsNeeded) {
            var jsInjectorType = entry.Key;
            var patterns = entry.Value;

            if (patterns.Any(pattern => Regex.IsMatch(scriptContent, pattern))) {
              var jsInjector = (JsCodeInjectingMonoBehavior)GameObject.FindObjectOfType(jsInjectorType);
              if (jsInjector != null && !neededJsInjects.Contains(jsInjector)) {
                neededJsInjects.Add(jsInjector);
              }
            }
          }
        }
      }
      // Joined report
      Debug.Log($"{logPrefix} Found <color=cyan>{neededJsInjects.Count}</color> JsInjectors needed in scene: [ {string.Join(", ", neededJsInjects.Select(jci => jci.GetType().Name))} ]");
      return neededJsInjects.ToArray();
    }
  #endif
}
