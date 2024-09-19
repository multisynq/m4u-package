using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Mq_Settings", order = 1)]
public class Mq_Settings : ScriptableObject
{
    public string apiKey;
    public string appPrefix;
    public int preferredPort;
#if !UNITY_EDITOR_OSX
    [HideInInspector]
#endif
    public string pathToNode;
}
