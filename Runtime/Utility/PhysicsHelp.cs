using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PhysicsSetupInfo {
  public RigidBodyInfo rb = new();
  public ColliderInfo col = new();

  [System.Serializable]
  public class RigidBodyInfo {
    public string type = "dynamic";
    public float mass = 1.0f;
    public float linearDamping = 0.0f;
    public float angularDamping = 0.05f;
    public bool useGravity = true;
    public bool kinematic = false;
    public bool ccdEnabled = true;
    public float[] translation = new float[3];
    public float[] rotation = new float[4];
    public float[] linearVelocity = new float[3];
    public float[] angularVelocity = new float[3];
  }

  [System.Serializable]
  public class ColliderInfo {
    public string type = "box";
    public bool isTrigger = false;
    public float[] dimensions = new float[3];
    public float[] center = new float[3];
    public float friction = 0.5f;
    public float restitution = 0.2f;
    public float density = 1.0f;
  }
}

public static class PhysicsSetupInfoExtensions {
  public static PhysicsSetupInfo AsPhysicsSetupInfo(this GameObject gob) {
    var info = new PhysicsSetupInfo();
    var rb = gob.GetComponent<Rigidbody>();
    var col = gob.GetComponent<Collider>();

    if (rb) {
      info.rb.mass = rb.mass;
      info.rb.linearDamping = rb.drag;
      info.rb.angularDamping = rb.angularDrag;
      info.rb.useGravity = rb.useGravity;
      info.rb.kinematic = rb.isKinematic;
      info.rb.type = rb.isKinematic ? "kinematic" : (rb.mass == 0 ? "static" : "dynamic");
      
      var pos = rb.position;
      info.rb.translation = new[] { pos.x, pos.y, pos.z };
      
      var rot = rb.rotation;
      info.rb.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
      
      var vel = rb.velocity;
      info.rb.linearVelocity = new[] { vel.x, vel.y, vel.z };
      
      var angVel = rb.angularVelocity;
      info.rb.angularVelocity = new[] { angVel.x, angVel.y, angVel.z };
    }

    if (col) {
      info.col.isTrigger = col.isTrigger;
      var center = col.bounds.center - gob.transform.position;
      info.col.center = new[] { center.x, center.y, center.z };

      switch (col) {
        case BoxCollider box:
          info.col.type = "box";
          info.col.dimensions = new[] { box.size.x, box.size.y, box.size.z };
          break;
          
        case SphereCollider sphere:
          info.col.type = "sphere";
          info.col.dimensions = new[] { sphere.radius, 0, 0 };
          break;
          
        case CapsuleCollider capsule:
          info.col.type = "capsule";
          info.col.dimensions = new[] { capsule.radius, capsule.height, 0 };
          break;
      }

      if (col.sharedMaterial) {
        info.col.friction = col.sharedMaterial.dynamicFriction;
        info.col.restitution = col.sharedMaterial.bounciness;
      }
    }

    return info;
  }

  public static string AsJsonOfPhysicsSetupInfo(this GameObject gob) {
    return JsonUtility.ToJson(gob.AsPhysicsSetupInfo(), true);
  }

  // find the HashSet of all GameObjects with either a Collider, a RigidBody, or both
  public static HashSet<GameObject> FindAllGameObjectsWithPhysics() {
    var justColliders   = Object.FindObjectsOfType<Collider>().Select(c => c.gameObject).ToHashSet();
    var justRigidBodies = Object.FindObjectsOfType<Rigidbody>().Select(rb => rb.gameObject).ToHashSet();
    return justColliders.Union(justRigidBodies).ToHashSet();
  }

  // PhysicsSetupInfo[] of all GameObjects with either a Collider, a RigidBody, or both
  public static PhysicsSetupInfo[] ScenePhysicsSetupInfos() {
    var gobSet = FindAllGameObjectsWithPhysics();
    return gobSet.Select(gob => gob.AsPhysicsSetupInfo()).ToArray();
  }
  // json of those PhysicsSetupInfos
  public static string ScenePhysicsSetupInfosAsJson() {
    return JsonUtility.ToJson(ScenePhysicsSetupInfos(), true);
  }
}