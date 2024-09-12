using Mono.CecilX;
using Mono.CecilX.Cil;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;

public class MethodWrapper
{
    private static AssemblyDefinition assemblyDefinition;
    private static string assemblyPath;

    public static void Initialize(Assembly assembly)
    {
        assemblyPath = assembly.Location;
        var readerParameters = new ReaderParameters { ReadWrite = true };
        assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
    }

    public static void ReplaceMethod(MethodInfo methodToReplace, MethodInfo injectMethod)
    {
        if (assemblyDefinition == null)
        {
            throw new InvalidOperationException("MethodWrapper not initialized. Call Initialize() first.");
        }

        var module = assemblyDefinition.MainModule;

        var targetType = module.Types.FirstOrDefault(t => t.FullName == methodToReplace.DeclaringType.FullName);
        if (targetType == null)
            throw new ArgumentException($"Type {methodToReplace.DeclaringType.FullName} not found in assembly.");

        var targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == methodToReplace.Name);
        if (targetMethod == null)
            throw new ArgumentException($"Method {methodToReplace.Name} not found in type {methodToReplace.DeclaringType.FullName}.");

        var injectMethodRef = module.ImportReference(injectMethod);

        var processor = targetMethod.Body.GetILProcessor();
        
        // Clear existing instructions
        targetMethod.Body.Instructions.Clear();

        // Create a variable to store the return value if the method returns something
        VariableDefinition returnVariable = null;
        if (!targetMethod.ReturnType.Equals(module.TypeSystem.Void))
        {
            returnVariable = new VariableDefinition(targetMethod.ReturnType);
            targetMethod.Body.Variables.Add(returnVariable);
        }

        // Load all parameters onto the stack for the inject method
        for (int i = 0; i < targetMethod.Parameters.Count; i++)
        {
            processor.Emit(OpCodes.Ldarg, i);
        }

        // Call the inject method
        processor.Emit(OpCodes.Call, injectMethodRef);

        // Load all parameters again for the original method
        for (int i = 0; i < targetMethod.Parameters.Count; i++)
        {
            processor.Emit(OpCodes.Ldarg, i);
        }

        // Call the original method body
        processor.Emit(OpCodes.Call, targetMethod);

        // If the method returns a value, store it in the local variable
        if (returnVariable != null)
        {
            processor.Emit(OpCodes.Stloc, returnVariable);
            processor.Emit(OpCodes.Ldloc, returnVariable);
        }

        // Return
        processor.Emit(OpCodes.Ret);
    }

    public static void ApplyChanges()
    {
        if (assemblyDefinition == null)
        {
            throw new InvalidOperationException("MethodWrapper not initialized or changes already applied.");
        }

        try
        {
            // Write the modified assembly to a new file
            string newAssemblyPath = Path.Combine(Path.GetDirectoryName(assemblyPath), 
                                                  Path.GetFileNameWithoutExtension(assemblyPath) + "_modified" + Path.GetExtension(assemblyPath));
            
            assemblyDefinition.Write(newAssemblyPath);

            // Load the modified assembly
            Assembly.LoadFrom(newAssemblyPath);

            Debug.Log($"Modified assembly written to: {newAssemblyPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying changes: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            // Clean up
            assemblyDefinition.Dispose();
            assemblyDefinition = null;
        }
    }
}
