using UnityEngine;

namespace Multisynq {


public class SynqClones : SynqBehaviour {
  public bool shouldClone = false;
  private static bool isCloning = false;
  private bool hasCloned = false;

  void Start() {
    Debug.Log($"[{Time.frameCount}] SynqInstances Start: {gameObject.name} (netId: {netId}, shouldClone: {shouldClone}, isCloning: {isCloning}, hasCloned: {hasCloned})");
    SynqClones_Mgr.I.RegisterSynqBehaviour(this);
    
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

    var (clone, clonesSb) = SynqClones_Mgr.SynqClone(this);

    var si = clonesSb as SynqClones ?? clonesSb.GetComponent<SynqClones>();
    si.shouldClone = false;
    si.hasCloned = true;
    si.MakeNewId();

    isCloning = false;
    Debug.Log($"[{Time.frameCount}] SynqInstances: %gn%CLONE%gy%: %ye%{clone.name}%gy%: %mg%{netId}%gy% to %mg%{si.netId}".TagColors());
  }

}

} // namespace MultisynqNS