#nullable enable
using System;
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
    
    /* 
      Given file "index.js" with:
        import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
        import { MyModelRoot } from "./Models";
        StartSession(MyModelRoot, GameViewRoot);

      and file: "./Models" with:
        export class MyModelRoot extends GameModelRoot {
          init() {
            super.init();
          }
        }

      (1) Finds the first parameter of the StartSession() function, which is the model class name (here "MyModelRoot")
      (2) Finds the file that imports the model class (here "./Models")
      (3) If not found in imports, assumes it's here in the "index.js" file
      (4) Reads the content of the model class file
      (5) Finds the init method in a class that extends GameModelRoot, and
      (6) inserts the initCode into the init() method
    */

  // try {
      // (1) Find the first parameter of the StartSession() function
      // string indexJsPath = CqFile.AppIndexJs().longPath;
      // string indexContent = File.ReadAllText(indexJsPath);

      string? modelClassName = JavaScriptParser.FindFunctionParamN(indexContent, "StartSession", 1);

      if (string.IsNullOrEmpty(modelClassName)) {
        throw new Exception("Could not find model class name in index.js");
      }

      Debug.Log($"Found model class name: {modelClassName}");

      // (2) Find the file that imports the model class
      var indexParser = new JavaScriptParser(indexContent);
      string? modelJsName = indexParser.FindFileOfImported(modelClassName);

      // (3) If not found in imports, assume it's in the index.js file
      string modelJsPath;
      if (modelJsName != null) {
        modelJsPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(indexJsPath) ?? "", modelJsName+".js"));
        // check if it exists
        if (!File.Exists(modelJsPath)) {
          throw new Exception($"Model class file not found: {modelJsPath}");
        }
      } else {
        modelJsPath = indexJsPath;
        Debug.Log($"Model class not found in imports, assuming it's in index.js");
      }

      // (4) Read the content of the model class file
      string modelContent = File.ReadAllText(modelJsPath);
      Debug.Log("Successfully read model content: "+modelContent);

      // (5) Find the init method in a class that extends GameModelRoot
      var modelParser = new JavaScriptParser(modelContent);
      Debug.Log($"Attempting to find {modelClassName} extends GameModelRoot .init() in {modelJsPath}");
      CodeBlock? initMethod = modelParser.FindMethodInClass("init", modelClassName, "GameModelRoot");

      if (initMethod == null) {
        throw new Exception($"Could not find init method in class {modelClassName}");
      }

      Debug.Log("Found init method");

      // (6) Insert the initCode into the init() method
      string codeToInsert = "    this.syncer = SyncVarActor.create({});\n";
      string modifiedContent = modelParser.InsertCodeIntoMethod(initMethod, codeToInsert);

      Debug.Log("Modified content:");
      Debug.Log(modifiedContent.Trim());

      // Write the modified content back to the file
      File.WriteAllText(modelJsPath, modifiedContent);
      Debug.Log($"Successfully updated {modelJsPath}");

      // Optionally, append the SyncVarActor class if it doesn't exist
      if (!modelContent.Contains("class SyncVarActor extends Actor")) {
        string updatedContent = modelParser.AppendSyncVarActorClass();
        File.WriteAllText(modelJsPath, updatedContent);
        Debug.Log("Appended SyncVarActor class to the file");
      }
    // }
    // catch (Exception ex) {
    //   Debug.Log($"An error occurred: {ex.Message}");
    //   Debug.Log(ex.StackTrace);
    // }
  }
}
