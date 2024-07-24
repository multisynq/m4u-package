
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
    public VisualElement iconToCloneBgFrom_VE;
    public StatusSet statusSet;

    public void Set() {
      label.text = message;
      statusIconToSetBgOn_VE.style.unityBackgroundImageTintColor = color;
      statusSet.status = statusStr;
      if (iconToCloneBgFrom_VE != null) {
        // switch background image to the one that matches the status
        statusIconToSetBgOn_VE.style.backgroundImage = iconToCloneBgFrom_VE.style.backgroundImage;
        statusIconToSetBgOn_VE.MarkDirtyRepaint();
        // Func<string, Func<string, string>> NoSfx = (sfx)  => (name) => name.Replace(sfx, "");
        // Func<string, string>               NoLbl = (name) => NoSfx("_Lbl")(name);
        // Func<string, string>               NoImg = (name) => NoSfx("_Img")(name);
        Debug.Log($"Status.Set() image of {statusIconToSetBgOn_VE.name} to be {iconToCloneBgFrom_VE.name}");
      } else {
        statusIconToSetBgOn_VE.style.backgroundImage = null;
        statusIconToSetBgOn_VE.MarkDirtyRepaint();
        Debug.LogError("No source image for status: " + statusStr);
      }
    }
    public Status(string statusStr, Label label, VisualElement imgToSet, VisualElement imgSrc, string message, Color color, StatusSet statusSet) {
      this.message   = message;
      this.color     = color;
      this.label     = label;
      this.statusIconToSetBgOn_VE  = imgToSet;
      this.iconToCloneBgFrom_VE    = imgSrc;
      this.statusSet = statusSet;
      this.statusStr = statusStr;
    }
  }
  //=============================================================================
  public class StatusSet {
    static public VisualElement readyImg;
    static public VisualElement warningImg;
    static public VisualElement errorImg;
    static public VisualElement successImg;
    static public VisualElement blankImg;

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
      ready   = new Status("ready",   label, img, readyImg,   _info,    colz.green,  this);
      warning = new Status("warning", label, img, warningImg, _warning, colz.yellow, this);
      error   = new Status("error",   label, img, errorImg,   _error,   colz.red,    this);
      success = new Status("success", label, img, successImg, _success, colz.lime,   this);
      blank   = new Status("blank",   label, img, blankImg,   _blank,   colz.grey,   this);
    }
  }
  