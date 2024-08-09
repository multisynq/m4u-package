using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class StatusItem {

  public MultisynqBuildAssistantEW edWin;
  public StatusSet statusSet;
  public VisualElement statusImage;
  public Label messageLabel;
  public Button[] buttons;

  // CONSTRUCTOR
  public StatusItem(MultisynqBuildAssistantEW parent = null) {
    if (parent == null) {
      Debug.LogError("StatusItem needs a parent MultisynqBuildAssistantEW");
      return;
    }
    edWin = parent;
    Init();
  }

  // MUST MAKE THESE METHODS IN SUBCLASSES (abstract)
  abstract public bool Check();
  abstract public void InitUI();
  abstract public void InitText();

  // --- CLASS METHODS ----------------------
  public void Init() {
    InitUI();
    InitText();
  }

  // --- STATICS ----------------------------
  public static void ShowVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(true, ve);
  }

  public static void HideVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(false, ve);
  }

  public static void SetVEViz(bool seen, params VisualElement[] ves) {
    foreach (var ve in ves) ve.style.visibility = seen ? Visibility.Visible : Visibility.Hidden;
  }

  public void SetupButton(string buttonName, ref Button button, Action buttonAction) {
    button = edWin.rootVisualElement.Query<Button>(buttonName).First();
    if (button == null) {
      Debug.LogError("Could not find button: " + buttonName);
      return;
    }
    if (buttonAction!=null) button.clicked += buttonAction;
    edWin.allButtons.Add(button);
  }

  public void SetupLabel(string labelName, ref Label label) {
    label = edWin.rootVisualElement.Query<Label>(labelName).First();
    if (label == null) Debug.LogError("Could not find label: " + labelName);
    else messageLabel = label;
  }

  public void SetupVisElem(string visElemName, ref VisualElement visElem) {
    visElem = edWin.rootVisualElement.Query<VisualElement>(visElemName).First();
    if (visElem == null) Debug.LogError("Could not find VisualElement: " + visElemName);
    else statusImage = visElem;
  }

  public T FindElement<T>( string nm ) where T : VisualElement {
    return edWin.rootVisualElement.Query<T>(nm).First();
  }

  //=============================================================================

  static public void NotifyAndLog(string msg, float seconds = 4) {
    MultisynqBuildAssistantEW.Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.Log(msg.Replace("\n", " "));
  }
  static public void NotifyAndLogError(string msg, float seconds = 4) {
    MultisynqBuildAssistantEW.Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.LogError(msg.Replace("\n", " "));
  }
  static public void NotifyAndLogWarning(string msg, float seconds = 4) {
    MultisynqBuildAssistantEW.Instance.ShowNotification(new GUIContent(msg), seconds);
    Debug.LogWarning(msg.Replace("\n", " "));
  }

  static public void Notify(string msg, float seconds = 4) {
    MultisynqBuildAssistantEW.Instance.ShowNotification(new GUIContent(msg), seconds);
  }

}