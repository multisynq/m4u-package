using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SyncCommandAttribute : Attribute {
  public string CustomName { get; set; }
}

public class SyncCommandMgr : JsCodeInjectingMonoBehavior {
  
  private Dictionary<string, SyncCommandInfo> syncCommands;
  private SyncCommandInfo[] syncCommandsArr;
  private static char msgSeparator = '|';
  private static string scLogPrefix = "<color=#7777FF>[SyncCommand]</color> ";

    public SyncCommandMgr Instance { get; private set; }

    #region JavaScript
    public override string JsPluginFileName() { return "plugins/SyncCommandMgrModel.js"; }

  public override string JsPluginCode() {
    return @"
      import { Model } from '@croquet/croquet';
      
      export class SyncCommandMgrModel extends Model {
          init(options) {
              super.init(options);
              this.subscribe('SyncCommand', 'execute1', this.onSyncCommandExecute);
              console.log('### <color=magenta>SyncCommandMgrModel.init() <<<<<<<<<<<<<<<<<<<<< </color>');
          }
          onSyncCommandExecute(msg) {
              console.log(`<color=blue>[SyncCommand]</color> <color=yellow>JS</color> CroquetModel <color=magenta>SyncCommandMgrModel.onSyncCommandExecute()</color> msg = <color=white>${JSON.stringify(msg)}</color>`);
              this.publish('SyncCommand', 'execute2', msg);
          }
      }
      SyncCommandMgrModel.register('SyncCommandMgrModel');
    ".LessIndent();
  }

  public override void OnInjectJsPluginCode() {
    Debug.Log($"{logPrefix} override public void OnInjectJsPluginCode()");
    base.OnInjectJsPluginCode();
  }
  #endregion
    public class SyncCommandInfo {
        public readonly string commandId;
        public readonly int commandIdx;
        public readonly MethodInfo MethodInfo;
        public readonly SyncedBehaviour syncedBehaviour;
        public readonly SyncCommandAttribute attribute;
        public Func<object[], object> Invoker { get; set; }
        public bool ConfirmedInArr { get; set; }

        public SyncCommandInfo(string commandId, int commandIdx, MethodInfo methodInfo, SyncedBehaviour syncedBehaviour, SyncCommandAttribute attribute) {
            this.commandId = commandId;
            this.commandIdx = commandIdx;
            MethodInfo = methodInfo;
            this.syncedBehaviour = syncedBehaviour;
            this.attribute = attribute;
            ConfirmedInArr = false;
            Invoker = parameters => methodInfo.Invoke(syncedBehaviour, parameters);
        }
    }

    void Awake() {
        Instance = this;
        ProcessSyncCommands();
    }

    void Start() {
        Croquet.Subscribe("SyncCommand", "execute2", ReceiveAsMsg);

        foreach (var syncCommand in syncCommands) {
            Debug.Log($"{scLogPrefix} Found <color=white>{syncCommand.Key}</color>");
        }
    }

    private void ProcessSyncCommands() {
        syncCommands = new Dictionary<string, SyncCommandInfo>();
        List<SyncCommandInfo> syncCommandsList = new List<SyncCommandInfo>();

        int commandIdx = 0;
        foreach (SyncedBehaviour syncBeh in FindObjectsOfType<SyncedBehaviour>()) {
            var type = syncBeh.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods) {
                var attribute = method.GetCustomAttribute<SyncCommandAttribute>();
                if (attribute != null) {
                    var syncCommandInfo = CreateSyncCommandInfo(syncBeh, method, attribute, commandIdx++);
                    syncCommands.Add(syncCommandInfo.commandId, syncCommandInfo);
                    syncCommandsList.Add(syncCommandInfo);

                    // Create a wrapper method
                    CreateWrapperMethod(syncBeh, method, syncCommandInfo.commandId);
                }
            }
        }

