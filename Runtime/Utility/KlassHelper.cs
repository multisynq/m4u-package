using System;
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

      if (method != null) {
        return method;
      }

      type = type.BaseType;
    }

    return null;
  }

  public static Type[] GetSubclassTypes(Type type) {
    return Assembly.GetAssembly(type)
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(type))
      .ToHashSet() // remove duplicates
      .ToArray();
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
}