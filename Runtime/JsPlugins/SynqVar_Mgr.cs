using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
using System.Linq;

namespace Multisynq {


#region Attribute
  //========== |||||||||||||||| ================
  //========| [SynqVar] | ======================
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public class SynqVarAttribute : Attribute { // C# Attribute
    // Usage options: 
    // [SynqVar] 
    // [SynqVar(CustomName = "shrtNm")]
    // [SynqVar(OnChangedCallback = "MethodNameOfClassWithTheVar")] // Method to call when the value changes
    // [SynqVar(MinSyncInterval = 0.5f)] // Minimum time between syncs in seconds
    // [SynqVar(CustomName = "myVar", OnChangedCallback = "MyMethod", MinSyncInterval = 0.5f)] // any combo of options
    // [SynqVar(updateEveryInterval = true)] // Forces update every interval, even if value hasn't changed
    public string CustomName          { get; set; } // Custom name for the variable, useful for shortening to reduce message size
    public float  updateInterval      { get; set; } = 0.1f; // Minimum time between syncs in seconds
    public bool   updateEveryInterval { get; set; } = false; //   true: Force update every interval, even if value hasn't changed.   false: Only sync when the value has changed.
    public string OnChangedCallback   { get; set; } // Name of the method to call on the var's class when the value changes
    public string hook                { get; set; } // Name of the method to call on the var's class when the value changes
    public SynqVarUIAttribute DeepCopy() => (SynqVarUIAttribute)MemberwiseClone();
    override public string ToString() => $"SynqVarAttribute: CustomName{CustomName} updateInterval:{updateInterval} updateEveryInterval:{updateEveryInterval}";
  }
#endregion

//========== ||||||||||| ============================================ ||||||||||| ================
public class SynqVar_Mgr : JsPlugin_Behaviour { // <<<<<<<<<<<< class SynqVar_Mgr <<<<<<<<<<<<

  #region Types
    public class SynqVarsByName : Dictionary<string, SynqVarInfo> { }
  #endregion

  #region Fields
    [SerializeField] public SynqVarsByName syncVars;
    [SerializeField] public SynqVarInfo[]  syncVarsArr;
    static public           char           msgSeparator = '|';
    static public           string         svLogPrefix = "<color=#5555FF>[SynqVar]</color> ";
    static public           bool           dbg = false;
  #endregion

  // Array of patterns to check if the app's C# codebase will have need of this JS plugin code
  new static public string[] CsCodeMatchesToNeedThisJs() => new[] {@"\[SynqVar"};

  #region JavaScript
    //---------------------------- ||||||||||||||| -------------------------
    new static public JsPluginCode GetJsPluginCode() {
      return new(
        pluginName: "SynqVar_Mgr",
        pluginExports: new[] {"SynqVar_Mgr_Model", "SynqVar_Mgr_View"},
        pluginCode: @"
          import { Model, View } from '@croquet/croquet';
          //---------- ||||||||||||||||| -------------------
          export class SynqVar_Mgr_Model extends Model {
            varValuesAsMessages = []
            get gamePawnType() { return '' }

            init(options) {
              super.init(options)
              this.subscribe('SynqVar', 'pleaseSetVar', this.onPleaseSetVar) // sent from Unity to JS
              console.log('### <color=magenta>SynqVar_Mgr_Model.init() <<<<<<<<<<<<<<<<<<<<< </color>')
            }

            onPleaseSetVar(msg) {
              const varIdx = parseInt(msg.split('|')[0])
              this.varValuesAsMessages[varIdx] = msg // store the value in the array at the index specified in the message
              this.publish('SynqVar', 'everybodySetVar', msg) // sent from JS to Unity
            }
          }
          SynqVar_Mgr_Model.register('SynqVar_Mgr_Model')
                
          //---------- |||||||||||||||| -------------------
          export class SynqVar_Mgr_View extends View {
            constructor(model) {
              super(model)
              this.model = model
              console.log('### <color=green>SynqVar_Mgr_View.constructor() <<<<<<<<<<<<<<<<<<<<< </color>')
              const messages = model.varValuesAsMessages.map( (msg) => (
                `croquetPub\x01SynqVar\x01everybodySetVar\x01${msg}`
              ))
              globalThis.theGameEngineBridge.sendBundleToUnity(messages) // MIMICS  model.publish('SynqVar', 'everybodySetVar', msg)
            }
          }
        ".LessIndent()
      );
    }
    //------------------ |||||||||||||||||| -------------------------
    override public void WriteMyJsPluginFile(JsPluginCode jsPlugin) { // TODO: remove since this does the same as the base, but it does demo how to override for fancy Inject usage we might want later
      // if (dbg)  Debug.Log($"{svLogPrefix} override public void OnInjectJsPluginCode()");
      base.WriteMyJsPluginFile(jsPlugin);
    }
  #endregion

