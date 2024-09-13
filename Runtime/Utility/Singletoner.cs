using UnityEngine;

public static class Singletoner {
  public static T EnsureInst<T>(T instance) where T : MonoBehaviour {
    if (instance == null) {
      instance = Object.FindObjectOfType<T>();
      if (instance == null) {
        GameObject singleton = new GameObject();
        instance = singleton.AddComponent<T>();
        singleton.name = $"[Singletoner] {typeof(T)}";
        Object.DontDestroyOnLoad(singleton);
        Debug.Log($"[Singletoner] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
      }
      // else Debug.Log($"[Singletoner] Using instance already created: {typeof(T).Name}");
    }
    return instance;
  }
}