using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DeleteM4uSupportFiles: EditorWindow {

  [MenuItem("Multisynq/======= Delete M4U Support Files ======", priority=50)]
  public static void Main() {
    string rootDir = Directory.GetParent(Application.dataPath).FullName;
    Debug.Log($"Root path: {rootDir}");

    // Delete build result files by wildcard
    // DeleteFilesRecursivelyByWildcard(rootDir, "*.csproj");
    DeleteFilesRecursivelyByWildcard(rootDir, ".last-installed-tools");
    DeleteFilesRecursivelyByWildcard(rootDir, ".last-build-state");
    DeleteFilesRecursivelyByWildcard(rootDir, ".last-build-state");

    // DeleteFile(Path.Combine(rootDir, "Tutorials.sln")); // Delete VSCode Solution

    // Delete specific files and folders within Assets
    string assetsPath = Path.Combine(rootDir, "Assets");
    Debug.Log($"Assets path: {assetsPath}");
    DeleteDirectory(Path.Combine(assetsPath, "StreamingAssets"));
    DeleteDirectory(Path.Combine(assetsPath, "WebGLTemplates"));
    // DeleteDirectory(Path.Combine(assetsPath, "AddressableAssetsData"));
    // DeleteDirectory(Path.Combine(assetsPath, "TextMesh Pro"));

    // Delete specific files and folders within Assets/MultisynqJS
    string multisynqJSPath = Path.Combine(assetsPath, "MultisynqJS");
    Debug.Log($"MultisynqJS path: {multisynqJSPath}");
    DeleteDirectory(Path.Combine(multisynqJSPath, "build-tools"));
    DeleteDirectory(Path.Combine(multisynqJSPath, "unity-js"));
    DeleteDirectory(Path.Combine(multisynqJSPath, "_Runtime"));

    // rootDir files and folders
    DeleteDirectory(Path.Combine(rootDir, "node_modules"));
    DeleteFile(Path.Combine(rootDir, "package.json"));
    DeleteFile(Path.Combine(rootDir, "package-lock.json"));
    Debug.Log("File and folder deletion complete.");

    // refresh asset db
    AssetDatabase.Refresh();
  }

  static void DeleteFile(string path) {
    if (File.Exists(path)) {
      File.Delete(path);
      Debug.Log($"Deleted file: {path}");
    }
  }

  static void DeleteDirectory(string path) {
    if (Directory.Exists(path)) {
      string metaPath = path + ".meta";
      if (File.Exists(metaPath)) {
        File.Delete(metaPath);
        Debug.Log($"Deleted file: {metaPath}");
      }
      Directory.Delete(path, true);
      Debug.Log($"Deleted directory: {path}");
      // also delete the .meta file if present
    }
  }

  static void DeleteFilesRecursivelyByWildcard(string directory, string searchPattern) {
    foreach (string file in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)) {
      File.Delete(file);
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