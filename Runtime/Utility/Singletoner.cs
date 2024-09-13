using UnityEngine;

//================= ||||||||||| ====================
public static class Singletoner {

  //------------- |||||||||| -----------------------------------------
  public static T EnsureInst<T>(T instance) where T : MonoBehaviour {
    if (instance == null) {
      instance = Object.FindObjectOfType<T>();
      if (instance == null) {
        // Ensure a Gob _Singletons is present
        GameObject singletons = GameObject.Find("_Singletons");
        if (singletons == null) {
          singletons = new GameObject();
          singletons.name = "_Singletons";
          Object.DontDestroyOnLoad(singletons);
        }
        // add a new Gob with the desired component with the name of the component
        GameObject singleton = new GameObject();
        singleton.transform.parent = singletons.transform;
        instance = singleton.AddComponent<T>();
        singleton.name = $"[{typeof(T)}]";
        Object.DontDestroyOnLoad(singleton);
        Debug.Log($"[Singletoner] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
      }
      // else Debug.Log($"[Singletoner] Using instance already created: {typeof(T).Name}");
    }
    return instance;
  }

}