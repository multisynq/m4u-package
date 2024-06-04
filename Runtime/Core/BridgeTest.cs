using System.Collections;
using UnityEngine;

public class BridgeTest : MonoBehaviour
{
    private CroquetBridge bridge;

    void Start()
    {
        bridge = gameObject.GetComponent<CroquetBridge>();
        Debug.Log("Bridge component added.");
        bridge.SendMessageToJavaScript("Hello from Unity");
        Debug.Log("Initial message sent.");
        StartCoroutine(tickForBridgeTest());
        Debug.Log("Coroutine started.");
    }

    IEnumerator tickForBridgeTest()
    {
        int count = 0;
        while (true)
        {
            count = count+60;
            yield return new WaitForSeconds(60);
            Debug.Log("Tick count: " + count); // Added debug logging
            bridge.SendMessageToJavaScript("Hello from Unity every second, we're on second " + count);
        }
    }
}
