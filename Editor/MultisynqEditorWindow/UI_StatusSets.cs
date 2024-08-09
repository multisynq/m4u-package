using System.IO;
using UnityEditor;
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
    static public HandyColors handyColors;

    public string status = "blank";
    public Status ready;
    public Status warning;
    public Status error;
    public Status success;
    public Status blank;
    public Label label;
    public VisualElement img;

    // --- CONSTRUCTOR ----------------------
    public StatusSet(Label label, VisualElement img, string _info, string _warning, string _error, string _success, string _blank) {
      var clr = EnsureHandyColors();
      EnsureTextures();
      ready   = new Status("ready",   label, img, readyImgStyle,   _info,    clr.green,  this);
      warning = new Status("warning", label, img, warningImgStyle, _warning, clr.yellow, this);
      error   = new Status("error",   label, img, errorImgStyle,   _error,   clr.red,    this);
      success = new Status("success", label, img, successImgStyle, _success, clr.lime,   this);
      blank   = new Status("blank",   label, img, blankImgStyle,   _blank,   clr.grey,   this);
    }
    // --- STATICS ----------------------
    static public void EnsureTextures() {
      if (readyImgStyle == null) InitTextures();
    }
    static public void InitTextures() {
      readyImgStyle   = LoadTexture("Checkmark.png");
      warningImgStyle = LoadTexture("Warning.png");
      errorImgStyle   = LoadTexture("Multiply.png");
      successImgStyle = LoadTexture("Checkmark.png");
      blankImgStyle   = LoadTexture("Blank.png");
    }

    //=============================================================================
    static public StyleBackground LoadTexture(string fNm) {
      string path = Path.Combine(CqFile.img_root, fNm);
      return new StyleBackground(AssetDatabase.LoadAssetAtPath<Sprite>(path));
    }

    static public HandyColors EnsureHandyColors() {
      if (handyColors == null) handyColors = new HandyColors();
      return handyColors;
    }

    // --- METHODS ----------------------
    public bool IsOk() {
      return (status == "ready") || (status == "success");
    }
    public void SuccessToReady() {
      if (status == "success") {
        status = "ready";
        ready.Set();
      }
    }
    public void SetIsGood(bool isGood) {
      if (isGood) success.Set();
      else        error.Set();
    }

  }
  