  #region Start/Update
    //------------------ ||||| ------------------------------------------
    override public void Start() { // SynqVar_Mgr.Start()
      base.Start();

      Croquet.Subscribe("SynqVar", "everybodySetVar", ReceiveAsMsg); // <<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq

      #if UNITY_EDITOR
        AttributeHelper.CheckForBadAttrParents<SynqBehaviour, SynqVarAttribute>();
      #endif

      syncVars = new SynqVarsByName();
      List<SynqVarInfo> syncVarsList = new();
      int varIdx = 0; // Index for the syncVarsArr array for the fast lookup system
      // Find SynqVar attribute on fields & properties of SyncedBehaviours and add them to the syncVars dictionary
      foreach (SynqBehaviour syncBeh in FindObjectsOfType<SynqBehaviour>()) {

        if (!syncBeh.enabled) continue; // skip inactives
        var type       = syncBeh.GetType();
        var fields     = type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // Find FIELDS w/ [SynqVar] attributes
        foreach (var field in fields) {
          var svAttr = field.GetCustomAttribute<SynqVarAttribute>();
          if (svAttr != null) {
            var syncFieldInfo = CreateSynqFieldInfo(syncBeh, field, svAttr, varIdx++);
            // log out gameObj path is already contained
            if (syncVars.ContainsKey(syncFieldInfo.varId)) {
              Debug.LogError($"{svLogPrefix} {gameObject.name} Duplicate varId found: <color=#4ff>{syncBeh.GetType().Name}</color>.<color=white>{syncFieldInfo.varId}</color> in <color=yellow>{syncBeh.gameObject.Path()}</color>");
            } else {
              syncVars.Add(syncFieldInfo.varId, syncFieldInfo);
            }
            syncVarsList.Add(syncFieldInfo);
          }
        }
        // Find PROPERTIES w/ [SynqVar] attributes
        foreach (var prop in properties) {
          var svAttr = prop.GetCustomAttribute<SynqVarAttribute>();
          if (svAttr != null) {
            var syncPropInfo = CreateSynqPropInfo(syncBeh, prop, svAttr, varIdx++);
            syncVars.Add(syncPropInfo.varId, syncPropInfo);
            syncVarsList.Add(syncPropInfo);
          }
        }
      }
      // make unique array of syncVars
      syncVarsArr = syncVarsList.ToArray();

      if (dbg)  {
        foreach (var syncVar in syncVars) {
          Debug.Log($"{svLogPrefix} Found <color=white>{syncVar.Key}</color>, value is <color=yellow>{syncVar.Value.Getter()}</color>");
        }
      }
    } // end Start()

    //------------ |||||| -------------------------------------------------------
    protected void Update() { // TODO: Remove this once we get CreateSetter() callbacks perfected
      if (syncVarsArr==null) Debug.LogError($"{svLogPrefix} {gameObject.Path()}.syncVarsArr is null");
      for (int i = 0; i < syncVarsArr.Length; i++) {
        SendMsgIfChanged( syncVarsArr[i], i );
      }
    }
    #if UNITY_EDITOR
      //-- ||||| -------------------------------------------------------
      void OnGUI() {
        AttributeHelper.OnGUI_FailMessage();
      }
    #endif
  #endregion

