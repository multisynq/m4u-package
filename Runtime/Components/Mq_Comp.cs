using UnityEngine;

namespace MultisynqNS {

public abstract class Mq_Comp : MonoBehaviour {
  public bool mq_Comp;  // Helps tools resolve "missing Script" problems
  
  public abstract Mq_System croquetSystem  { get; set; }

  void Awake() {
    // if (croquetSystem == null) Debug.Log($"futile attempt to awaken {this}");
    if (croquetSystem != null) croquetSystem.RegisterComponent(this);
  }

}

}