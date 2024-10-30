using UnityEngine;

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