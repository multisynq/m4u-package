using UnityEngine;

namespace MultisynqNS {


public class PresentOncePositionUpdated : MonoBehaviour
{
    public bool waitUntilMove = false;
    public float timeout = 0.1f; // present after this time even if not moved
    private Mq_Spatial_Comp sc;
    private float startTime;

    private void Start()
    {
        sc = GetComponent<Mq_Spatial_Comp>();
        startTime = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (sc.hasBeenMoved || (!waitUntilMove && sc.hasBeenPlaced) || Time.realtimeSinceStartup - startTime >= timeout)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
            Destroy(this);
        }
    }
}

}