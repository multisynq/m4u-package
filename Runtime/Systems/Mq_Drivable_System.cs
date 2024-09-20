using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Multisynq {

public class Mq_Drivable_System : Mq_System {

  public bool mq_Drivable_System;
  
  private Mq_Drivable_Comp lastKnownActiveDrivable;

  public override List<string>                KnownCommands { get; } = new() { };
  protected override Dictionary<int, Mq_Comp> components { get; set; } =
    new Dictionary<int, Mq_Comp>();
  public static Mq_Drivable_System            Instance { get; private set; }

  private void Awake() {
    if (Instance != null && Instance != this) Destroy(this); // If there is an instance, and it's not me, delete myself.
    else Instance = this; // Singleton Accessor
  }

  public override void PawnInitializationComplete(GameObject go) {
    if (Croquet.HasActorSentProperty(go, "driver")) {
      SetDrivenFlag(go);
    }
  }

  public override void ActorPropertySet(GameObject go, string propName) {
    // we're being notified that a watched property on an object that we are
    // known to have an interest in has changed (or been set for the first time).
    if (propName == "driver") {
      SetDrivenFlag(go);
    }
  }

  private void SetDrivenFlag(GameObject go) {
    Mq_Drivable_Comp drivable = go.GetComponent<Mq_Drivable_Comp>();
    if (drivable != null) {
      string driver = Croquet.ReadActorString(go, "driver");
      drivable.isDrivenByThisView = driver == Mq_Bridge.Instance.croquetViewId;
    }
  }

  void CheckForActiveDrivable() {
    // for the case where only a single gameObject is expected to be drivable, provide a lookup
    // that can be used from any script to find the drivable component - if any - that is set
    // to be driven by the local view.
    // this needs to be efficient, so it can be called from update loops if wanted.
    string croquetViewId = Mq_Bridge.Instance.croquetViewId;
    if (croquetViewId != "") {
      if (lastKnownActiveDrivable != null) {
        // we think we know, but check just in case the driver has changed
        if (!lastKnownActiveDrivable.isDrivenByThisView) {
          Debug.Log("drivable lost its active status");
          lastKnownActiveDrivable = null;
        }
      }

      if (lastKnownActiveDrivable == null) {
        // TODO: for efficiency, we probably need to switch the base class to use generics
        foreach (var kvp in components) {
          Mq_Drivable_Comp c = kvp.Value as Mq_Drivable_Comp;
          if (c != null && c.isDrivenByThisView) {
            // Debug.Log("found active drivable");
            lastKnownActiveDrivable = c;
            return;
          }
        }
      }
    }
  }

  [CanBeNull]
  public Mq_Drivable_Comp GetActiveDrivableComponent() {
    CheckForActiveDrivable();
    return lastKnownActiveDrivable;
  }
}

}