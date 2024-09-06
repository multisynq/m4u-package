using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
using System.Linq;


#region Attribute
//========== |||||||||||||||| ================
//========| [SyncVar] | ====================== C# Attribute
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SyncVarAttribute : Attribute { // C# Attribute
  // Usage options: 
  // [SyncVar] 
  // [SyncVar(CustomName = "shrtNm")] // Custom name for the variable, useful for shortening to reduce message size
  // [SyncVar(OnChangedCallback = "MethodNameOfClassWithTheVar")] // Method to call when the value changes
  // [SyncVar(MinSyncInterval = 0.5f)] // Minimum time between syncs in seconds
  // [SyncVar(CustomName = "myVar", OnChangedCallback = "MyMethod", MinSyncInterval = 0.5f)] // any combo of options
  public string CustomName { get; set; }
  public float updateInterval { get; set; } = 0.1f; // Minimum time between syncs in seconds
  public bool updateEveryInterval { get; set; } = false; // Normally only sync when the value has changed
  public string OnChangedCallback { get; set; } // Name of the method to call on the var's class when the value changes
}
#endregion

//========== |||||||||| ====================================
public class SyncVarMgr : JsCodeInjectingMonoBehavior {
  #region Fields
    private Dictionary<string, SyncVarInfo> syncVars;
    private SyncVarInfo[]                   syncVarsArr;
    static char msgSeparator = '|';
    static string svLogPrefix = "<color=#5555FF>[SyncVar]</color> ";
  #endregion

  #region JavaScript
  override public void InjectJsCode() {
    string fName = "Proxies/SyncVarActor.js";
    string classCode = @"
    class SyncVarActor extends Actor {
      get gamePawnType() { return '' }
      init(options) {
        super.init(options)
        this.subscribe('SyncVar', 'set1', this.syncVarChange)
      }
      syncVarChange(msg) {
        this.publish('SyncVar', 'set2', msg)
      }
    }
    SyncVarActor.register('SyncVarActor')";
    string initCode = "this.syncer = SyncVarActor.create({});\n";
    Debug.Log($"{svLogPrefix} {JsCodeInjectingMgr.logPrefix} '{fName}' '{initCode}' {classCode.Trim()}");
    
    JsCodeInjectingMgr.I.InjectCode(fName, classCode, initCode);
  }
  #endregion

  #region SubClasses
    // ------------------- ||||||||||| ---
    private abstract class SyncVarInfo {
      public readonly string varId;
      public readonly int varIdx;
      public readonly Func<object> Getter;
      public readonly Action<object> Setter;
      public readonly Action<object> onChangedCallback;
      public readonly SyncedBehaviour syncedBehaviour;
      public readonly Type varType;
      public readonly SyncVarAttribute attribute;
      public bool blockLoopySend = false;

      public object LastValue { get; set; }
      public bool ConfirmedInArr { get; set; }
      public float LastSyncTime { get; set; }

      protected SyncVarInfo(string varId, int varIdx, Func<object> getter, Action<object> setter,
                            SyncedBehaviour monoBehaviour, Type varType, SyncVarAttribute attribute,
                            object initialValue, Action<object> onChangedCallback) {
        this.varId = varId; this.varIdx = varIdx;
        Getter = getter; Setter = setter;
        syncedBehaviour = monoBehaviour;
        this.varType = varType; this.attribute = attribute;
        LastValue = initialValue; ConfirmedInArr = false;
        LastSyncTime = 0f;
        this.onChangedCallback = onChangedCallback;
      }
    }

    // ---------- ||||||||||||| ---
    private class SyncFieldInfo : SyncVarInfo {
      public readonly FieldInfo FieldInfo;

      public SyncFieldInfo(string fieldId, int fieldIdx, Func<object> getter, Action<object> setter,
                            SyncedBehaviour monoBehaviour, FieldInfo fieldInfo, SyncVarAttribute attribute,
                            object initialValue, Action<object> onChangedCallback)
          : base(fieldId, fieldIdx, getter, setter, monoBehaviour, fieldInfo.FieldType, attribute, initialValue, onChangedCallback) {
        FieldInfo = fieldInfo;
      }
    }

    // ---------- |||||||||||| ---
    private class SyncPropInfo : SyncVarInfo {
      public readonly PropertyInfo PropInfo;