        syncCommandsArr = syncCommandsList.ToArray();
    }

    private SyncCommandInfo CreateSyncCommandInfo(SyncedBehaviour syncBeh, MethodInfo method, SyncCommandAttribute attribute, int commandIdx) {
        string commandId = GenerateCommandId(syncBeh, attribute.CustomName ?? method.Name);
        return new SyncCommandInfo(commandId, commandIdx, method, syncBeh, attribute);
    }

    private void CreateWrapperMethod(SyncedBehaviour syncBeh, MethodInfo originalMethod, string commandId) {
        var parameters = originalMethod.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
        
        // Create a delegate type that matches the original method signature
        var delegateType = Expression.GetDelegateType(parameterTypes.Concat(new[] { originalMethod.ReturnType }).ToArray());
        
        // Create parameters for the lambda expression
        var lambdaParams = parameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        
        // Create an expression to call ExecuteCommand
        var executeCommandMethod = typeof(SyncCommandMgr).GetMethod("ExecuteCommand");
        var executeCommandCall = Expression.Call(
            Expression.Constant(this),
            executeCommandMethod,
            Expression.Constant(commandId),
            Expression.NewArrayInit(
                typeof(object),
                lambdaParams.Select(p => Expression.Convert(p, typeof(object)))
            )
        );
        
        // If the original method returns void, just call ExecuteCommand
        // Otherwise, cast the result of ExecuteCommand to the return type
        Expression body;
        if (originalMethod.ReturnType == typeof(void))
        {
            body = executeCommandCall;
        }
        else
        {
            body = Expression.Convert(executeCommandCall, originalMethod.ReturnType);
        }
        
        // Create and compile the lambda expression
        var lambda = Expression.Lambda(delegateType, body, lambdaParams);
        var compiledWrapper = lambda.Compile();
        
        // Use reflection to replace the original method with the wrapper
        var field = syncBeh.GetType().GetField("<" + originalMethod.Name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            // If it's an auto-property, replace the backing field
            field.SetValue(syncBeh, compiledWrapper);
        }
        else
        {
            // Otherwise, try to find and replace a delegate field with the same name as the method
            field = syncBeh.GetType().GetField(originalMethod.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(syncBeh, compiledWrapper);
            }
            else
            {
                Debug.LogWarning($"Could not find a suitable field to replace for method {originalMethod.Name}. The SyncCommand may not work as expected.");
            }
        }
    }

    public static string GenerateCommandId(Type type, string commandName) {
        var syncBeh = FindObjectsOfType(type).FirstOrDefault() as SyncedBehaviour;
        if (syncBeh != null) {
            return $"{syncBeh.netId}_{commandName}";
        }
        return $"{type.Name}_{commandName}";
    }

    private string GenerateCommandId(SyncedBehaviour syncBeh, string commandName) {
        return $"{syncBeh.netId}_{commandName}";
    }

    public void ExecuteCommand(string commandId, params object[] parameters) {
        if (syncCommands.TryGetValue(commandId, out var syncCommandInfo)) {
            // Invoke the command locally
            syncCommandInfo.Invoker(parameters);

            // Serialize and send the command
            string serializedParams = string.Join(msgSeparator.ToString(), parameters.Select(p => SerializeValue(p)));
            var msg = $"{syncCommandInfo.commandIdx}{msgSeparator}{commandId}{msgSeparator}{serializedParams}";
            Debug.Log($"{scLogPrefix} <color=#ff22ff>SEND</color> msg:'<color=cyan>{msg}</color>' for command <color=cyan>{syncCommandInfo.commandIdx}</color>|<color=white>{commandId}</color>");
            Croquet.Publish("SyncCommand", "execute1", msg);
        }
        else {
            Debug.LogError($"{scLogPrefix} Command not found: {commandId}");
        }
    }
  private void ReceiveAsMsg(string msg) {
    var logPrefix = $"<color=#ff22ff>RECEIVED</color> ";
    var logMsg = $"msg:'<color=cyan>{msg}</color>'";
    var parts = msg.Split(msgSeparator);
    if (parts.Length < 3) {
      Debug.LogError($"{scLogPrefix} Invalid message format: '<color=#ff4444>{msg}</color>'");
      return;
    }

    int commandIdx = int.Parse(parts[0]);
    string commandId = parts[1];
    var parameters = parts.Skip(2).Select(p => DeserializeValue(p)).ToArray();

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

  public SyncCommandInfo FindSyncCommandByDict(string commandId) {
    if (syncCommands.TryGetValue(commandId, out var syncCommand)) {
      return syncCommand;
    }
    else {
      Debug.LogError($"{scLogPrefix} Command ID not found in dictionary: <color=white>{commandId}</color>");
      return null;
    }
  }

  private string SerializeValue(object value) {
    // Placeholder for actual serialization logic
    return value.ToString();
  }

  private object DeserializeValue(string serializedValue) {
    // Placeholder for actual deserialization logic
    return serializedValue;
  }
}