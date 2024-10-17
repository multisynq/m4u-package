using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Multisynq {


/// <summary>
/// Handles Creation and Destruction of Objects.
/// Maintains the mapping between the model and the view objects.
/// </summary>
public class  Mq_Entity_System : Mq_System {
  public bool mq_Entity_System;

  private Dictionary<string, GameObject> addressableAssets = new(); // manages preloading the addressableAssets
  private string assetScene = ""; // the scene for which we've loaded the assets
  private int assetLoadKey = 0; // to distinguish the asynchronous loads
  public string assetManifestString;

  public bool addressablesReady = false; // make public read or emit event to inform other systems that the assets are loaded
  private Dictionary<int, int> CroquetHandleToInstanceID = new Dictionary<int, int>();

  public override List<string> KnownCommands { get; } = new List<string>() {
    "makeObject", "destroyObject"
  };
  protected override Dictionary<int, Mq_Comp> components { get; set; } =
    new Dictionary<int, Mq_Comp>();
  public static Mq_Entity_System Instance { get; private set; }
  
  private void Awake() {
    if (Instance != null && Instance != this) Destroy(this); // If there is an instance, and it's not me, delete myself.
    else Instance = this; // Singleton Accessor
  }

  private void AssociateCroquetHandleToInstanceID(int croquetHandle, int id) {
    CroquetHandleToInstanceID.Add(croquetHandle, id);
  }

  private void DisassociateCroquetHandleToInstanceID(int croquetHandle) {
    CroquetHandleToInstanceID.Remove(croquetHandle);
  }

  /// <summary>
  /// Get GameObject with a specific Croquet Handle
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public GameObject GetGameObjectByCroquetHandle(int croquetHandle) {
    Mq_Comp croquetComponent;

    if (CroquetHandleToInstanceID.ContainsKey(croquetHandle)) {
      int instanceID = CroquetHandleToInstanceID[croquetHandle];
      if (components.TryGetValue(instanceID, out croquetComponent)) {
        return croquetComponent.gameObject;
      }
    }

    // Debug.Log($"Failed to find object {croquetHandle}");
    return null;
  }

  public static int GetInstanceIDByCroquetHandle(int croquetHandle) {
    GameObject go = Instance.GetGameObjectByCroquetHandle(croquetHandle);
    if (go != null) {
      return go.GetInstanceID();
    }

    return 0; // TODO: remove sentinel in favor of unwrapping optional
  }

  private void Start() {
  }

  public override void LoadedScene(string sceneName) {
    base.LoadedScene(sceneName);

    // this is sent *after* switching the scene
    if (sceneName == assetScene) return; // already loaded (or being searched for)

    assetScene = sceneName;
    addressablesReady = false;
    assetLoadKey++;
    StartCoroutine(LoadAddressableAssetsWithLabel(sceneName)); // NB: used to be the appName (despite what our docs said)
  }

  public override bool ReadyToRunScene(string sceneName) {
    return assetScene == sceneName && addressablesReady;
  }

  public override void TearDownScene() {
    // destroy everything in the scene, in preparation either for rebuilding the same scene after
    // a connection glitch or for loading/reloading due to a requested scene change.

    List<Mq_Comp> componentsToDelete = components.Values.ToList();
    foreach (Mq_Comp component in componentsToDelete) {
      Mq_Entity_Comp entityComponent = component as Mq_Entity_Comp;
      if (entityComponent != null) {
        DestroyObject(entityComponent.croquetHandle);
      }
    }

    base.TearDownScene();
  }

  public List<GameObject> UninitializedObjectsInScene() {
    List<GameObject> needingInit = new List<GameObject>();
    foreach (Mq_Comp c in components.Values) {
      Mq_Entity_Comp ec = c as Mq_Entity_Comp;
      if (ec.croquetHandle.Equals(-1)) {
        needingInit.Add(ec.gameObject);
      }
    }

    return needingInit;
  }

  IEnumerator LoadAddressableAssetsWithLabel(string sceneName) {
    // LoadAssetsAsync throws an error - asynchronously - if there are
    // no assets that match the key.  One way to avoid that error is to run
    // the following code to get a list of locations matching the key.
    // If the list is empty, don't run the LoadAssetsAsync.

    int key = assetLoadKey; // if a new load is started while we're processing, we'll abandon this one

    List<string> labels = new List<string>() { "default", sceneName };

    //Returns IResourceLocations that are mapped to any of the supplied labels
    AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(
      labels,
      Addressables.MergeMode.Union);
    yield return handle;

    if (key != assetLoadKey) yield break; // scene has changed while assets were being found

    IList<IResourceLocation> result = handle.Result;
    int prefabs = 0;
    foreach (var loc in result) {
      if (loc.ToString().EndsWith(".prefab")) prefabs++;
    }

    Addressables.Release(handle);

    if (prefabs != 0) {
      // Load any assets labelled with this appName from the Addressable Assets
      Addressables.LoadAssetsAsync<GameObject>(
      labels,
      o => { },
      Addressables.MergeMode.Union).Completed += objects => {
        // check again that the scene hasn't been changed during the async operation
        if (key == assetLoadKey) {
          addressableAssets.Clear(); // now that we're ready to fill it
          foreach (var go in objects.Result) {
            Mq_ActorManifest manifest = go.GetComponent<Mq_ActorManifest>();
            if (manifest != null) {
              string assetName = manifest.pawnType;
              Debug.Log($"Loaded asset for {assetName} pawnType");
              addressableAssets.Add(assetName, go);
            }
          }

          addressablesReady = true;
          // prepare this now, because trying within the Socket's OnOpen
          // fails.  presumably a thread issue.
          assetManifestString = AssetManifestsAsString();
        }
      };
    }
    else {
      Debug.Log($"No addressable assets are labeled '{sceneName}'");
      addressablesReady = true;
    }
  }

  public string AssetManifestsAsString() {
    // we expect each addressable asset to have an attached CroquetActorManifest, that contains
    //    string[] mixins;
    //    string[] staticProperties;
    //    string[] watchedProperties;

    // here we build a single string that combines all assets' manifest properties.
    // arbitrarily, the string format is
    //   assetName1:mixinsList1:staticsList1:watchedList1:assetName2:mixinsList2:...
    // where ':' is in fact \x03, and the lists are comma-separated

    List<string> allManifests = new List<string>();
    foreach (KeyValuePair<string, GameObject> kv in Instance.addressableAssets) {
      GameObject asset = kv.Value;
      Mq_ActorManifest manifest = asset.GetComponent<Mq_ActorManifest>();
      if (manifest != null) {
        List<string> oneAssetStrings = new() {
          kv.Key, // asset name
          string.Join(',', manifest.mixins),
          string.Join(',', manifest.staticProperties),
          string.Join(',', manifest.watchedProperties)
        };
        allManifests.Add(string.Join('\x03', oneAssetStrings.ToArray()));
      }
    }

    string result = allManifests.Count == 0 ? "" : string.Join('\x03', allManifests.ToArray());
    return result;
  }

  public override void ProcessCommand(string command, string[] args) {
    if (command.Equals("makeObject")) {
      MakeObject(args);
    }
    else if (command.Equals("destroyObject")) {
      DestroyObject(int.Parse(args[0]));
    }
  }

  void MakeObject(string[] args) {
    // Debug.Log($"Making object {args[0]}");
    ObjectSpec spec = JsonUtility.FromJson<ObjectSpec>(args[0]);
    // Debug.Log($"making object {spec.cH}");

    // try to find a prefab with the given name
    GameObject gameObjectToMake;
    if (spec.type.StartsWith("primitive")) {
      PrimitiveType primType = PrimitiveType.Cube;
      if (spec.type == "primitiveSphere") primType = PrimitiveType.Sphere;
      else if (spec.type == "primitiveCapsule") primType = PrimitiveType.Capsule;
      else if (spec.type == "primitiveCylinder") primType = PrimitiveType.Cylinder;
      else if (spec.type == "primitivePlane") primType = PrimitiveType.Plane;

      gameObjectToMake = CreateCroquetPrimitive(primType, Color.blue);
    }
    else {
      if (addressableAssets.ContainsKey(spec.type)) {
        gameObjectToMake = Instantiate(addressableAssets[spec.type]);
      }
      else {
        Debug.Log( $"Specified spec.type ({spec.type}) is not found as a prefab! Creating Cube as Fallback Object");
        gameObjectToMake = CreateCroquetPrimitive(PrimitiveType.Cube, Color.magenta);
      }
    }

    if (gameObjectToMake.GetComponent<Mq_Entity_Comp>() == null){
      gameObjectToMake.AddComponent<Mq_Entity_Comp>();
    }

    Mq_Entity_Comp entity = gameObjectToMake.GetComponent<Mq_Entity_Comp>();
    entity.croquetHandle = spec.cH;
    int instanceID = gameObjectToMake.GetInstanceID();
    AssociateCroquetHandleToInstanceID(spec.cH, instanceID);

    // croquetName (actor.id)
    if (spec.cN != "") {
      entity.croquetActorId = spec.cN;
      Mq_Bridge.Instance.FixUpEarlyListens(gameObjectToMake, entity.croquetActorId);
    }

    // allComponents
    if (spec.cs != "") {
      string[] comps = spec.cs.Split(',');
      foreach (string compName in comps) {
        try {
          Type typeToAdd = Type.GetType("Multisynq." + compName);
          if (typeToAdd == null) {
            string assemblyQualifiedName =
              System.Reflection.Assembly.CreateQualifiedName("Assembly-CSharp", "Multisynq." + compName);
            typeToAdd = Type.GetType(assemblyQualifiedName);
          }
          if (typeToAdd == null) {
            // blew it
            Debug.LogError($"Unable to find component {compName} in package or main assembly");
          }
          else {
            if (gameObjectToMake.GetComponent(typeToAdd) == null) {
              // Debug.Log($"adding component {typeToAdd}");
              gameObjectToMake.AddComponent(typeToAdd);
            }
          }
        }
        catch (Exception e) {
          Debug.LogError($"Error in adding component {compName}: {e}");
        }
      }
    }

    // propertyValues
    if (spec.ps.Length != 0) {
      // an array with pairs   propName1, propVal1, propName2,...
      string[] props = spec.ps;
      for (int i = 0; i < props.Length; i += 2) {
        SetPropertyValueString(entity, props[i], props[i + 1]);
      }
    }

    // watchers
    if (spec.ws.Length != 0) {
      foreach (string propName in spec.ws) {
        string eventName = propName + "Set";
        Croquet.Listen(gameObjectToMake, eventName, (string stringyVal) => {
          SetPropertyValueString(entity, propName, stringyVal);
        });
      }
    }

    // waitToPresent
    if (spec.wTP) {
      foreach (Renderer renderer in gameObjectToMake.GetComponentsInChildren<Renderer>()) {
        renderer.enabled = false;
      }
    }

    foreach (IMq_Driven component in gameObjectToMake.GetComponents<IMq_Driven>()) {
      component.PawnInitializationComplete();
    }

    foreach (Mq_System system in Mq_Bridge.Instance.croquetSystems) {
      if (system.KnowsObject(gameObjectToMake)) {
        system.PawnInitializationComplete(gameObjectToMake);
      }
    }

    // confirmCreation
    if (spec.cC) {
      Mq_Bridge.Instance.SendToCroquet("objectCreated", spec.cH.ToString(), DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
    }

  }

  private void SetPropertyValueString(Mq_Entity_Comp entity, string propertyName, string stringyValue) {
    // @@ messy that this takes a component, while GetPropertyValueString takes
    // a game object.  but that is public, and this is private; around here we
    // know all about the components.

    // Debug.Log($"setting {propertyName} to {stringyValue}");
    entity.actorProperties[propertyName] = stringyValue;
    GameObject go = entity.gameObject;
    foreach (Mq_System system in Mq_Bridge.Instance.croquetSystems) {
      if (system.KnowsObject(go)) {
        system.ActorPropertySet(go, propertyName);
      }
    }
  }

  public bool HasActorSentProperty(GameObject gameObject, string propertyName) {
    Mq_Entity_Comp entity = components[gameObject.GetInstanceID()] as Mq_Entity_Comp;
    if (entity == null) {
      Debug.LogWarning($"failed to find Entity component for {gameObject}");
      return false;
    }

    StringStringSerializableDict properties = entity.actorProperties;
    return properties.ContainsKey(propertyName);
  }

  public string GetPropertyValueString(GameObject gameObject, string propertyName) {
    Mq_Entity_Comp entity = components[gameObject.GetInstanceID()] as Mq_Entity_Comp;
    if (entity == null) {
      Debug.LogWarning($"failed to find Entity component for {gameObject}");
      return null;
    }

    StringStringSerializableDict properties = entity.actorProperties;
    if (!properties.ContainsKey(propertyName)) {
      Debug.LogWarning($"failed to find property {propertyName} in {gameObject}");
      return "";
    }
    return properties[propertyName];
  }

  void DestroyObject(int croquetHandle) {
    // Debug.Log( "Destroying Object " + croquetHandle.ToString());

    if (CroquetHandleToInstanceID.ContainsKey(croquetHandle)) {
      int instanceID = CroquetHandleToInstanceID[croquetHandle];
      //components.Remove(instanceID);

      // INFORM OTHER COMPONENT'S SYSTEMS THEY ARE TO BE UNREGISTERED
      GameObject go = GetGameObjectByCroquetHandle(croquetHandle);
      Mq_Comp[] componentsToUnregister  = go.GetComponents<Mq_Comp>();
      foreach (var componentToUnregister in componentsToUnregister) {
        Mq_System system = componentToUnregister.croquetSystem;
        system.UnregisterComponent(componentToUnregister); //crosses fingers
      }


      Mq_Bridge.Instance.RemoveCroquetSubscriptionsFor(go);


      DisassociateCroquetHandleToInstanceID(croquetHandle);

      Destroy(go);
    }
    else {
      // asking to destroy a pawn for which there's no view can happen just because of
      // creation/destruction timing in worldcore.  not necessarily a problem.
      Debug.Log($"attempt to destroy absent object {croquetHandle}");
    }
  }

  GameObject CreateCroquetPrimitive(PrimitiveType type, Color color) {
    GameObject go = new GameObject();
    go.name = $"primitive{type.ToString()}";
    go.AddComponent<Mq_Entity_Comp>();
    GameObject inner = GameObject.CreatePrimitive(type);
    inner.transform.parent = go.transform;
    return go;
  }
}

[System.Serializable]
public class ObjectSpec {
  public int cH; // handle used by this client's Croquet bridge to address this object
  public string cN; // Croquet name (generally, the model id)
  public bool cC; // confirmCreation: whether Croquet is waiting for a confirmCreation message for this
  public bool wTP; // waitToPresent:  whether to make visible immediately
  public string type;
  public string cs; // comma-separated list of extra components
  public string[] ps; // actor properties and their values
  public string[] ws; // actor properties to be watched
}

public interface IMq_Driven {
  void PawnInitializationComplete();
}

}