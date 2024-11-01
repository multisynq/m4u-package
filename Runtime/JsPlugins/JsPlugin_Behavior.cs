using UnityEngine;

namespace Multisynq {

//=================== |||||||||||||||||| ================
abstract public class JsPlugin_Behaviour : MonoBehaviour {

  static public string logPrefix = "[%ye%Js%cy%Plugin%gy%]".TagColors();
  
  /// <summary>
  /// Retrieves the JavaScript plugin code for this behavior.
  /// </summary>
  /// <returns>A JsPluginCode object containing the plugin's name and code.</returns>
  /// <remarks>
  /// Subclasses must implement this method to provide the actual JavaScript code for the plugin.
  /// </remarks>
  abstract public JsPluginCode GetJsPluginCode();
  
  /// <summary>
  /// Defines patterns to check if the app's C# codebase requires this JS plugin code.
  /// </summary>
  /// <returns>An array of string patterns to match against the codebase.</returns>
  /// <remarks>
  /// Subclasses should override this method to provide specific patterns that indicate
  /// when their JS plugin functionality is needed. The default implementation returns
  /// a reminder message to define patterns.
  /// </remarks>
  static public string[] CodeMatchPatterns() => new string[]{"You should define CodeMatchPatterns() in your subclass of JsPlugin_Behaviour"};

  virtual public void Start() {
    #if UNITY_EDITOR
      JsPlugin_Writer.JsPluginFileExists(GetJsPluginCode(), this.GetType().Name);
    #endif
  }

  #if UNITY_EDITOR
    /// <summary>
    /// Writes the JavaScript plugin file for this behavior.
    /// </summary>
    /// <remarks>
    /// This method is called from the Build Assistant in the Unity Editor 
    /// to write the JavaScript plugin file for this behavior to:
    ///   Assets/MultisynqJs.<appName>/JsPlugins/<pluginName>.js
    /// </remarks>
    virtual public void WriteMyJsPluginFile() {
      // if (dbg) Debug.Log($"{logPrefix} <color=white>BASE</color> virtual public void WriteMyJsPluginFile()");
      var jsPlugin = GetJsPluginCode();
      JsPlugin_Writer.WriteOneJsPluginFile(jsPlugin);
    }
  #else
    // skipped in builds
    virtual public void WriteMyJsPluginFile() { }
  #endif

}


} // namespace Multisynq