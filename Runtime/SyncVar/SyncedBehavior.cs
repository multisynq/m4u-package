using UnityEngine;
//========== ||||||||||||||| =============
public class SyncedBehaviour : MonoBehaviour {
  public int netId = 0;
  // at editor time, set a value if it is zero
  void OnValidate() {
    // Debug.Log($"netId={netId}    GetInstanceID()={GetInstanceID()}");
    if (netId == 0) {
      netId = GetInstanceID();
    }
  }
}