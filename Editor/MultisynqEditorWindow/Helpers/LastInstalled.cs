using System;
using System.IO;
using UnityEngine;

class LastInstalled {

  public string packageVersion;
  public int localToolsLevel;

  static public LastInstalled LoadPath(string path) {
    try {
      var json = File.ReadAllText(path);
      return FromJson(json);
    } catch (Exception e) {
      var relativePath = path.Replace(Application.dataPath, "Assets");
      Debug.LogWarning($"Need to attempt a build to make the missing file: /{relativePath}\n{e}");
      return new LastInstalled();
    }
  }

  static public LastInstalled FromJson(string json) {
    return JsonUtility.FromJson<LastInstalled>(json);
  }

  public bool IsSameAs(LastInstalled other) {
    // return false if any are null
    if (other == null) return false;
    if (packageVersion == null || other.packageVersion == null) return false;
    bool isSamePkgVersion = packageVersion == other.packageVersion;
    bool isSameToolsLevel = localToolsLevel == other.localToolsLevel;
    return isSamePkgVersion && isSameToolsLevel;
  }

  public string ReportDiffs(LastInstalled other) {
    string diffs = "";
    if (packageVersion != other.packageVersion) {
      diffs += $"packageVersion: '{packageVersion}' != '{other.packageVersion}'\n";
    }
    if (localToolsLevel != other.localToolsLevel) {
      diffs += $"localToolsLevel: '{localToolsLevel}' != '{other.localToolsLevel}'\n";
    }
    return (diffs=="") ? "match" : diffs;
  }

}
