using System.Linq;
using UnityEditor;
using UnityEngine;

//=============================================================================
static public class StatusSetMgr {
  static public StatusSet ready;
  static public StatusSet settings;
  static public StatusSet node;
  static public StatusSet apiKey;
  static public StatusSet bridge;
  static public StatusSet hasCqSys;
  static public StatusSet bridgeHasSettings;
  static public StatusSet jsBuildTools;
  static public StatusSet jsBuild;
  static public StatusSet hasAppJs;
  static public StatusSet versionMatch;
  static public StatusSet builtOutput;

  static public StatusSet[] AllMyStatusSets() {
    // return new StatusSet[]{ ready, settings, node, apiKey, bridge, bridgeHasSettings, jsBuildTools, versionMatch, jsBuild };
    // use reflection to get all the static fields of this class
    return typeof(StatusSetMgr).GetFields().Select(f => f.GetValue(null)).OfType<StatusSet>().ToArray();
  }
  //=============================================================================
  static public CroquetSettings FindProjectCqSettings() {
    // First check for a CroquetSettings on the scene's CroquetBridge
    var bridge = SceneHelp.FindComp<CroquetBridge>();
    if (bridge != null && bridge.appProperties != null) {
      return bridge.appProperties; // appProperties is a CroquetSettings
    }
    // Then look in all project folders for a file of CroquetSettings type
    CroquetSettings cqSettings = SceneHelp.FindCompInProject<CroquetSettings>();
    if (cqSettings == null) {
      Debug.LogWarning("Could not find CroquetSettings.asset in your Assets folders.");
      StatusSetMgr.settings.error.Set();
      StatusSetMgr.node.error.Set();
      StatusSetMgr.apiKey.error.Set();
      StatusSetMgr.ready.error.Set();
    }

    return cqSettings;
  }


}