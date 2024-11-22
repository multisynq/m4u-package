using System;
using System.Linq;
using UnityEngine;

namespace Multisynq {

public interface IWithNetId {
  public uint netId {get; set;}
}

public enum RpcTarget { Others, All };
//========== ||||||||||||| =============
public class SynqBehaviour : MonoBehaviour, IWithNetId {

  // static public SynqBehaviour currentlyConstructingSynqBehaviour;

  [field: SerializeField]
  public uint netId { get; set; } = 0;
  
  public void CallSynqCommand(string methodName, params object[] parameters) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, methodName, parameters);
  }
  public void RPC(string mn, RpcTarget tgt, params object[] ps) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, tgt, mn, ps); 
  }
  // Action method. No parameters
  public void CallSynqCommand(Action method) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name);
  }
  public void RPC(Action m, RpcTarget tgt = RpcTarget.All) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, tgt, m.Method.Name); 
  }
  // Action method. Single array of parameters.
  public void CallSynqCommand(Action method, params object[] parameters) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name, parameters);
  }
  public void RPC(Action m, RpcTarget tgt, params object[] ps) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, tgt, m.Method.Name, ps); 
  }
  // Action<T> method
  public void CallSynqCommand<T>(Action<T> method, T parameter) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name, new object[] { parameter });
  }
  public void RPC<T>(Action<T> m, RpcTarget tgt, T p) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, tgt, m.Method.Name, new object[] { p }); 
  }
  // Action<T1, T2> method
  public void CallSynqCommand<T1, T2>(Action<T1, T2> method, T1 param1, T2 param2) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name, new object[] { param1, param2 });
  }
  public void RPC<T1, T2>(Action<T1, T2> m, RpcTarget tgt, T1 p1, T2 p2) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall( this, tgt, m.Method.Name, new object[] { p1, p2 }); 
  }
  // Action<T1, T2, T3> method
  public void CallSynqCommand<T1, T2, T3>(Action<T1, T2, T3> method, T1 param1, T2 param2, T3 param3) {
      SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name, new object[] { param1, param2, param3 });
  }
  public void RPC<T1, T2, T3>(Action<T1, T2, T3> m, RpcTarget tgt, T1 p1, T2 p2, T3 p3) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall( this, tgt, m.Method.Name, new object[] { p1, p2, p3 }); 
  }
  // Action<T1, T2, T3, T4> method
  public void CallSynqCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 param1, T2 param2, T3 param3, T4 param4) {
    SynqCommand_Mgr.I.PublishSynqCommandCall(this, method.Method.Name, new object[] { param1, param2, param3, param4 });
  }
  public void RPC<T1, T2, T3, T4>(Action<T1, T2, T3, T4> m, RpcTarget tgt, T1 p1, T2 p2, T3 p3, T4 p4) { 
    SynqCommand_Mgr.I.PublishSynqCommandCall( this, tgt, m.Method.Name, new object[] { p1, p2, p3, p4 }); 
  }
  #if UNITY_EDITOR
    // At editor time, set a new netId  ONLY IF  it is zero and unititialized
    void OnValidate() {
      if (netId == 0) {
        MakeNewId();
      }
    }
  #endif
  public uint MakeNewId() {    
    uint startId = unchecked((uint)GetInstanceID());
    netId = GenerateNewId(startId);
    EnsureUnique();
    Debug.Log($"new netId={netId}");
    return netId;
  }

  public void EnsureUnique() {
    var allSynqBehs = FindObjectsOfType<SynqBehaviour>();
    int attempts = 0;
    int maxAttempts = 1000; // Prevent infinite loop

    while (allSynqBehs.Count(sb => sb.netId == netId) > 1 && attempts < maxAttempts) {
      netId = GenerateNewId(netId);
      attempts++;
    }

    if (attempts >= maxAttempts) {
      Debug.LogWarning($"Failed to find a unique netId for {gameObject.name} after {maxAttempts} attempts.");
    }
  }

  private uint GenerateNewId(uint currentId) {
    unchecked {
      uint hash = currentId;
      hash = (hash ^ 61) ^ (hash >> 16);
      hash += (hash << 3);
      hash ^= (hash >> 4);
      hash *= 0x2d4eb2d9; // Prime number close to 2^32 / φ
      // Use more bits in final shuffle
      hash ^= (hash >> 11);
      hash ^= (hash >> 19);
      return hash % 10000000u; // Keep it within 0-9999999 range
    }
  }

  // public SynqBehaviour() { currentlyConstructingSynqBehaviour = this; }

}

} // namespace MultisynqNS