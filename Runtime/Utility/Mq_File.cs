using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
  using UnityEditor;
#endif

namespace Multisynq {


static public class Mq_File {

  static public FileThing SceneDefForApp(string appNm) {
    return new FileThing(Path.Combine(Application.streamingAssetsPath, "..", "MultisynqJS", appNm, "scene-definitions.txt"));
  }

  static public string cqSettingsAssetOutputPath = "Assets/Multisynq/Mq_Settings.asset";
  static public string pkgRootFolder = "Packages/io.multisynq.multiplayer";

  public static string ewFolder = pkgRootFolder + "/Editor/MultisynqEditorWindow/";
  public static string img_root = pkgRootFolder + "/Editor/MultisynqEditorWindow/Images/";

  static public string GetAppNameForOpenScene() {
    Mq_Bridge cb = Object.FindObjectOfType<Mq_Bridge>();
    if (cb == null) {
      Debug.LogError("Could not find Mq_Bridge in scene!");
      return null;
    }
    string appName = cb.appName;
    if (appName == null || appName == "") {
      Debug.LogError("App Name is not set in Mq_Bridge!");
      return null;
    }
    return appName;
  }

  static public FolderThing PrefabJsFolder() {
    return new FolderThing(Path.GetFullPath(pkgRootFolder + "/PrefabActorJS"));
  }

  static public string GetStarterTemplateFolder() {
    return Path.Combine(PrefabJsFolder().longPath, "templates", "starter");
  }

  static public FolderThing StarterTemplateFolder() {
    return new FolderThing(GetStarterTemplateFolder());
  }

  static public FolderThing MultisynqJS() {
    return new FolderThing("Assets/MultisynqJS/");
  }
  static public FolderThing RootFolder() {
    return new FolderThing("Assets/../");
  }

  static public FolderThing AppFolder(bool canBeMissing = false) {
    return new FolderThing("Assets/MultisynqJS/" + GetAppNameForOpenScene(), canBeMissing);
  }

  static public FolderThing AppPluginsFolder(bool canBeMissing = false) {
    return new FolderThing("Assets/MultisynqJS/" + GetAppNameForOpenScene() + "/plugins", canBeMissing);
  }

  static public FolderThing AppStreamingAssetsOutputFolder(bool canBeMissing = false) {
    return new FolderThing(Application.streamingAssetsPath + "/" + GetAppNameForOpenScene(), canBeMissing);
  }

  static public FolderThing WebGL_BuildOutputFolder( bool canBeMissing = false) {
    return new FolderThing( Application.dataPath + "/WebGLTemplates/MultisynqLoader/", canBeMissing);
  }

  static public FolderThing BuildOutputFolder(bool canBeMissing = false) {
    #if UNITY_EDITOR
      return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
              ? WebGL_BuildOutputFolder(canBeMissing) 
              : AppStreamingAssetsOutputFolder(canBeMissing);
    #else
      return AppStreamingAssetsOutputFolder(canBeMissing);
    #endif
  }

  static public FileThing AppIndexJs() {
    return new FileThing("Assets/MultisynqJS/" + GetAppNameForOpenScene() + "/index.js");
  }

  static public FolderThing PkgPrefabFolder() {
    return new FolderThing(pkgRootFolder + "/Prefabs");
  }

  static public FileThing CqSettingsTemplateFile() {
    return new FileThing( PkgPrefabFolder().shortPath + "/Mq_Settings_Template.asset");
  }

  static public FolderThing StreamingAssets_Dir() {
    return new FolderThing(Application.streamingAssetsPath);
  }
  static public FolderThing StreamingAssetsAppFolder(string _appNm = null) {
    string appNm = (_appNm != null) ? _appNm : GetAppNameForOpenScene();
    if (appNm == null) {
      Debug.LogError("Could not find App Name in Mq_Bridge!");
      return MakeBlank();
    }
    var ft = new FolderThing(Path.Combine(Application.streamingAssetsPath, appNm), true);
    // Debug.Log($"StreamingAssetsAppFolder: {ft.shortPath}");
    return ft;
  }

  static public FolderThing MakeBlank() {
    return new FolderThing("");
  }

  static public FileThing AddAppNameOutputMarker(string appName) {
    // add a "MyFolderIsMultisynqBuildOutput.txt" file to the folder to mark it as a Multisynq output folder
    FileThing markerFile = StreamingAssetsAppFolder(appName).DeeperFile("MyFolderIsMultisynqBuildOutput.txt");
    markerFile.WriteAllText("This file marks that its containing folder is a Multisynq build output folder.\nDo not delete, please.\nThanks!");
    return markerFile;
  }

  static public bool AppNameOutputFolderHasMarker(string appName) {
    FileThing markerFile = StreamingAssetsAppFolder(appName).DeeperFile("MyFolderIsMultisynqBuildOutput.txt");
    return markerFile.Exists();
  }

  static public List<FolderThing> ListAppNameOutputFolders() {
    var dirs = new FolderThing(Application.streamingAssetsPath).ChildFolders();
    // filter out non-Multisynq output folders without MyFolderIsMultisynqBuildOutput.txt using Linq
    return dirs.Where(dir => dir.DeeperFile("MyFolderIsMultisynqBuildOutput.txt").Exists()).ToList();
  }
  static public NodeModules NodeModules { get; set; }
}

public class NodeModules {
  
  public FolderThing folder;
  public NodeModules() {
    folder = new FolderThing(Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "..", "MultisynqJS", "node_modules")));
    var folder2 = new FolderThing(Path.GetFullPath(Path.Combine(Application.dataPath, "MultisynqJS", "node_modules")));
    // throw if different
    if (folder.longPath != folder2.longPath) {
      throw new System.Exception($"Mq_File.NodeModules(): folder.longPath != folder2.longPath\nfolder: '{folder.longPath}'\nfolder2: '{folder2.longPath}'");
    }
  }
  public FileThing NodeDataChannel_File() {
    return folder.DeeperFile("node-datachannel", "build", "Release", "node_datachannel.node");
  }
}

}