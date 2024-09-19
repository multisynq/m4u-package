
using UnityEngine;
using Multisynq;

public class SyncTransform: SyncBehaviour {

  [SyncVar] public Vector3    pos;
  [SyncVar] public Quaternion rot;
  [SyncVar] public Vector3    scl;

  Vector3    lastPos;
  Quaternion lastRot;
  Vector3    lastScl;

  float posEpsilon = 0.01f;
  float rotEpsilon = 0.01f;
  float scaleEpsilon = 0.01f;

  void Start() {
    pos   = transform.position;
    rot   = transform.rotation;
    scl   = transform.localScale;
    lastPos = pos;
    lastRot = rot;
    lastScl = scl;
  }

  void Update() {

    if (      Vector3.SqrMagnitude(pos  -           lastPos) > posEpsilon  ) {
      transform.position =         pos;
      lastPos =                    pos;
    } else if(Vector3.SqrMagnitude(pos  - transform.position) > posEpsilon) {
      pos = transform.position;
    }

    if (      Quaternion.Angle(rot, lastRot) > rotEpsilon) {
      transform.rotation =     rot;
      lastRot =                rot;
    } else if(Quaternion.Angle(rot, transform.rotation) > rotEpsilon) {
      rot = transform.rotation;
    }

    if (      Vector3.SqrMagnitude(scl  -              lastScl) > scaleEpsilon) {
      transform.localScale =       scl;
      lastScl =                    scl;
    } else if(Vector3.SqrMagnitude(scl  - transform.localScale) > scaleEpsilon) {
      scl = transform.localScale;
    }

  }

}