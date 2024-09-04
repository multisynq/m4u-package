using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
#region Attribute
//========== |||||||||||||||| ============
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SyncVarAttribute : Attribute { 
  // Usage options: 
  // [SyncVar] 
  // [SyncVar(CustomName = "shrtNm")] // Custom name for the variable, useful for shortening to reduce message size
  // [SyncVar(OnChangedCallback = "MethodNameOfClassWithTheVar")] // Method to call when the value changes
  // [SyncVar(MinSyncInterval = 0.5f)] // Minimum time between syncs in seconds
  // [SyncVar(CustomName = "myVar", OnChangedCallback = "MyMethod", MinSyncInterval = 0.5f)] // any combo of options
  public string CustomName { get; set; }
  public float MinSyncInterval { get; set; } = 0.1f; // Minimum time between syncs in seconds
  public bool OnlyWhenChanged { get; set; } = true; // Normally only sync when the value has changed
  public string OnChangedCallback { get; set; } // Name of the method to call on the var's class when the value changes
}
#endregion
//========== ||||||||||||||||| ====================================
public class CroquetSyncVarMgr : MonoBehaviour {
  #region Vars
    private Dictionary<string, SyncVarInfo> syncVars;
    private SyncVarInfo[]                   syncVarsArr;
    static char msgSeparator = '|';
  #endregion

  #region Classes
    // ------------------- ||||||||||| ---
    private abstract class SyncVarInfo {
      public readonly string VarId;
      public readonly int VarIdx;
      public readonly Func<object> Getter;
      public readonly Action<object> Setter;
      public readonly MonoBehaviour MonoBehaviour;
      public readonly Type VarType;
      public readonly SyncVarAttribute Attribute;
      public readonly Action<object> OnChangedCallback;
      public bool blockLoopySend = false;

      public object LastValue { get; set; }
      public bool ConfirmedInArr { get; set; }
      public float LastSyncTime { get; set; }

      protected SyncVarInfo(string varId, int varIdx, Func<object> getter, Action<object> setter,
                            MonoBehaviour monoBehaviour, Type varType, SyncVarAttribute attribute,
                            object initialValue, Action<object> onChangedCallback) {
        VarId = varId; VarIdx = varIdx;
        Getter = getter; Setter = setter;
        MonoBehaviour = monoBehaviour;
        VarType = varType; Attribute = attribute;
        LastValue = initialValue; ConfirmedInArr = false;
        LastSyncTime = 0f;
        OnChangedCallback = onChangedCallback;
      }
    }

    // ---------- ||||||||||||| ---
    private class SyncFieldInfo : SyncVarInfo {
      public readonly FieldInfo FieldInfo;

      public SyncFieldInfo(string fieldId, int fieldIdx, Func<object> getter, Action<object> setter,
                            MonoBehaviour monoBehaviour, FieldInfo fieldInfo, SyncVarAttribute attribute,
                            object initialValue, Action<object> onChangedCallback)
          : base(fieldId, fieldIdx, getter, setter, monoBehaviour, fieldInfo.FieldType, attribute, initialValue, onChangedCallback) {
        FieldInfo = fieldInfo;
      }
    }

    // ---------- |||||||||||| ---
    private class SyncPropInfo : SyncVarInfo {
      public readonly PropertyInfo PropInfo;

      public SyncPropInfo(string propId, int propIdx, Func<object> getter, Action<object> setter,
                          MonoBehaviour monoBehaviour, PropertyInfo propInfo, SyncVarAttribute attribute,
                          object initialValue, Action<object> onChangedCallback)
          : base(propId, propIdx, getter, setter, monoBehaviour, propInfo.PropertyType, attribute, initialValue, onChangedCallback) {
        PropInfo = propInfo;
      }
    }
  #endregion
  #region Start/Update
    //-- ||||| ---
    void Start() { // CroquetSynchVarSystem.Start()

      Croquet.Subscribe("SynchVarSystem", "setValue", ReceiveAsMsg);

      syncVars = new Dictionary<string, SyncVarInfo>();
      List<SyncVarInfo> syncVarsList = new List<SyncVarInfo>();

      int varIdx = 0;
      foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>()) {
        var type = mb.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields) {
          var attribute = field.GetCustomAttribute<SyncVarAttribute>();
          if (attribute != null) {
            var syncFieldInfo = CreateSyncFieldInfo(mb, field, attribute, varIdx++);
            syncVars.Add(syncFieldInfo.VarId, syncFieldInfo);
            syncVarsList.Add(syncFieldInfo);
          }
        }

