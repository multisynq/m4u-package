using System.Text.RegularExpressions;
using UnityEngine;

abstract public class JsCodeInjectingMonoBehavior : MonoBehaviour {

  abstract public void InjectJsCode();
}

public class JsCodeInjectingMgr : MonoBehaviourSingleton<JsCodeInjectingMgr> {

  string myAppIndexJsFile;
  static public string logPrefix = "[<color=yellow>Js</color><color=cyan>CodeInject</color>]";

  public void InjectAllJsCode() {
    foreach (var jci in FindObjectsOfType<JsCodeInjectingMonoBehavior>()) {
      jci.InjectJsCode();
    }
  }
  public void InjectCode(string classCode, string initCode) {
    string indexJsTxt = CqFile.AppIndexJs().ReadAllText();
    /* Example file index.js:

    import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
    import { MyModelRoot } from "./Models";

    StartSession(MyModelRoot, GameViewRoot);
    */
    // use regex to find the first param of StartSession
    string pattern = @"StartSession\(([^,]+),";
    Match match = Regex.Match(indexJsTxt, pattern);
    string modelRootClassNm = match.Groups[1].Value.Trim() + ".js";

    // find the file imported from to get modelRootClassNm in an import statement
    pattern = $@"import.*{modelRootClassNm}.*""([^""]+)""";
    match = Regex.Match(indexJsTxt, pattern);
    FileThing modelRootFile;
    bool modelInIndexJs = false;
    if (match.Success) {
      string modelRootFileNm = match.Groups[1].Value.Trim();
      modelRootFile = CqFile.AppFolder().DeeperFile(modelRootFileNm);
    } else {
      // if no match, the code might be in the same file
      modelRootFile = CqFile.AppIndexJs();
      modelInIndexJs = true;
    }
    string modelRootTxt = modelRootFile.ReadAllText();
      

    /* Example code for ModelRoot.js
      export class MyModelRoot extends GameModelRoot {

        static modelServices() {
          return [MyUserManager, ...super.modelServices()];
        }

        init(options) {
          super.init(options);
          this.syncer = SyncVarActor.create({});
          this.base = BaseActor.create();
        }
      }
    */
    // find the full class code of the class that extends GameModelRoot including newlines
    // make sure to have all newlines in the pattern
    pattern = @"export\s+class\s+\w+\s+extends\s+GameModelRoot\s+\{\s+.*\s+\}";
    match = Regex.Match(modelRootTxt, pattern, RegexOptions.Multiline);
    Debug.Log($"{logPrefix} GameModelRoot: {match.Value}");
    // TODO: more regex and replace until the 
  }

}
