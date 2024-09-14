using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SyncCommandAttribute : Attribute {
  public string CustomName { get; set; }
}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SyncRPCAttribute : SyncCommandAttribute {
}

//========== |||||||||||||| =====================================================
public class SyncCommand_Mgr : JsCodeInjecting_MonoBehavior {
  #region Fields
    private Dictionary<string, SyncCommandInfo> syncCommands;
    private SyncCommandInfo[] syncCommandsArr;
    private static char msgSeparator = '|';
    private static string scLogPrefix = "<color=#7777FF>[SyncCommand]</color> ";
  #endregion

  #region Singleton
    private static SyncCommand_Mgr _Instance = null;
    public static SyncCommand_Mgr I { 
      get { 
        _Instance = Singletoner.EnsureInst(_Instance);
        return _Instance;
      }
      private set { _Instance = value; }
    }
  #endregion

  #region JavaScript
    public override string JsPluginFileName() { return "plugins/SyncCommand_Mgr_Model.js"; }

    public override string JsPluginCode() {
      return @"
        import { Model } from '@croquet/croquet';
        
        export class SyncCommand_Mgr_Model extends Model {
            init(options) {
                super.init(options);
                this.subscribe('SyncCommand', 'execute1', this.onSyncCommandExecute);
                console.log('### <color=magenta>SyncCommand_Mgr_Model.init() <<<<<<<<<<<<<<<<<<<<< </color>');
            }
            onSyncCommandExecute(msg) {
                console.log(`<color=blue>[SyncCommand]</color> <color=yellow>JS</color> CroquetModel <color=magenta>SyncCommandMgrModel.onSyncCommandExecute()</color> msg = <color=white>${JSON.stringify(msg)}</color>`);
                this.publish('SyncCommand', 'execute2', msg);
            }
        }
        SyncCommand_Mgr_Model.register('SyncCommandMgrModel');
      ".LessIndent();
    }

    public override void InjectJsPluginCode() {
      Debug.Log($"{logPrefix} override public void InjectJsPluginCode()");
      base.InjectJsPluginCode();
    }
  #endregion

  #region Internal Classes
    //========== ||||||||||||||| ===================
    public class SyncCommandInfo {
      public readonly string commandId;
      public readonly int commandIdx;
      public readonly MethodInfo MethodInfo;
      public readonly SyncedBehaviour syncedBehaviour;
      public readonly SyncCommandAttribute attribute;
      public bool ConfirmedInArr { get; set; }

      public SyncCommandInfo(string commandId, int commandIdx, MethodInfo methodInfo, SyncedBehaviour syncedBehaviour, SyncCommandAttribute attribute) {
        this.commandId = commandId;
        this.commandIdx = commandIdx;
        MethodInfo = methodInfo;
        this.syncedBehaviour = syncedBehaviour;
        this.attribute = attribute;
        ConfirmedInArr = false;
      }
    }
  #endregion

  #region Start/Update
    //------------------ ||||| ----------------------------------------
    override public void Start() {
      base.Start();

      Croquet.Subscribe("SyncCommand", "execute2", ReceiveAsMsg); // <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 

      syncCommands = new Dictionary<string, SyncCommandInfo>();
      List<SyncCommandInfo> syncCommandsList = new List<SyncCommandInfo>();

      int commandIdx = 0;
      foreach (SyncedBehaviour syncBeh in FindObjectsOfType<SyncedBehaviour>()) {
        var type = syncBeh.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods) {
          var attribute = method.GetCustomAttribute<SyncCommandAttribute>() ?? method.GetCustomAttribute<SyncRPCAttribute>();
          if (attribute != null) {
            var syncCommandInfo = CreateSyncCommandInfo(syncBeh, method, attribute, commandIdx++);
            syncCommands.Add(syncCommandInfo.commandId, syncCommandInfo);
            syncCommandsList.Add(syncCommandInfo);
          }
        }
      }

      syncCommandsArr = syncCommandsList.ToArray();

