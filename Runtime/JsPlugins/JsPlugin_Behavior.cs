using UnityEngine;
using System.Linq;
using System;

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
  static public string[] CsCodeMatchesToNeedThisJs() => new string[]{"You should define CsCodeMatchesToNeedThisJs() in your subclass of JsPlugin_Behaviour"};
  
  /// <summary>
  /// Defines the behaviours that need this JS plugin.
  /// </summary>
  static public Type[] BehavioursThatNeedThisJs() => null;

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
      if (jsPlugin!=null) JsPlugin_Writer.WriteOneJsPluginFile(jsPlugin);
    }
  #else
    // skipped in builds
    virtual public void WriteMyJsPluginFile() { }
  #endif

  /// <summary>
  /// Checks if any of the behaviours that need this JS plugin are present in the scene.
  /// </summary>
  /// <returns>True if any of the needed behaviours are present, false otherwise.</returns>
  public string CheckIfANeededBehaviourIsPresent() {
    var neededBehaviours = BehavioursThatNeedThisJs();
    string isNullStr = (neededBehaviours == null) ? "<color=#ff4444>null</color>" : "<color=#44ff44>not null</color>";
    // Log out my type
    // Debug.Log($"{logPrefix} %ye%{this.GetType().Name}%gy%.BehavioursThatNeedThisJs()=={isNullStr} for %cy%{this.name}".TagColors());
    if (neededBehaviours == null) return null;
    Debug.Log($"{logPrefix} CheckIfANeededBehaviourIsPresent() for %cy%{this.name}%gy% looking for %wh%[%ye%{string.Join(", ", neededBehaviours.Select(b => b.Name))}%wh%]".TagColors());
    // Looks in scene for any of the behaviours that need this JS plugin
    var matches = neededBehaviours.Where(b => FindObjectsOfType(b).Length > 0).ToArray();
    string rpt = (matches.Length > 0) ? string.Join(",", matches.Select(b => b.Name)) : null;
    // Debug.Log($"{logPrefix} CheckIfANeededBehaviourIsPresent() for %cy%{this.name}%gy% FOUND %wh%[%ye%{rpt}%wh%]".TagColors());
    return rpt;
  }
}


} // namespace Multisynq