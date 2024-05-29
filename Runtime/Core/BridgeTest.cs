using UnityEngine;

public class BridgeTest : MonoBehaviour
{
    private CroquetBridge bridge;

    void Start()
    {
        bridge = gameObject.AddComponent<CroquetBridge>();
        bridge.SendMessageToJavaScript("Hello from Unity");
    }
}
