// using System;
// using System.Linq;
// using Mono.CecilX;
// using Mono.CecilX.Cil;
// using UnityEngine;

// // using Mono.CecilX.Rocks;

// public static class SyncCommandProcessor
// {
//     public static void ProcessSyncCommand(ModuleDefinition module, TypeDefinition typeDefinition, MethodDefinition method)
//     {
//         // Create a new method with the original implementation
//         MethodDefinition originalMethod = new MethodDefinition(
//             "Original" + method.Name,
//             method.Attributes,
//             method.ReturnType
//         );
        
//         // Copy the original method body to the new method
//         foreach (var instruction in method.Body.Instructions)
//         {
//             originalMethod.Body.Instructions.Add(instruction);
//         }
        
//         // Add the new method to the type
//         typeDefinition.Methods.Add(originalMethod);

//         // Clear the original method body
//         method.Body.Instructions.Clear();
        
//         // Generate new IL for the original method
//         ILProcessor worker = method.Body.GetILProcessor();

//         // Generate command ID
//         GenerateCommandId(module, worker, method);

//         // Add code to prepare parameters
//         GenerateParameterPreparation(module, worker, method);

//         // Add code to execute the command
//         GenerateCommandExecution(module, worker, method);

//         // Return if the method is not void
//         if (method.ReturnType.FullName != "System.Void")
//         {
//             worker.Emit(OpCodes.Ldloca_S, (byte)0);
//             worker.Emit(OpCodes.Initobj, method.ReturnType);
//             worker.Emit(OpCodes.Ldloc_0);
//         } else {
//             worker.Emit(OpCodes.Pop);
//             Debug.LogWarning("SyncCommandProcessor: Method " + method.Name + " is void"); 
//         }

//         worker.Emit(OpCodes.Ret);
//     }

//     private static void GenerateCommandId(ModuleDefinition module, ILProcessor worker, MethodDefinition method)
//     {
//         // Get the SyncCommandAttribute
//         var syncCommandAttribute = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "SyncCommandAttribute");
        
//         // Generate command ID based on the custom name or method name
//         string commandName = method.Name;
//         if (syncCommandAttribute != null)
//         {
//             var customNameProperty = syncCommandAttribute.Properties.FirstOrDefault(p => p.Name == "CustomName");
//             if (customNameProperty.Name != null && customNameProperty.Argument.Value is string customName)
//             {
//                 commandName = customName;
//             }
//         }
        
//         // Load 'this'
//         worker.Emit(OpCodes.Ldarg_0);
        
//         // Call GetType() to get the runtime type
//         var getTypeMethod = module.ImportReference(typeof(object).GetMethod("GetType"));
//         worker.Emit(OpCodes.Callvirt, getTypeMethod);
        
//         // Load the command name
//         worker.Emit(OpCodes.Ldstr, commandName);
        
//         // Call GenerateCommandId on SyncCommandMgr
//         var syncCommandMgrType = module.ImportReference(typeof(SyncCommandMgr)).Resolve();
//         var generateCommandIdMethod = syncCommandMgrType.Methods.First(m => m.Name == "GenerateCommandId" && m.Parameters.Count == 2);
//         worker.Emit(OpCodes.Call, module.ImportReference(generateCommandIdMethod));
        
//         // Store the result in a local variable
//         worker.Emit(OpCodes.Stloc_0);
//     }

//     private static void GenerateParameterPreparation(ModuleDefinition module, ILProcessor worker, MethodDefinition method)
//     {
//         // Create an object array to hold the parameters
//         worker.Emit(OpCodes.Ldc_I4, method.Parameters.Count);
//         var objectArrayType = module.ImportReference(typeof(object[]));
//         worker.Emit(OpCodes.Newarr, module.ImportReference(typeof(object)));
//         worker.Emit(OpCodes.Stloc_1);

//         // Store each parameter in the array
//         for (int i = 0; i < method.Parameters.Count; i++)
//         {
//             worker.Emit(OpCodes.Ldloc_1);
//             worker.Emit(OpCodes.Ldc_I4, i);
//             worker.Emit(OpCodes.Ldarg, i + 1);
            
//             // Box value types
//             if (method.Parameters[i].ParameterType.IsValueType)
//             {
//                 worker.Emit(OpCodes.Box, method.Parameters[i].ParameterType);
//             }
            
//             worker.Emit(OpCodes.Stelem_Ref);
//         }
//     }

//     private static void GenerateCommandExecution(ModuleDefinition module, ILProcessor worker, MethodDefinition method)
//     {
//         // Get the SyncCommandMgr type
//         var syncCommandMgrType = module.ImportReference(typeof(SyncCommandMgr)).Resolve();

//         // Get the Instance property
//         var instanceProperty = syncCommandMgrType.Properties.First(p => p.Name == "Instance");
//         var getInstanceMethod = module.ImportReference(instanceProperty.GetMethod);

//         // Call get_Instance to get the SyncCommandMgr instance
//         worker.Emit(OpCodes.Call, getInstanceMethod);
        
//         // Load the command ID
//         worker.Emit(OpCodes.Ldloc_0);
        
//         // Load the parameter array
//         worker.Emit(OpCodes.Ldloc_1);
        
//         // Get and call the ExecuteCommand method
//         var executeCommandMethod = syncCommandMgrType.Methods.First(m => m.Name == "ExecuteCommand" && m.Parameters.Count == 2);
//         worker.Emit(OpCodes.Callvirt, module.ImportReference(executeCommandMethod));
//     }
// }