using System;
using System.Reflection;
using UnityEngine;
using Multisynq;

public static class AttributeHelper {

  public static string failMessage = null;

  //---------------- ||||||||||||||||||||||||||||||||| ------------------------------------------
  public static bool CheckForBadAttributeParent<TRequiredBase, TAttribute>(MonoBehaviour mb)
      where TAttribute : Attribute {
    var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    var properties = mb.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    foreach (var field in fields) {
      var attribute = field.GetCustomAttribute<TAttribute>();
      if (attribute != null && !(mb is TRequiredBase)) {
        string attrName = typeof(TAttribute).Name.Replace("Attribute","");
        string msg = $"%gy%For %ye%[{attrName}]%gy% of %ye%{mb.GetType().Name}%gy%.%cy%{field.Name}%gy% The %cy%class {mb.GetType().Name} %red%MUST%gy% extend %gre%class {typeof(TRequiredBase).Name}%gy%, not %red%MonoBehaviour".TagColors();
        Debug.LogError(msg);
        failMessage += msg;
        return false;
      }
    }

    foreach (var property in properties) {
      var attribute = property.GetCustomAttribute<TAttribute>();
      if (attribute != null && !(mb is TRequiredBase)) {
        string msg = $"%gy%For %ye%[{typeof(TAttribute).Name.Replace("Attribute","")}]%gy% of %ye%{mb.GetType().Name}%gy%.%cy%{property.Name}%gy% The %cy%class {mb.GetType().Name} %red%MUST%gy% extend %gre%class {typeof(TRequiredBase).Name}%gy%, not %red%MonoBehaviour".TagColors();
        Debug.LogError(msg);
        failMessage += msg;
        return false;
      }
    }

    return true;
  }
  //---------------- |||||||||||||||||||||| ------------------------------------------
  public static bool CheckForBadAttrParents<TRequiredBase, TAttribute>() where TAttribute : Attribute {
    bool allGood = true;
    #if UNITY_EDITOR
      foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()) {
        if (!mb.enabled) continue; // skip inactives
        if (!CheckForBadAttributeParent<TRequiredBase, TAttribute>(mb)) {
          allGood = false;
        }
      }
    #endif
    return allGood;
  }

  public static void OnGUI_FailMessage() {
    if (failMessage != null) {
      // big red panel in screen center
      GUI.color = new Color(.2f, 0, 0, 0.9f);
      GUI.DrawTexture(new Rect(0, Screen.height / 4, Screen.width, Screen.height / 2), Texture2D.whiteTexture);
      GUIStyle style = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 15, wordWrap = true };
      style.normal.textColor = Color.white;
      GUI.color = style.normal.textColor;
      GUI.Label(new Rect(0, Screen.height / 4, Screen.width, Screen.height / 2), AttributeHelper.failMessage, style);
    }
  }
}