      public SyncPropInfo(string propId, int propIdx, Func<object> getter, Action<object> setter,
                          SyncedBehaviour monoBehaviour, PropertyInfo propInfo, SyncVarAttribute attribute,
                          object initialValue, Action<object> onChangedCallback)
          : base(propId, propIdx, getter, setter, monoBehaviour, propInfo.PropertyType, attribute, initialValue, onChangedCallback) {
        PropInfo = propInfo;
      }
    }
  #endregion
  #region Start/Update
    //-- ||||| ---
    void Start() { // CroquetSynchVarMgr.Start()

      Croquet.Subscribe("SyncVar", "set2", ReceiveAsMsg);

      JsCodeInjectingMgr.I.InjectAllJsCode();

      syncVars = new Dictionary<string, SyncVarInfo>();
      List<SyncVarInfo> syncVarsList = new List<SyncVarInfo>();

      int varIdx = 0;
      #if UNITY_EDITOR
        foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>()) {
          // check for SyncVar attribute on fields of non-SyncedBehaviours
          var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          var properties = mb.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          // give errors for SyncVar attributes on non-SyncedBehaviours
          foreach (var field in fields) {
            var attribute = field.GetCustomAttribute<SyncVarAttribute>();
            if (attribute != null && !(mb is SyncedBehaviour)) {
              Debug.LogError($"{svLogPrefix} {mb.GetType().Name}.<color=white>{field.Name}</color>  The <color=yellow>class {mb.GetType().Name}</color> <color=red>MUST</color> extend <color=white>class SyncedBehaviour</color>, not MonoBehaviour");
            }
          }
        }
      #endif
      foreach (SyncedBehaviour syncBeh in FindObjectsOfType<SyncedBehaviour>()) {
        var type       = syncBeh.GetType();
        var fields     = type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields) {
          var attribute = field.GetCustomAttribute<SyncVarAttribute>();
          if (attribute != null) {
            var syncFieldInfo = CreateSyncFieldInfo(syncBeh, field, attribute, varIdx++);
            syncVars.Add(syncFieldInfo.varId, syncFieldInfo);
            syncVarsList.Add(syncFieldInfo);
          }
        }

