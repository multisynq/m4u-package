using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
  using UnityEditor;
#endif
using UnityEngine;

namespace Multisynq {

public class JsPlugin_Writer: MonoBehaviour {
  static public string logPrefix = "[%ye%Js%cy%Plugin_Writer%gy%]".TagColors();
  #if UNITY_EDITOR
    //------------------ ||||||||||||||||||||||||| -------------------------
    static public string MakeIndexOfPlugins_JsCode( List<JsPluginCode> jsPluginCodes ) {
      string imports = "";
      string modelInits = "";
      string viewInits = "";
      foreach( JsPluginCode plugCode in jsPluginCodes) {
        string[] expts    = plugCode._pluginExports;
        string exptsStr   = string.Join(", ", expts);
        string plugNm     = plugCode._pluginName;
        bool hasView  = expts.Contains(plugNm+"_View");
        bool hasModel = expts.Contains(plugNm+"_Model");

        imports                  += $"        import {{ {exptsStr} }} from './{plugNm}'\n";
        if (hasModel) modelInits += $"            this.pluginModels['{plugNm}_Model'] = {plugNm}_Model.create({{}})\n";
        if (hasView) viewInits   += $"            this.pluginViews['{plugNm}_View'] = new {plugNm}_View(model.pluginModels['{plugNm}_Model'])\n";
      }

      string code =  $@"
        // DO NOT EDIT THIS GENERATED FILE, please.  =]
        // This file is generated by M4U's JsPlugin_Behaviour.cs
        import {{ GameModelRoot, GameViewRoot }} from '@multisynq/unity-js';

        // ######## imports generated from each JsPlugin_Behavior.cs subclass
{imports.Trim('\n')}
        // ########

        //========== |||||||||||||||| =================================================================
        export class PluginsModelRoot extends GameModelRoot {{
          pluginModels={{}}
          init(options) {{
            //@ts-expect-error: init() missing
            super.init(options);

            // ######## modelInits
{modelInits.Trim('\n')}
            // ########

          }}
        }}
        //@ts-expect-error: register() missing
        PluginsModelRoot.register('PluginsModelRoot');

        //========== ||||||||||||||| =================================================================
        export class PluginsViewRoot extends GameViewRoot {{
          pluginViews={{}}
          constructor(model) {{
            super(model);

            // ######### viewInits
{viewInits.Trim('\n')}
            // #########

          }}
          detach() {{
            Object.values(this.pluginViews).forEach(vPlug => vPlug.detach());
            //@ts-expect-error: detach() missing
            super.detach();
          }}
        }}
      ".LessIndent();
      return code;
    }
    //---------------- ||||||||||||||||||||||| -------------------------
    public static void WriteIndexOfPluginsFile(List<JsPluginCode> jsPluginCodes) {
      var plugFldr = Mq_File.AppPluginsFolder();
      var outp = plugFldr.DeeperFile("indexOfPlugins.js");
      outp.WriteAllText(MakeIndexOfPlugins_JsCode(jsPluginCodes));
    }

    //---------------- |||||||||||||||||||||||||||||||||||||||||||||||||||| -------------------------
    public static void PrependPluginCodeAndWrapExistingCodeInCommentMarkers(bool needsPlugins) {
      var idxFile = Mq_File.AppFolder().DeeperFile("index.js");
      var code = idxFile.ReadAllText();
      code = IndexJsCode(needsPlugins, code);
      idxFile.WriteAllText(code);
    }
    //---------------- ||||||||||||||||||||||| -------------------------
    public static bool IndexJsHasPluginsImport(bool needsSomePlugins = false) {
      var idxFile = Mq_File.AppFolder().DeeperFile("index.js");
      var code = (idxFile.Exists()) ? idxFile.ReadAllText() : "";
      // expect these to be in the file: "PluginsModelRoot", "PluginsViewRoot"
      // use RegExp so we know if they are on a commented comment of // or not
      bool isOk = Regex.IsMatch(code, "[^//]*PluginsModelRoot") && Regex.IsMatch(code, "[^//]*PluginsViewRoot");
      if (!isOk && needsSomePlugins) {
        Debug.LogError(@$"{logPrefix} Missing the 'PluginsModelRoot' and 'PluginsViewRoot' in {idxFile.shortPath} Needed code: --->
          {IndexJsCode(true)}" + "\n\n\n"
        );
      }
      return isOk;
    }
    //---------------- |||||||||||||||||||| ----------------------------
    static public void WriteOneJsPluginFile(JsPluginCode jsPlugin) {
      // if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void WriteOneJsPlugin()");
      var file = Mq_File.AppPluginsFolder().EnsureExists().DeeperFile(jsPlugin._pluginName+".js");
      file.WriteAllText(jsPlugin._pluginCode);
      Debug.Log($"{logPrefix} Wrote %gr%{file.shortPath}%gy%".Replace(jsPlugin._pluginName, $"%ye%{jsPlugin._pluginName}%gr%").TagColors());
    }

