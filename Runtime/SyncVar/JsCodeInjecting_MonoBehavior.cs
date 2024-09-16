using UnityEngine;
using System.Threading.Tasks;


#if UNITY_EDITOR
using System.Text.RegularExpressions;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;
#endif

//=================== ||||||||||||||||||||||||||| =========================
abstract public class JsCodeInjecting_MonoBehaviour : MonoBehaviour {

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
    //---------------- |||||||||||||||||||||| ----------------------------------------
    static public void InjectMissingJsPlugins() {
      foreach (var missingJsPluginType in AnalyzeAllJsPlugins().tsMissingSomePart) {
        Debug.Log($"{logPrefix} EnsuringInstance for {missingJsPluginType.Name}");
        var jsInjectorInstance = Singletoner.EnsureInstByType(missingJsPluginType) as JsCodeInjecting_MonoBehaviour;
        Debug.Log($"{logPrefix} Injecting JsPluginCode for {missingJsPluginType.Name}");
        jsInjectorInstance.InjectJsPluginCode();
      }
    }
    //---------------------------------------- ||||||||||||||||||||||||| ----------------------------------------
    static public JsCodeInjecting_MonoBehaviour EnsureJsInjectorIsInScene( System.Type jsInjectorType ) {
      return Singletoner.EnsureInstByType(jsInjectorType) as JsCodeInjecting_MonoBehaviour;
    }
    //---------------- |||||||||||||||||||||||||||| ----------------------------------------
    static public bool JsFileForThisClassTypeExists( System.Type jsInjectorType ) {
      // ensure this is a subclass of JsCodeInjecting_MonoBehaviour
      if (!typeof(JsCodeInjecting_MonoBehaviour).IsAssignableFrom(jsInjectorType)) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() called with a non-JsCodeInjecting_MonoBehaviour subclass: {jsInjectorType.Name}");
        return false;
      }
      // Call static JsPluginFileName() method for this class
      // Calling the .I getter will also ensure the instance is created in the scene if it doesn't exist
      var jsInjectorMB = (JsCodeInjecting_MonoBehaviour)jsInjectorType.GetMethod("I")?.Invoke(null, null);
      if (jsInjectorMB == null) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() could not find a JsPluginFileName() method for {jsInjectorType.Name}");
        return false;
      }
      string jsPluginFileName = jsInjectorMB.JsPluginFileName();
      var modelClassPath = CqFile.AppFolder().DeeperFile(jsPluginFileName);
      return modelClassPath.Exists();
    }
    //========== |||||||||||||| ====================
    public class JsPluginReport {
      public HashSet<System.Type> neededTs            = new();
      public HashSet<System.Type> missingSceneInstancesOfTs  = new();
      public HashSet<System.Type> haveSceneInstancesOfTs  = new();
      public HashSet<System.Type> tsThatAreReady = new();
      public HashSet<System.Type> tsMissingSomePart = new();
      public HashSet<string> filesThatNeedPlugins = new();
      public HashSet<string> filesThatAreReady    = new();
      public HashSet<string> filesMissingPlugins  = new();
      public string needTxt;
      public string neededOnesTxt;
      public string haveInstOnesTxt;
      public string haveJsFileOnesTxt;
      public string missingPartOnesTxt;

    }
    //------------------------- |||||||||||||||||||||||| ----------------------------------------
    static public JsPluginReport AnalyzeAllJsPlugins() {

      JsPluginReport rpt = new();

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
        MonoScript sbScript = MonoScript.FromMonoBehaviour(behaviour);
        string sbPath = AssetDatabase.GetAssetPath(sbScript);
        if (sbScript.text == null) {
          Debug.LogError($"{logPrefix} FindMissingJsPluginTypes() found a SyncedBehaviour with no script: {behaviour.name}");
          continue;
        }
        // 2. Check if it contains a pattern with a needed JsInjector
        foreach (var jsInjectorType in codeMatchPatternsByJsInjectorsNeeded.Keys) {
          foreach (var pattern in codeMatchPatternsByJsInjectorsNeeded[jsInjectorType]) {
            if (Regex.IsMatch(sbScript.text, pattern)) {
              // 3. If it does, add the JsInjector to the neededInjectors list
              rpt.neededTs.Add(jsInjectorType);
              string sbPathAndPattern = $"{sbPath}<color=grey> needs: </color> <color=yellow>{jsInjectorType}</color> for: <color=white>{(pattern.Replace("\\",""))}</color>";
              rpt.filesThatNeedPlugins.Add(sbPathAndPattern);
              // 4. Check if the class has an instance in the scene
              var jsInjectorInstance = (JsCodeInjecting_MonoBehaviour)Object.FindObjectOfType(jsInjectorType);
              // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance. 
              // Also continue if it is disabled
              if (jsInjectorInstance == null || !jsInjectorInstance.enabled) {
                rpt.missingSceneInstancesOfTs.Add(jsInjectorType);
                continue;
              }
              rpt.haveSceneInstancesOfTs.Add(jsInjectorType);
              // 6. Call JsPluginFileName() method for this class
              string jsPluginFileName = jsInjectorInstance.JsPluginFileName();
              // 7. Check if the file exists
              var modelClassPath = CqFile.AppFolder().DeeperFile(jsPluginFileName);
              if (modelClassPath.Exists()) {
                rpt.tsThatAreReady.Add(jsInjectorInstance.GetType());
                rpt.filesThatAreReady.Add(sbPathAndPattern);
              }
              
            }
          }
        }
      }
      rpt.tsMissingSomePart   = rpt.neededTs.Except(rpt.tsThatAreReady).ToHashSet();
      rpt.filesMissingPlugins = rpt.filesThatNeedPlugins.Except(rpt.filesThatAreReady).ToHashSet();
      // lambda for report text from List
      var rptList = new System.Func<HashSet<System.Type>, string>((types) => {
        return "[ " + string.Join(", ", types.Select(x => $"%ye%{x.Name}%gy%")) + " ]";
      });
      // lambda for report text "Count:%cy%{A.Length}%gy% of %cy%{B.Count}%gy%
      var countOfCount = new System.Func<HashSet<System.Type>, HashSet<System.Type>, string>((A, B) => {
        return $"Count:%cy%{A.Count}%gy% of %cy%{B.Count}%gy%";
      });
      string rptMissings = rptList(rpt.missingSceneInstancesOfTs);
      string rptAOKs     = rptList(rpt.tsThatAreReady);
      string rptNeededs  = rptList(rpt.neededTs);
      string rptHaves    = rptList(rpt.haveSceneInstancesOfTs);
      rpt.neededOnesTxt          = $"{logPrefix} %cy%{rpt.neededTs.Count}%gy% needed JsInjectors: {rptNeededs}".TagColors();
      rpt.haveInstOnesTxt        = $"{logPrefix} {countOfCount(rpt.haveSceneInstancesOfTs, rpt.neededTs)} JsInjectors %gre%have%gy% an instance in scene: {rptHaves}".TagColors();
      rpt.haveJsFileOnesTxt      = $"{logPrefix} {countOfCount(rpt.tsThatAreReady,         rpt.neededTs)} JsInjectors are %gre%ready%gy% to go: {rptAOKs}".TagColors();
      rpt.missingPartOnesTxt     = $"{logPrefix} {countOfCount(rpt.tsMissingSomePart,      rpt.neededTs)} JsInjectors are %red%MISSING%gy% a part: {rptList(rpt.tsMissingSomePart)}".TagColors();
      return rpt;
    }
    //---------------- ||||||||||||||||| ----------------------------------------
    public static bool LogJsPluginReport(JsPluginReport pluginRpt) {
      // lambda function for report text from List
      var rptList = new System.Func<HashSet<System.Type>, string>((types) => {
        return "[ " + string.Join(", ", types.Select(x => $"<color=yellow>{x.Name}</color>")) + " ]";
      });

      var fldr = $"<color=#ff55ff>Assets/CroquetJS/{CqFile.GetAppNameForOpenScene()}/plugins/</color>";
      int missingCnt = pluginRpt.tsMissingSomePart.Count;
      int neededCnt = pluginRpt.neededTs.Count;
      bool amMissingPlugins = pluginRpt.tsMissingSomePart.Count > 0;
      if (amMissingPlugins) {
        Debug.Log(pluginRpt.neededOnesTxt);
        Debug.Log(pluginRpt.haveInstOnesTxt);
        Debug.Log(pluginRpt.haveJsFileOnesTxt);
        // for each missing file, log the file
        foreach (var missingFile in pluginRpt.filesMissingPlugins) {
          Debug.Log($"|    Missing its Js Plugin: <color=#ff7777>{missingFile}</color>");
        }
        // for all ready files, log the file
        foreach (var readyFile in pluginRpt.filesThatAreReady) {
          Debug.Log($"|    Js Plugin is ready for: <color=#55ff55>{readyFile}</color>");
        }
        // Debug.Log(pluginRpt.missingPartOnesTxt);
        Debug.Log($"| <color=#ff5555>MISSING</color>  <color=cyan>{missingCnt}</color> of <color=cyan>{neededCnt}</color> JS Plugins: {rptList(pluginRpt.tsMissingSomePart)} in {fldr}");
        Debug.Log($"|    To Add Missing JS Plugin Files, in Menu:");
        Debug.Log($"|    <color=white>Croquet > Open Build Assistant Window > [Check If Ready], then [Add Missing JS Plugin Files]</color>");
      }
      else {
        Debug.Log($"All needed JS Plugins found in {fldr}: {rptList(pluginRpt.neededTs)}");
      }

      return amMissingPlugins;
    }
  #endif
}
