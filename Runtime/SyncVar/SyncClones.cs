using UnityEngine;

namespace MultisynqNS {


public class SyncClones : SyncBehaviour {
  public bool shouldClone = false;
  private static bool isCloning = false;
  private bool hasCloned = false;

  void Start() {
    Debug.Log($"[{Time.frameCount}] SyncInstances Start: {gameObject.name} (netId: {netId}, shouldClone: {shouldClone}, isCloning: {isCloning}, hasCloned: {hasCloned})");
    SyncClones_Mgr.I.RegisterSyncedBehaviour(this);
    
    if (shouldClone && !isCloning && !hasCloned) {
      StartCloning();
    }
  }

  private void StartCloning() {
    if (isCloning || hasCloned) {
      Debug.LogWarning($"[{Time.frameCount}] Cloning blocked for {gameObject.name} (netId: {netId}, isCloning: {isCloning}, hasCloned: {hasCloned})");
      return;
    }

    isCloning = true;
    hasCloned = true;
    Debug.Log($"[{Time.frameCount}] Starting clone process for {gameObject.name} (netId: {netId})");

    var (clone, clonesSb) = SyncClones_Mgr.SyncClone(this);

    var si = clonesSb as SyncClones ?? clonesSb.GetComponent<SyncClones>();
    si.shouldClone = false;
    si.hasCloned = true;
    si.MakeNewId();

    isCloning = false;
    Debug.Log($"[{Time.frameCount}] SyncInstances: %gn%CLONE%gy%: %ye%{clone.name}%gy%: %mg%{netId}%gy% to %mg%{si.netId}".TagColors());
  }

}

} // namespace MultisynqNS