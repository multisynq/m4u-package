using System;
using System.Linq;
using UnityEngine;

//========== ||||||||||||||| =============
public class SyncedBehaviour : MonoBehaviour {

  public int netId = 0;

  public void CallSyncCommand(Action method, params object[] parameters) {
      string methodName = method.Method.Name;
      SyncCommandMgr.I.PublishCommandCall(this, methodName, parameters);
  }

  public void CallSyncCommand<T>(Action<T> method, T parameter) {
      string methodName = method.Method.Name;
      SyncCommandMgr.I.PublishCommandCall(this, methodName, new object[] { parameter });
  }

  // Add more overloads for different number of parameters as needed
  public void CallSyncCommand<T1, T2>(Action<T1, T2> method, T1 param1, T2 param2) {
      string methodName = method.Method.Name;
      SyncCommandMgr.I.PublishCommandCall(this, methodName, new object[] { param1, param2 });
  }

  public void CallSyncCommand<T1, T2, T3>(Action<T1, T2, T3> method, T1 param1, T2 param2, T3 param3) {
      string methodName = method.Method.Name;
      SyncCommandMgr.I.PublishCommandCall(this, methodName, new object[] { param1, param2, param3 });
  }

  public void CallSyncCommand<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 param1, T2 param2, T3 param3, T4 param4) {
      string methodName = method.Method.Name;
      SyncCommandMgr.I.PublishCommandCall(this, methodName, new object[] { param1, param2, param3, param4 });
  }
  
  #if UNITY_EDITOR
    // At editor time, set a new netId  ONLY IF  it is zero and unititialized
    void OnValidate() {
      if (netId == 0) {
        netId = GenerateNewId(GetInstanceID());
        EnsureUnique();
        Debug.Log($"new netId={netId}");
      }
    }

    void EnsureUnique() {
      var allSyncedBeh = FindObjectsOfType<SyncedBehaviour>();
      int attempts = 0;
      int maxAttempts = 1000; // Prevent infinite loop

      while (allSyncedBeh.Count(sb => sb.netId == netId) > 1 && attempts < maxAttempts) {
        netId = GenerateNewId(netId);
        attempts++;
      }

      if (attempts >= maxAttempts) {
        Debug.LogWarning($"Failed to find a unique netId for {gameObject.name} after {maxAttempts} attempts.");
      }
    }

    private int GenerateNewId(int currentId) {
      unchecked {
        int hash = currentId;
        hash = (hash ^ 61) ^ (hash >> 16);
        hash += (hash << 3);
        hash ^= (hash >> 4);
        hash *= 0x27d4eb2d; // Prime number
        hash ^= (hash >> 15);
        return Mathf.Abs(hash) % 10000000; // Keep it within 0-9999999 range
      }
    }
  #endif
}