// SynqPhysics_Mgr.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Multisynq {

  //========== ||||||||||||||| ==================================
  public class SynqPhysics_Mgr : JsPlugin_Behaviour {
    private Dictionary<uint, SynqBehaviour> collidersByNetId = new();
    new static public string[] CsCodeMatchesToNeedThisJs() => new string[0];
    
    new static public Type[] BehavioursThatNeedThisJs() => new [] { 
      typeof(Rigidbody),
      typeof(Collider), 
      // typeof(SphereCollider),
      // typeof(BoxCollider),
      // typeof(CapsuleCollider)
    };

    new void Start() {
      base.Start();
      Croquet.Subscribe("collider", "setup", OnSetup);
      Croquet.Subscribe("collider", "collided",  OnCollided    );
      // crawl through all the Colliders in the scene and setup them
      foreach (var collider in FindObjectsOfType<Collider>()) {
        SetupCollider(collider.gameObject);
      }
    }

    new static public JsPluginCode GetJsPluginCode() {
      return new(
        pluginName: "SynqPhysics_Mgr",
        pluginExports: new[] {"SynqPhysics_Mgr_Model", "SynqPhysics_Mgr_View"},
        pluginCode: @"
          import { Actor, AM_Spatial, mix } from '@croquet/worldcore-kernel';
          import { Model, View } from '@croquet/croquet';
          import { RapierManager, AM_RapierWorld, AM_RapierRigidBody, RAPIER } from '@croquet/worldcore-rapier';
          import { GameModelRoot, AM_InitializationClient } from '../../unity-js/src/game-support-models';

          function arr2V3(arr) { return new RAPIER.Vector3(arr[0], arr[1], arr[2]) }
          function arr2Q(arr)  { return new RAPIER.Quaternion(arr[0], arr[1], arr[2], arr[3]) }
          function arrHalfed3(arr) { const hds = arr.map(x => x * 0.5); return [hds[0], hds[1], hds[2]] }
          // function arr3(arr) { return [arr[0], arr[1], arr[2]] }
          // function arr4(arr) { return [arr[0], arr[1], arr[2], arr[3]] }

          /* @ts-ignore */
          export class SynqPhysics_Mgr_Model extends GameModelRoot {
            world;
            rigidBodies = new Map();
            colliders = new Map();
            eventQueue;

            static modelServices() {
              return [RapierManager, ...super.modelServices()];
            }

            init(options) {
              super.init(); //super.init(options);   options are disallowed in GameModelRoot???? Odd.
              this.subscribe('collider', 'setup', this.onSetup);
              console.log('+++[Start] SynqPhysics_Mgr_Model.init()');
              // RapierManager.create({});
              this.base = BaseActor.create({ gravity: [0, -12, 0], translation: [0, 0, 0] });
              console.log('   [End] SynqPhysics_Mgr_Model.init()');
            }

            onSetup(msg) {
              const [netId, colliderDataJson] = msg.split('|');
              const colliderData = JSON.parse(colliderDataJson);

              // Create a static rigid body for the collider
              const rbDesc = RAPIER.RigidBodyDesc.fixed();
              const pos = colliderData.position;
              const rot = colliderData.rotation;
              rbDesc.setTranslation(pos[0], pos[1], pos[2]);
              rbDesc.setRotation(rot);
              const rigidBody = this.world.createRigidBody(rbDesc);
              this.rigidBodies ??= new Map(); // ensure Map exists
              this.rigidBodies.set(netId, rigidBody);

              // Create the collider
              let colliderDesc;
              switch (colliderData.type) {
                case 'box':
                  const hs = arrHalfed3(colliderData.size); // halfSize
                  colliderDesc = RAPIER.ColliderDesc.cuboid(hs[0], hs[1], hs[2]);
                  break;
                case 'sphere':
                  colliderDesc = RAPIER.ColliderDesc.ball(colliderData.radius);
                  break;
                case 'capsule':
                  colliderDesc = RAPIER.ColliderDesc.capsule(
                    colliderData.height / 2,
                    colliderData.radius
                  );
                  break;
              }

              // Set collision properties
              colliderDesc?.setSensor(colliderData.isTrigger);
              if (colliderData.offset) {
                const ct = colliderData.offset;
                colliderDesc?.setTranslation(ct[0], ct[1], ct[2]);
              }

              // Store userData for collision identification
              colliderDesc?.setActiveEvents(RAPIER.ActiveEvents.COLLISION_EVENTS);

              const collider = this.world.createCollider(colliderDesc, rigidBody);
              collider?.setActiveCollisionTypes(RAPIER.ActiveCollisionTypes.ALL);

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
                  this.publish('collider', 'collided',
                    `${netId1}|${netId2}|${started ? 'enter' : 'exit'}`
                  );
                }
              });

              this.future(50).tick();
            }
          }
          SynqPhysics_Mgr_Model.register('SynqPhysics_Mgr_Model');

          export class SynqPhysics_Mgr_View extends View {
            constructor(model) {
              super(model);
              this.model = model;
            }
          }


          class BaseActor extends mix(Actor).with(AM_Spatial, AM_RapierWorld, AM_InitializationClient) {
            get pawn() { return 'BasePawn' }
            get gamePawnType() { return '' } // no Unity pawn

            init(options) {
              super.init(options);
              this.active = [];
              this.dynamics = new Set();
              this.versionBump = 0;
            }
          }
          BaseActor.register('BaseActor');

          export class PhysicsActor extends mix(Actor).with(AM_Spatial, AM_RapierRigidBody) {
            static okayToIgnore() { return [...super.okayToIgnore(), '$rigidBody'] }

            get pawn() { return 'GamePawn' } // if not otherwise specialised
            get gamePawnType() { return this._type } // Unity prefab to use
            get type() { return this._type || 'primitiveCube' }
            get color() { return this._color || [0.3, 1.0, 0.3] } // greenish
            get alpha() { return this._alpha === undefined ? 1 : this._alpha }

            init(options) {
              super.init(options);
              this.subscribe('physics', 'setup', this.onSetup);
            }

            onSetup(setupInfo) {
              // Configure RigidBody
              const rbd = this.createRigidBodyDesc(setupInfo.rb);
              this.rigidBodyHandle = this.worldActor.createRigidBody(this, rbd);

              // Configure Collider
              const cd = this.createColliderDesc(setupInfo.col);
              this.createCollider(cd);

              // Apply initial velocities if specified
              if (setupInfo.rb.linearVelocity) {
                this.rigidBody.setLinvel(arr2V3(setupInfo.rb.linearVelocity), true);
              }
              if (setupInfo.rb.angularVelocity) {
                this.rigidBody.setAngvel(arr2V3(setupInfo.rb.angularVelocity), true);
              }
            }

            createRigidBodyDesc(rbInfo) {
              let rbd;
              switch (rbInfo.type) {
                case 'static':
                  rbd = RAPIER.RigidBodyDesc.fixed();
                  break;
                case 'kinematic':
                  rbd = RAPIER.RigidBodyDesc.kinematicPositionBased();
                  break;
                default:
                  rbd = RAPIER.RigidBodyDesc.dynamic();
              }

              rbd?.setCcdEnabled(rbInfo.ccdEnabled);
              rbd?.setLinearDamping(rbInfo.linearDamping);
              rbd?.setAngularDamping(rbInfo.angularDamping);
              rbd.translation = arr2V3(rbInfo.translation);
              rbd.rotation    = arr2Q(rbInfo.rotation);

              return rbd;
            }

            createColliderDesc(colInfo) {
              let cd;
              switch (colInfo.type) {
                case 'sphere':
                  cd = RAPIER.ColliderDesc.ball(colInfo.dimensions[0]);
                  break;
                case 'capsule':
                  cd = RAPIER.ColliderDesc.capsule(colInfo.dimensions[1], colInfo.dimensions[0]);
                  break;
                default:
                  const hds = arrHalfed3(colInfo.dimensions);
                  cd = RAPIER.ColliderDesc.cuboid(hds[0], hds[1], hds[2]);
              }

              cd.setDensity(colInfo.density);
              cd.setFriction(colInfo.friction);
              cd.setRestitution(colInfo.restitution);
              cd.setSensor(colInfo.isTrigger);

              if (colInfo.center) {
                const ct = colInfo.center;
                cd.setTranslation(ct[0], ct[1], ct[2]);
              }

              return cd;
            }
          }
          PhysicsActor.register('PhysicsActor');
        ".LessIndent()
      );
    }

    private void OnSetup(string msg) {
      // Register collider when setupd
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

    private void OnCollided(string msg) {
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
      var sb = go.EnsureComp<SynqBehaviour>();
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
      Croquet.Publish("collider", "setup", initMsg);
    }

    #region Singleton
    private static SynqPhysics_Mgr _Instance;
    public static SynqPhysics_Mgr I {
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
    #endregion
  } // class SynqPhysics_Mgr

  // Optional helper component to automatically set up collider
  // [RequireComponent(typeof(Collider), typeof(SynqBehaviour))]
  // public class SynqPhysics : MonoBehaviour {
  //   void Start() {
  //     SynqPhysics_Mgr.SetupCollider(gameObject);
  //   }
  // }
}