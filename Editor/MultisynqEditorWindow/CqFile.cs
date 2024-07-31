using UnityEngine;
using UnityEditor;
using System.IO;

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
  static public FileThing AppIndexJs() {
    return new FileThing("Assets/CroquetJS/" + GetAppNameForOpenScene() + "/index.js");
  }
  static public FolderThing PkgPrefabFolder() {
    return new FolderThing(pkgRootFolder + "/Prefabs");
  }
  static public FileThing CqSettingsTemplateFile() {
    return new FileThing( PkgPrefabFolder().shortPath + "/CroquetSettings_Template.asset");
  }
  static public FolderThing StreamingAssetsAppFolder() {
    string appNm = GetAppNameForOpenScene();
    if (appNm == null) {
      Debug.LogError("Could not find App Name in CroquetBridge!");
      return MakeBlank();
    }
    var ft = new FolderThing(Path.Combine(Application.streamingAssetsPath, appNm));
    Debug.Log($"StreamingAssetsAppFolder: {ft.shortPath}");
    return ft;
  }
  static public FolderThing MakeBlank() {
    return new FolderThing("");
  }
}

//=================== |||||||||| ====================
public abstract class PathyThing {

  public string shortPath;
  public string longPath;
  public string folderShort;
  public string folderLong;

  public UnityEngine.Object unityObj;

  public PathyThing(string maybeShortPath) {
    // if it contains Assets/ or Packages/, strip back to that
    string projectFolder = Path.GetFullPath(Application.dataPath + "/..");
    // use replace to remove prefix
    string _shortPath = maybeShortPath.Replace(projectFolder+"/", "");
    // if (!_shortPath.StartsWith("Assets/") && !maybeShortPath.StartsWith("Packages/")) {
    //   MultisynqBuildAssistantEW.NotifyAndLogError($"Got '{maybeShortPath}'. Path must start with 'Assets/' or 'Packages/'");
    //   return;
    // }
    bool isBlank = (_shortPath == "");
    shortPath    = _shortPath;
    longPath     = (isBlank) ? "" : Path.GetFullPath(shortPath);
    Debug.Log($"PathyThing: shortPath: {shortPath} longPath: {longPath}");
    folderShort  = (isBlank) ? "" : Path.GetDirectoryName(shortPath);
    folderLong   = (isBlank) ? "" : Path.GetFullPath(folderShort);
  }

  abstract public bool Exists();

  public void LookupUnityObj() {
    if (unityObj != null) return;
    unityObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shortPath);
    if (unityObj == null) {
      Debug.LogError("PathyThing: unityObj is null for " + shortPath);
    }
  }

  public bool Select() {
    LookupUnityObj();
    Selection.activeObject = unityObj;
    EditorUtility.FocusProjectWindow();
    return true;
  }

  public void SelectAndPing() {
    if (Select()) {
      EditorGUIUtility.PingObject(unityObj);
    }
  }
}

//========== ||||||||||| ====================
public class FolderThing : PathyThing {

  public FolderThing(string _shortPath, bool canBeMissing = false) : base(_shortPath) {
    bool isValidAstDbFolder = AssetDatabase.IsValidFolder(shortPath);
    if (isValidAstDbFolder) return;
    if (Directory.Exists(longPath)) return;
    if (!canBeMissing) Debug.LogError($"Got '{shortPath}' path must be a valid folder\n longPath='{longPath}'");
  }

  override public bool Exists() {
    bool doesExist =  Directory.Exists(longPath);
    if (!doesExist) {
      Debug.LogError($"FolderThing: does not exist: '{longPath}'");
    }
    return doesExist;
  }

  // Deeper sub-folder
  public FolderThing DeeperFolder(params string[] deeperPaths) {    
    string newPath = longPath;
    foreach (string deeperPath in deeperPaths) {
      newPath = Path.Combine(newPath, deeperPath);
    }
    return new FolderThing(newPath);
  }
  // file in this folder
  public FileThing DeeperFile(params string[] file) {
    // return new FileThing(Path.Combine(longPath, file));
    // do the equivalent of JS ...file
    string newPath = longPath;
    foreach (string deeperPath in file) {
      newPath = Path.Combine(newPath, deeperPath);
    }
    return new FileThing(newPath);
  }

  public FileThing FirstFile() {
    string[] files = Directory.GetFiles(longPath);
    if (files.Length == 0) {
      MultisynqBuildAssistantEW.NotifyAndLogError("FolderThing: no files in folder");
      return null;
    }
    return new FileThing(files[0]);
  }
}
//========== ||||||||| ====================
public class FileThing : PathyThing {

  public FileThing(string _shortPath) : base(_shortPath) {
    if (AssetDatabase.IsValidFolder(shortPath)) {
      MultisynqBuildAssistantEW.NotifyAndLogError("FileThing: path must be a file, not a folder");
    }
    unityObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shortPath);
  }

  override public bool Exists() {
    bool doesExist = File.Exists(longPath);
    if (!doesExist) {
      Debug.LogError($"FileThing: does not exist: '{longPath}'");
    }
    return doesExist;
  }
}