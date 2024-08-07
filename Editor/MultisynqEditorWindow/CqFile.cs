using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

static public class CqFile {

  static public string cqSettingsAssetOutputPath = "Assets/Croquet/CroquetSettings_XXXXXXXX.asset";
  static public string pkgRootFolder = "Packages/io.croquet.multiplayer";

  public static string ewFolder = pkgRootFolder + "/Editor/MultisynqEditorWindow/";
  public static string img_root = pkgRootFolder + "/Editor/MultisynqEditorWindow/Images/";

  static public string GetAppNameForOpenScene() {
    CroquetBridge cb = Object.FindObjectOfType<CroquetBridge>();
    if (cb == null) {
      Debug.LogError("Could not find CroquetBridge in scene!");
      return null;
    }
    string appName = cb.appName;
    if (appName == null || appName == "") {
      Debug.LogError("App Name is not set in CroquetBridge!");
      return null;
    }
    return appName;
  }

  static public void EnsureAssetsFolder(string folder) {
    string croquetFolder = Path.Combine("Assets", folder);
    if (!AssetDatabase.IsValidFolder(croquetFolder)) {
      AssetDatabase.CreateFolder("Assets", folder);
    }
  }
  // static public string GetCroquetJSFolder(bool isShortPath = true) {
  //   if (isShortPath) { 
  //     return "Assets/CroquetJS/"; 
  //   } else { 
  //     return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS")); 
  //   }
  // }

  // static public string GetAppNameFolderForOpenScene(bool isShortPath = true) {
  //   string appName = GetAppNameForOpenScene();
  //   if (appName == null || appName == "") {
  //     MultisynqBuildAssistantEW.NotifyAndLogError("Could not find App Name in CroquetBridge!");
  //     return null;
  //   }
  //   string croquetJSFolder = GetCroquetJSFolder(isShortPath);
  //   return Path.Combine(croquetJSFolder, appName);
  // }

  // static public string GetAppNamePathForOpenScene(bool isShortPath = true) {
  //   return GetAppNameFolderForOpenScene(isShortPath) + "/index.js";
  // }

  static public FolderThing PrefabJsFolder() {
    return new FolderThing(Path.GetFullPath(pkgRootFolder + "/PrefabActorJS"));
  }

  static public string GetStarterTemplateFolder() {
    return Path.Combine(PrefabJsFolder().longPath, "templates", "starter");
  }
  static public FolderThing StarterTemplateFolder() {
    return new FolderThing(GetStarterTemplateFolder());
  }
  static public FolderThing CroquetJS() {
    return new FolderThing("Assets/CroquetJS/");
  }
  static public FolderThing AppFolder(bool canBeMissing = false) {
    return new FolderThing("Assets/CroquetJS/" + GetAppNameForOpenScene(), canBeMissing);
  }
  static public FolderThing AppStreamingAssetsOutputFolder(bool canBeMissing = false) {
    return new FolderThing(Application.streamingAssetsPath + "/" + GetAppNameForOpenScene(), canBeMissing);
  }
  static public FileThing AppIndexJs() {
    return new FileThing("Assets/CroquetJS/" + GetAppNameForOpenScene() + "/index.js");
  }
  static public FolderThing PkgPrefabFolder() {
    return new FolderThing(pkgRootFolder + "/Prefabs");
  }
  static public FileThing CqSettingsTemplateFile() {
    return new FileThing( PkgPrefabFolder().shortPath + "/CroquetSettings_Template.asset");
  }
  static public FolderThing StreamingAssetsAppFolder(string _appNm = null) {
    string appNm = (_appNm != null) ? _appNm : GetAppNameForOpenScene();
    if (appNm == null) {
      Debug.LogError("Could not find App Name in CroquetBridge!");
      return MakeBlank();
    }
    var ft = new FolderThing(Path.Combine(Application.streamingAssetsPath, appNm));
    // Debug.Log($"StreamingAssetsAppFolder: {ft.shortPath}");
    return ft;
  }
  static public FolderThing MakeBlank() {
    return new FolderThing("");
  }
  static public FileThing AddAppNameOutputMarker(string appName) {
    // add a "MyFolderIsCroquetBuildOutput.txt" file to the folder to mark it as a Croquet output folder
    FileThing markerFile = StreamingAssetsAppFolder(appName).DeeperFile("MyFolderIsCroquetBuildOutput.txt");
    markerFile.MakeFile("This file marks that its containing folder is a Croquet build output folder.\nDo not delete, please.\nThanks!");
    return markerFile;
  }
  static public bool AppNameOutputFolderHasMarker(string appName) {
    FileThing markerFile = StreamingAssetsAppFolder(appName).DeeperFile("MyFolderIsCroquetBuildOutput.txt");
    return markerFile.Exists();
  }
  static public List<FolderThing> ListAppNameOutputFolders() {
    var dirs = new FolderThing(Application.streamingAssetsPath).ChildFolders();
    // filter out non-Croquet output folders without MyFolderIsCroquetBuildOutput.txt using Linq
    return dirs.Where(dir => dir.DeeperFile("MyFolderIsCroquetBuildOutput.txt").Exists()).ToList();
  }
  static public void RenameToUnHideAppNameOutputFolders() {
    foreach (FolderThing dir in ListAppNameOutputFolders()) {
      if (dir.shortPath.Contains("~")) {
        string newName = dir.shortPath.Replace("~", "");
        AssetDatabase.RenameAsset(dir.shortPath, newName);
      }
    }
  }
  static public void RenameToHideAppNameOutputFoldersExceptOne(string appName) {
    foreach (FolderThing dir in ListAppNameOutputFolders()) {
      if (dir.shortPath.Contains(appName)) {
        dir.SelectAndPing();
      } else {
        string newName = dir.shortPath + "~";
        AssetDatabase.RenameAsset(dir.shortPath, newName);
      }
    }
  }
  static public bool AllScenesHaveBridgeWithAppNameSet() {
      string[] buildingScenes = EditorBuildSettings.scenes.Where( s => s.enabled ).Select( s => s.path ).ToArray();
      // load each scene in the list and get its CroquetBridge
      foreach (string scenePath in buildingScenes) {
        EditorSceneManager.OpenScene(scenePath);
        var bridge = Object.FindObjectOfType<CroquetBridge>();
        if (bridge == null) {
          Debug.LogError("Could not find CroquetBridge in scene: " + scenePath);
          return false;
        } else {
          // grab the appName from the CroquetBridge and make sure there is a folder for it in the StreamingAssets folder
          string appName = bridge.appName;
          if (appName == "") {
            Debug.LogError("CroquetBridge in scene: " + scenePath + " has no appName set.");
            return false;
          } else {
            var appFolder = CqFile.StreamingAssetsAppFolder(appName);
            if (!appFolder.Exists()) {
              Debug.LogError("Could not find app folder: " + appFolder);
              return false;
            }
          }
        }
      }
      if (buildingScenes.Length == 0) {
        Debug.LogError("No scenes in Build Settings.\nAdd some scenes to build.");
        return false;
      } else {
        Debug.Log("Yay! >> All scenes have CroquetBridge with appName set and app folder in StreamingAssets.");
        return true;
      }

  }

