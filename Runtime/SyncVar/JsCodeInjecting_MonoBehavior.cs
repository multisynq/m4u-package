using UnityEngine;
using System.Threading.Tasks;


#if UNITY_EDITOR
using System.Text.RegularExpressions;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;
#endif

//=================== ||||||||||||||||||||||||||| =========================
abstract public class JsCodeInjecting_MonoBehavior : MonoBehaviour {

  static public string logPrefix = "[ <color=yellow>Js</color><color=cyan>CodeInject</color> ]";
  static bool dbg = true;

  abstract public string JsPluginFileName();
  abstract public string JsPluginCode();

  #if UNITY_EDITOR
    static public Dictionary<System.Type, string[]> codeMatchPatternsByJsInjectorsNeeded = new() {
      { typeof(SyncVar_Mgr), new[] { @"\[SyncVar\]" } },
      { typeof(SyncCommand_Mgr), new[] { @"\[SyncCommand\]", @"\[SyncRPC\]" } }
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
      if (dbg)  Debug.Log($"{logPrefix} <color=white>BASE</color>   virtual public void InjectJsPluginCode()");

      var modelClassPath = CqFile.AppFolder().DeeperFile(JsPluginFileName());
      if (modelClassPath.Exists()) {
        Debug.Log($"{logPrefix} '{modelClassPath.shortPath}' already present. Skip.");
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
      foreach (var jsInjectorType in FindMissingJsPluginTypes()) {
        // jsInjector.InjectJsPluginCode();
        var jsInjectorInstance = (JsCodeInjecting_MonoBehavior)Object.FindObjectOfType(jsInjectorType, false);
        Debug.Log($"{logPrefix} EnsuringInstance for {jsInjectorType.Name}");
        jsInjectorInstance = Singletoner.EnsureInstByType(jsInjectorType) as JsCodeInjecting_MonoBehavior;
        jsInjectorInstance.InjectJsPluginCode();
      }
    }
    //---------------------------------------- ||||||||||||||||||||||||| ----------------------------------------
    static public JsCodeInjecting_MonoBehavior EnsureJsInjectorIsInScene( System.Type jsInjectorType ) {
      return Singletoner.EnsureInstByType(jsInjectorType) as JsCodeInjecting_MonoBehavior;
    }
    //---------------- |||||||||||||||||||||||||||| ----------------------------------------
    static public bool JsFileForThisClassTypeExists( System.Type jsInjectorType ) {
      // ensure this is a subclass of JsCodeInjecting_MonoBehavior
      if (!typeof(JsCodeInjecting_MonoBehavior).IsAssignableFrom(jsInjectorType)) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() called with a non-JsCodeInjecting_MonoBehavior subclass: {jsInjectorType.Name}");
        return false;
      }
      // Call static JsPluginFileName() method for this class
      // Calling the .I getter will also ensure the instance is created in the scene if it doesn't exist
      var jsInjectorMB = (JsCodeInjecting_MonoBehavior)jsInjectorType.GetMethod("I")?.Invoke(null, null);
      if (jsInjectorMB == null) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() could not find a JsPluginFileName() method for {jsInjectorType.Name}");
        return false;
      }
      string jsPluginFileName = jsInjectorMB.JsPluginFileName();
      var modelClassPath = CqFile.AppFolder().DeeperFile(jsPluginFileName);
      return modelClassPath.Exists();
    }
    //------------------------- |||||||||||||||||||||||| ----------------------------------------
    static public System.Type[] FindMissingJsPluginTypes() {

      HashSet<System.Type> neededTs            = new();
      HashSet<System.Type> missingSceneInstancesOfTs  = new();
      HashSet<System.Type> haveSceneInstancesOfTs  = new();
      HashSet<System.Type> tsWhereJsFilesExist = new();
      // List<System.Type> missingInjectors  = new();

      // 0. For each SyncedBehavior
      // 1. Read the script file
      // 2. Check if it contains a pattern with a needed JsInjector
      // 3. If it does, add the JsInjector to the neededInjectors list
      // 4. Check if the class has an instance in the scene
      // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance
      // 6. Call JsPluginFileName() method for this class
      // 7. Check if the file exists

      // 0. For each SyncedBehavior
      foreach (var behaviour in FindObjectsOfType<SyncedBehaviour>(false)){ // false means we skip inactives
        // 1. Read the SyncedBehavior script file
        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
        if (script.text == null) {
          Debug.LogError($"{logPrefix} FindMissingJsPluginTypes() found a SyncedBehaviour with no script: {behaviour.name}");
          continue;
        }
        // 2. Check if it contains a pattern with a needed JsInjector
        foreach (var jsInjectorType in codeMatchPatternsByJsInjectorsNeeded.Keys) {
          foreach (var pattern in codeMatchPatternsByJsInjectorsNeeded[jsInjectorType]) {
            if (Regex.IsMatch(script.text, pattern)) {
              // 3. If it does, add the JsInjector to the neededInjectors list
              neededTs.Add(jsInjectorType);
              // 4. Check if the class has an instance in the scene
              var jsInjectorInstance = (JsCodeInjecting_MonoBehavior)Object.FindObjectOfType(jsInjectorType);
              // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance. 
              // Also continue if it is disabled
              if (jsInjectorInstance == null || !jsInjectorInstance.enabled) {
                missingSceneInstancesOfTs.Add(jsInjectorType);
                continue;
              }
              haveSceneInstancesOfTs.Add(jsInjectorType);
              // 6. Call JsPluginFileName() method for this class
              string jsPluginFileName = jsInjectorInstance.JsPluginFileName();
              // 7. Check if the file exists
              var modelClassPath = CqFile.AppFolder().DeeperFile(jsPluginFileName);
              if (modelClassPath.Exists()) {
                tsWhereJsFilesExist.Add(jsInjectorInstance.GetType());
              }
              
            }
          }
        }
      }
      var tsMissingSomePart = neededTs.Except(tsWhereJsFilesExist).ToHashSet();
      if (tsMissingSomePart.Count == 0) {
        return new System.Type[0]; // no missing injectors, return empty array
      }
      // lambda for report text from List
      var rptList = new System.Func<HashSet<System.Type>, string>((types) => {
        return "[ " + string.Join(", ", types.Select(x => $"%ye%{x.Name}%gy%")) + " ]";
      });
      // lambda for report text "Count:%cy%{A.Length}%gy% of %cy%{B.Count}%gy%
      var countOfCount = new System.Func<HashSet<System.Type>, HashSet<System.Type>, string>((A, B) => {
        return $"Count:%cy%{A.Count}%gy% of %cy%{B.Count}%gy%";
      });
      string rptMissings = rptList(missingSceneInstancesOfTs);
      string rptAOKs     = rptList(tsWhereJsFilesExist);
      string rptNeededs  = rptList(neededTs);
      string rptHaves    = rptList(haveSceneInstancesOfTs);
      Debug.Log($"{logPrefix} %cy%{neededTs.Count}%gy% needed JsInjectors: {rptNeededs}".TagColors());
      Debug.Log($"{logPrefix} {countOfCount(haveSceneInstancesOfTs, neededTs)} JsInjectors %gre%have%gy% an instance in scene: {rptHaves}".TagColors());
      Debug.Log($"{logPrefix} {countOfCount(tsWhereJsFilesExist, neededTs)} JsInjectors %gre%have%gy% instance & a js file: {rptAOKs}".TagColors());
      Debug.Log($"{logPrefix} {countOfCount(tsMissingSomePart, neededTs)} JsInjectors are %red%MISSING%gy% a part: {rptList(tsMissingSomePart)}".TagColors());
      return tsMissingSomePart.ToArray();
    }
  #endif
}
