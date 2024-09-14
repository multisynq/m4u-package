using UnityEngine;
using System.IO;
#if UNITY_EDITOR
  using UnityEditor;
#endif

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
    // Debug.Log($"PathyThing: shortPath: {shortPath} longPath: {longPath}");
    folderShort  = (isBlank) ? "" : Path.GetDirectoryName(shortPath);
    folderLong   = (isBlank) ? "" : Path.GetFullPath(folderShort);
  }

  abstract public bool Exists();

  public void LookupUnityObj() {
    #if UNITY_EDITOR //# # # # # # # # # # # # # # # # # # # # # # #
      if (unityObj != null) return;
      unityObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shortPath);
      if (unityObj == null) {
        Debug.LogWarning("PathyThing: unityObj is null for " + shortPath);
      }
    #else
      Debug.LogWarning("PathyThing: LookupUnityObj only works in editor");
    #endif //# # # # # # # # # # # # # # # # # # # # # # # # # # # #
  }

  public bool Select( bool focus = true) {
    LookupUnityObj();

    #if UNITY_EDITOR
      Selection.activeObject = unityObj;
      if (focus) EditorUtility.FocusProjectWindow();
    #endif

    return true;
  }

  public void SelectAndPing(bool focus = true) {
    #if UNITY_EDITOR
      if (Select(focus)) {
        EditorGUIUtility.PingObject(unityObj);
      }
    #endif
  }
}

//========== ||||||||||| ====================
public class FolderThing : PathyThing {

  public FolderThing(string _shortPath, bool canBeMissing = false) : base(_shortPath) {
    #if UNITY_EDITOR
      bool isValidAstDbFolder = AssetDatabase.IsValidFolder(shortPath);
      if (isValidAstDbFolder) return;
      if (Directory.Exists(longPath)) return;
      if (!canBeMissing) Debug.LogWarning($"Got '{shortPath}' path must be a valid folder\n longPath='{longPath}'");
    #endif
  }

  override public bool Exists() {
    bool doesExist =  Directory.Exists(longPath);
    if (!doesExist) {
      Debug.LogWarning($"FolderThing: does not exist: '{longPath}'");
    }
    return doesExist;
  }

  public FolderThing[] ChildFolders() {
    string[] dirs = Directory.GetDirectories(longPath);
    FolderThing[] folders = new FolderThing[dirs.Length];
    for (int i = 0; i < dirs.Length; i++) {
      folders[i] = new FolderThing(dirs[i]);
    }
    return folders;
  }

  // Deeper sub-folder
  public FolderThing DeeperFolder(params string[] deeperPaths) {    
    string newPath = longPath;
    foreach (string deeperPath in deeperPaths) {
      newPath = Path.Combine(newPath, deeperPath);
    }
    return new FolderThing(newPath);
  }
  public FolderThing EnsureExists() {
    if (!Directory.Exists(longPath)) {
      Directory.CreateDirectory(longPath);
      #if UNITY_EDITOR
        AssetDatabase.Refresh();
      #endif
    }
    return this;
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
      Debug.LogWarning("FolderThing: no files in folder");
      return null;
    }
    // skip files that start with .
    for (int i = 0; i < files.Length; i++) {
      var justFileNamePart = Path.GetFileName(files[i]);
      if (justFileNamePart.StartsWith(".")) continue;
      if (justFileNamePart.EndsWith(".meta")) continue;
      return new FileThing(files[i]);
    }
    return null;
  }
}
//========== ||||||||| ====================
public class FileThing : PathyThing {

  public FileThing(string _shortPath) : base(_shortPath) {
    #if UNITY_EDITOR
      if (AssetDatabase.IsValidFolder(shortPath)) {
        Debug.LogWarning("FileThing: path must be a file, not a folder");
      }
      unityObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shortPath);
    #endif
  }

  override public bool Exists() {
    bool doesExist = File.Exists(longPath);
    if (!doesExist) {
      Debug.LogWarning($"FileThing: does not exist: '{longPath}'");
    }
    return doesExist;
  }
  public bool WriteAllText(string txt, bool ensureFolders = false) {
    if (ensureFolders) {
      string folder = Path.GetDirectoryName(longPath);
      // check if folder exists
      if (!Directory.Exists(folder)) {
        Directory.CreateDirectory(folder);
        #if UNITY_EDITOR
          AssetDatabase.Refresh();
        #endif
      }
    }
    File.WriteAllText(longPath, txt);
    return Exists();
  }
  public string ReadAllText() {
    return File.ReadAllText(longPath);
  }
}