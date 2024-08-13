using UnityEngine;

static public class Logger {

  static private string blue = "#006AFF";
  static private string lightBlue = "#0196FF";

  static private string hspacer = "=============";

  public static void Header(string message) {
    Debug.Log($"<color={blue}>{hspacer} [ <color={lightBlue}>{message}</color> ] {hspacer}</color>");
  }

  static public void MethodHeader() {
    Header(GetClassAndMethod());
  }

  static public string GetClassAndMethod(int depth = 2) {
    System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
    System.Diagnostics.StackFrame stackFrame = stackTrace.GetFrame(depth);
    System.Reflection.MethodBase methodBase = stackFrame.GetMethod();
    return $"{methodBase.ReflectedType.Name}.{methodBase.Name}";
  }
}