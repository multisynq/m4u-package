using System.Linq;
using UnityEditor;
using UnityEngine;
using Multisynq;

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
  static public StatusSet jsPlugins;

  static public StatusSet[] AllMyStatusSets() {
    // return new StatusSet[]{ ready, settings, node, apiKey, bridge, bridgeHasSettings, jsBuildTools, versionMatch, jsBuild };
    // use reflection to get all the static fields of this class
    return typeof(StatusSetMgr).GetFields().Select(f => f.GetValue(null)).OfType<StatusSet>().ToArray();
  }
  //=============================================================================
  static public Mq_Settings FindProjectCqSettings() {
    // First check for a Mq_Settings on the scene's Mq_Bridge
    var bridge = SceneHelp.FindComp<Mq_Bridge>();
    if (bridge != null && bridge.appProperties != null) {
      return bridge.appProperties; // appProperties is a Mq_Settings
    }
    // Then look in all project folders for a file of Mq_Settings type
    Mq_Settings cqSettings = SceneHelp.FindCompInProject<Mq_Settings>();
    if (cqSettings == null) {
      Debug.LogWarning("Could not find Mq_Settings.asset in your Assets folders.");
      StatusSetMgr.settings.error.Set();
      StatusSetMgr.node.error.Set();
      StatusSetMgr.apiKey.error.Set();
      StatusSetMgr.ready.error.Set();
    }

    return cqSettings;
  }


}