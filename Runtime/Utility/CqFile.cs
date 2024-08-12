using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

static public class CqFile {

  static public FileThing SceneDefForApp(string appNm) {
    return new FileThing(Path.Combine(Application.streamingAssetsPath, "..", "CroquetJS", appNm, "scene-definitions.txt"));
  }

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
    markerFile.WriteAllText("This file marks that its containing folder is a Croquet build output folder.\nDo not delete, please.\nThanks!");
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

}
