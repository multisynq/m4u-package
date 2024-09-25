using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

static public class Logger {

  static private string blue = "#006AFF";
  static private string lightBlue = "#0196FF";

  static private string spacer  = "-------------";
  // static private string hspacer = "=============";
  // static private string wave    = "◠‿◠‿◠‿◠‿◠‿◠‿◠‿";
  // static private string wave2   = "'``'-.,_,.-'``'-.,_,.";
  static private string wave3   = "ø,¸¸,ø¤º°`°º¤";
  static private string wave3r  = "¤º°`°º¤ø,¸¸,ø";

  static private string docsRootUrl = "https://croquet.io/dev/docs/unity/#";

  public static void Header(string message, string s1 = null, string s2 = null, string c1 = null, string c2 = null, string suffix = null) {
    if (s1 == null) s1 = wave3;
    if (s2 == null) s2 = wave3r;
    if (c1 == null) c1 = blue;
    if (c2 == null) c2 = lightBlue;
    UnityEngine.Debug.Log($"<color={c1}>{s1} [ <color={c2}>{message}</color> ] {s2}</color>{suffix}");
  }

  static public void MethodHeader(int depth=4) {
    Header(GetClassAndMethod(depth), spacer, spacer);
  }
  static public void MethodHeaderAndOpenUrl(int depth=0) {
    var shortNm = Regex.Replace(GetMethodName(), @"^Clk_(.+)_Docs$", "$1");
    string url = $"{docsRootUrl}{shortNm}";
    Header(GetClassAndMethod(depth), spacer, spacer, null, null, "\n   " + url);
    Application.OpenURL(url);
  }
  
  static public StackFrame GetStackFrame(int depth = 2, string withoutSubstringInMethod = null) {
    StackTrace stackTrace = new StackTrace();
    var frame = stackTrace.GetFrame(depth);
    int maxDepth = stackTrace.FrameCount;
    if (withoutSubstringInMethod != null) {
      while (frame.GetMethod().Name.Contains(withoutSubstringInMethod) && depth < maxDepth) {
        frame = stackTrace.GetFrame(depth++);
      }
    }
    return frame;
  }

  static public MethodBase GetMethod(int depth = 2) {
    StackFrame stackFrame = GetStackFrame(depth);
    return stackFrame.GetMethod();
  }

  static public string GetMethodName(int depth = 2) {
    MethodBase methodBase = GetMethod(depth);
    return methodBase.Name;
  }

  static public string GetClassAndMethod(int depth = 2) {
    MethodBase methodBase = GetMethod(depth);
    return $"{methodBase.ReflectedType.Name}.{methodBase.Name}";
  }
}