        foreach (var prop in properties) {
          var attribute = prop.GetCustomAttribute<SyncVarAttribute>();
          if (attribute != null) {
            var syncPropInfo = CreateSyncPropInfo(syncBeh, prop, attribute, varIdx++);
            syncVars.Add(syncPropInfo.varId, syncPropInfo);
            syncVarsList.Add(syncPropInfo);
          }
        }
      }

      syncVarsArr = syncVarsList.ToArray();

      foreach (var syncVar in syncVars) {
        Debug.Log($"{svLogPrefix} Found <color=white>{syncVar.Key}</color>, value is <color=yellow>{syncVar.Value.Getter()}</color>");
      }
    } // end Start()

    // - |||||| -------------------------------------------------------
    void Update() {
      for (int i = 0; i < syncVarsArr.Length; i++) {
        SendMsgIfChanged( syncVarsArr[i] );
      }
    }
  #endregion
  #region Factories
    // ------------------ ||||||||||||||||||| ---
    private SyncFieldInfo CreateSyncFieldInfo(SyncedBehaviour syncBeh, FieldInfo field, SyncVarAttribute attribute, int fieldIdx) {
      string fieldId = (attribute.CustomName != null) 
        ? GenerateVarId(syncBeh, attribute.CustomName) 
        : GenerateVarId(syncBeh, field.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(syncBeh, attribute.OnChangedCallback);
      return new SyncFieldInfo(
          fieldId, fieldIdx,
          CreateGetter(field, syncBeh), CreateSetter(field, syncBeh),
          syncBeh, field, attribute,
          field.GetValue(syncBeh),
          onChangedCallback
      );
    }
    // ----------------- |||||||||||||||||| ---
    private SyncPropInfo CreateSyncPropInfo(SyncedBehaviour syncBeh, PropertyInfo prop, SyncVarAttribute attribute, int propIdx) {
      string propId = (attribute.CustomName != null) 
        ? GenerateVarId(syncBeh, attribute.CustomName) 
        : GenerateVarId(syncBeh, prop.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(syncBeh, attribute.OnChangedCallback);
      return new SyncPropInfo(
          propId, propIdx,
          CreateGetter(prop, syncBeh), CreateSetter(prop, syncBeh),
          syncBeh, prop, attribute,
          prop.GetValue(syncBeh),
          onChangedCallback
      );
    }
    // ------------------- ||||||||||||||||||||||| ---
    private Action<object> CreateOnChangedCallback(SyncedBehaviour syncBeh, string methodName) {
      if (string.IsNullOrEmpty(methodName))
        return null;

      var method = syncBeh.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (method == null) {
        Debug.LogWarning($"{svLogPrefix} OnChanged method <color=yellow>'{methodName}'</color> not found in <color=yellow>{syncBeh.GetType().Name}</color>");
        return null;
      }

      return (value) => method.Invoke(syncBeh, new[] { value });
    }
    // ----------------- |||||||||||| ---
    private Func<object> CreateGetter(MemberInfo member, object target) {
      var targetExpression = Expression.Constant(target);
      var memberExpression = member is PropertyInfo prop
          ? Expression.Property(targetExpression, prop)
          : Expression.Field(targetExpression, (FieldInfo)member);
      var convertExpression = Expression.Convert(memberExpression, typeof(object));
      var lambda = Expression.Lambda<Func<object>>(convertExpression);
      return lambda.Compile();
    }
    // ------------------- |||||||||||| ---
    private Action<object> CreateSetter(MemberInfo member, object target) {
      var targetExpression = Expression.Constant(target);
      var valueParam = Expression.Parameter(typeof(object), "value");
      var memberExpression = member is PropertyInfo prop
          ? Expression.Property(targetExpression, prop)
          : Expression.Field(targetExpression, (FieldInfo)member);
      var assignExpression = Expression.Assign(memberExpression, Expression.Convert(valueParam, memberExpression.Type));
      var lambda = Expression.Lambda<Action<object>>(assignExpression, valueParam);
      return lambda.Compile();
    }
    // ----------- ||||||||||||| ---
    private string GenerateVarId(SyncedBehaviour syncBeh, string varName) {
      return $"{syncBeh.netId}_{varName}";
    }
  #endregion
  #region Messaging
    // - |||||||||||||||| -------------------------------------------------------
    void SendMsgIfChanged(SyncVarInfo syncVar) {
      if ((Time.time - syncVar.LastSyncTime) < syncVar.attribute.updateInterval) {// Skip sending if the update interval has not passed for this var
        return;
      } else syncVar.LastSyncTime = Time.time; // Restart the timer until we can send again

      object currentValue = syncVar.Getter();
      bool   changedVal   = !currentValue.Equals(syncVar.LastValue);

      if (changedVal || syncVar.attribute.updateEveryInterval) { // might send every interval, but usually only when changed
        SendAsMsg( syncVar.varIdx, syncVar.varId, currentValue, syncVar.varType);
        syncVar.LastValue = currentValue;
        syncVar.onChangedCallback?.Invoke(currentValue);
      }
    }

    // - ||||||||| ---
    void SendAsMsg(int varIdx, string varId, object value, Type varType) {
      string serializedValue = SerializeValue(value, varType);
      var msg = $"{varIdx}{msgSeparator}{varId}{msgSeparator}{serializedValue}";
      Debug.Log($"{svLogPrefix} <color=#ff22ff>SEND</color>  msg:'<color=cyan>{msg}</color>' for var <color=cyan>{varIdx}</color>|<color=white>{varId}</color>|<color=yellow>{serializedValue}</color>");
      Croquet.Publish("SyncVar", "set1", msg);
    }

    // ------------------------- |||||||| ---
    public (int, string, string) ParseMsg(string msg) {
      var parts = msg.Split(msgSeparator);
      if (parts.Length != 3) {
        Debug.LogError($"{svLogPrefix} Invalid message format: '<color=#ff4444>{msg}</color>'");
        return (-1, "", "");
      }
      int varIdx = int.Parse(parts[0]);
      string varId = parts[1];
      string serializedValue = string.Join(msgSeparator.ToString(), parts.Skip(2).ToArray());// join the rest of the message back together with the separator
      return (varIdx, varId, serializedValue);
    }

    // -------- |||||||||||| ---
    public void ReceiveAsMsg(string msg) {
      var logPrefix = $"<color=#ff22ff>RECEIVED</color> ";
      var logMsg = $"msg:'<color=cyan>{msg}</color>'";
      var (varIdx, varId, serializedValue) = ParseMsg(msg);
      var logIds = $"varId=<color=white>{varId}</color> varIdx=<color=cyan>{varIdx}</color>";

      // Find the syncVar fast by varIdx, or slower by varId if that fails
      var syncVar = FindSyncVarByArr(varIdx, varId);
      var arrLookupFailed = (syncVar == null);
      if (arrLookupFailed) syncVar = FindSyncVarByDict(varId); // Array find failed, try to find by dictionary
      if (syncVar == null) { // Still null, not found!! Error.
        Debug.LogError($"{svLogPrefix} {logMsg} {logPrefix} message for <color=#ff4444>UNKNOWN</color> {logIds}");
        return;
      }
      // Parse, then set the value (if it changed)
      object deserializedValue = DeserializeValue(serializedValue, syncVar.varType);
      string logMsgVal = $"'<color=yellow>{deserializedValue}</color>'";
      object hadVal = syncVar.Getter();
      bool valIsSame = hadVal.Equals(deserializedValue); // TODO: replace with blockLoopySend logic
      if (valIsSame) {
        Debug.Log($"{svLogPrefix} {logPrefix} {logMsg} Skipping SET. '<color=yellow>{hadVal}</color>' == {logMsgVal} <color=yellow>Unchanged</color> value. {logIds} blockLoopySend:{syncVar.blockLoopySend}");
        return;
      }
      syncVar.blockLoopySend = true;     // Make sure we Skip sending the value we just received
      syncVar.Setter(deserializedValue); // Set the value using the fancy, speedy Lambda Setter
      syncVar.blockLoopySend = false;
      syncVar.LastValue = deserializedValue;
      syncVar.onChangedCallback?.Invoke(deserializedValue);

      Debug.Log( (arrLookupFailed) // Report how we found the syncVar
        ?  $"{svLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Did SET!</color>  using <color=#ff4444>SLOW varId</color> dictionary lookup. {logIds} value='{logMsgVal}'"
        :  $"{svLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Did SET!</color>  using <color=#44ff44>FAST varIdx</color>. {logIds} value='{logMsgVal}'"
      );

    } // end ReceiveAsMsg()
    // ---------------- ||||||||||||||||| ---
    private SyncVarInfo FindSyncVarByDict( string varId ) {
      if (syncVars.TryGetValue(varId, out var syncVar)) {
        return syncVar;
      } else {
        Debug.LogError($"{svLogPrefix} Var ID not found in dictionary: <color=white>{varId}</color>");
        return null;
      }
    }
    // ---------------- |||||||||||||||| ---
    private SyncVarInfo FindSyncVarByArr( int varIdx, string varId ) {
      if (varIdx >= 0 && varIdx < syncVarsArr.Length) {
        var syncVar = syncVarsArr[varIdx];
        if (!syncVar.ConfirmedInArr && syncVar.varId != varId) {
          Debug.LogError($"{svLogPrefix} Var ID mismatch at varIdx:<color=cyan>{varIdx}</color>. Expected <color=white>{syncVar.varId}</color>, got <color=#ff4444>{varId}</color>");
          return null;
        } else {
          syncVar.ConfirmedInArr = true;
          Debug.Log($"{svLogPrefix} <color=green>✔️</color>Confirmed syncVars[varId:'<color=white>{varId}</color>'] matches entry at syncVarsArr[varIdx:<color=cyan>{varIdx}</color>]");
          return syncVar;
        }
      }
      return null;
    }
  #endregion
  #region Serialization
    // ----------- |||||||||||||| ---
    private string SerializeValue(object value, Type type) {
      // Placeholder for actual serialization logic
      return value.ToString();
    }

    // ----------- |||||||||||||||| ---
    private object DeserializeValue(string serializedValue, Type type) {
      // Placeholder for actual deserialization logic
      return Convert.ChangeType(serializedValue, type);
    }
  #endregion
}

// Extension methods for serialization (placeholder)
public static class SerializationExtensions {
  public static string Serialize(this object obj) {
    // Implement your serialization logic here
    return obj.ToString();
  }

  public static T Deserialize<T>(this string serialized) {
    // Implement your deserialization logic here
    return (T)Convert.ChangeType(serialized, typeof(T));
  }
}