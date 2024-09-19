using UnityEngine;

namespace Multisynq {

public class Mq_ActorManifest : MonoBehaviour
{
    public string pawnType = "";
    public string defaultActorClass = ""; // only used on pre-load objects
    public string[] mixins;
    public string[] staticProperties;
    public string[] watchedProperties;
}

}