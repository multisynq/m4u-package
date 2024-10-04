using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // TODO: include as many of these similarly as we can

#if UNITY_EDITOR
  using UnityEditor;
#endif

namespace Multisynq {

//=================== |||||||||||||||||| ================
abstract public class JsPlugin_Behaviour : MonoBehaviour {

  static public string logPrefix = "[%ye%Js%cy%Plugin%gy%]".TagColors();
  // static bool dbg = true;
  abstract public JsPluginCode GetJsPluginCode(); // not static since this must find scene MonoBehaviours
  static public string[] CodeMatchPatterns() => new string[]{"You should define CodeMatchPatterns() in your subclass of JsPlugin_Behaviour"};

  virtual public void Start() {
    #if UNITY_EDITOR
      JsPluginFileExists();
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

  #if UNITY_EDITOR
    public void JsPluginFileExists() {
      JsPlugin_Writer.JsPluginFileExists(GetJsPluginCode(), this.GetType().Name);
    }
    //---------------- |||||||||||||||||||||||||||| -------------------------
    public static void WriteMissingJsPlugins() {
      JsPlugin_Writer.WriteMissingJsPlugins();
    }
    static public JsPlugin_Writer.JsPluginReport AnalyzeAllJsPlugins() {
      return JsPlugin_Writer.AnalyzeAllJsPlugins();
    }
  #endif
}


} // namespace MultisynqNS