#nullable enable
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

abstract public class JsCodeInjectingMonoBehavior : MonoBehaviour {

  abstract public void InjectJsCode();
}

public class JsCodeInjectingMgr : MonoBehaviourSingleton<JsCodeInjectingMgr> {

  // string myAppIndexJsFile;
  static public string logPrefix = "[<color=yellow>Js</color><color=cyan>CodeInject</color>]";

  public void InjectAllJsCode() {
    foreach (var jci in FindObjectsOfType<JsCodeInjectingMonoBehavior>()) {
      jci.InjectJsCode();
    }
  }

  public void InjectCode(string fileNm, string classCode, string initCode) {
    
    CqFile.AppFolder().DeeperFile(fileNm).WriteAllText(classCode, true); // true = create needed folders
    string indexJsPath  = CqFile.AppIndexJs().longPath;
    string indexContent = File.ReadAllText(indexJsPath);
    
    string? modelClassName = JavaScriptParser.FindFunctionParamN(indexContent, "StartSession", 1);

    if (string.IsNullOrEmpty(modelClassName)) {
        Debug.Log("Could not find model class name in index.js");
        return;
    }

    var indexParser = new JavaScriptParser(indexContent);
    string? modelJsName = indexParser.FindFileOfImported(modelClassName);
    string modelJsPath = modelJsName != null 
      ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(indexJsPath) ?? "", modelJsName))
      : indexJsPath;  // If not found in imports, assume it's in the index.js file

    string modelContent = File.ReadAllText(modelJsPath);
    var modelParser = new JavaScriptParser(modelContent);
    CodeBlock? initMethod = modelParser.FindMethodInClass("init", modelClassName, "GameModelRoot");

    if (initMethod != null) {
        // string codeToInsert = "    this.syncer = SyncVarActor.create({});\n";
        string modifiedContent = modelParser.InsertCodeIntoMethod(initMethod, initCode);
        UnityEngine.Debug.Log($"Modified content: {modifiedContent.Trim()}");
        // File.WriteAllText(modelJsPath, modifiedContent);
        // Debug.Log($"Successfully modified {modelJsPath}");
    }  else {
        Debug.Log($"Could not find init method in class {modelClassName}");
    }
  }

}
