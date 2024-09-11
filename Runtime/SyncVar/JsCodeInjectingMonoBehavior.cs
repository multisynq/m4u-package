using UnityEngine;

abstract public class JsCodeInjectingMonoBehavior : MonoBehaviour {
  abstract public void InjectJsCode();
}

public class JsCodeInjectingMgr : MonoBehaviourSingleton<JsCodeInjectingMgr> {

  // string myAppIndexJsFile;
  static public string logPrefix = "[<color=yellow>Js</color><color=cyan>CodeInject</color>]";

  public void InjectAllJsCode() {
    foreach (var jci in FindObjectsOfType<JsCodeInjectingMonoBehavior>()) {
      jci.InjectJsCode();
    }
  }

  public void InjectCode(string fileNm, string classCode) { 
    CqFile.AppFolder().DeeperFile(fileNm).WriteAllText(classCode, true); // true = create needed folders
  }
}
