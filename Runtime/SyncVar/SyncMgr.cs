using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class SyncInstanceAttribute : Attribute { }

public class SyncMgr : MonoBehaviour {

  public Dictionary<int, SyncBehaviour> allSBs = new Dictionary<int, SyncBehaviour>();
  public Dictionary<int, SyncBehaviour> instancingSBs = new Dictionary<int, SyncBehaviour>();

  void Awake() {
    foreach (var sb in FindObjectsOfType<SyncBehaviour>()) {
      RegisterSyncedBehaviour(sb);
    }
  }

  public void RegisterSyncedBehaviour(SyncBehaviour sb) {
    if (!allSBs.ContainsKey(sb.netId)) {
      allSBs.Add(sb.netId, sb);
      if (Attribute.IsDefined(sb.GetType(), typeof(SyncInstanceAttribute))) {
        instancingSBs.Add(sb.netId, sb);
      }
    }
    else {
      Debug.LogWarning($"SyncBehaviour with netId {sb.netId} already registered.");
    }
  }

  public void UnregisterSyncedBehaviour(SyncBehaviour sb) {
    allSBs.Remove(sb.netId);
    instancingSBs.Remove(sb.netId);
  }

  public SyncBehaviour FindSB(int netId) {
    if (allSBs.TryGetValue(netId, out SyncBehaviour sb)) {
      return sb;
    }
    return null;
  }

  public SyncBehaviour FindInstancingSB(int netId) {
    if (instancingSBs.TryGetValue(netId, out SyncBehaviour sb)) {
      return sb;
    }
    return null;
  }
  static public GameObject Instantiate(GameObject go, bool includeSelf = true) {
    go.EnsureComp<SyncBehaviour>();
    return Instantiate(go);
  }   
  #region Singleton
    //------------------------------- | -------------------------
    public static SyncMgr I { // Usage:   SyncedBehaviour_Mgr.I.JsPluginFileName();
      get { 
        _instance = Singletoner.EnsureInst(_instance);
        return _instance;
      }
      private set { _instance = value; }
    }
    private static SyncMgr _instance;
  #endregion


}