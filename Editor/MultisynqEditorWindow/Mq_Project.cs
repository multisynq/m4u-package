

using System.Net;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Multisynq;

static public class Mq_Project {

  static public void EnsureAssetsFolder(string folder) {
    string assetsFolder = Path.Combine("Assets", folder);
    if (!AssetDatabase.IsValidFolder(assetsFolder)) {
      AssetDatabase.CreateFolder("Assets", folder);
    }
  }



  static public void RenameToUnHideAppNameOutputFolders() {
    foreach (FolderThing dir in Mq_File.ListAppNameOutputFolders()) {
      if (dir.shortPath.Contains("~")) {
        string newName = dir.shortPath.Replace("~", "");
        AssetDatabase.RenameAsset(dir.shortPath, newName);
      }
    }
  }
  static public void RenameToHideAppNameOutputFoldersExceptOne(string appName) {
    foreach (FolderThing dir in Mq_File.ListAppNameOutputFolders()) {
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
    // load each scene in the list and get its Mq_Bridge
    foreach (string scenePath in buildingScenes) {
      EditorSceneManager.OpenScene(scenePath);
      var bridge = Object.FindObjectOfType<Mq_Bridge>();
      if (bridge == null) {
        Debug.LogError("Could not find Mq_Bridge in scene: " + scenePath);
        return false;
      } else {
        // grab the appName from the Mq_Bridge and make sure there is a folder for it in the StreamingAssets folder
        string appName = bridge.appName;
        if (appName == "") {
          Debug.LogError("Mq_Bridge in scene: " + scenePath + " has no appName set.");
          return false;
        } else {
          var appFolder = Mq_File.StreamingAssetsAppFolder(appName);
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
      Debug.Log("Yay! >> All scenes have Mq_Bridge with appName set and app folder in StreamingAssets.");
      return true;
    }
  }

  static public Mq_Settings CopyDefaultSettingsFile() {
    // string path = ewFolder + "resources/Mq_Settings_Template.asset";
    // croquet-for-unity-package/Prefabs/Mq_Settings_Template.asset
    string path = Mq_File.CqSettingsTemplateFile().shortPath;
    EnsureAssetsFolder("Multisynq");
    Debug.Log($"Copying from '{path}' to '{Mq_File.cqSettingsAssetOutputPath}'");
    bool sourceFileExists = File.Exists(path);
    bool targFolderExists = Directory.Exists(Path.GetDirectoryName(Mq_File.cqSettingsAssetOutputPath));

    // #if UNITY_EDITOR_WIN
      try {
        string outputPath = Path.GetFullPath(Mq_File.cqSettingsAssetOutputPath);
        File.Copy(path, outputPath, true);
        AssetDatabase.Refresh();
      } catch (System.Exception e) {
        Debug.LogError($"Error copying file: {e.Message}");
        return null;
      }
    // #else
    //   AssetDatabase.CopyAsset(path, Mq_File.cqSettingsAssetOutputPath);
    // #endif

    bool copiedFileExists = File.Exists(Mq_File.cqSettingsAssetOutputPath);
    Debug.Log($"Source file exists: {sourceFileExists}  Target folder exists: {targFolderExists}, Copied file exists: {copiedFileExists}");
    return AssetDatabase.LoadAssetAtPath<Mq_Settings>(Mq_File.cqSettingsAssetOutputPath);
  }

  static public Mq_Settings EnsureSettingsFile() {
    Mq_Settings cqSettings = StatusSetMgr.FindProjectCqSettings();
    // If not, copy file from ./resources/Mq_Settings_Template.asset
    // into Assets/Settings/Mq_Settings.asset
    if (cqSettings == null) cqSettings = CopyDefaultSettingsFile();
    return cqSettings;
  }

}

