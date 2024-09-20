using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multisynq {


[AttributeUsage(AttributeTargets.Class)]
public class SynqInstanceAttribute : Attribute { }

public class SynqMgr : MonoBehaviour {

  public Dictionary<int, SynqBehaviour> allSBs = new Dictionary<int, SynqBehaviour>();
  public Dictionary<int, SynqBehaviour> instancingSBs = new Dictionary<int, SynqBehaviour>();

  void Awake() {
    foreach (var sb in FindObjectsOfType<SynqBehaviour>()) {
      RegisterSynqBehaviour(sb);
    }
  }

  public void RegisterSynqBehaviour(SynqBehaviour sb) {
    if (!allSBs.ContainsKey(sb.netId)) {
      allSBs.Add(sb.netId, sb);
      if (Attribute.IsDefined(sb.GetType(), typeof(SynqInstanceAttribute))) {
        instancingSBs.Add(sb.netId, sb);
      }
    }
    else {
      Debug.LogWarning($"SynqBehaviour with netId {sb.netId} already registered.");
    }
  }

  public void UnregisterSynqBehaviour(SynqBehaviour sb) {
    allSBs.Remove(sb.netId);
    instancingSBs.Remove(sb.netId);
  }

  public SynqBehaviour FindSB(int netId) {
    if (allSBs.TryGetValue(netId, out SynqBehaviour sb)) {
      return sb;
    }
    return null;
  }

  public SynqBehaviour FindInstancingSB(int netId) {
    if (instancingSBs.TryGetValue(netId, out SynqBehaviour sb)) {
      return sb;
    }
    return null;
  }
  static public GameObject Instantiate(GameObject go, bool includeSelf = true) {
    if (go.GetComponent<SynqBehaviour>() == null) { // make sure clone source has a SynqBehaviour with a netId
      go.AddComponent<SynqBehaviour>().MakeNewId();
    }
    var newGo = Instantiate(go, includeSelf);
    newGo.GetComponent<SynqBehaviour>().MakeNewId(); // give the clone a new netId
    return newGo;
  }   
  #region Singleton
    //------------------- | -------------------------
    public static SynqMgr I { // Usage:   SynqMgr.I.JsPluginFileName();
      get { 
        _instance = Singletoner.EnsureInst(_instance);
        return _instance;
      }
      private set { _instance = value; }
    }
    private static SynqMgr _instance;
  #endregion


}

}