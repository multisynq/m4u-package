
using UnityEngine;
using UnityEngine.UIElements;

  //=============================================================================
  public class Status {
    public string message;
    public string statusStr;
    public Color color;
    public Label label;
    public VisualElement imgToSet;
    public VisualElement imgSrc;
    public StatusSet statusSet;

    public void Set() {
      label.text = message;
      imgToSet.style.unityBackgroundImageTintColor = color;
      statusSet.status = statusStr;
      if (imgSrc != null) {
        // switch background image to the one that matches the status
        imgToSet.style.backgroundImage = imgSrc.style.backgroundImage; // TODO: DOESN'T WORK. WHY???    =/
        imgToSet.MarkDirtyRepaint();
        string labelNameWithoutSuffix = label.name.Replace("_Lbl", "");
        // Debug.Log($"{labelNameWithoutSuffix} => Status.set( {statusStr} )  srcImg.name={imgSrc.name}");
      } else {
        imgToSet.style.backgroundImage = null;
        imgToSet.MarkDirtyRepaint();
        Debug.LogError("No source image for status: " + statusStr);
      }
    }
    public Status(string statusStr, Label label, VisualElement imgToSet, VisualElement imgSrc, string message, Color color, StatusSet statusSet) {
      this.message   = message;
      this.color     = color;
      this.label     = label;
      this.imgToSet  = imgToSet;
      this.imgSrc    = imgSrc;
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
  