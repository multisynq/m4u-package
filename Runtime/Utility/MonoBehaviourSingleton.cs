using UnityEngine;


public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T> {
  protected static T s_Instance = null;

  protected static bool s_ShuttingDown = false;
	
	public static T Instance {
    get {
      CreateInstance();
      return s_Instance;
    }
  }
	public static T I {
    get { return Instance; }
  }
	
	public static void CreateInstance() {
		if (s_ShuttingDown) {
			return;
		}
		
    if (s_Instance == null) {
      // first try to find T anywhere in scene
      s_Instance = GameObject.FindObjectOfType<T>();

      if (s_Instance == null) {
        // find the singleton hub
        GameObject hub = GameObject.Find("_Singletons");
        if (hub == null) {
          hub = new GameObject();
          hub.name = "_Singletons";
          // if not in editor, don't destroy on load
          if (!Application.isEditor) {
            DontDestroyOnLoad(hub);
          }
        }
        // create the singleton on _Singletons
        s_Instance = hub.AddComponent<T>();
      }
    }
	}
	
	public static void DestroyInstance() {
		if (s_Instance == null) {
			return;
		}
		s_Instance = null;
		UnityEngine.Object.Destroy(s_Instance);
	}
	
  public static bool InstanceExists
  {
    get { return s_Instance != null; }
  }

  protected virtual void Awake() {
    if (s_Instance == null) {
      s_Instance = this as T;
    }
  }
	
	protected void OnDestroy() {
		if (this == s_Instance) {
			s_Instance = null;
		}
	}

  protected void OnApplicationQuit() {
    s_ShuttingDown = true;
  }
}