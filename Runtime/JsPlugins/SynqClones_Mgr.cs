using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Multisynq {

//========== |||||||||||||| ==================================
public class SynqClones_Mgr : JsPlugin_Behaviour {
  #region Fields
    private Dictionary<uint, SynqBehaviour> sbsByNetId = new();

    new static public Type[] BehavioursThatNeedThisJs() => new[] {typeof(SynqClones)};
  #endregion
  //------------------ ||||| ----------------------
  override public void Start() {
    base.Start();
    Croquet.Subscribe("SynqClone", "everybodyClone", OnEverybodyClone);
  }
  #region JavaScript
    //---------------------------- ||||||||||||||| -------------------------
    new static public JsPluginCode GetJsPluginCode() {
      return new(
        pluginName: "SynqClones_Mgr",
        pluginExports: new[] {"SynqClones_Mgr_Model", "SynqClones_Mgr_View"},
        pluginCode: @"
          import { Model, View } from '@croquet/croquet';

          export class SynqClones_Mgr_Model extends Model { // ☭ - There is no I, only we (in the Model)
            cloneMsgs = []
            init(options) {
              super.init(options);
              this.subscribe('SynqClone', 'pleaseClone', this.onPleaseClone); // i.e. a bullet was made in Unity
              console.log(this.now(), '<color=yellow>[JS]</color> <color=magenta>SynqClones_Mgr_Model.init()</color>');
            }
            onPleaseClone(data) {
              console.log(this.now(), '<color=blue>SynqClone</color> <color=yellow>[JS]</color> <color=magenta>SynqClones_Mgr_Model.onAskForInstance()</color><color=cyan>' + data + '</color>');
              this.publish('SynqClone', 'everybodyClone', data); // i.e. tell everybody to see the bullet
              this.cloneMsgs.push(data);
            }
          }
          SynqClones_Mgr_Model.register('SynqClones_Mgr_Model');

          export class SynqClones_Mgr_View extends View {
            constructor(model) {
              super(model);
              this.model = model;
              globalThis.theGameEngineBridge.bulkPublishToUnity('SynqClone', 'everybodyClone', model.cloneMsgs);
            }

          }
        ".LessIndent()
      );
    }
  #endregion

  //--------------------------------------- ||||||||| ----------------------
  static public (GameObject, SynqBehaviour) SynqClone(GameObject gob) {
    var sb = gob.EnsureComp<SynqBehaviour>();
    if (sb.netId == 0) sb.MakeNewId();
    return SynqClone(sb);
  }
  //--------------------------------------- ||||||||| ----------------------
  static public (GameObject, SynqBehaviour) SynqClone(SynqBehaviour sb=null) {
    uint     cloneMeNetId = sb.netId;
    GameObject      clone = Instantiate(sb.gameObject);
    SynqBehaviour   newSb = clone.EnsureComp<SynqBehaviour>();
    uint     madeOneNetId = newSb.MakeNewId();

    Vector3    position = clone.transform.position;
    Quaternion rotation = clone.transform.rotation;
    Vector3    scale    = clone.transform.localScale;

    string msg = $"{cloneMeNetId}|{madeOneNetId}|{position.x},{position.y},{position.z}|{rotation.x},{rotation.y},{rotation.z},{rotation.w}|{scale.x},{scale.y},{scale.z}";
    Croquet.Publish("SynqClone", "pleaseClone", msg);
    Debug.Log($"SynqClone, pleaseClone, %cy%{msg}".TagColors());
    return (clone, newSb);
  }
  //---------- |||||||||||||||| ----------------------
  private void OnEverybodyClone(string msg) {
    string[] parts = msg.Split('|');
    if (parts.Length != 5) {
      Debug.LogError($"SynqInstance_Mgr.OnTellToInstance() Invalid message: {msg}");
      return;
    }
    Debug.Log($"SynqClone, everybodyClone, %cy%{msg}".TagColors());

    uint cloneMeNetId   = uint.Parse(     parts[0]);
    uint madeOneNetId   = uint.Parse(     parts[1]);
    Vector3    position = ParseVector3(   parts[2]);
    Quaternion rotation = ParseQuaternion(parts[3]);
    Vector3       scale = ParseVector3(   parts[4]);

    // check if already here
    SynqBehaviour madeSb = FindInDictOrOnOtherSynqBehaviour(madeOneNetId);
    if (madeSb != null) {
      Debug.Log($"Already instantiated object. cloneMeNetId: {cloneMeNetId}, madeOneNetId: {madeOneNetId}");
      return;
    }
    SynqBehaviour cloneMeSb = FindInDictOrOnOtherSynqBehaviour(cloneMeNetId);
    if (cloneMeSb != null) {
      //===========================================================
      GameObject instance = Instantiate(cloneMeSb.gameObject, position, rotation); // <<<<<<<<<<<<<<<<<<<<<
      //===========================================================
      instance.transform.localScale = scale;

      SynqBehaviour newSb = instance.EnsureComp<SynqBehaviour>();
      newSb.netId = madeOneNetId; // Manually set the netId because the other networked object has this netId
      RegisterPrefab(instance);

      Debug.Log($"Remotely instantiated object. cloneMeNetId: {cloneMeNetId}, madeOneNetId: {madeOneNetId}");
    } else {
      Debug.LogError($"Prefab not found for cloneMeNetId: {cloneMeNetId}");
    }
  }
  //--------- ||||||||||||||||||||| ----------------------
  public void RegisterSynqBehaviour(SynqBehaviour sb) {
    RegisterPrefab(sb.gameObject);
  }

  //--------- |||||||||||||| ----------------------
  public void RegisterPrefab(GameObject prefab) {
    SynqBehaviour syncBehaviour = prefab.EnsureComp<SynqBehaviour>();
    uint netId = syncBehaviour.netId;
    if (!sbsByNetId.ContainsKey(netId)) {
      sbsByNetId[netId] = syncBehaviour;
      Debug.Log($"Registered prefab with netId: {netId}");
    }
  }
  
  //------------------- |||||||||||||||||||||||||||||||| ----------------------
  private SynqBehaviour FindInDictOrOnOtherSynqBehaviour( uint netId) {
    if (sbsByNetId.TryGetValue(netId, out SynqBehaviour sb)) {
      return sb;
    } else {
      return FindObjectsOfType<SynqBehaviour>().FirstOrDefault(sb => sb.netId == netId);
    }
  }

  Vector3 Vector3_FromBytes(Byte[] bytes) {
    return new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
  }
  Byte[] Vector3_ToBytes(Vector3 vector) {
    Byte[] bytes = new Byte[12];
    BitConverter.GetBytes(vector.x).CopyTo(bytes, 0);
    BitConverter.GetBytes(vector.y).CopyTo(bytes, 4);
    BitConverter.GetBytes(vector.z).CopyTo(bytes, 8);
    return bytes;
  }
  Quaternion Quaternion_FromBytes(Byte[] bytes) {
    return new Quaternion(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8), BitConverter.ToSingle(bytes, 12));
  }
  Byte[] Quaternion_ToBytes(Quaternion quaternion) {
    Byte[] bytes = new Byte[16];
    BitConverter.GetBytes(quaternion.x).CopyTo(bytes, 0);
    BitConverter.GetBytes(quaternion.y).CopyTo(bytes, 4);
    BitConverter.GetBytes(quaternion.z).CopyTo(bytes, 8);
    BitConverter.GetBytes(quaternion.w).CopyTo(bytes, 12);
    return bytes;
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
    private static SynqClones_Mgr _Instance;
    public  static SynqClones_Mgr I { // Usage:   SynqClones_Mgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
  #endregion
    }

}