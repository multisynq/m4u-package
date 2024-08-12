

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static public class CqProject {

  static public void EnsureAssetsFolder(string folder) {
    string croquetFolder = Path.Combine("Assets", folder);
    if (!AssetDatabase.IsValidFolder(croquetFolder)) {
      AssetDatabase.CreateFolder("Assets", folder);
    }
  }



  static public void RenameToUnHideAppNameOutputFolders() {
    foreach (FolderThing dir in CqFile.ListAppNameOutputFolders()) {
      if (dir.shortPath.Contains("~")) {
        string newName = dir.shortPath.Replace("~", "");
        AssetDatabase.RenameAsset(dir.shortPath, newName);
      }
    }
  }
  static public void RenameToHideAppNameOutputFoldersExceptOne(string appName) {
    foreach (FolderThing dir in CqFile.ListAppNameOutputFolders()) {
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

  static public CroquetSettings CopyDefaultSettingsFile() {
    // string path = ewFolder + "resources/CroquetSettings_Template.asset";
    // croquet-for-unity-package/Prefabs/CroquetSettings_Template.asset
    string path = CqFile.CqSettingsTemplateFile().shortPath;
    EnsureAssetsFolder("Croquet");
    Debug.Log($"Copying from '{path}' to '{CqFile.cqSettingsAssetOutputPath}'");
    bool sourceFileExists = File.Exists(path);
    bool targFolderExists = Directory.Exists(Path.GetDirectoryName(CqFile.cqSettingsAssetOutputPath));
    AssetDatabase.CopyAsset(path, CqFile.cqSettingsAssetOutputPath);
    bool copiedFileExists = File.Exists(CqFile.cqSettingsAssetOutputPath);
    Debug.Log($"Source file exists: {sourceFileExists}  Target folder exists: {targFolderExists}, Copied file exists: {copiedFileExists}");
    return AssetDatabase.LoadAssetAtPath<CroquetSettings>(CqFile.cqSettingsAssetOutputPath);
  }

  static public CroquetSettings EnsureSettingsFile() {
    CroquetSettings cqSettings = StatusSetMgr.FindProjectCqSettings();
    // If not, copy file from ./resources/CroquetSettings_Template.asset
    // into Assets/Settings/CroquetSettings.asset
    if (cqSettings == null) cqSettings = CopyDefaultSettingsFile();
    return cqSettings;
  }

}

