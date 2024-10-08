using UnityEngine;

namespace Multisynq {

[AddComponentMenu("Multisynq/Mq_Spatial_Comp")]
[SelectionBase]
public class  Mq_Spatial_Comp : Mq_Comp {
  public bool mq_Spatial_Comp;  // Helps tools resolve "missing Script" problems

  public override Mq_System croquetSystem { get; set; } = Mq_Spatial_System.Instance;

  [Header("Model Transform (read-only)")]
  public bool hasBeenPlaced = false;
  public bool hasBeenMoved = false;
  public Vector3 position = Vector3.zero;
  public Quaternion rotation = Quaternion.identity ;
  public Vector3 scale = Vector3.one;
  public bool viewOverride = false;
  [HideInInspector] public Vector3 stashedPosition = Vector3.zero; // for use during render-time adjustment
  [HideInInspector] public long lastGeometryUpdate;

  [Header("SCENE LAYOUT")]
  public int positionMaxDecimals = 4;
  public int rotationMaxDecimals = 6;
  public int scaleMaxDecimals = 4;

  [Header("OPTIONS")]
  public bool includeOnSceneInit = true;
  [Header("Smoothing")]
  public float positionSmoothTime = 0.25f; // used by SmoothDamp
  public float positionEpsilon = 0.01f;
  [HideInInspector] public Vector3 dampedVelocity = Vector3.zero; // used by SmoothDamp
  [Space(5)]
  public float rotationLerpPerFrame = 0.2f;
  public float rotationEpsilon = 0.01f;
  [Space(5)]
  public float scaleLerpPerFrame = 0.2f;
  public float scaleEpsilon = 0.01f;
  [Space(5)]
  [Header("Ballistic motion")]
  public float desiredLag = 0.05f; // seconds behind
  [HideInInspector] public float currentLag = 0f;
  [HideInInspector] public Vector3? ballisticVelocity = null;
  public float ballisticNudgeLerp = 0.2f; // adjustments to "ballistic" movement

  // public List<string> telemetry = new List<string>(); // for testing
  // public int telemetryDumpTrigger = -1;

  //TODO: lerpCurve support

}

}