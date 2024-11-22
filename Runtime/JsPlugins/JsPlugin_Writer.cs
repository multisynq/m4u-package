using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
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
      // filter our nulls
      jsPluginCodes = jsPluginCodes.Where(x => x != null).ToList();
      // make distinct using type name
      jsPluginCodes = jsPluginCodes.GroupBy(x => x.pluginName).Select(g => g.First()).ToList();
      
      string imports = "";
      string modelInits = "";
      string viewInits = "";
      foreach( JsPluginCode plugCode in jsPluginCodes) {
        string[] expts    = plugCode.pluginExports;
        string exptsStr   = string.Join(", ", expts);
        string plugNm     = plugCode.pluginName;
        bool hasView  = expts.Contains(plugNm+"_View");
        bool hasModel = expts.Contains(plugNm+"_Model");

        imports                  += $"        import {{ {exptsStr} }} from './{plugNm}'\n";
        if (hasModel) modelInits += $"            this.pluginModels['{plugNm}_Model'] = {plugNm}_Model.create({{}})\n";
        if (hasView) viewInits   += $"            this.pluginViews['{plugNm}_View'] = new {plugNm}_View(model.pluginModels['{plugNm}_Model'])\n";
      }

      string code =  $@"
        // DO NOT EDIT THIS GENERATED FILE, please.  =]
        // This file is generated by M4U's JsPlugin_Writer.cs
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
      if (jsPlugin==null) {
        Debug.LogError($"{logPrefix} WriteOneJsPluginFile() called with a null JsPluginCode");
        return;
      }
      // if (dbg) if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void WriteOneJsPlugin()");
      var file = Mq_File.AppPluginsFolder(true).EnsureExists().DeeperFile(jsPlugin.pluginName+".js");
      bool wasThere = file.Exists();
      string wasThereStr = wasThere ? "<color=#4f4>already there</color>" : "<b><color=#f44>NEW</color></b>";
      file.WriteAllText(jsPlugin.pluginCode);
      Debug.Log($"{logPrefix} Wrote %gr%{file.shortPath}%gy% ({wasThereStr})".Replace(jsPlugin.pluginName, $"%ye%{jsPlugin.pluginName}%gr%").TagColors());
    }

    //---------------- |||||||||||||||||| ------------------------------
    static public void JsPluginFileExists(JsPluginCode jsPlugin, string className) {
      if (jsPlugin==null) return;
        var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin.pluginName}.js");
        if (modelClassPath.Exists()) {
            // if (dbg) Debug.Log($"{logPrefix} '{jsPlugin.pluginName}.js' already present at '{modelClassPath.longPath}'");
        } else {
            modelClassPath.SelectAndPing();
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"   v");
            Debug.LogError($"MISSING JS FILE {jsPlugin.pluginName}.js for {className}.cs");
            Debug.Log(      "  FIX  in Menu: <color=white>Multisynq > Open Build Assistant > [Check if Ready]</color>");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            Debug.LogError($"   ^");
            EditorApplication.isPlaying = false;
        }
    }
    //---------------- |||||||||||||||||||||||| -------------------------------
    public static void WriteNeededJsPluginFiles(JsPluginReport jsPluginRpt =  null) {
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins(false);
      if (!rpt.needsSomePlugins) return;
      Debug.Log($"%mag%WRITE ALL%gy%{rpt.neededOnesTxt}".TagColors());
      JsPluginToScene_File_And_IndexFile(   rpt.needed_Plugins, jsPluginRpt );
    }
    //---------------- ||||||||||||||||||||| ---------------------------
    public static void WriteMissingJsPlugins( JsPluginReport jsPluginRpt = null) {
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins(false);
      JsPluginToScene_File_And_IndexFile( rpt.missingPart_Plugins, rpt );
    }
    //---------------- ||||||||||||||||||||||||||| ----------------------------------
    public static void JsPluginToScene_File_And_IndexFile(List<AnalysisOfOneJsPlugin> jsPlugs, JsPluginReport jsPluginRpt = null) {
      var rpt = jsPluginRpt ?? AnalyzeAllJsPlugins(false);
      // if one is a subclass and has the same JsPluginCode as another, we only write one of them
      // List<AnalysisOfOneJsPlugin> onesToRemove = new();
      // foreach (var jpt_1 in jsPlugs) {
      //   foreach (var jpt_2 in jsPlugs) {
      //     if (jpt_1.type.IsSubclassOf(jpt_2.type)) {
      //       var jpc1 = jpt_1.jsPluginCode;
      //       var jpc2 = jpt_2.jsPluginCode;
      //       if (jpc1.pluginName == jpc2.pluginName) {
      //         onesToRemove.Add(jpt_1);
      //       }
      //     }
      //   }
      // }
      // foreach (var jpt in onesToRemove) jsPlugs.Remove(jpt); // remove the ones we don't want to write

      List<JsPluginCode> jsFilesToWrite = new();
      foreach (var jpt in jsPlugs) {
        var jsPluginMB = Singletoner.EnsureInstByType(jpt.type) as JsPlugin_Behaviour;
        jsPluginMB.WriteMyJsPluginFile( jpt.jsPluginCode );
      }
      Debug.Log($"%wh%-- JS PLUGINS TO WRITE=%wh%[{string.Join(", ", jsPlugs.Select(x=>$"%yel%{x?.name ?? "<null>"}%gy%") )}%wh%]".TagColors());
      WriteIndexOfPluginsFile( rpt.needed_Plugins.Select(x => x.jsPluginCode).ToList() );
      #if UNITY_EDITOR
        AssetDatabase.Refresh();
      #endif
    }
    //------------------------------ ||||||||||||||||||||||| ----------------------------------
    // static public Type JsPlugin_ToSceneAndFile( Type jsPluginType ) {
    //   var jsPluginMB = Singletoner.EnsureInstByType(jsPluginType) as JsPlugin_Behaviour;
    //   // Debug.Log($"{logPrefix} Ensured GameObject with a '%ye%{jsPluginType.Name}%gy%' on it.".TagColors(), jsPluginMB.gameObject);
    //   jsPluginMB.WriteMyJsPluginFile();
    //   return jsPluginMB.GetType();
    // }

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
      var jsPlugin = jsPluginMB._GetJsPluginCode();
      var modelClassPath = Mq_File.AppFolder().DeeperFile($"plugins/{jsPlugin.pluginName}.js");
      return modelClassPath.Exists();
    }
    //========== |||||||||||||| ====================
    public class JsPluginReport {
      public List<Type>                  allPluginTypes      = new();
      public List<AnalysisOfOneJsPlugin> needed_Plugins      = new();
      public List<AnalysisOfOneJsPlugin> notInScene_Plugins  = new();
      public List<AnalysisOfOneJsPlugin> inScene_Plugins     = new();
      public List<AnalysisOfOneJsPlugin> ready_Plugins       = new();
      public List<AnalysisOfOneJsPlugin> missingPart_Plugins = new();
      public string needTxt;
      public string neededOnesTxt;
      public string inScene_Txt;
      public string ready_Txt;
      public string missingPart_Txt;
      public bool   needsSomePlugins = false;
      public List<AnalysisOfOneJsPlugin> analyses = new();
      public List<SynqBehaviour> sceneSynqBehaviours = new();
    }

    public class AnalysisOfOneJsPlugin {
      public Type     type;
      public string   name;
      public GameObject gob;
      public MonoBehaviour manager;

      public string[] codeMatchesToCheck;
      public SynqBehaviour[] synqBehsWithCodeMatches;
      public bool     hasCodeMatches = false;

      public JsPluginCode jsPluginCode;
      public string jsFilePath;

      public Type[]   neededBehaviours = null;
      public bool     aSceneBehNeedsMe = false;

      public bool     isInScene = false;
      public bool     jsFilePresent = false;
      public bool     jsFileOk = false;
      public Type[] neededBehsInScene;
    }
    static public List<T> ActuallyFindObjectsOfType<T>(bool includeInactive) where T : MonoBehaviour {
      if (includeInactive) return FindObjectsOfType<T>().ToList();
      else                 return FindObjectsOfType<T>().Where(x => x.enabled).ToList();
    }
    //-------------------------- ||||||||||||||||||| ----------------------------------------
    static public JsPluginReport AnalyzeAllJsPlugins(bool dbg = true) {

      JsPluginReport rpt = new();
      var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
      // rpt.allPluginTypes = Assembly.GetExecutingAssembly().GetTypes()
      rpt.allPluginTypes.AddRange( allAssemblies.SelectMany(x => x.GetTypes())
        .Where(t => t.IsSubclassOf(typeof(JsPlugin_Behaviour))).ToList());
      if (dbg) Debug.Log($"%wh%-- ALL   %cy%JsPlugins=%wh%[{string.Join(", ", rpt.allPluginTypes.Select(x=>$"%yel%{x.Name}%gy%") )}%wh%]".TagColors());

      var inSceneComps  = FindObjectsOfType<JsPlugin_Behaviour>(false);
      var inSceneTuples = inSceneComps.Select((JsPlugin_Behaviour x) => (x.GetType(), x)).ToList();
      
      rpt.sceneSynqBehaviours = FindObjectsOfType<SynqBehaviour>(false).Where(x => x.enabled).ToList(); // false means we skip inactives

      foreach( var type in rpt.allPluginTypes) {
        AnalysisOfOneJsPlugin an = new();
        an.type = type;
        an.name = type.Name;
        an.codeMatchesToCheck = type.CallStaticMethod("CsCodeMatchesToNeedThisJs") as string[];
        an.synqBehsWithCodeMatches = rpt.sceneSynqBehaviours
          .SelectMany(beh => an.codeMatchesToCheck
            .Where(code => Regex.IsMatch(GetCodeOfBeh(beh), code)) // if any of the code matches (like [SyncVar]) are found in the beh's code, add it to the list
            .Select(code => beh)
          ).ToArray();
        an.hasCodeMatches   = an.synqBehsWithCodeMatches.Length > 0;

        an.jsPluginCode  = type.CallStaticMethod("GetJsPluginCode") as JsPluginCode;
        an.jsFilePath    = $"plugins/{an.jsPluginCode.pluginName}.js";
        an.jsFilePresent = Mq_File.AppFolder(true).DeeperFile(an.jsFilePath).Exists();
        an.jsFileOk      = an.jsFilePresent || an.jsPluginCode == null;

        an.neededBehaviours = type.CallStaticMethod("BehavioursThatNeedThisJs") as Type[] ?? new Type[0];
        an.neededBehsInScene = an.neededBehaviours.Where(x => 
          // FindObjectOfType(x, false) != null
          InSceneAndEnabled(x)
        ).ToArray();
        an.aSceneBehNeedsMe  = an.neededBehsInScene.Length > 0;
        an.manager   = inSceneComps.FirstOrDefault(x => x.GetType().Name == type.Name);
        an.isInScene = an.manager != null;
        an.gob       = an.manager?.gameObject;
        rpt.analyses.Add(an);
        string neededBehs  = string.Join(", ", an.neededBehaviours.Select(x => (
          // (FindObjectOfType(x, false) != null) ? $"%red%{x.Name}%gy%" : $"%wh%{x.Name}%gy%"
          InSceneAndEnabled(x) ? $"%wh%{x.Name}%gy%" : $"%wh%{x.Name}%gy%"
        )));
        // string neededYN = (an.aSceneBehNeedsMe || an.hasCodeMatches) ? "%gr%Needed%gy%" : "";
        string codeMatches = string.Join(", ", an.synqBehsWithCodeMatches.Select(x => x.GetType().Name));
        Debug.Log($" |  %cy%{an.name}%gy% is needed for... Behaviours:[%yel%{neededBehs}%gy%] codeMatches:[%yel%{codeMatches}%gy%]".TagColors());
      }
      rpt.needed_Plugins      = rpt.analyses.Where(x => x.aSceneBehNeedsMe || x.hasCodeMatches).ToList();
      rpt.notInScene_Plugins  = rpt.analyses.Where(x => !x.isInScene).ToList();
      rpt.inScene_Plugins     = rpt.analyses.Where(x => x.isInScene).ToList();
      
      rpt.ready_Plugins       = rpt.needed_Plugins.Where(x => x.isInScene && x.jsFileOk).ToList();
      rpt.missingPart_Plugins = rpt.needed_Plugins.Where(x => !x.isInScene || !x.jsFileOk).ToList();
      rpt.needsSomePlugins    = rpt.needed_Plugins.Count > 0;

      if (dbg) Debug.Log($"%wh%-- Scene's %cy%JsPlugins=%wh%[{string.Join(", ", inSceneTuples.Select(x=>$"%yel%{x.Item2.GetType().Name}%gy%") )}%wh%]".TagColors());

      return rpt;
    }
    //---------------- ||||||||||||||||| ----------------------------------------
    static public bool InSceneAndEnabled(Type behType) {
      var foundComponents = FindObjectsOfType(behType, false);
      // Debug.Log($"Checking {behType.Name} found %cy%{foundComponents?.Length ?? 0}%gy% components".TagColors());
      
      var enabledProperty = behType.GetProperty("enabled");
      return foundComponents?.Any(c => enabledProperty?.GetValue(c) as bool? ?? false) ?? false;
    }
    //------------------ |||||||||||| ----------------------------------------
    static public string GetCodeOfBeh(MonoBehaviour beh) {
      MonoScript script = MonoScript.FromMonoBehaviour(beh);
      return script.text;
    }
    //---------------- ||||||||||||||||| ----------------------------------------
    public static bool LogJsPluginReport(JsPluginReport rpt, bool dbg = true) {

      var rptList = new Func<List<AnalysisOfOneJsPlugin>, string>((plugins) => {
        return "[ " + string.Join(", ", plugins.Select(x => $"<color=yellow>{x.name}</color>")) + " ]";
      });
      
      var countOfCount = new Func<List<AnalysisOfOneJsPlugin>, List<AnalysisOfOneJsPlugin>, string>((A, B) => {
        return $"Count:%cy%{A.Count}%gy% of %cy%{B.Count}%gy%";
      });

      string neededOnes      = rptList(rpt.needed_Plugins      );
      string inSceneOnes     = rptList(rpt.inScene_Plugins     );
      string readyOnes       = rptList(rpt.ready_Plugins       );
      string missingPartOnes = rptList(rpt.missingPart_Plugins );
      rpt.neededOnesTxt    = $" %ye%|%gy% %cy%{rpt.needed_Plugins.Count}%gy% needed JsPlugins: {neededOnes}".TagColors();
      rpt.inScene_Txt      = $" %ye%|%gy% {countOfCount(rpt.inScene_Plugins,     rpt.needed_Plugins)} JsPlugins %gre%have%gy% an instance in scene: {inSceneOnes}".TagColors();
      rpt.ready_Txt        = $" %ye%|%gy% {countOfCount(rpt.ready_Plugins,       rpt.needed_Plugins)} JsPlugins are %gre%ready%gy% to go: {readyOnes}".TagColors();
      rpt.missingPart_Txt  = $" %ye%|%gy% {countOfCount(rpt.missingPart_Plugins, rpt.needed_Plugins)} JsPlugins are %red%MISSING%gy% a part: {missingPartOnes}".TagColors();
      rpt.needsSomePlugins = rpt.needed_Plugins.Count > 0;

      var fldr = $"<color=#ff55ff>Assets/MultisynqJS/{Mq_File.GetAppNameForOpenScene()}/plugins/</color>";
      int missingCnt = rpt.missingPart_Plugins.Count;
      int neededCnt = rpt.needed_Plugins.Count;
      bool amMissingPlugins = rpt.missingPart_Plugins.Count > 0;
      if (amMissingPlugins) {
        if (dbg) Debug.Log(rpt.neededOnesTxt);
        if (dbg) Debug.Log(rpt.inScene_Txt);
        if (dbg) Debug.Log(rpt.ready_Txt);
        // for each missing file, log the file
        foreach (var plug in rpt.missingPart_Plugins) {
          string neededBy = (plug.hasCodeMatches ? $"(code match)" : "") + (plug.aSceneBehNeedsMe ? $"(a beh)" : "");
          if (dbg) Debug.Log($"|    Missing a part: <color=#ff7777>{plug.name}</color> neededBy:{neededBy} isInScene:{plug.isInScene}, jsFile present:{plug.jsFilePresent}");
        }
        // for all ready files, log the file
        foreach (var plug in rpt.ready_Plugins) {
          if (dbg) Debug.Log($"|    Js Plugin is ready for: <color=#55ff55>{plug.name}</color>");
        }
        // if (dbg) Debug.Log(pluginRpt.missingPartOnesTxt);
        if (dbg) Debug.Log($"| <color=#ff5555>MISSING</color>  <color=cyan>{missingCnt}</color> of <color=cyan>{neededCnt}</color> JS Plugins: {rptList(rpt.missingPart_Plugins)} in {fldr}");
        if (dbg) Debug.Log($"|    <color=#55ff55>TO FIX:</color>  Add Missing JS Plugin Files, in Menu:");
        if (dbg) Debug.Log($"|    <color=white>Multisynq > Open Build Assistant Window > [Check If Ready], then [Add Missing JS Plugin Files]</color>");
      }
      else {
        if (dbg) Debug.Log($"All needed JS Plugins found in scene & in {fldr}: {rptList(rpt.needed_Plugins)}");
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