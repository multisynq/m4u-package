using UnityEngine;
using UnityEditor;

public class GameObjectPathCopier {

  [MenuItem("GameObject/Copy Path", false, 0)]
  static void CopyGameObjectPath() {
    GameObject selectedObject = Selection.activeGameObject;
    if (selectedObject == null)
      return;

    string path = selectedObject.Path();
    GUIUtility.systemCopyBuffer = path;
    Debug.Log($"Copied path to clipboard: {path}");
  }

  [MenuItem("GameObject/Copy Path", true)]
  static bool ValidateGameObjectPath() {
    // This enables/disables the menu item based on whether a GameObject is selected
    return Selection.activeGameObject != null;
  }

  
}

static public class GameObjectExtensions {

  static public string Path(this GameObject obj) {
    string path = "/" + obj.name;
    Transform parent = obj.transform.parent;
    while (parent != null) {
      path = "/" + parent.name + path;
      parent = parent.parent;
    }
    return path;
  }
}