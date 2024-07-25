
using System;
using Unity.VisualScripting.IonicZip;
using UnityEngine;
using UnityEngine.UIElements;

  //=============================================================================
  public class Status {
    public string message;
    public string statusStr;
    public Color color;
    public Label label;
    public VisualElement statusIconToSetBgOn_VE;
    public StyleBackground iconStyleToClone;
    public StatusSet statusSet;

    public void Set() {
      if (label==null) {
        Debug.LogError("Status.Set() label is null for status: " + statusStr + " " + message);
        return;
      }
      label.text = message;
      statusIconToSetBgOn_VE.style.unityBackgroundImageTintColor = color;
      statusIconToSetBgOn_VE.style.backgroundImage = iconStyleToClone;
      statusSet.status = statusStr;
    }
    public Status(string statusStr, Label label, VisualElement imgToSet, StyleBackground imgSrc, string message, Color color, StatusSet statusSet) {
      this.message   = message;
      this.color     = color;
      this.label     = label;
      this.statusIconToSetBgOn_VE = imgToSet;
      this.iconStyleToClone = imgSrc;
      this.statusSet = statusSet;
      this.statusStr = statusStr;
    }
  }
  //=============================================================================
  public class StatusSet {
    static public StyleBackground readyImgStyle;
    static public StyleBackground warningImgStyle;
    static public StyleBackground errorImgStyle;
    static public StyleBackground successImgStyle;
    static public StyleBackground blankImgStyle;

    public string status = "blank";
    public Status ready;
    public Status warning;
    public Status error;
    public Status success;
    public Status blank;
    public Label label;
    public VisualElement img;
    public bool IsOk() {
      return (status == "ready") || (status == "success");
    }
    public void SuccessToReady() {
      if (status == "success") {
        status = "ready";
        ready.Set();
      }
    }

    public StatusSet(Label label, VisualElement img, string _info, string _warning, string _error, string _success, string _blank) {
      var colz = MultisynqBuildAssistantEW.colz;
      ready   = new Status("ready",   label, img, readyImgStyle,   _info,    colz.green,  this);
      warning = new Status("warning", label, img, warningImgStyle, _warning, colz.yellow, this);
      error   = new Status("error",   label, img, errorImgStyle,   _error,   colz.red,    this);
      success = new Status("success", label, img, successImgStyle, _success, colz.lime,   this);
      blank   = new Status("blank",   label, img, blankImgStyle,   _blank,   colz.grey,   this);
    }
  }
  