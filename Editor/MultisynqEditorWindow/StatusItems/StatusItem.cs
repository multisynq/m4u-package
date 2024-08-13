using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public abstract class StatusItem {

  //--- instance vars -------------------
  public MultisynqBuildAssistantEW edWin;
  public StatusSet statusSet;
  public VisualElement statusImage;
  public Label messageLabel;
  public Button[] buttons;
  //--- static vars ------------------------------
  static public List<StatusItem> allStatusItems = new();
  static public List<Button> allButtons = new();

  // --- CONSTRUCTOR ----------------------
  public StatusItem(MultisynqBuildAssistantEW parent = null) {
    if (parent == null) {
      Debug.LogError("StatusItem needs a parent MultisynqBuildAssistantEW");
      return;
    }
    edWin = parent;
    Init();
    allStatusItems.Add(this);
  }

  // --- (abstracts) MUST MAKE THESE METHODS IN SUBCLASSES ----------------------
  abstract public bool Check();
  abstract public void InitUI();
  abstract public void InitText();

  // --- INIT METHOD ----------------------
  public void Init() {
    InitUI();
    InitText();
  }

  // --- STATIC METHODS ----------------------------
  static public void ClearStaticLists() {
    allStatusItems.Clear();
    allButtons.Clear();
  }

  public static void ShowVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(true, ve);
  }

  public static void HideVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(false, ve);
  }

  public static void SetVEViz(bool seen, params VisualElement[] ves) {
    foreach (var ve in ves) ve.style.visibility = seen ? Visibility.Visible : Visibility.Hidden;
  }

  static public void AllSuccessesToReady() {
    foreach (var si in allStatusItems) {
      si.statusSet.SuccessToReady();
    }
  }

  static public void AllStatusSetsToBlank() {
    foreach (var si in allStatusItems) {
      si.statusSet.blank.Set();
    }
  }

  static public  void HideMostButtons() {
    string[] whitelisted = {
      "CheckIfReady_Btn",
      "_Docs_",
    };
    foreach (Button button in allButtons) {
      bool isWhitelisted = whitelisted.Any(button.name.Contains);
      StatusItem.SetVEViz(isWhitelisted, button);
    }
  }

  // --- ELEMENT METHODS -------------------
  public void SetupButton(string buttonName, ref Button button, Action buttonAction) {
    button = FindElement<Button>(buttonName, "Button");
    if (buttonAction != null) button.clicked += buttonAction;
    allButtons.Add(button);
  }

  public void SetupLabel(string labelName, ref Label label) {
    label = FindElement<Label>(labelName, "Label");
    if (label == null) Debug.LogError("Could not find label: " + labelName);
    else messageLabel = label;
  }

  public void SetupVisElem(string visElemName, ref VisualElement visElem) {
    visElem = FindElement<VisualElement>(visElemName);
    if (visElem == null) Debug.LogError("Could not find VisualElement: " + visElemName);
    else statusImage = visElem;
  }

  public T FindElement<T>( string nm, string type = "VisualElement" ) where T : VisualElement {
    var ve = edWin.rootVisualElement.Query<T>(nm).First();
    if (ve == null) Debug.LogError($"Could not find {type}: " + nm);
    return ve;
  }


  //--- NOTIFICATION METHODS -----------------------

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