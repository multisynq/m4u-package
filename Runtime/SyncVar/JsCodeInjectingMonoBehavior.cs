using UnityEngine;

abstract public class JsCodeInjectingMonoBehavior : MonoBehaviour {
  // TODO: make #if UNITY_EDITOR throughout code injection
  static public string logPrefix = "[<color=yellow>Js</color><color=cyan>CodeInject</color>]";
  abstract public string JsPluginFileName();
  abstract public string JsPluginCode();
  static bool dbg = true;

  virtual public void OnInjectJsPluginCode() {
    if (dbg)  Debug.Log($"{logPrefix} <color=white>BASE</color>   virtual public void OnInjectJsPluginCode()");

    var modelClassPath = CqFile.AppFolder().DeeperFile(JsPluginFileName());
    // if (modelClassPath.Exists()) {
    //   Debug.LogWarning($"{svLogPrefix} '{modelClassPath.shortPath}' already present at '{modelClassPath.longPath}'");
    // } else {
      if (dbg)  Debug.Log($"{logPrefix} Writing new file '{modelClassPath.shortPath}'");
      string jsCode = JsPluginCode().LessIndent();
      modelClassPath.WriteAllText(jsCode, true); // true = create needed folders
    // }
  }
}

public class JsCodeInjectingMgr : SingletonMB<JsCodeInjectingMgr> {

  // string myAppIndexJsFile;
  static public string logPrefix = "[<color=yellow>Js</color><color=cyan>CodeInject</color>]";

  public void InjectAllJsPluginCode() {
    foreach (var jci in FindObjectsOfType<JsCodeInjectingMonoBehavior>()) {
      jci.OnInjectJsPluginCode();
    }
  }

  public void InjectCode(string fileNm, string classCode) { 
    CqFile.AppFolder().DeeperFile(fileNm).WriteAllText(classCode, true); // true = create needed folders
  }
}
