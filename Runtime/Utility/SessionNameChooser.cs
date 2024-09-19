using UnityEngine;

public abstract class SessionNameChooser : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