      foreach (var syncCommand in syncCommands) {
        Debug.Log($"{scLogPrefix} Found <color=white>{syncCommand.Key}</color>");
      }
    }
  #endregion
  #region Messaging
    //--------- |||||||||||||||||||||| ----------------------------------------
    public void PublishSyncCommandCall(SyncedBehaviour syncBeh, RpcTarget tgt, string commandId, params object[] parameters) {
      // TODO: Implement the actual logic to send the command only other clients or all clients
      // if (tgt == RpcTarget.Others) {
      // else if (tgt == RpcTarget.All) {
      if (parameters.Length == 0) { PublishSyncCommandCall(syncBeh, commandId); }
      else { PublishSyncCommandCall(syncBeh, commandId, parameters); }
    }
    //--------- |||||||||||||||||||||| ----------------------------------------
    public void PublishSyncCommandCall(SyncedBehaviour syncBeh, string commandId, params object[] parameters) {

      string cmdWithNetId = $"{syncBeh.netId}_{commandId}";
      string serializedParams = (parameters.Length == 0) ? "" : msgSeparator+string.Join(msgSeparator.ToString(), parameters.Select(p => SerializeValue(p)));
      var msg = $"{syncCommands[cmdWithNetId].commandIdx}{msgSeparator}{cmdWithNetId}{serializedParams}";
      Debug.Log($"{scLogPrefix} <color=#ff22ff>Publish</color> msg:'<color=cyan>{msg}</color>'");

      Croquet.Publish("SyncCommand", "execute1", msg);// <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 

    }
    //--------- ||||||||||||| ----------------------------------------
    private void ReceiveAsMsg(string msg) { // <<<<< SUBSCRIBED Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 
      // Croquet.Subscribe( "SyncCommand", "execute2", ReceiveAsMsg); // <<<<< Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq Cq 
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

      var syncCommand = FindSyncCommandByArr(commandIdx, commandId);
      var arrLookupFailed = (syncCommand == null);
      if (arrLookupFailed) syncCommand = FindSyncCommandByDict(commandId);
      if (syncCommand == null) {
        Debug.LogError($"{scLogPrefix} {logMsg} {logPrefix} message for <color=#ff4444>UNKNOWN</color> {logIds}");
        return;
      }
      syncCommand.MethodInfo.Invoke(syncCommand.syncedBehaviour, parameters);

      Debug.Log(arrLookupFailed
          ? $"{scLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Executed!</color> using <color=#ff4444>SLOW commandId</color> dictionary lookup. {logIds}"
          : $"{scLogPrefix} {logPrefix} {logMsg} <color=#33FF33>Executed!</color> using <color=#44ff44>FAST commandIdx</color>. {logIds}"
      );
    }
    //--------------------- |||||||||||||||||||| ----------------------------------------
    private SyncCommandInfo FindSyncCommandByArr(int commandIdx, string commandId) {
      if (commandIdx >= 0 && commandIdx < syncCommandsArr.Length) {
        var syncCommand = syncCommandsArr[commandIdx];
        if (!syncCommand.ConfirmedInArr && syncCommand.commandId != commandId) {
          Debug.LogError($"{scLogPrefix} Command ID mismatch at commandIdx:<color=cyan>{commandIdx}</color>. Expected <color=white>{syncCommand.commandId}</color>, got <color=#ff4444>{commandId}</color>");
          return null;
        }
        else {
          syncCommand.ConfirmedInArr = true;
          Debug.Log($"{scLogPrefix} <color=green>✔️</color>Confirmed syncCommands[commandId:'<color=white>{commandId}</color>'] matches entry at syncCommandsArr[commandIdx:<color=cyan>{commandIdx}</color>]");
          return syncCommand;
        }
      }
      return null;
    }
    //-------------------- ||||||||||||||||||||| ----------------------------------------
    public SyncCommandInfo FindSyncCommandByDict(string commandId) {
      if (syncCommands.TryGetValue(commandId, out var syncCommand)) {
        return syncCommand;
      }
      else {
        Debug.LogError($"{scLogPrefix} Command ID not found in dictionary: <color=white>{commandId}</color>");
        return null;
      }
    }
  #endregion

  #region Messaging Utilities
    //------------ ||||||||||||||||| ----------------------------------------
    private string GenerateCommandId(SyncedBehaviour syncBeh, string commandName) {
      return $"{syncBeh.netId}_{commandName}";
    }
    //--------------------- ||||||||||||||||||||| ----------------------------------------
    private SyncCommandInfo CreateSyncCommandInfo(SyncedBehaviour syncBeh, MethodInfo method, SyncCommandAttribute attribute, int commandIdx) {
      string commandId = GenerateCommandId(syncBeh, attribute.CustomName ?? method.Name);
      return new SyncCommandInfo(commandId, commandIdx, method, syncBeh, attribute);
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
}