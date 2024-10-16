using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DeleteM4uSupportFiles: EditorWindow {

  [MenuItem("Multisynq/======= Delete M4U Support Files ======", priority=50)]
  public static void Main() {
    string rootDir = Directory.GetParent(Application.dataPath).FullName;
    Debug.Log($"Root path: {rootDir}");

    // Delete specific files
    // DeleteFile(Path.Combine(rootDir, "Tutorials.sln"));

    // Delete files by wildcard
    // DeleteFilesByWildcard(rootDir, "*.csproj");
    DeleteFilesByWildcard(rootDir, ".last-installed-tools");
    DeleteFilesByWildcard(rootDir, ".last-build-state");
    DeleteFilesByWildcard(rootDir, ".last-build-state");

    // Delete specific folders and their contents
    // DeleteDirectory(Path.Combine(rootPath, ".vscode"));
    // DeleteDirectory(Path.Combine(rootDir, "Logs"));
    // DeleteDirectory(Path.Combine(rootPath, "UserSettings"));

    // Delete specific files and folders within Assets
    string assetsPath = Path.Combine(rootDir, "Assets");
    Debug.Log($"Assets path: {assetsPath}");
    // DeleteDirectory(Path.Combine(assetsPath, "AddressableAssetsData"));
    DeleteDirectory(Path.Combine(assetsPath, "StreamingAssets"));
    // DeleteDirectory(Path.Combine(assetsPath, "TextMesh Pro"));
    DeleteDirectory(Path.Combine(assetsPath, "WebGLTemplates"));

    // Delete specific files and folders within Assets/MultisynqJS
    string multisynqJSPath = Path.Combine(assetsPath, "MultisynqJS");
    Debug.Log($"MultisynqJS path: {multisynqJSPath}");
    DeleteDirectory(Path.Combine(multisynqJSPath, "build-tools"));
    DeleteDirectory(Path.Combine(multisynqJSPath, "node_modules"));
    DeleteDirectory(Path.Combine(multisynqJSPath, "m4u-package"));

    Debug.Log("File and folder deletion complete.");
  }

  static void DeleteFile(string path) {
    if (File.Exists(path)) {
      //File.Delete(path);
      Debug.Log($"Deleted file: {path}");
    }
  }

  static void DeleteDirectory(string path) {
    if (Directory.Exists(path)) {
      //Directory.Delete(path, true);
      Debug.Log($"Deleted directory: {path}");
    }
  }

  static void DeleteFilesByWildcard(string directory, string searchPattern) {
    foreach (string file in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)) {
      //File.Delete(file);
      Debug.Log($"Deleted file: {file}");
    }

    // Delete empty directories
    // foreach (string dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)) {
    //   if (Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length == 0 &&
    //     Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Length == 0) {
    //     //Directory.Delete(dir, false);
    //     Debug.Log($"Deleted empty directory: {dir}");
    //   }
    // }
  }
}