        foreach (var prop in properties) {
          var attribute = prop.GetCustomAttribute<SyncVarAttribute>();
          if (attribute != null) {
            var syncPropInfo = CreateSyncPropInfo(mb, prop, attribute, varIdx++);
            syncVars.Add(syncPropInfo.VarId, syncPropInfo);
            syncVarsList.Add(syncPropInfo);
          }
        }
      }

      syncVarsArr = syncVarsList.ToArray();

      foreach (var syncVar in syncVars) {
        Debug.Log($"[SyncVar] Found {syncVar.Key}, value is {syncVar.Value.Getter()}");
      }
    } // end Start()

    // - |||||| -------------------------------------------------------
    void Update() {
      float currentTime = Time.time;
      for (int i = 0; i < syncVarsArr.Length; i++) {
        var syncVar = syncVarsArr[i];
        if (currentTime - syncVar.LastSyncTime < syncVar.Attribute.MinSyncInterval)
          continue;
        syncVar.LastSyncTime = currentTime;
        object currentValue = syncVar.Getter();
        Debug.Log($"Checking syncVar {syncVar.VarId} (index {syncVar.VarIdx}) CanCheckEquals:{!syncVar.Attribute.OnlyWhenChanged} currVal:{currentValue} lastVal:{syncVar.LastValue}");
        if (!syncVar.Attribute.OnlyWhenChanged || !Equals(currentValue, syncVar.LastValue)) {
          if (!syncVar.blockLoopySend) { // Skip sending the value we just received
            SendAsMsg( syncVar.VarIdx, syncVar.VarId, currentValue, syncVar.VarType);
            syncVar.blockLoopySend = false;
          }
          syncVar.LastValue = currentValue;
          syncVar.OnChangedCallback?.Invoke(currentValue);
        }
      }
    }
  #endregion
  #region Factories
    // ------------------ ||||||||||||||||||| ---
    private SyncFieldInfo CreateSyncFieldInfo(MonoBehaviour mb, FieldInfo field, SyncVarAttribute attribute, int fieldIdx) {
      string fieldId = (attribute.CustomName != null) 
        ? GenerateVarId(mb, attribute.CustomName) 
        : GenerateVarId(mb, field.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(mb, attribute.OnChangedCallback);
      return new SyncFieldInfo(
          fieldId, fieldIdx,
          CreateGetter(field, mb), CreateSetter(field, mb),
          mb, field, attribute,
          field.GetValue(mb),
          onChangedCallback
      );
    }
    // ----------------- |||||||||||||||||| ---
    private SyncPropInfo CreateSyncPropInfo(MonoBehaviour mb, PropertyInfo prop, SyncVarAttribute attribute, int propIdx) {
      string propId = (attribute.CustomName != null) 
        ? GenerateVarId(mb, attribute.CustomName) 
        : GenerateVarId(mb, prop.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(mb, attribute.OnChangedCallback);
      return new SyncPropInfo(
          propId, propIdx,
          CreateGetter(prop, mb), CreateSetter(prop, mb),
          mb, prop, attribute,
          prop.GetValue(mb),
          onChangedCallback
      );
    }
    // ------------------- ||||||||||||||||||||||| ---
    private Action<object> CreateOnChangedCallback(MonoBehaviour mb, string methodName) {
      if (string.IsNullOrEmpty(methodName))
        return null;

      var method = mb.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (method == null) {
        Debug.LogWarning($"[SyncVar] OnChanged method '{methodName}' not found in {mb.GetType().Name}");
        return null;
      }

      return (value) => method.Invoke(mb, new[] { value });
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
    private string GenerateVarId(MonoBehaviour mb, string varName) {
      return $"{mb.GetInstanceID()}_{varName}";
    }
  #endregion
  #region Messaging
    // -------- |||||||||| ---
    private void SendAsMsg(int varIdx, string varId, object value, Type varType) {
      string serializedValue = SerializeValue(value, varType);
      Debug.Log($"[SyncVar] Sending message for var {varId} (index {varIdx}): {serializedValue}");
      // NetworkManager.Send(new Message(varIdx, varId, serializedValue));
      Croquet.Publish("SynchVarSystem", "setValue", $"{varIdx}{msgSeparator}{varId}{msgSeparator}{serializedValue}");
    }
    // -------- |||||||||||| ---
    public void ReceiveAsMsg(string msg) {
      int varIdx; string varId; string serializedValue;
      var parts = msg.Split(msgSeparator);
      if (parts.Length != 3) {
        Debug.LogError($"[SyncVar] Invalid message format: {msg}");
        return;
      }
      varIdx = int.Parse(parts[0]);
      varId = parts[1];
      serializedValue = parts[2];
      if (varIdx >= 0 && varIdx < syncVarsArr.Length) {
        var syncVar = syncVarsArr[varIdx];
        if (!syncVar.ConfirmedInArr && syncVar.VarId != varId) {
          Debug.LogError($"[SyncVar] Var ID mismatch at index {varIdx}. Expected {syncVar.VarId}, got {varId}");
          return;
        } else {
          syncVar.ConfirmedInArr = true;
          Debug.Log($"[SyncVar] Confirmed varId:{varId} matches entry at syncVarsArr[varIdx:{varIdx}]");
        }

        object deserializedValue = DeserializeValue(serializedValue, syncVar.VarType);

        syncVar.Setter(deserializedValue);
        syncVar.blockLoopySend = true;

        syncVar.LastValue = deserializedValue;
        syncVar.OnChangedCallback?.Invoke(deserializedValue);
        Debug.Log($"[SyncVar] <color=#33FF33>Received</color> and applied value for var {varId} (index {varIdx}): <color=yellow>{deserializedValue}</color>");
      }
      else if (syncVars.TryGetValue(varId, out var dictSyncVar)) {
        Debug.LogWarning($"[SyncVar] Var index {varIdx} out of range, falling back to dictionary lookup for {varId}");
        object deserializedValue = DeserializeValue(serializedValue, dictSyncVar.VarType);

        dictSyncVar.Setter(deserializedValue);
        dictSyncVar.blockLoopySend = true;

        dictSyncVar.LastValue = deserializedValue;
        dictSyncVar.OnChangedCallback?.Invoke(deserializedValue);
        Debug.Log($"[SyncVar] <color=#33FF33>Received</color> and applied value for var {varId} <color=#ff8800>(dictionary fallback): {deserializedValue}</color=#ff8800>");
      }
      else {
        Debug.LogError($"[SyncVar] Received message for unknown var: {varId} (index {varIdx})");
      }
    } // end ReceiveAsMsg()
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