  #region Factories
    //------------------- ||||||||||||||||||| -----------------------
    private SynqFieldInfo CreateSynqFieldInfo(SynqBehaviour syncBeh, FieldInfo field, SynqVarAttribute attribute, int fieldIdx) {
      string fieldId = GenerateVarId(syncBeh, attribute.CustomName ?? field.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(syncBeh, attribute.OnChangedCallback ?? attribute.hook);
      string classAndFieldName = $"{syncBeh.GetType().Name}.{field.Name}";
      return new SynqFieldInfo(
        fieldId, fieldIdx,
        CreateGetter(field, syncBeh), CreateSetter(field, syncBeh),
        syncBeh, field, attribute,
        field.GetValue(syncBeh),
        onChangedCallback, classAndFieldName
      );
    }
    //------------------ |||||||||||||||||| -----------------------
    private SynqPropInfo CreateSynqPropInfo(SynqBehaviour syncBeh, PropertyInfo prop, SynqVarAttribute attribute, int propIdx) {
      string propId = GenerateVarId(syncBeh, attribute.CustomName ?? prop.Name);
      Action<object> onChangedCallback = CreateOnChangedCallback(syncBeh, attribute.OnChangedCallback);
      string classAndFieldName = $"{syncBeh.GetType().Name}.{prop.Name}";
      return new SynqPropInfo(
        propId, propIdx,
        CreateGetter(prop, syncBeh), CreateSetter(prop, syncBeh), // TODO: pass in propId & propIdx so we can make a callback handler and remove Update checking for changes
        syncBeh, prop, attribute,
        prop.GetValue(syncBeh),
        onChangedCallback, classAndFieldName
      );
    }
    //-------------------- ||||||||||||||||||||||| -----------------------
    private Action<object> CreateOnChangedCallback(SynqBehaviour syncBeh, string methodName) {
      if (string.IsNullOrEmpty(methodName))
        return null;

      var method = syncBeh.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (method == null) {
        Debug.LogWarning($"{svLogPrefix} OnChanged method <color=yellow>'{methodName}'</color> not found in <color=yellow>{syncBeh.GetType().Name}</color>");
        return null;
      }

      return (value) => method.Invoke(syncBeh, new[] { value });
    }
    //------------------ |||||||||||| -------------------------------------------
    private Func<object> CreateGetter(MemberInfo member, object target) {
      var targetExpression = Expression.Constant(target);
      var memberExpression = member is PropertyInfo prop
          ? Expression.Property(targetExpression, prop)
          : Expression.Field(targetExpression, (FieldInfo)member);
      var convertExpression = Expression.Convert(memberExpression, typeof(object));
      var lambda = Expression.Lambda<Func<object>>(convertExpression);
      return lambda.Compile();
    }
    //-------------------- |||||||||||| -------------------------------------------
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
    //------------ ||||||||||||| -------------------------------------------
    private string GenerateVarId(SynqBehaviour syncBeh, string varName) {
      return $"{syncBeh.netId}_{varName}";
    }
  #endregion

  #region Messaging
    //-- |||||||||||||||| -------------------------------------------------------
    void SendMsgIfChanged(SynqVarInfo syncVar, int i) {
      if ((Time.time - syncVar.LastSyncTime) < syncVar.attribute.updateInterval) {// Skip sending if the update interval has not passed for this var
        return;
      } else syncVar.LastSyncTime = Time.time; // Restart the timer until we can send again

      object currentValue  = syncVar.Getter();
      bool   valHasChanged = !currentValue.Equals(syncVar.LastValue);

      if (valHasChanged || syncVar.attribute.updateEveryInterval) { // might send every interval, but usually only when changed
        if (dbg)  Debug.Log($"{svLogPrefix} <color=#ff22ff>SEND</color>  [<color=#0ff>{i}</color>] {syncVar.varName} = {currentValue}");

        // local set
        // syncVar.blockLoopySend = true;
        // syncVar.Setter(currentValue);
        // syncVar.blockLoopySend = false;

        SendAsMsg( syncVar.varIdx, syncVar.varId, currentValue, syncVar.varType);
        syncVar.LastValue = currentValue;
        syncVar.onChangedCallback?.Invoke(currentValue);
        // also UI
        var castVal = Convert.ChangeType(currentValue, syncVar.varType);
        syncVar.onUICallback?.Invoke( castVal );
      }
    }

    //--------- ||||||||| -------------------------------------------------------
    public void SendAsMsg(int varIdx, string varId, object value, Type varType) {
      string serializedValue = SerializeValue(value, varType);
      var msg = $"{varIdx}{msgSeparator}{varId}{msgSeparator}{serializedValue}";
      if (dbg)  Debug.Log($"{svLogPrefix} <color=#ff22ff>SEND</color>  msg:'<color=cyan>{msg}</color>' for var <color=cyan>{varIdx}</color>|<color=white>{varId}</color>|<color=yellow>{serializedValue}</color>");

      Croquet.Publish("SynqVar", "pleaseSetVar", msg);  // <<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq

    }

    //-------------------------- |||||||| --------------------------
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
    // Type of ReceiveAsMsg() is (string) => (SynqVarInfo,bool)
    // as a delegate, it is Func<string, (SynqVarInfo,bool)>
    //----------------------- |||||||||||| --------------------------
    public (SynqVarInfo,bool) ReceiveAsMsg(string msg) {
      var logPrefix = $"<color=#ff22ff>RECEIVED</color> ";
      var logMsg = $"msg:'<color=cyan>{msg}</color>'";
      var (varIdx, varId, serializedValue) = ParseMsg(msg);
      var logIds = $"varId=<color=white>{varId}</color> varIdx=<color=cyan>{varIdx}</color>";

      // Find the syncVar fast by varIdx, or slower by varId if that fails
      var synqVar = FindSynqVarByArr(varIdx, varId);
      var arrLookupFailed = (synqVar == null);
      if (arrLookupFailed) synqVar = FindSynqVarByDict(varId); // Array find failed, try to find by dictionary
      if (synqVar == null) { // Still null, not found!! Error.
        Debug.LogError($"{svLogPrefix} {logMsg} {logPrefix} message for <color=#ff4444>UNKNOWN</color> {logIds}");
        return (null, false);
      }
      // Parse, then set the value (if it changed)
      object deserializedValue = DeserializeValue(serializedValue, synqVar.varType);
      string logMsgVal = $"'<color=yellow>{deserializedValue}</color>'";
      object hadVal = synqVar.Getter();
      bool valIsSame = hadVal.Equals(deserializedValue); // TODO: replace with blockLoopySend logic
      if (valIsSame) {
        if (dbg)  Debug.Log($"{svLogPrefix} {logPrefix} {logMsg} Skipping SET. '<color=yellow>{hadVal}</color>' == {logMsgVal} <color=yellow>Unchanged</color> value. {logIds} blockLoopySend:{synqVar.blockLoopySend}");
        return (synqVar, valIsSame);
      }
      synqVar.blockLoopySend = true;     // Make sure we Skip sending the value we just received
      synqVar.Setter(deserializedValue); // Set the value using the fancy, speedy Lambda Setter
      synqVar.blockLoopySend = false;
      synqVar.LastValue = deserializedValue;
      synqVar.onChangedCallback?.Invoke(deserializedValue);
      var castVal = Convert.ChangeType(deserializedValue, synqVar.varType);
      synqVar.onUICallback?.Invoke( castVal );

      if (dbg)  Debug.Log( (arrLookupFailed) // Report how we found the syncVar
        ?  $"{svLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Did SET!</color>  using <color=#ff4444>SLOW varId</color> dictionary lookup. {logIds} value={logMsgVal}"
        :  $"{svLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Did SET!</color>  using <color=#44ff44>FAST varIdx</color>. {logIds} value={logMsgVal}"
      );
      return (synqVar, valIsSame);
    } // end ReceiveAsMsg()
    //----------------- ||||||||||||||||| --------------------------
    private SynqVarInfo FindSynqVarByDict( string varId ) {
      if (syncVars.TryGetValue(varId, out var syncVar)) {
        return syncVar;
      } else {
        Debug.LogError($"{svLogPrefix} Var ID not found in dictionary: <color=white>{varId}</color>");
        return null;
      }
    }
    //------------------- |||||||||||||||| --------------------------
    protected SynqVarInfo FindSynqVarByArr( int varIdx, string varId ) {
      if (varIdx >= 0 && varIdx < syncVarsArr.Length) {
        var syncVar = syncVarsArr[varIdx];
        if (!syncVar.ConfirmedInArr && syncVar.varId != varId) {
          Debug.LogError($"{svLogPrefix} Var ID mismatch at varIdx:<color=cyan>{varIdx}</color>. Expected <color=white>{syncVar.varId}</color>, got <color=#ff4444>{varId}</color>");
          return null;
        } else {
          syncVar.ConfirmedInArr = true;
          if (dbg)  Debug.Log($"{svLogPrefix} <color=green>✔️</color>Confirmed syncVars[varId:'<color=white>{varId}</color>'] matches entry at syncVarsArr[varIdx:<color=cyan>{varIdx}</color>]");
          return syncVar;
        }
      }
      return null;
    }
  #endregion
  #region Serialization
    //------------ |||||||||||||| --------------------------
    private string SerializeValue(object value, Type type) {
      return (type == typeof(Vector3)) 
        ? ((Vector3)value).Serialize()
        : (type == typeof(Quaternion)) 
          ? ((Quaternion)value).Serialize()
          : value.ToString();
          // : value.Serialize();
    }

    //------------ |||||||||||||||| --------------------------
    private object DeserializeValue(string serializedValue, Type type) {
      return (type == typeof(Vector3)) 
        ? serializedValue.DeserializeVector3()
        : (type == typeof(Quaternion)) 
          ? serializedValue.DeserializeQuaternion()
          : Convert.ChangeType(serializedValue, type);
    }
  #endregion

  #region Singleton
    private static SynqVar_Mgr _Instance;
    public  static SynqVar_Mgr I { // Usage:   SynqVarMgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
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

  // Vector3
  public static string Serialize(this Vector3 obj) {
    return $"{obj.x},{obj.y},{obj.z}";
  }
  public static Vector3 DeserializeVector3(this string serialized) {
    var parts = serialized.Split(',');
    return new Vector3(
      float.Parse(parts[0]),
      float.Parse(parts[1]),
      float.Parse(parts[2])
    );
  }

  //Quaternion
  public static string Serialize(this Quaternion obj) {
    return $"{obj.x},{obj.y},{obj.z},{obj.w}";
  }
  public static Quaternion DeserializeQuaternion(this string serialized) {
    var parts = serialized.Split(',');
    return new Quaternion(
      float.Parse(parts[0]),
      float.Parse(parts[1]),
      float.Parse(parts[2]),
      float.Parse(parts[3])
    );
  }
}

#region Classes

  //================================== ||||||||||| ===
  [Serializable] public abstract class SynqVarInfo {
    public string varId;
    public string varName;
    public int varIdx;
    public Func<object> Getter;
    public Action<object> Setter;
    public Action<object> onChangedCallback;
    public Action<object> onUICallback;
    public SynqBehaviour syncedBehaviour;
    public Type varType;
    public SynqVarAttribute attribute;
    public bool blockLoopySend = false;

    public object LastValue { get; set; }
    public bool ConfirmedInArr { get; set; }
    public float LastSyncTime { get; set; }

    public SynqVarInfo( // constructor
      string varId, int varIdx,      
      Func<object> getter, Action<object> setter,
      SynqBehaviour monoBehaviour, Type varType, 
      SynqVarAttribute attribute, object initialValue, 
      Action<object> onChangedCallback, string varName
    ) {
      this.varId = varId; this.varIdx = varIdx;
      Getter = getter; Setter = setter;
      syncedBehaviour = monoBehaviour; this.varType = varType; 
      this.attribute = attribute; LastValue = initialValue; 
      this.onChangedCallback = onChangedCallback;
      ConfirmedInArr = false;
      LastSyncTime = 0f;
      this.varName = varName;
    }
    override public string ToString() {
      return $"var {varName}:{varType.Name} = {Getter()} varId:{varId} varIdx:{varIdx} attribute:{attribute}";
    }
  }

  //========== ||||||||||||| =========================
  public class SynqFieldInfo : SynqVarInfo {
    public readonly FieldInfo FieldInfo;

    public SynqFieldInfo( // constructor
      string fieldId, int fieldIdx, 
      Func<object> getter, Action<object> setter,
      SynqBehaviour monoBehaviour, FieldInfo fieldInfo, 
      SynqVarAttribute attribute, object initialValue, 
      Action<object> onChangedCallback, string varName
    ) : base(
      fieldId, fieldIdx, getter, setter, 
      monoBehaviour, fieldInfo.FieldType, 
      attribute, initialValue, 
      onChangedCallback, varName
    ) {
      FieldInfo = fieldInfo;
    }
  }

  //========== |||||||||||| =========================
  public class SynqPropInfo : SynqVarInfo {
    public readonly PropertyInfo PropInfo;

    public SynqPropInfo( // constructor
      string propId, int propIdx, 
      Func<object> getter, Action<object> setter,
      SynqBehaviour monoBehaviour, PropertyInfo propInfo, 
      SynqVarAttribute attribute, object initialValue, 
      Action<object> onChangedCallback, string varName
    ) : base(
      propId, propIdx, getter, setter, 
      monoBehaviour, propInfo.PropertyType, 
      attribute, initialValue, 
      onChangedCallback, varName
    ) {
      PropInfo = propInfo;
    }
  }
#endregion

} // END namespace Multisynq