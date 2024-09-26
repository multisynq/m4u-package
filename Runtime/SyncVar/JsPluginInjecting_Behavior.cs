using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;              // TODO: move this into the #if UNITY_EDITOR block and wrap dependent code below it
using System.Text.RegularExpressions; // TODO: include as many of these similarly as we can


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Multisynq {

//========== |||||||||||||||| ================
public class CodeBlockForATag {
  public string tag;
  public string code;
  public int indent;
  //-------- |||||||||||||||| --- constructor
  public     CodeBlockForATag(string tag, string code, int indent=2) {
    this.tag = tag;
    this.code = code;
    this.indent = indent;
  }
}
//========== |||||||||||| ================
public class CodeTemplate {
  public string template;

  //-------- |||||||||||| --- constructor
  public     CodeTemplate(string template) {
    this.template = template;
  }

  public string MergeCodeBlocks( CodeBlockForATag[] codeBlocksForTags ) {
    string result = template;
    var tcsGrouped = codeBlocksForTags.GroupBy(tc => tc.tag);
    
    foreach (IGrouping<string, CodeBlockForATag> group in tcsGrouped) {
      int indent = group.First()?.indent ?? 0;
      string indentedCodeBlocks = group.Select(cb => cb.code).JoinIndented(indent, "\n");
      result = Regex.Replace(result, $@"(?<=\n|^)\s*\[\[{Regex.Escape(group.Key)}]]", indentedCodeBlocks);
    }
    return result;
  }
}
//=================== ||||||||||||||||||||||||||| ================
abstract public class JsPluginInjecting_Behaviour : MonoBehaviour {

    static public string logPrefix = "[%ye%Js%cy%Plugin%gy%]".TagColors();
    static bool dbg = true;
    abstract public JsPluginCode GetJsPluginCode();
    static public string[] CodeMatchPatterns() => new string[]{"You should define CodeMatchPatterns() in your subclass of JsPluginInjecting_Behaviour"};  


    static public string template = @"
      %[ImportStatements]%
      
      export function init() {
        // each klassName's initCode inserts below here
        %[ModelInits]%
        // each klassName's initCode inserts above here
      }
      // Usage:
      //   import { init } from './plugins/indexPlugins';
      //   init();
    ".LessIndent();
    //---------------- |||||||||||||||||||| -------------------------
    static public void UpdateIndexPluginsJs(IEnumerable<JsPluginCode> allPlugins) {
      CodeTemplate indexPluginTemplate = new CodeTemplate(template);
      var indexPluginFile = Mq_File.AppFolder().DeeperFile("plugins", "indexPlugins.js");
      // extract the TaggedCode[] from allPlugins
      CodeBlockForATag[] taggedCodes = allPlugins.SelectMany(jpc => jpc._taggedCodes).ToArray();
      string code = indexPluginTemplate.MergeCodeBlocks(taggedCodes); // include importStatements and inits
      indexPluginFile.WriteAllText(code);
    }
    
    #if UNITY_EDITOR
    // static public Dictionary<System.Type, string[]> codeMatchPatternsByJsInjectorsNeeded = new() {
    //     { typeof(SynqVar_Mgr), new[] { @"\[SynqVar\]" } },
    //     { typeof(SynqCommand_Mgr), new[] { @"\[SynqCommand\]", @"\[SynqRPC\]" } },
    //     { typeof(SynqClones_Mgr), new[] {@"SynqClones_Mgr.*SynqClone", @"\[SyncedInstances\]"} },
    //     // Add more patterns here as needed
    // };
      static public Dictionary<Type, string[]> _codeMatchPatternsByJsInjectorsNeeded = null;
      static public Dictionary<Type, string[]> codeMatchPatternsByJsInjectorsNeeded { 
        get { 
          if (_codeMatchPatternsByJsInjectorsNeeded == null) {
            _codeMatchPatternsByJsInjectorsNeeded = findCodeMatchPatternsByJsInjectorsNeeded();
          }
          return _codeMatchPatternsByJsInjectorsNeeded;
        } 
      }
      static public Dictionary<Type, string[]> findCodeMatchPatternsByJsInjectorsNeeded() {
          Type[] subClasses = KlassHelper.GetSubclassTypes( typeof(JsPluginInjecting_Behaviour));
          Debug.Log($"{logPrefix} Found {subClasses.Length} subclasses of JsPluginInjecting_Behaviour [{string.Join(", ", subClasses.Select(x => x.Name))}]"); 
          Dictionary<Type, string[]> res = new();
          foreach (Type subClass in subClasses) {
            res[subClass] = (
              KlassHelper.FindMethod(
                subClass, "CodeMatchPatterns", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
              )?.Invoke(null, null) as string[])
              ?? new string[] {$"XXX"};
          }
          string report = string.Join(", ", res.Select(x => $"{x.Key.Name}: {string.Join(", ", x.Value)}"));
          Debug.Log($"{logPrefix} %%%% {report}");
          return res;
      }
    #endif

    #if !UNITY_EDITOR
    virtual public void InjectJsPluginCode() { }
    #endif

    virtual public void Start() {
        #if UNITY_EDITOR
        CheckIfMyJsCodeIsPresent();
        #endif
    }

    #if UNITY_EDITOR

    virtual public void InjectJsPluginCode() {
        // if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void InjectJsPluginCode()");
        var jsPlugin = GetJsPluginCode();
        var file = Mq_File.AppFolder().DeeperFile(jsPlugin.GetRelPath());
        file.WriteAllText(jsPlugin._klassCode);
        Debug.Log($"{logPrefix} Wrote %gr%{file.shortPath}%gy%".Replace(jsPlugin._klassName, $"%ye%{jsPlugin._klassName}%gr%").TagColors());  
    }

    public void CheckIfMyJsCodeIsPresent() {
        var jsPlugin = GetJsPluginCode();
        var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin._klassName}.js");
        if (modelClassPath.Exists()) {
            Debug.Log($"{logPrefix} '{jsPlugin._klassName}.js' already present at '{modelClassPath.longPath}'");
        } else {
            modelClassPath.SelectAndPing();
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"MISSING JS FILE {jsPlugin._klassName}.js for {this.GetType().Name}.cs");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            EditorApplication.isPlaying = false;
        }
    }

    static public void InjectMissingJsPlugins___OLD() {
      var allPlugins = new List<JsPluginCode>();
      foreach (var missingJsPluginType in AnalyzeAllJsPlugins().tsMissingSomePart) {
        Debug.Log($"{logPrefix} EnsuringInstance for {missingJsPluginType.Name}");
        var jsInjectorInstance = Singletoner.EnsureInstByType(missingJsPluginType) as JsPluginInjecting_Behaviour;
        Debug.Log($"{logPrefix} Injecting JsPluginCode for {missingJsPluginType.Name}");
        jsInjectorInstance.InjectJsPluginCode();
        allPlugins.Add(jsInjectorInstance.GetJsPluginCode());
      }
      UpdateIndexPluginsJs(allPlugins);
    }

    public static void InjectAllJsPlugins() {
      var az = AnalyzeAllJsPlugins();
      var allPluginTypes = az.neededTs;
      Debug.Log($"%mag%ALL%gy%{az.neededOnesTxt}".TagColors());
      InjectJsPluginsList( allPluginTypes.ToList() );
    }

    public static void InjectMissingJsPlugins() {
      var missingPluginTypes = AnalyzeAllJsPlugins().tsMissingSomePart;
      InjectJsPluginsList( missingPluginTypes.ToList() );
    }

    public static void InjectJsPluginsList(List<Type> jsPluginTypes) {
      var jpcs = jsPluginTypes.Select(jcp => InjectOneJsPlugin(jcp).GetJsPluginCode());
      UpdateIndexPluginsJs(jpcs);
    }

    static public JsPluginInjecting_Behaviour InjectOneJsPlugin( Type jsPluginType ) {
      var jsInjectorInstance = Singletoner.EnsureInstByType(jsPluginType) as JsPluginInjecting_Behaviour;
      Debug.Log($"{logPrefix} Ensured GameObject with a '%ye%{jsPluginType.Name}%gy%' on it. Click this log line to ping in Hierarchy.".TagColors(), jsInjectorInstance.gameObject);  
      jsInjectorInstance.InjectJsPluginCode();
      return jsInjectorInstance;
    }

    static public JsPluginInjecting_Behaviour EnsureJsInjectorIsInScene(System.Type jsInjectorType) {
      return Singletoner.EnsureInstByType(jsInjectorType) as JsPluginInjecting_Behaviour;
    }

    static public bool JsFileForThisClassTypeExists(System.Type jsInjectorType) {
      if (!typeof(JsPluginInjecting_Behaviour).IsAssignableFrom(jsInjectorType)) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() called with a non-JsCodeInjecting_MonoBehaviour subclass: {jsInjectorType.Name}");
        return false;
      }

      var jsInjectorMB = (JsPluginInjecting_Behaviour)jsInjectorType.GetMethod("I")?.Invoke(null, null);
      if (jsInjectorMB == null) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() could not find a GetJsPluginCode() method for {jsInjectorType.Name}");
        return false;
      }
      var jsPlugin = jsInjectorMB.GetJsPluginCode();
      var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin._klassName}.js");
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
    //-------------------------- ||||||||||||||||||| ----------------------------------------
    static public JsPluginReport AnalyzeAllJsPlugins() {

      JsPluginReport rpt = new();

      // 0. For each SynqBehavior
      // 1. Read the script file
      // 2. Check if it contains a pattern with a needed JsInjector
      // 3. If it does, add the JsInjector to the neededInjectors list
      // 4. Check if the class has an instance in the scene
      // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance
      // 6. Call JsPluginFileName() method for this class
      // 7. Check if the file exists

      // 0. For each SynqBehavior
      foreach (var behaviour in FindObjectsOfType<SynqBehaviour>(false)){ // false means we skip inactives
        // 1. Read the SynqBehavior script file
        MonoScript sbScript = MonoScript.FromMonoBehaviour(behaviour);
        string sbPath = AssetDatabase.GetAssetPath(sbScript);
        if (sbScript.text == null) {
          Debug.LogError($"{logPrefix} FindMissingJsPluginTypes() found a SynqBehaviour with no script: {behaviour.name}");
          continue;
        }
        // 2. Check if it contains a pattern with a needed JsInjector
        foreach (var jsInjectorType in codeMatchPatternsByJsInjectorsNeeded.Keys) {
          foreach (var pattern in codeMatchPatternsByJsInjectorsNeeded[jsInjectorType]) {
            if (Regex.IsMatch(sbScript.text, pattern)) {
              // 2.5 ensure it is not inside a comment
              // if (Regex.IsMatch(sbScript.text, @"//.*" + pattern)) continue; // TODO: add this and test it

              // 3. If it does, add the JsInjector to the neededInjectors list
              rpt.neededTs.Add(jsInjectorType);
              string sbPathAndPattern = $"{sbPath}<color=grey> needs: </color> <color=yellow>{jsInjectorType}</color> for: <color=white>{(pattern.Replace("\\",""))}</color>";
              rpt.filesThatNeedPlugins.Add(sbPathAndPattern);
              // 4. Check if the class has an instance in the scene
              var jsInjectorInstance = (JsPluginInjecting_Behaviour)FindObjectOfType(jsInjectorType);
              // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance. 
              // Also continue if it is disabled
              if (jsInjectorInstance == null || !jsInjectorInstance.enabled) {
                rpt.missingSceneInstancesOfTs.Add(jsInjectorType);
                continue;
              }
              rpt.haveSceneInstancesOfTs.Add(jsInjectorType);
              // 6. Call JsPluginFileName() method for this class
              string jsPluginFileName = $"plugins/{jsInjectorInstance.GetJsPluginCode()._klassName}.js";
              // 7. Check if the file exists
              var modelClassPath = Mq_File.AppFolder().DeeperFile(jsPluginFileName);
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

      var fldr = $"<color=#ff55ff>Assets/MultisynqJS/{Mq_File.GetAppNameForOpenScene()}/plugins/</color>";
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


} // namespace MultisynqNS