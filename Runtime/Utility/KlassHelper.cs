using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

public static class KlassHelper {

  public static MethodInfo FindMethod(
    Type type, 
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

  public static Type[] GetImplementingTypes(Type interfaceType) {
    return Assembly.GetAssembly(interfaceType)
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
      .ToArray();
  }

  public static PropertyInfo[] GetPublicProperties(Type type) {
    return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
  }

  public static MethodInfo[] GetPublicMethods(Type type) {
    return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
  }

  public static IEnumerable<TResult> InvokeStaticMethodOnSubclasses<TBase, TResult>(string methodName, params object[] parameters) {
    return GetSubclassTypes(typeof(TBase))
      .Select(subclass => {
        var method = subclass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null) {
          Console.WriteLine($"Method {methodName} not found in {subclass.Name}");
          return default;
        }
        return (TResult)method.Invoke(null, parameters);
      })
      .Where(result => result != null);
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

  public static Type[] GetSubclassTypes(Type baseType) {
    return Assembly.GetAssembly(baseType)
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
      .ToHashSet()
      .ToArray(); // remove duplicates;
  }
}