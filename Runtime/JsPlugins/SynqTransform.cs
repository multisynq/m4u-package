using UnityEngine;
using Multisynq;

public class SynqTransform: SynqBehaviour {

  [SynqVar(hook=nameof(OnPos))] public Vector3    pos;
  [SynqVar(hook=nameof(OnRot))] public Quaternion rot;
  [SynqVar(hook=nameof(OnScl))] public Vector3    scl;

  Vector3    lastPos;
  Quaternion lastRot;
  Vector3    lastScl;

  float posEpsilon   = 0.001f;
  float rotEpsilon   = 0.001f;
  float scaleEpsilon = 0.001f;

  static public bool dbg = false;

  void Start() {
    pos = transform.position;
    rot = transform.rotation;
    scl = transform.localScale;
    lastPos = pos;
    lastRot = rot;
    lastScl = scl;
  }

  void Update() {

    if ( Vector3.SqrMagnitude(pos  -  transform.position  ) > posEpsilon   ) {
      pos = transform.position;
      if (dbg) Debug.Log($"pos={pos}");
    }

    if ( Quaternion.Angle(    rot,    transform.rotation  ) > rotEpsilon   ) {
      rot = transform.rotation;
      if (dbg) Debug.Log($"rot={rot}");
    }

    if ( Vector3.SqrMagnitude(scl  -  transform.localScale) > scaleEpsilon ) {
      scl = transform.localScale;
      if (dbg) Debug.Log($"scl={scl}");
    }

  }

  void OnPos(   Vector3 newPos) { // hook method called on changes to the field: pos
    transform.position = newPos;
    lastPos = newPos;
    if (dbg) Debug.Log($"OnPos({newPos})");
  }

  void OnRot(Quaternion newRot) { // hook method called on changes to the field: rot
    transform.rotation = newRot;
    lastRot = newRot;
    if (dbg) Debug.Log($"OnRot({newRot})");
  }

  void OnScl(   Vector3 newScl) { // hook method called on changes to the field: scl
    transform.localScale = newScl;
    lastScl = newScl;
    if (dbg) Debug.Log($"OnScl({newScl})");
  }

}