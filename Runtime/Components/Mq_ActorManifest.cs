using UnityEngine;

namespace MultisynqNS {

public class  Mq_ActorManifest : MonoBehaviour {
  public bool mq_ActorManifest;  // Helps tools resolve "missing Script" problems
  public string pawnType = "";
  public string defaultActorClass = ""; // only used on pre-load objects
  public string[] mixins;
  public string[] staticProperties;
  public string[] watchedProperties;
}

}