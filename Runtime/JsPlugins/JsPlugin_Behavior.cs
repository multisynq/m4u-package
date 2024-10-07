using UnityEngine;

namespace Multisynq {

//=================== |||||||||||||||||| ================
abstract public class JsPlugin_Behaviour : MonoBehaviour {

  static public string logPrefix = "[%ye%Js%cy%Plugin%gy%]".TagColors();
  
  abstract public JsPluginCode GetJsPluginCode();
  static public string[] CodeMatchPatterns() => new string[]{"You should define CodeMatchPatterns() in your subclass of JsPlugin_Behaviour"};

  virtual public void Start() {
    #if UNITY_EDITOR
      JsPlugin_Writer.JsPluginFileExists(GetJsPluginCode(), this.GetType().Name);
    #endif
  }

  #if UNITY_EDITOR
    virtual public void WriteMyJsPluginFile() {
      // if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void WriteMyJsPluginFile()");
      var jsPlugin = GetJsPluginCode();
      JsPlugin_Writer.WriteOneJsPluginFile(jsPlugin);
    }
  #else
    virtual public void WriteMyJsPluginFile() { }
  #endif

}


} // namespace MultisynqNS