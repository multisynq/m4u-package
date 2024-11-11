using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace Multisynq {


[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SynqCommandAttribute : Attribute {
  public string CustomName { get; set; }
}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class SynqRPCAttribute : SynqCommandAttribute {
}

//========== ||||||||||||||| ===================================================== ||||||||||||||| ============
public class SynqCommand_Mgr : JsPlugin_Behaviour { // <<<<<<<<<<<< class SynqCommand_Mgr <<<<<<<<<<<<
  #region Fields
    private Dictionary<string, SynqCommandInfo> SynqCommands;
    private SynqCommandInfo[] SynqCommandsArr;
    private static char msgSeparator = '|';
    private static string scLogPrefix = "<color=#7777FF>[SynqCommand]</color> ";
    static bool dbg = false;
    new static public string[] CsCodeMatchesToNeedThisJs() => new[] {@"\[SynqCommand", @"\[SynqRPC"}; 
  #endregion

  #region JavaScript
  public override JsPluginCode GetJsPluginCode() {
    return new(
      pluginName: "SynqCommand_Mgr",
      pluginExports: new[] {"SynqCommand_Mgr_Model"},
      pluginCode: @"
        import { Model } from '@croquet/croquet';

        export class SynqCommand_Mgr_Model extends Model {
          dbg = false
          init(options) {
            super.init(options);
            this.subscribe('SynqCommand', 'pleaseRun', this.onPleaseRun);
            if (this.dbg) console.log('### <color=magenta>SynqCommand_Mgr_Model.init() <<<<<<<<<<<<<<<<<<<<< </color>');
          }
          onPleaseRun(msg) {
            if (this.dbg) console.log(`<color=blue>[SynqCommand]</color> <color=yellow>JS</color> CroquetModel <color=magenta>SynqCommandMgrModel.onSynqCommandExecute()</color> msg = <color=white>${JSON.stringify(msg)}</color>`);
            this.publish('SynqCommand', 'everybodyRun', msg);
          }
        }
        SynqCommand_Mgr_Model.register('SynqCommand_Mgr_Model');
      ".LessIndent()
    );
  }
  //------------------ |||||||||||||||||| -------------------------
  override public void WriteMyJsPluginFile() { // TODO: remove since this does the same as the base, but it does demo how to override for fancy Inject usage we might want later
    // if (dbg)  Debug.Log($"{logPrefix} override public void OnInjectJsPluginCode()");
    base.WriteMyJsPluginFile();
  }
  #endregion

  #region Start/Update
  //------------------ ||||| ----------------------------------------
  override public void Start() {
    base.Start();

    Croquet.Subscribe("SynqCommand", "everybodyRun", OnEverybodyRun); // <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 

    #if UNITY_EDITOR
      AttributeHelper.CheckForBadAttrParents<SynqBehaviour, SynqCommandAttribute>();
      AttributeHelper.CheckForBadAttrParents<SynqBehaviour, SynqRPCAttribute>();
    #endif

    SynqCommands = new Dictionary<string, SynqCommandInfo>();
    List<SynqCommandInfo> SynqCommandsList = new List<SynqCommandInfo>();

    int commandIdx = 0; // Index for the SynqCommandsArr array for the fast lookup system
    foreach (SynqBehaviour syncBeh in FindObjectsOfType<SynqBehaviour>()) {
      var type = syncBeh.GetType();
      var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      foreach (var method in methods) {
        var attribute = method.GetCustomAttribute<SynqCommandAttribute>() ?? method.GetCustomAttribute<SynqRPCAttribute>();
        if (attribute != null) {
          var SynqCommandInfo = CreateSynqCommandInfo(syncBeh, method, attribute, commandIdx++);
          SynqCommands.Add(SynqCommandInfo.commandId, SynqCommandInfo);
          SynqCommandsList.Add(SynqCommandInfo);
        }
      }
    }

    SynqCommandsArr = SynqCommandsList.ToArray();

    if (dbg) {
      foreach (var SynqCommand in SynqCommands) {
        Debug.Log($"{scLogPrefix} Found <color=white>{SynqCommand.Key}</color>");
      }
    }
  }
  #if UNITY_EDITOR
      // - ||||| -------------------------------------------------------
      void OnGUI() {
    AttributeHelper.OnGUI_FailMessage();
      }
  #endif
  #endregion
  #region Messaging
  //--------- |||||||||||||||||||||| ----------------------------------------
  public void PublishSynqCommandCall(SynqBehaviour syncBeh, RpcTarget tgt, string commandId, params object[] parameters) {
      // TODO: Implement the actual logic to send the command only other clients or all clients
      // if (tgt == RpcTarget.Others) {
      // else if (tgt == RpcTarget.All) {
      if (parameters.Length == 0) { PublishSynqCommandCall(syncBeh, commandId); }
      else { PublishSynqCommandCall(syncBeh, commandId, parameters); }
  }
  //--------- |||||||||||||||||||||| ----------------------------------------
  public void PublishSynqCommandCall(SynqBehaviour syncBeh, string commandId, params object[] parameters) {

      string cmdWithNetId = $"{syncBeh.netId}_{commandId}";
      string serializedParams = (parameters.Length == 0) ? "" : msgSeparator+string.Join(msgSeparator.ToString(), parameters.Select(p => SerializeValue(p)));
      var msg = $"{SynqCommands[cmdWithNetId].commandIdx}{msgSeparator}{cmdWithNetId}{serializedParams}";
      if (dbg) Debug.Log($"{scLogPrefix} <color=#ff22ff>Publish</color> msg:'<color=cyan>{msg}</color>'");

      Croquet.Publish("SynqCommand", "pleaseRun", msg);// <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 

  }
  //--------- ||||||||||||||| ----------------------------------------
  private void OnEverybodyRun(string msg) { // <<<<< SUBSCRIBED Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 
      // Croquet.Subscribe( "SynqCommand", "everybodyRun", ReceiveAsMsg); // <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 
      var logPrefix = $"<color=#ff22ff>RECEIVED</color> ";
      var logMsg = $"msg:'<color=cyan>{msg}</color>'";
      var parts = msg.Split(msgSeparator);
      if (parts.Length < 2) {
    Debug.LogError($"{scLogPrefix} Invalid message format: '<color=#ff4444>{msg}</color>'");
    return;
      }

      int commandIdx = int.Parse(parts[0]);
      string commandId = parts[1];
      var parameters = (parts.Length == 2) 
    ? null
    : parts.Skip(2).Select(p => DeserializeValue(p)).ToArray();

      var logIds = $"commandId=<color=white>{commandId}</color> commandIdx=<color=cyan>{commandIdx}</color>";

      var SynqCommand = FindSynqCommandByArr(commandIdx, commandId);
      var arrLookupFailed = (SynqCommand == null);
      if (arrLookupFailed) SynqCommand = FindSynqCommandByDict(commandId);
      if (SynqCommand == null) {
    Debug.LogError($"{scLogPrefix} {logMsg} {logPrefix} message for <color=#ff4444>UNKNOWN</color> {logIds}");
    return;
      }
      SynqCommand.MethodInfo.Invoke(SynqCommand.syncedBehaviour, parameters);

      if (dbg) Debug.Log( (arrLookupFailed)
          ? $"{scLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Executed!</color> using <color=#ff4444>SLOW commandId</color> dictionary lookup. {logIds}"
          : $"{scLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Executed!</color> using <color=#44ff44>FAST commandIdx</color>. {logIds}"
      );
  }
  //--------------------- |||||||||||||||||||| ----------------------------------------
  private SynqCommandInfo FindSynqCommandByArr(int commandIdx, string commandId) {
      if (commandIdx >= 0 && commandIdx < SynqCommandsArr.Length) {
    var SynqCommand = SynqCommandsArr[commandIdx];
    if (!SynqCommand.ConfirmedInArr && SynqCommand.commandId != commandId) {
          Debug.LogError($"{scLogPrefix} Command ID mismatch at commandIdx:<color=cyan>{commandIdx}</color>. Expected <color=white>{SynqCommand.commandId}</color>, got <color=#ff4444>{commandId}</color>");
          return null;
    }
    else {
          SynqCommand.ConfirmedInArr = true;
          if (dbg) Debug.Log($"{scLogPrefix} <color=green>✔️</color>Confirmed SynqCommands[commandId:'<color=white>{commandId}</color>'] matches entry at SynqCommandsArr[commandIdx:<color=cyan>{commandIdx}</color>]");
          return SynqCommand;
    }
      }
      return null;
  }
  //-------------------- ||||||||||||||||||||| ----------------------------------------
  public SynqCommandInfo FindSynqCommandByDict(string commandId) {
      if (SynqCommands.TryGetValue(commandId, out var SynqCommand)) {
    return SynqCommand;
      }
      else {
    Debug.LogError($"{scLogPrefix} Command ID not found in dictionary: <color=white>{commandId}</color>");
    return null;
      }
  }
  #endregion

  #region Messaging Utilities
  //------------ ||||||||||||||||| ----------------------------------------
  private string GenerateCommandId(SynqBehaviour syncBeh, string commandName) {
      return $"{syncBeh.netId}_{commandName}";
  }
  //--------------------- ||||||||||||||||||||| ----------------------------------------
  private SynqCommandInfo CreateSynqCommandInfo(SynqBehaviour syncBeh, MethodInfo method, SynqCommandAttribute attribute, int commandIdx) {
      string commandId = GenerateCommandId(syncBeh, attribute.CustomName ?? method.Name);
      return new SynqCommandInfo(commandId, commandIdx, method, syncBeh, attribute);
  }
  //------------ |||||||||||||| ----------------------------------------
  private string SerializeValue(object value) {
      // Placeholder for actual serialization logic
      return value.ToString();
  }

  //------------ |||||||||||||||| ----------------------------------------
  private object DeserializeValue(string serializedValue) {
      // Placeholder for actual deserialization logic
      return serializedValue;
  }
  #endregion

  #region Singleton
    private static SynqCommand_Mgr _Instance = null;
    public  static SynqCommand_Mgr I { // Usage:   SynqCommand_Mgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
  #endregion

  #region Internal Classes
  //========== ||||||||||||||| ===================
  public class SynqCommandInfo {
      public readonly string commandId;
      public readonly int commandIdx;
      public readonly MethodInfo MethodInfo;
      public readonly SynqBehaviour syncedBehaviour;
      public readonly SynqCommandAttribute attribute;
      public bool ConfirmedInArr { get; set; }

      public SynqCommandInfo(string commandId, int commandIdx, MethodInfo methodInfo, SynqBehaviour syncedBehaviour, SynqCommandAttribute attribute) {
    this.commandId = commandId;
    this.commandIdx = commandIdx;
    MethodInfo = methodInfo;
    this.syncedBehaviour = syncedBehaviour;
    this.attribute = attribute;
    ConfirmedInArr = false;
      }
  }
  #endregion
}

} // namespace MultisynqNS