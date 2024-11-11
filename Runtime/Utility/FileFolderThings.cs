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
  public bool canBeMissing = false;

  public UnityEngine.Object unityObj;

  public PathyThing(string inputPath) {
    string projectFolder = Path.GetFullPath(Application.dataPath + "/..");
    string fullPath = (inputPath=="") ? "" :Path.GetFullPath(inputPath);
    
    bool isProjectPath = fullPath.StartsWith(projectFolder);
    bool isAbsolutePath = Path.IsPathRooted(inputPath);
    bool isAboveProject = inputPath.Contains("../") || 
                          inputPath.Contains(".." + Path.DirectorySeparatorChar) ||
                          !fullPath.StartsWith(Path.Combine(projectFolder, "Assets")) &&
                          !fullPath.StartsWith(Path.Combine(projectFolder, "Packages"));

    string _shortPath = isAboveProject ? fullPath : fullPath.Substring(projectFolder.Length).TrimStart(Path.DirectorySeparatorChar);

    bool isBlank = string.IsNullOrEmpty(_shortPath);
    shortPath = _shortPath;
    longPath = fullPath;

    if (isBlank) {
      folderShort = "";
      folderLong = "";
    } else {
      folderShort = shortPath;
      folderLong = longPath;
      if (!Directory.Exists(longPath) && File.Exists(longPath)) {
        folderShort = Path.GetDirectoryName(shortPath);
        folderLong = Path.GetDirectoryName(longPath);
      }
    }

    // list files in the folder
    string fileList = "";
    if (Directory.Exists(longPath)) {
      string[] files = Directory.GetFiles(longPath);
      fileList = string.Join("\n  -  ", files);
    }
    // Debug.Log($"PathyThing: inputPath:'{inputPath}' shortPath: '{shortPath}', longPath: '{longPath}', folderShort: '{folderShort}', folderLong: '{folderLong}', isProjectPath: {isProjectPath}, isAbsolutePath: {isAbsolutePath}, isAboveProject: {isAboveProject}\nFiles in folder:\n  -  {fileList}");
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

  public FolderThing(string _shortPath, bool _canBeMissing = false) : base(_shortPath) {
    #if UNITY_EDITOR
      bool isValidAstDbFolder = AssetDatabase.IsValidFolder(shortPath);
      canBeMissing = _canBeMissing;
      if (isValidAstDbFolder) return;
      if (Directory.Exists(longPath)) return;
      if (!canBeMissing) Debug.LogWarning($"Folder missing: shortPath='{shortPath}' longPath='{longPath}'");
    #endif
  }

  override public bool Exists() {
    bool doesExist =  Directory.Exists(longPath);
    if (!doesExist && !canBeMissing) {
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
  public FolderThing DeeperFolderCanBeMissing(params string[] deeperPaths) {    
    string newPath = longPath;
    foreach (string deeperPath in deeperPaths) {
      newPath = Path.Combine(newPath, deeperPath);
    }
    return new FolderThing(newPath, true);
  }
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
    var newFile = new FileThing(newPath, canBeMissing);
    return newFile;
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

  public FileThing(string _shortPath, bool _canBeMissing = false) : base(_shortPath) {
    #if UNITY_EDITOR
      canBeMissing = _canBeMissing;
      if (AssetDatabase.IsValidFolder(shortPath)) {
        Debug.LogWarning("FileThing: path must be a file, not a folder");
      }
      // check if path is outside project
      bool isAboveProject = !longPath.StartsWith(Path.Combine(Path.GetFullPath(Application.dataPath + "/.."), "Assets") );
      if (!isAboveProject) unityObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shortPath);
    #endif
  }

  override public bool Exists() {
    bool doesExist = File.Exists(longPath);
    if (!doesExist && !canBeMissing) {
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