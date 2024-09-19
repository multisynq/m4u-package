using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multisynq {

public abstract class Mq_System : MonoBehaviour {
  public bool mq_System;
  /// <summary>
  /// Commands this system understands.
  /// </summary>
  public abstract List<String> KnownCommands { get;}

  /// <summary>
  /// Components that this system will update.
  /// </summary>
  protected abstract Dictionary<int, Mq_Comp> components { get; set; }

  public virtual void RegisterComponent(Mq_Comp component) {
    // Debug.Log($"register {component.gameObject} in {this}");
    components.Add(component.gameObject.GetInstanceID(), component);
  }

  public virtual void UnregisterComponent(Mq_Comp component) {
    components.Remove(component.gameObject.GetInstanceID());
  }

  public bool KnowsObject(GameObject go) {
    return components.ContainsKey(go.GetInstanceID());
  }

  public virtual void PawnInitializationComplete(GameObject go) {
    // by default, nothing
  }

  public virtual void ActorPropertySet(GameObject go, string propName) {
    // by default, nothing
  }

  public virtual void ProcessCommand(string command, string[] args) {
    throw new NotImplementedException();
  }

  public virtual void ProcessCommand(string command, byte[] data, int startIndex) {
    throw new NotImplementedException();
  }

  public virtual void LoadedScene(string sceneName) {
    // by default, nothing
  }

  public virtual bool ReadyToRunScene(string sceneName) {
    return true;
  }

  public virtual void ClearSceneBeforeRunning() {
    components.Clear(); // wipe out anything that registered as the scene came up
  }

  public virtual void TearDownScene() {
    // by default, just clear the components
    components.Clear();
  }

  public virtual void TearDownSession() {
    // by default, just invoke TearDownScene
    TearDownScene();
  }

  public virtual List<string> InitializationStringsForObject(GameObject go) {
    return new List<string>();
  }

}

}