  //=============================================================================
  static public CroquetSettings FindProjectCqSettings() {
    // First check for a CroquetSettings on the scene's CroquetBridge
    var bridge = SceneHelp.FindComp<CroquetBridge>();
    if (bridge != null && bridge.appProperties != null) {
      return bridge.appProperties; // appProperties is a CroquetSettings
    }
    // Then look in all project folders for a file of CroquetSettings type
    CroquetSettings cqSettings = SceneHelp.FindCompInProject<CroquetSettings>();
    if (cqSettings == null) {
      Debug.LogWarning("Could not find CroquetSettings.asset in your Assets folders.");
      MqWelcome_StatusSets.settings.error.Set();
      MqWelcome_StatusSets.node.error.Set();
      MqWelcome_StatusSets.apiKey.error.Set();
      MqWelcome_StatusSets.ready.error.Set();
    }

    return cqSettings;
  }

  static public CroquetSettings CopyDefaultSettingsFile() {
    // string path = ewFolder + "resources/CroquetSettings_Template.asset";
    // croquet-for-unity-package/Prefabs/CroquetSettings_Template.asset
    string path = CqFile.CqSettingsTemplateFile().shortPath;
    CqFile.EnsureAssetsFolder("Croquet");
    Debug.Log($"Copying from '{path}' to '{CqFile.cqSettingsAssetOutputPath}'");
    bool sourceFileExists = File.Exists(path);
    bool targFolderExists = Directory.Exists(Path.GetDirectoryName(CqFile.cqSettingsAssetOutputPath));
    AssetDatabase.CopyAsset(path, CqFile.cqSettingsAssetOutputPath);
    bool copiedFileExists = File.Exists(CqFile.cqSettingsAssetOutputPath);
    Debug.Log($"Source file exists: {sourceFileExists}  Target folder exists: {targFolderExists}, Copied file exists: {copiedFileExists}");
    return AssetDatabase.LoadAssetAtPath<CroquetSettings>(CqFile.cqSettingsAssetOutputPath);
  }

  static public CroquetSettings EnsureSettingsFile() {
    CroquetSettings cqSettings = FindProjectCqSettings();
    // If not, copy file from ./resources/CroquetSettings_Template.asset
    // into Assets/Settings/CroquetSettings.asset
    if (cqSettings == null) cqSettings = CopyDefaultSettingsFile();
    return cqSettings;
  }
}
