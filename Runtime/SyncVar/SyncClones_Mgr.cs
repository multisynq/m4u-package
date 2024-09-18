using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SyncClones_Mgr : JsPluginInjecting_Behaviour {

  private Dictionary<int, SyncBehaviour> sbsByNetId = new();

  override public void Start() {
    base.Start();
    Croquet.Subscribe("SyncClone", "tellToClone", OnTellToInstance);
  }

  public override string JsPluginFileName() {
    return "plugins/SyncClones_Mgr_Model.js";
  }

  public override string JsPluginCode() {
    return @"
      import { Model, View } from '@croquet/croquet';

      export class SyncClones_Mgr_Model extends Model {
        init(options) {
          super.init(options);
          this.subscribe('SyncClone', 'askToClone', this.onAskForInstance);
          console.log('<color=yellow>[JS]</color> <color=magenta>SyncClones_Mgr_Model.init()</color>');
        }
        
        onAskForInstance(data) {
          console.log('<color=blue>SyncClone</color> <color=yellow>[JS]</color> <color=magenta>SyncClones_Mgr_Model.onAskForInstance()</color><color=cyan>' + data + '</color>');
          this.publish('SyncClone', 'tellToClone', data);
        }
      }
      SyncClones_Mgr_Model.register('SyncClones_Mgr_Model');

      export class SyncClones_Mgr_View extends View {
        constructor(model) {
          super(model);
          this.model = model;
        }
      }
    ".LessIndent();
  }

  //----------------------------------------- ||||||||||||||||| ----------------------
  static public (GameObject, SyncBehaviour) SyncClone(GameObject gob) {
    var sb = gob.EnsureComp<SyncBehaviour>();
    if (sb.netId == 0) sb.MakeNewId();
    return SyncClone(sb);
  }
  //----------------------------------------- ||||||||||||||||| ----------------------
  static public (GameObject, SyncBehaviour) SyncClone(SyncBehaviour sb=null) {
    int      cloneMeNetId = sb.netId;
    GameObject      clone = Instantiate(sb.gameObject);
    SyncBehaviour newSb = clone.EnsureComp<SyncBehaviour>();
    int      madeOneNetId = newSb.MakeNewId();

    Vector3    position = clone.transform.position;
    Quaternion rotation = clone.transform.rotation;
    Vector3    scale    = clone.transform.localScale;

    string msg = $"{cloneMeNetId}|{madeOneNetId}|{position.x},{position.y},{position.z}|{rotation.x},{rotation.y},{rotation.z},{rotation.w}|{scale.x},{scale.y},{scale.z}";
    Croquet.Publish("SyncClone", "askToClone", msg);
    Debug.Log($"SyncClone, askToClone, %cy%{msg}".TagColors());
    return (clone, newSb);
  }
  //---------- |||||||||||||||| ----------------------
  private void OnTellToInstance(string msg) {
    string[] parts = msg.Split('|');
    if (parts.Length != 5) {
      Debug.LogError($"SyncInstance_Mgr.OnTellToInstance() Invalid message: {msg}");
      return;
    }
    Debug.Log($"SyncClone, tellToClone, %cy%{msg}".TagColors());

    int cloneMeNetId    = int.Parse(      parts[0]);
    int madeOneNetId    = int.Parse(      parts[1]);
    Vector3    position = ParseVector3(   parts[2]);
    Quaternion rotation = ParseQuaternion(parts[3]);
    Vector3       scale = ParseVector3(   parts[4]);

    // check if already here
    SyncBehaviour madeSb = FindInDictOrOnOtherSyncedBehaviour(madeOneNetId);
    if (madeSb != null) {
      Debug.Log($"Already instantiated object. cloneMeNetId: {cloneMeNetId}, madeOneNetId: {madeOneNetId}");
      return;
    }
    SyncBehaviour cloneMeSb = FindInDictOrOnOtherSyncedBehaviour(cloneMeNetId);
    if (cloneMeSb != null) {
      GameObject instance = Instantiate(cloneMeSb.gameObject, position, rotation);
      instance.transform.localScale = scale;

      SyncBehaviour newSb = instance.EnsureComp<SyncBehaviour>();
      newSb.netId = madeOneNetId; // Manually set the netId because the other networked object has this netId
      RegisterPrefab(instance);

      Debug.Log($"Remotely instantiated object. cloneMeNetId: {cloneMeNetId}, madeOneNetId: {madeOneNetId}");
    }
    else {
      Debug.LogError($"Prefab not found for cloneMeNetId: {cloneMeNetId}");
    }
  }
  //--------- |||||||||||||| ----------------------
  public void RegisterSyncedBehaviour(SyncBehaviour sb) {
    RegisterPrefab(sb.gameObject);
  }

  //--------- |||||||||||||| ----------------------
  public void RegisterPrefab(GameObject prefab) {
    SyncBehaviour syncBehaviour = prefab.EnsureComp<SyncBehaviour>();
    int netId = syncBehaviour.netId;
    if (!sbsByNetId.ContainsKey(netId)) {
      sbsByNetId[netId] = syncBehaviour;
      Debug.Log($"Registered prefab with netId: {netId}");
    }
  }
  
  //--------------------- |||||||||||||||||||||||||||||||||| ----------------------
  private SyncBehaviour FindInDictOrOnOtherSyncedBehaviour( int netId) {
    if (sbsByNetId.TryGetValue(netId, out SyncBehaviour sb)) {
      return sb;
    }
    else {
      return FindObjectsOfType<SyncBehaviour>().FirstOrDefault(sb => sb.netId == netId);
    }
  }



  private Vector3 ParseVector3(string data) {
    string[] parts = data.Split(',');
    return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
  }

  private Quaternion ParseQuaternion(string data) {
    string[] parts = data.Split(',');
    return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
  }

  #region Singleton
    public static SyncClones_Mgr I {
      get {
        _Instance = Singletoner.EnsureInst(_Instance);
        return _Instance;
      }
      private set { _Instance = value; }
    }
    private static SyncClones_Mgr _Instance;
  #endregion
}