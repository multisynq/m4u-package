using UnityEngine;

namespace Multisynq {

public abstract class Mq_Comp : MonoBehaviour
{
    public abstract Mq_System croquetSystem  { get; set; }

    void Awake()
    {
        // if (croquetSystem == null) Debug.Log($"futile attempt to awaken {this}");
        if (croquetSystem != null) croquetSystem.RegisterComponent(this);
    }

}

}