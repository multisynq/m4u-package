// SynqCollider_Mgr.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Multisynq {

  //========== |||||||||||||||| ==================================
  public class SynqCollider_Mgr : JsPlugin_Behaviour {
    private Dictionary<uint, SynqBehaviour> collidersByNetId = new();
    new static public string[] CsCodeMatchesToNeedThisJs() => new[] { @"SynqCollider_Mgr.*Collider", @"\[SyncedCollider\]" };
    
    new static public Type[] BehavioursThatNeedThisJs() => new [] { 
      typeof(Rigidbody),
      typeof(Collider), 
      // typeof(SphereCollider),
      // typeof(BoxCollider),
      // typeof(CapsuleCollider)
    };

    new void Start() {
      base.Start();
      Croquet.Subscribe("collider", "initialize", OnColliderInitialize);
      Croquet.Subscribe("collider", "collision",  OnCollisionEvent    );
      // crawl through all the Colliders in the scene and initialize them
      foreach (var collider in FindObjectsOfType<Collider>()) {
        SetupCollider(collider.gameObject);
      }
    }

    new static public JsPluginCode GetJsPluginCode() {
      return new(
        pluginName: "SynqCollider_Mgr",
        pluginExports: new[] {"SynqCollider_Mgr_Model", "SynqCollider_Mgr_View"},
        pluginCode: @"
          import { Model, View } from '@croquet/croquet';
          import { RAPIER } from '@croquet/worldcore-rapier';

          export class SynqCollider_Mgr_Model extends Model {
            init(options) {
              super.init(options);
              this.subscribe('collider', 'initialize', this.onInitialize);

              // Initialize Rapier world if not already done (static only for collisions)
              if (!this.world) {
                this.world = new RAPIER.World(new RAPIER.Vector3(0, 0, 0));
                this.colliders = new Map();
                this.rigidBodies = new Map();
                this.eventQueue = new RAPIER.EventQueue(true);
                this.future(50).tick();
              }
            }

            onInitialize(msg) {
              const [netId, colliderDataJson] = msg.split('|');
              const colliderData = JSON.parse(colliderDataJson);

              // Create a static rigid body for the collider
              const rbDesc = RAPIER.RigidBodyDesc.fixed();
              const pos = colliderData.position;
              const rot = colliderData.rotation;
              rbDesc.setTranslation(pos[0], pos[1], pos[2]);
              rbDesc.setRotation(rot);
              const rigidBody = this.world.createRigidBody(rbDesc);
              this.rigidBodies.set(netId, rigidBody);

              // Create the collider
              let colliderDesc;
              switch (colliderData.type) {
                case 'box':
                  const halfSize = colliderData.size.map(x => x/2);
                  colliderDesc = RAPIER.ColliderDesc.cuboid(...halfSize);
                  break;
                case 'sphere':
                  colliderDesc = RAPIER.ColliderDesc.ball(colliderData.radius);
                  break;
                case 'capsule':
                  colliderDesc = RAPIER.ColliderDesc.capsule(
                    colliderData.height/2,
                    colliderData.radius
                  );
                  break;
              }

              // Set collision properties
              colliderDesc.setSensor(colliderData.isTrigger);
              if (colliderData.offset) {
                colliderDesc.setTranslation(...colliderData.offset);
              }

              // Store userData for collision identification
              colliderDesc.setActiveEvents(RAPIER.ActiveEvents.COLLISION_EVENTS);
              
              const collider = this.world.createCollider(colliderDesc, rigidBody);
              collider.setActiveCollisionTypes(RAPIER.ActiveCollisionTypes.ALL);
              
              // Store netId in userData for collision events
              rigidBody.userData = { netId: netId };
              
              this.colliders.set(netId, collider);
              console.log('Created collider for netId:', netId);
            }

            tick() {
              if (!this.world) return;
              
              // Step the world (minimal step since we only care about collisions)
              this.world.step(this.eventQueue);

              // Process collision events
              this.eventQueue.drainCollisionEvents((handle1, handle2, started) => {
                const rb1 = this.world.getRigidBody(handle1.rigidBodyHandle());
                const rb2 = this.world.getRigidBody(handle2.rigidBodyHandle());
                
                if (rb1 && rb2 && rb1.userData && rb2.userData) {
                  const netId1 = rb1.userData.netId;
                  const netId2 = rb2.userData.netId;
                  
                  // Publish collision event
                  this.publish('collider', 'collision', 
                    `${netId1}|${netId2}|${started ? 'enter' : 'exit'}`
                  );
                }
              });

              this.future(50).tick();
            }
          }
          SynqCollider_Mgr_Model.register('SynqCollider_Mgr_Model');

          export class SynqCollider_Mgr_View extends View {
            constructor(model) {
              super(model);
              this.model = model;
            }
          }
        ".LessIndent()
      );
    }

    private void OnColliderInitialize(string msg) {
      // Register collider when initialized
      string[] parts = msg.Split('|');
      if (parts.Length != 2) return;
      
      uint netId = uint.Parse(parts[0]);
      var colliderGo = FindObjectsOfType<SynqBehaviour>().ToList()
        .FirstOrDefault(sb => sb.netId == netId)?.gameObject;
      
      if (colliderGo != null) {
        var syncBehaviour = colliderGo.GetComponent<SynqBehaviour>();
        collidersByNetId[netId] = syncBehaviour;
      }
    }

    private void OnCollisionEvent(string msg) {
      string[] parts = msg.Split('|');
      if (parts.Length != 3) return;

      uint netId1 = uint.Parse(parts[0]);
      uint netId2 = uint.Parse(parts[1]);
      string eventType = parts[2]; // "enter" or "exit"

      // Find the GameObjects involved
      if (collidersByNetId.TryGetValue(netId1, out var sb1) && 
        collidersByNetId.TryGetValue(netId2, out var sb2)) {
        
        // Dispatch collision events to any listeners
        if (eventType == "enter") {
          sb1.SendMessage("OnSynCollisionEnter", sb2.gameObject, SendMessageOptions.DontRequireReceiver);
          sb2.SendMessage("OnSynCollisionEnter", sb1.gameObject, SendMessageOptions.DontRequireReceiver);
        } else {
          sb1.SendMessage("OnSynCollisionExit", sb2.gameObject, SendMessageOptions.DontRequireReceiver);
          sb2.SendMessage("OnSynCollisionExit", sb1.gameObject, SendMessageOptions.DontRequireReceiver);
        }
      }
    }

    // Helper component to set up a collider
    public static void SetupCollider(GameObject go) {
      var sb = go.GetComponent<SynqBehaviour>();
      if (sb == null) {
        Debug.LogError("GameObject must have SynqBehaviour to set up collider");
        return;
      }

      var collider = go.GetComponent<Collider>();
      if (collider == null) {
        Debug.LogError("GameObject must have a Collider component");
        return;
      }

      var colliderData = new Dictionary<string, object>();
      
      // Get transform data
      colliderData["position"] = new[] { 
        go.transform.position.x,
        go.transform.position.y,
        go.transform.position.z 
      };
      colliderData["rotation"] = new[] {
        go.transform.rotation.x,
        go.transform.rotation.y,
        go.transform.rotation.z,
        go.transform.rotation.w
      };

      // Get collider data
      if (collider is BoxCollider box) {
        colliderData["type"] = "box";
        colliderData["size"] = new[] { 
          box.size.x * go.transform.lossyScale.x,
          box.size.y * go.transform.lossyScale.y,
          box.size.z * go.transform.lossyScale.z
        };
        colliderData["offset"] = new[] { 
          box.center.x, box.center.y, box.center.z 
        };
      }
      else if (collider is SphereCollider sphere) {
        colliderData["type"] = "sphere";
        colliderData["radius"] = sphere.radius * Mathf.Max(
          go.transform.lossyScale.x,
          go.transform.lossyScale.y,
          go.transform.lossyScale.z
        );
        colliderData["offset"] = new[] { 
          sphere.center.x, sphere.center.y, sphere.center.z 
        };
      }
      else if (collider is CapsuleCollider capsule) {
        colliderData["type"] = "capsule";
        colliderData["radius"] = capsule.radius * Mathf.Max(
          go.transform.lossyScale.x,
          go.transform.lossyScale.z
        );
        colliderData["height"] = capsule.height * go.transform.lossyScale.y;
        colliderData["offset"] = new[] { 
          capsule.center.x, capsule.center.y, capsule.center.z 
        };
      }

      colliderData["isTrigger"] = collider.isTrigger;

      // Send initialization message to Croquet
      string initMsg = $"{sb.netId}|{JsonUtility.ToJson(colliderData)}";
      Croquet.Publish("collider", "initialize", initMsg);
    }

    #region Singleton
    private static SynqCollider_Mgr _Instance;
    public static SynqCollider_Mgr I {
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
    #endregion
  } // class SynqCollider_Mgr

  // Optional helper component to automatically set up collider
  // [RequireComponent(typeof(Collider), typeof(SynqBehaviour))]
  // public class SynqCollider : MonoBehaviour {
  //   void Start() {
  //     SynqCollider_Mgr.SetupCollider(gameObject);
  //   }
  // }
}