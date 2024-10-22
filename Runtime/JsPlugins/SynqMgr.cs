using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multisynq {


[AttributeUsage(AttributeTargets.Class)]
public class SynqInstanceAttribute : Attribute { }

public class SynqMgr : MonoBehaviour {

  public Dictionary<uint, SynqBehaviour> allSBs = new Dictionary<uint, SynqBehaviour>();
  public Dictionary<uint, SynqBehaviour> instancingSBs = new Dictionary<uint, SynqBehaviour>();

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

  public SynqBehaviour FindSB(uint netId) {
    if (allSBs.TryGetValue(netId, out SynqBehaviour sb)) {
      return sb;
    }
    return null;
  }

  public SynqBehaviour FindInstancingSB(uint netId) {
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
    private static SynqMgr _Instance;
    public  static SynqMgr I { // Usage:   SynqMgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
  #endregion


}

}