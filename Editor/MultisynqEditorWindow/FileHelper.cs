using UnityEngine;
using System.IO;

static public class FileHelper {

  static public string GetAppNameForOpenScene() {
    CroquetBridge cb = Object.FindObjectOfType<CroquetBridge>();
    if (cb == null) {
      MultisynqBuildAssistantEW.NotifyAndLogError("Could not find CroquetBridge in scene!");
      return null;
    }
    string appName = cb.appName;
    if (appName == null || appName == "") {
      MultisynqBuildAssistantEW.NotifyAndLogError("App Name is not set in CroquetBridge!");
      return null;
    }
    return appName;
  }

  static public string GetCroquetJSFolder(bool isShortPath = true) {
    if (isShortPath) { 
      return "Assets/CroquetJS/"; 
    } else { 
      return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS")); 
    }
  }

  static public string GetAppNameFolderForOpenScene(bool isShortPath = true) {
    string appName = GetAppNameForOpenScene();
    if (appName == null || appName == "") {
      MultisynqBuildAssistantEW.NotifyAndLogError("Could not find App Name in CroquetBridge!");
      return null;
    }
    string croquetJSFolder = GetCroquetJSFolder(isShortPath);
    return Path.Combine(croquetJSFolder, appName);
  }

  static public string GetAppNamePathForOpenScene(bool isShortPath = true) {
    return GetAppNameFolderForOpenScene(isShortPath) + "/index.js";
  }

  static public string GetPrefabJsFolder() {
    return Path.GetFullPath("Packages/io.croquet.multiplayer/PrefabActorJS");
  }

  static public string GetStarterTemplateFolder() {
    return Path.Combine(GetPrefabJsFolder(), "templates", "starter");
  }

}
