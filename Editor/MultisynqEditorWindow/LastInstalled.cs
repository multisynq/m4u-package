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
      Debug.LogError($"Need to attempt a build to have this. Could not load LastInstalled from path: {path}\n{e}");
      return new LastInstalled();
    }
  }
  static public LastInstalled FromJson(string json) {
    return JsonUtility.FromJson<LastInstalled>(json);
  }
  public Boolean IsSameAs(LastInstalled other) {
    // return false if any are null
    if (other == null) return false;
    if (packageVersion == null || other.packageVersion == null) return false;
    return packageVersion == other.packageVersion && localToolsLevel == other.localToolsLevel;
  }
  public string ReportDiffs(LastInstalled other) {
    string diffs = "";
    if (packageVersion != other.packageVersion) {
      diffs += $"packageVersion: {packageVersion} != {other.packageVersion}\n";
    }
    if (localToolsLevel != other.localToolsLevel) {
      diffs += $"localToolsLevel: {localToolsLevel} != {other.localToolsLevel}\n";
    }
    return (diffs=="") ? "match" : diffs;
  }
}
