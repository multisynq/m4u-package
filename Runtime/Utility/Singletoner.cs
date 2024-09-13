using UnityEngine;

public static class Singletoner {

  public static T EnsureInst<T>(T instance) where T : MonoBehaviour {
    if (instance == null) {
      instance = Object.FindObjectOfType<T>(false);
      if (instance != null && !instance.enabled) instance = null;
      if (instance == null) {
        instance = EnsureInstanceInternal(typeof(T)) as T;
        // Debug.Log($"[Singletoner] An instance of {typeof(T).Name} is needed in the scene, so one was created with DontDestroyOnLoad.", instance);
      }
    }
    // Debug.Log($"%mg%[Singletoner]%gy% Instance {typeof(T).Name}.".TagColors(), instance);
    return instance;
  }

  public static MonoBehaviour EnsureInstByType(System.Type type) {
    if (!typeof(MonoBehaviour).IsAssignableFrom(type)) {
      Debug.LogError($"[Singletoner] EnsureInstByType called with a non-MonoBehaviour type: {type.Name}");
      return null;
    }

    MonoBehaviour instance = Object.FindObjectOfType(type, false) as MonoBehaviour;
    if (instance != null && !instance.enabled) instance = null;
    if (instance == null) {
      instance = EnsureInstanceInternal(type);
      // Debug.Log($"%mg%[Singletoner]%gy% ByType An instance of {type.Name} is needed in the scene, so one was created.", instance);
    }
    // Debug.Log($"%mg%[Singletoner]%gy% ByType Instance {type.Name}.".TagColors(), instance);
    return instance;
  }

  private static MonoBehaviour EnsureInstanceInternal(System.Type compType) {
    // Ensure a GameObject _Singletons is present
    GameObject singletons = GameObject.Find("_Singletons") ?? new(){name="_Singletons"};
    // Add a new GameObject with the desired component
    GameObject singleton = new($"[{compType.Name}]", compType);
    singleton.transform.SetParent(singletons.transform);
    var instance = singleton.GetComponent(compType);
    Debug.Log($"%mg%[Singletoner]%gy% An instance of {compType.Name} is needed in the scene, so '{singleton.name}' was created.".TagColors(), instance);
    return instance as MonoBehaviour;
  }
}