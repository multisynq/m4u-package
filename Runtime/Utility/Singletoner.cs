using UnityEngine;
using System.Linq;
using Multisynq;

//================= ||||||||||| ==================
public static class Singletoner {

  //------------- |||||||||| -----------------------------------------------------------
  public static T EnsureInst<T>(T instance) where T : MonoBehaviour {
    if (instance == null) {
      // var instances = Object.FindObjectsOfType<T>(true);
      var instances = typeof(T).FindAllInScene(true) as T[];
      instance = instances.FirstOrDefault(i => i.enabled);
      if (instance == null && instances.Length > 0) {
        instance = instances[0];
        instance.enabled = true;
        instance.gameObject.SetActive(true);
        // Debug.Log($"[Singletoner] Enabled the only existing instance of {typeof(T).Name}.");
      }
      if (instance == null) {
        instance = EnsureInstanceInternal(typeof(T)) as T; //, dontDestroyOnLoad) as T;
        // Debug.Log($"[Singletoner] An instance of {typeof(T).Name} is needed in the scene, so one was created with DontDestroyOnLoad.", instance);
      }
      if (instances.Length > 1) {
        CleanupExtraInstances(instances, instance);
      }
    }
    // if (dontDestroyOnLoad && instance.gameObject.scene.name != null) {
    //   Object.DontDestroyOnLoad(instance.gameObject);
    // }
    // Debug.Log($"%mg%[Singletoner]%gy% Instance {typeof(T).Name}.".TagColors(), instance);
    return instance;
  }
  //------------------------- |||||||||||||||| -----------------------------------------------------------
  public static MonoBehaviour EnsureInstByType(System.Type type) {
    if (!typeof(MonoBehaviour).IsAssignableFrom(type)) {
      Debug.LogError($"[Singletoner] EnsureInstByType called with a non-MonoBehaviour type: {type.Name}");
      return null;
    }
    // Use Where to make sure it is an exact type match
    var instances = Object.FindObjectsOfType(type, true)
      .OfType<MonoBehaviour>()
      .Where(x => x.GetType() == type)  // Only exact type matches
      .ToArray();
    var instance = instances.FirstOrDefault(i => (i.GetType().Name == type.Name) && i.enabled);
    if (instance == null && instances.Length > 0) {
      instance = instances[0];
      instance.enabled = true;
      // Debug.Log($"[Singletoner] Enabled the only existing instance of {type.Name}.");
    }
    if (instance == null) {
      instance = EnsureInstanceInternal(type);
      Debug.Log($"%mg%[Singletoner]%gy% Created in scene: [%ye%{type.Name}%gy%]".TagColors(), instance);
    }
    string instancesRpt = string.Join(", ", instances.Select(x=>$"%yel%{x?.name ?? "<null>"}%gy%"));
    Debug.Log($"  |  Instances found: %wh%[{instancesRpt}%wh%] ".TagColors());
    if (instances.Length > 1) {
      CleanupExtraInstances(instances, instance);
    }
    return instance;
  }
  //-------------------------- |||||||||||||||||||||| ------------------------------------------------------
  private static MonoBehaviour EnsureInstanceInternal(System.Type compType) {
    // Ensure a GameObject _Singletons is present
    GameObject singletons = GameObject.Find("_Singletons") ?? new GameObject("_Singletons");
    // Add a new GameObject with the desired component
    GameObject singleton = new GameObject($"[{compType.Name}]", compType);
    singleton.transform.SetParent(singletons.transform);
    MonoBehaviour instance = singleton.GetComponent(compType) as MonoBehaviour;
    // Debug.Log($"%mg%[Singletoner]%gy% An instance of {compType.Name} is needed in the scene, so '{singleton.name}' was created.".TagColors(), instance);
    #if UNITY_EDITOR
      if (!Application.isPlaying) {
        // mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(singleton.scene);
      }
    #endif
    return instance as MonoBehaviour;
  }
  //----------------- ||||||||||||||||||||| -----------------------------------------------------------
  private static void CleanupExtraInstances(MonoBehaviour[] instances, MonoBehaviour keepInstance) {
    // Debug.LogWarning($"[Singletoner] Multiple instances of {keepInstance.GetType().Name} found. Keeping one instance and destroying others.");
    foreach (var instance in instances) {
      if (instance != keepInstance) {
        #if UNITY_EDITOR
          if (!Application.isPlaying) {
            Object.DestroyImmediate(instance.gameObject);
          } else {
            Object.Destroy(instance.gameObject);
          }
        #else
          Object.Destroy(instance.gameObject);
        #endif
      }
    }
  }
}