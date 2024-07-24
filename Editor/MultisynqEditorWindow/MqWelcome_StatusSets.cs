using System.Linq;
using UnityEditor;

//=============================================================================
static public class MqWelcome_StatusSets {
  static public StatusSet ready;
  static public StatusSet settings;
  static public StatusSet node;
  static public StatusSet apiKey;
  static public StatusSet bridge;
  static public StatusSet bridgeHasSettings;
  static public StatusSet jsBuildTools;
  static public StatusSet jsBuild;
  static public StatusSet hasAppJs;
  static public StatusSet versionMatch;

  static public StatusSet[] AllMyStatusSets() {
    // return new StatusSet[]{ ready, settings, node, apiKey, bridge, bridgeHasSettings, jsBuildTools, versionMatch, jsBuild };
    // use reflection to get all the static fields of this class
    return typeof(MqWelcome_StatusSets).GetFields().Select(f => f.GetValue(null)).OfType<StatusSet>().ToArray();
  }
  static public void SuccessesToReady() {
    foreach (var ss in AllMyStatusSets()) {
      ss.SuccessToReady();
    }
  }
  static public void AllStatusSetsToBlank() {
    foreach (var ss in AllMyStatusSets()) {
      ss?.blank.Set();
    }
  }
}