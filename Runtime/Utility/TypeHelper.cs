using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

public static class TypeHelper {

  public static MethodInfo FindMethod(
    this Type type, 
    string methodName, 
    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, 
    Type[] parameterTypes = null
  ) {
    while (type != null) {
      MethodInfo method = parameterTypes == null
        ? type.GetMethod(methodName, bindingFlags)
        : type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);

      if (method != null) return method;
      type = type.BaseType;
    }

    return null;
  }

  public static Type[] GetImplementingTypes(this Type interfaceType) {
    return Assembly.GetAssembly(interfaceType)
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
      .ToArray();
  }

  public static PropertyInfo[] GetPublicProperties(this Type type) {
    return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
  }

  public static MethodInfo[] GetPublicMethods(this Type type) {
    return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
  }

  public static Dictionary<Type, TResult> DictOfSubclassStaticMethodResults<TResult>(this Type type, string methodName, params object[] parameters) {
    Dictionary<Type, TResult> results = new();
    
    foreach (var subclass in type.GetSubclassTypes()) {
      var method = subclass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
      if (method == null) {
        Console.WriteLine($"Method {methodName} not found in {subclass.Name}");
        continue;
      }
      results.Add(subclass, (TResult)method.Invoke(null, parameters));
    }
    return results;
  }

  public static TResult InvokeStaticMethod<TBase, TResult>(string methodName, params object[] parameters) {
    return (TResult)typeof(TBase)
      .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
      .Invoke(null, parameters);
  }
  public static TResult InvokeStaticMethod<TResult>(Type type, string methodName, params object[] parameters) {
    UnityEngine.Debug.Log($"InvokeStaticMethod {type.Name}.{methodName}");
    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
    if (method != null) {
      return (TResult)method.Invoke(null, parameters);
    } else {
      throw new Exception($"Method {methodName} not found in {type.Name}");
    }
  }

  public static Type[] GetSubclassTypes(this Type baseType) {
    return Assembly.GetAssembly(baseType)
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
      .ToHashSet()
      .ToArray(); // remove duplicates;
  }
}