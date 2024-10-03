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
      CheckIfMyJsCodeIsPresent();
    #endif
  }

  #if UNITY_EDITOR
    virtual public void WriteJsPluginCode() {
        // if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void WriteJsPluginCode()");
        var jsPlugin = GetJsPluginCode();
        JsPlugin_Writer.WriteJsPluginCode(jsPlugin);
    }
  #else
      virtual public void WriteJsPluginCode() { }
  #endif

  #if UNITY_EDITOR
    public void CheckIfMyJsCodeIsPresent() {
      var jsPlugin = GetJsPluginCode();
      JsPlugin_Writer.CheckIfMyJsCodeIsPresent(jsPlugin, this.GetType().Name);
    }
    //---------------- |||||||||||||||||||||||||||| -------------------------
    public static bool CheckIndexJsForPluginsImport() {
      return JsPlugin_Writer.CheckIndexJsForPluginsImport();
    }

    public static void EnsureFinders() {
      // JsPlugin_Writer.activeSyncBehaviours = FindObjectsOfType<SynqBehaviour>(false);
      JsPlugin_Writer.FindSynqBehObjects      = FindObjectsOfType<SynqBehaviour>;
      JsPlugin_Writer.CopyOf_FindObjectOfType = FindObjectOfType;
    }

    public static void WriteAllJsPlugins() {
      EnsureFinders();
      JsPlugin_Writer.WriteAllJsPlugins();
    }
    public static void WriteMissingJsPlugins() {
      EnsureFinders();
      JsPlugin_Writer.WriteMissingJsPlugins();
    }
    static public JsPlugin_Writer.JsPluginReport AnalyzeAllJsPlugins() {
      EnsureFinders();
      return JsPlugin_Writer.AnalyzeAllJsPlugins();
    }
  #endif
}


} // namespace MultisynqNS