    //---------------- |||||||||||||||||| ------------------------------
    static public void JsPluginFileExists(JsPluginCode jsPlugin, string className) {
        var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin._pluginName}.js");
        if (modelClassPath.Exists()) {
            Debug.Log($"{logPrefix} '{jsPlugin._pluginName}.js' already present at '{modelClassPath.longPath}'");
        } else {
            modelClassPath.SelectAndPing();
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"MISSING JS FILE {jsPlugin._pluginName}.js for {className}.cs");
            Debug.Log(      "  FIX  in Menu: <color=white>Multisynq > Open Build Assistant > [Check if Ready]</color>");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            EditorApplication.isPlaying = false;
        }
    }
    //---------------- |||||||||||||||||||||||| -------------------------------
    public static void WriteNeededJsPluginFiles(JsPluginReport jsPluginRpt =  null) {
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins();
      if (!rpt.needsSomePlugins) return;
      Debug.Log($"%mag%WRITE ALL%gy%{rpt.neededOnesTxt}".TagColors());
      WriteJsPluginsAndTheirIndex(   rpt.neededTs.ToList(), jsPluginRpt );
    }
    //---------------- ||||||||||||||||||||| ---------------------------
    public static void WriteMissingJsPlugins( JsPluginReport jsPluginRpt = null) {
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins();
      WriteJsPluginsAndTheirIndex( rpt.tsMissingSomePart.ToList(), jsPluginRpt );
    }
    //---------------- ||||||||||||||||||||||||||| ----------------------------------
    public static void WriteJsPluginsAndTheirIndex(List<Type> jsPluginTypes, JsPluginReport jsPluginRpt = null) {
      var jpcs = jsPluginTypes.Select(jpt => JsPlugin_ToSceneAndFile(jpt).GetJsPluginCode()).ToList();
      if (jpcs.Count == 0) return;
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins();
      var allNeededJsPlugins = rpt.neededTs.Select(jpt => JsPlugin_ToSceneAndFile(jpt).GetJsPluginCode()).ToList();
      WriteIndexOfPluginsFile( allNeededJsPlugins );
    }
    //------------------------------ ||||||||||||||||||||||| ----------------------------------
    static public JsPlugin_Behaviour JsPlugin_ToSceneAndFile( Type jsPluginType ) {
      var jsPluginMB = Singletoner.EnsureInstByType(jsPluginType) as JsPlugin_Behaviour;
      Debug.Log($"{logPrefix} Ensured GameObject with a '%ye%{jsPluginType.Name}%gy%' on it.".TagColors(), jsPluginMB.gameObject);
      jsPluginMB.WriteMyJsPluginFile();
      return jsPluginMB;
    }

    static public bool JsFileForThisClassTypeExists(Type jsPluginType) {
      if (!typeof(JsPlugin_Behaviour).IsAssignableFrom(jsPluginType)) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() called with a non-JsCodeInjecting_MonoBehaviour subclass: {jsPluginType.Name}");
        return false;
      }

      var jsPluginMB = (JsPlugin_Behaviour)jsPluginType.GetMethod("I")?.Invoke(null, null);
      if (jsPluginMB == null) {
        Debug.LogError($"{logPrefix} JsFileForThisClassTypeExists() could not find a GetJsPluginCode() method for {jsPluginType.Name}");
        return false;
      }
      var jsPlugin = jsPluginMB.GetJsPluginCode();
      var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin._pluginName}.js");
      return modelClassPath.Exists();
    }
    //========== |||||||||||||| ====================
    public class JsPluginReport {
      public HashSet<Type> neededTs                  = new();
      public HashSet<Type> missingSceneInstancesOfTs = new();
      public HashSet<Type> haveSceneInstancesOfTs    = new();
      public HashSet<Type> tsThatAreReady            = new();
      public HashSet<Type> tsMissingSomePart         = new();
      public HashSet<string> filesThatNeedPlugins    = new();
      public HashSet<string> filesThatAreReady       = new();
      public HashSet<string> filesMissingPlugins     = new();
      public string needTxt;
      public string neededOnesTxt;
      public string haveInstOnesTxt;
      public string haveJsFileOnesTxt;
      public string missingPartOnesTxt;
      public bool   needsSomePlugins = false;
    }
    

    //-------------------------- ||||||||||||||||||| ----------------------------------------
    static public JsPluginReport AnalyzeAllJsPlugins() {

      JsPluginReport rpt = new();

      // 0. For each SynqBehavior
      // 1. Read the script file
      // 2. Check if it contains a pattern that needs a JsPlugin
      // 3. If it does, add the JsPlugin to the neededPlugins list
      // 4. Check if the class has an instance in the scene
      // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance
      // 6. Call JsPluginFileName() method for this class
      // 7. Check if the file exists

      // 0. For each SynqBehavior
      foreach (var behaviour in FindObjectsOfType<SynqBehaviour>(false)){ // false means we skip inactives
        // 1. Read the SynqBehavior script file
        MonoScript synqBehScript = MonoScript.FromMonoBehaviour(behaviour);
        string            sbPath = AssetDatabase.GetAssetPath(synqBehScript);
        string       synqBehCode = synqBehScript.text;
        if (synqBehCode == null) {
          Debug.LogError($"{logPrefix} FindMissingJsPluginTypes() found a SynqBehaviour with no script: {behaviour.name}");
          continue;
        }
        Dictionary<Type, string[]> codeMatchesByJsPlugin =
          typeof(JsPlugin_Behaviour).DictOfSubclassStaticMethodResults<string[]>( "CodeMatchPatterns" );

        // 2. Check if it contains a pattern that needs a JsPlugin
        foreach (var (jsPluginType, codeMatches) in codeMatchesByJsPlugin) {
          foreach (string codeMatchRegex in codeMatches) {
            if (Regex.IsMatch(synqBehCode, codeMatchRegex)) {
              // 2.5 ensure it is not inside a comment
              // if (Regex.IsMatch(sbScript.text, @"//.*" + pattern)) continue; // TODO: add this and test it

              // 3. If it does, add the JsPlugin to the neededPlugins list
              rpt.neededTs.Add(jsPluginType);
              string sbPathAndPattern = $"{sbPath}<color=grey> needs: </color> <color=yellow>{jsPluginType}</color> for: <color=white>{codeMatchRegex.Replace("\\","")}</color>";
              rpt.filesThatNeedPlugins.Add(sbPathAndPattern);
              // 4. Check if the class has an instance in the scene
              var jsPluginInstance = (JsPlugin_Behaviour)FindObjectOfType(jsPluginType, false);
              // 5. Continue if not in scene since we cannot get the JsPluginFileName() method from a non-instance.
              // Also continue if it is disabled
              if (jsPluginInstance == null || !jsPluginInstance.enabled) {
                rpt.missingSceneInstancesOfTs.Add(jsPluginType);
                continue;
              }
              rpt.haveSceneInstancesOfTs.Add(jsPluginType);
              // 6. Call JsPluginFileName() method for this class
              string jsPluginFileName = $"plugins/{jsPluginInstance.GetJsPluginCode()._pluginName}.js";
              // 7. Check if the file exists
              var modelClassPath = Mq_File.AppFolder().DeeperFile(jsPluginFileName);
              if (modelClassPath.Exists()) {
                rpt.tsThatAreReady.Add(jsPluginInstance.GetType());
                rpt.filesThatAreReady.Add(sbPathAndPattern);
              }
            } // if IsMatch
          } // foreach codeMatchPatterns
        } // foreach codeMatchPatternsByJsPlugin
      } // foreach SynqBehaviour
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
      rpt.neededOnesTxt          = $"{logPrefix} %cy%{rpt.neededTs.Count}%gy% needed JsPlugins: {rptNeededs}".TagColors();
      rpt.haveInstOnesTxt        = $"{logPrefix} {countOfCount(rpt.haveSceneInstancesOfTs, rpt.neededTs)} JsPlugins %gre%have%gy% an instance in scene: {rptHaves}".TagColors();
      rpt.haveJsFileOnesTxt      = $"{logPrefix} {countOfCount(rpt.tsThatAreReady,         rpt.neededTs)} JsPlugins are %gre%ready%gy% to go: {rptAOKs}".TagColors();
      rpt.missingPartOnesTxt     = $"{logPrefix} {countOfCount(rpt.tsMissingSomePart,      rpt.neededTs)} JsPlugins are %red%MISSING%gy% a part: {rptList(rpt.tsMissingSomePart)}".TagColors();
      rpt.needsSomePlugins = rpt.neededTs.Count > 0;
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
        Debug.Log($"|    <color=white>Multisynq > Open Build Assistant Window > [Check If Ready], then [Add Missing JS Plugin Files]</color>");
      }
      else {
        Debug.Log($"All needed JS Plugins found in {fldr}: {rptList(pluginRpt.neededTs)}");
      }

      return amMissingPlugins;
    }

    public static string IndexJsCode(bool usesPlugins, string existingCode=null) => @$"
      import {{ BUILD_IDENTIFIER }} from './buildIdentifier'
      import {{ StartSession }}     from '@multisynq/unity-js'
      // Choice A should auto-select
      // ==== Choice A: ==== If you are using any JsPlugins like [SynqVar] or [SynqRPC]
      {(usesPlugins?"":"//")} import {{ PluginsModelRoot as _ModelRoot, PluginsViewRoot as _ViewRoot }} from './plugins/indexOfPlugins'

      // ==== Choice B: ==== If you want to use the default base classes
      {(usesPlugins?"//":"")} import {{ GameModelRoot as _ModelRoot, GameViewRoot as _ViewRoot }} from '@multisynq/unity-js'

      //=== ||||||||||| =================================== ||||||| ||||||  ========
      class MyModelRoot extends _ModelRoot {{ // Learn about Croquet Models: https://croquet.io/dev/docs/croquet/Model.html
        init(options) {{
          // @ts-ignore-error: init() missing
          super.init(options)
        }}
      }}
      // @ts-expect-error: register() missing
      MyModelRoot.register('MyModelRoot')

      //=== |||||||||| ================================== ||||||| |||||  ========
      class MyViewRoot extends _ViewRoot {{ // Learn about Croquet Views: https://croquet.io/dev/docs/croquet/View.html
        constructor(model) {{ // calling StartSession() will pass an instance of the model above to tie them together
          super(model)
        }}
      }}

      //============ ||||||| ||||||||  ========
      // Learn about Croquet Sessions: https://croquet.io/dev/docs/croquet/Session.html
      StartSession(MyModelRoot, MyViewRoot, BUILD_IDENTIFIER)

      {RelegatedCode(existingCode)}
      ".LessIndent();

    public static string RelegatedCode(string existingCode=null) => (existingCode==null) ? "" : @$"
      /*
        NOTICE:
        In order to get you up and running, your code has been relegated down here as a comment.
        You can uncomment and merge the logic you desire into the code above.
        Primarily this occurs when you are using JsPlugins that require the import of PluginsModelRoot and PluginsViewRoot.
        If you want JsPlugins, make sure to keep references to PluginsModelRoot and PluginsViewRoot referenced.
        If you do not want JsPlugins, then hunt through your in-scene *.cs code to remove use of the SynqBehaviour classes.

        {existingCode.Replace("*/", "* /")}

      */
    ".LessIndent();

    static public void WriteIndexJsFile(bool usesPlugins, string existingCode=null) {
      var idxFile = Mq_File.AppIndexJs();
      var code = IndexJsCode(usesPlugins, existingCode);
      idxFile.WriteAllText(code);
    }

    //------------------ ||||||||||| -------------------------
    // public static string IndexJsCode = @$"
    //   import {{ StartSession }} from '@multisynq/unity-js'
    //   import {{ PluginsModelRoot, PluginsViewRoot }} from './plugins/indexOfPlugins'
    //   import {{ BUILD_IDENTIFIER }} from './buildIdentifier'
    //   StartSession(PluginsModelRoot, PluginsViewRoot, BUILD_IDENTIFIER)
    //   ".LessIndent();


  #endif
}

}