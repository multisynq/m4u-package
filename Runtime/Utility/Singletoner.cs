using UnityEngine;

public static class Singletoner
{
    public static T EnsureInst<T>(T instance) where T : MonoBehaviour
    {
        if (instance == null)
        {
            instance = Object.FindObjectOfType<T>();

            if (instance == null)
            {
                GameObject singleton = new GameObject();
                instance = singleton.AddComponent<T>();
                singleton.name = $"(singleton) {typeof(T)}";
                Object.DontDestroyOnLoad(singleton);
                Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
            }
            else
            {
                Debug.Log($"[Singleton] Using instance already created: {instance.gameObject.name}");
            }
        }

        return instance;
    }
}