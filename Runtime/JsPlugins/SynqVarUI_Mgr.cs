using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // for TMP_Text

namespace Multisynq {


#region Attribute
  public class ItemAction {
    public string label;
    public Func<string,string> action = (string val) => val;
    public List<string> needs = new();
    public ItemAction(string label, Func<string,string> action) {
      this.label = label;
      this.action = action;
    }
  }
  //========== ||||||||| |||||||| ================
  //========| [SynqVarUI] | ======================
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public class SynqVarUIAttribute : SynqVarAttribute { // C# Attribute
    // Usage options: 
    // [SynqVarUI] 
    // [SynqVarUI(labelTxt = "O2")]
    // [SynqVarUI(valueTxtFunc = (string val, object env)=>$"{(val/100f).ToString(1)}%")]
    public string                       theme         { get; set; }          // Key to look up in SynqVarUI_Mgr.uiAttributes
    public string                       clonePath     { get; set; }          // GameObject to clone for UI
    public string                       imgCompPath   { get; set; }          // Path to Image component under the cloned UI
    public string                       imgRsrcPath   { get; set; }          // Path in Resources folder for dynamic sprite loading
    public string                       formatStr     { get; set; }          // Method to make text for value
    public string                       uGuiTxtName   { get; set; }          // GameObject name for text under clonable parent
    public string                       labelTxt      { get; set; }          // Custom name for the variable, useful for shortening to reduce message size
    public string                       imgName       { get; set; }          // Name of image to load from Resources folder
    public int                          order         { get; set; } = 0;     // Order to display in UI

    public Func<string, object, string> valueTxtFunc  { get; set; }          // Method to make text for value
    public GameObject                   uiToClone     { get; set; }          // GameObject to clone for UI
    public List<ItemAction>             actions       { get; set; } = new(); // Actions to take on value change
    public Sprite                       defaultImg    { get; set; }          // Default sprite to show

    public SynqVarUIAttribute(
      string theme       = null, // specific
      string clonePath   = null, // common
      string imgCompPath = null, // common
      string imgRsrcPath = null, // common
      string formatStr   = null, // common
      string uGuiTxtName = null, // common
      string labelTxt    = null, // specific
      string imgName     = null, // specific
      int    order       = 0     // specific
    ) {
      this.theme        = theme;
      this.clonePath    = clonePath;
      this.imgCompPath  = imgCompPath;
      this.imgRsrcPath  = imgRsrcPath;
      this.formatStr    = formatStr;
      this.uGuiTxtName  = uGuiTxtName;
      this.labelTxt     = labelTxt;
      this.imgName      = imgName;
      this.order        = order;
      if (theme != null) SynqVarUI_Mgr.RegisterUITheme(theme, this);
    }

    public void StompNonNullValuesUsing(SynqVarUIAttribute attr) {// alternate method names: ReplaceNonNullValues, OverrideIfNotNull, OverloadIfNotNull, OvelayIfNotNull, SpreadValsInto
      // if attr.xxxxx has a non-null vlaue, then use it, otherwise leave it as is in this.xxxxx
      theme       ??= attr.theme; 
      clonePath   ??= attr.clonePath;
      imgCompPath ??= attr.imgCompPath;
      imgRsrcPath ??= attr.imgRsrcPath;
      formatStr   ??= attr.formatStr;
      uGuiTxtName ??= attr.uGuiTxtName;
      labelTxt    ??= attr.labelTxt;
      imgName     ??= attr.imgName;
      order       = attr.order != 0 ? attr.order : order;
    }
    override public string ToString() {
      return $"{base.ToString()} >> theme:{theme}, clonePath:{clonePath}, imgCompPath:{imgCompPath}, imgRsrcPath:{imgRsrcPath}, formatStr:{formatStr}, uGuiTxtName:{uGuiTxtName}, labelTxt:{labelTxt}, imgName:{imgName}, order:{order}";
    }
  }
#endregion

//========== ||||||||||||| ===================================== ||||||||||||| ================
public class SynqVarUI_Mgr : SynqVar_Mgr { // <<<<<<<<<<<< class SynqVarUI_Mgr <<<<<<<<<<<<

  #region Fields
    // [SerializeField] public UIDocument uiDoc;
    // VisualElement scoreTemplate;
    public GameObject defaultUGuiToClone;
    new static public string[] CsCodeMatchesToNeedThisJs() => new[] {@"\[SynqVarUI"};
    static public Dictionary<string, SynqVarUIAttribute> uiAttributes = new();
    static public void RegisterUITheme(string varName, SynqVarUIAttribute attr) => uiAttributes[varName] = attr;
    SynqVar_Mgr syncVarMgr;
  #endregion

  #region JavaScript
  // Grab SynqVarUI_Mgr.GetJsPluginCode() from base class by not overriding it here
    // //-------------------------- ||||||||||||||| -------------------------
    // public override JsPluginCode GetJsPluginCode() {
    //   // null  here means that JS code is neither required 
    //   // nor written to the MultisynqJS/<appName>/plugins/indexOfPlugins.js or its folder
    //   // for this plugin. 
    //   // (In fact, this plugin makes use of the JS code it is subclassed from, SynqVar_Mgr)
    //   return null; 
    // }
  #endregion

  #region Start/Update
    //------------------ ||||| ------------------------------------------
    override public void Start() { // SynqVarUI_Mgr.Start()
      // base.Start();
      // Wait a bit, then start Init()
      // Invoke(nameof(Init), 0.5f);
      Init();
    }
    // void Init() runs after Start()
    void Init() {
      // Do not want a derived class like SynqVarUI_Mgr, so can't use FindObjectOfType<SynqVar_Mgr>()
      syncVarMgr = FindObjectsOfType<SynqVar_Mgr>().Where(svMgr => svMgr.GetType() == typeof(SynqVar_Mgr)).FirstOrDefault();
      if (syncVarMgr == null || syncVarMgr.syncVarsArr == null) {
        Invoke(nameof(Init), 1f);
        Debug.Log($"{svLogPrefix} SynqVarUI_Mgr.Init() - waiting for syncVarMgr");
      } else {
        Debug.Log($"{svLogPrefix} SynqVarUI_Mgr.Init() - found syncVarMgr =========== &&&&&&&");
        svLogPrefix = "<color=#5555FF>[SynqVarUI]</color> ";
        Debug.Log($"{svLogPrefix} SynqVarUI_Mgr.Start()");
        var sortedSyncVars = syncVarMgr.syncVarsArr.OrderBy(sv => -((sv.attribute as SynqVarUIAttribute)?.order ?? int.MaxValue));
        foreach(SynqVarInfo sv in sortedSyncVars) AddUIElement(sv);
      }
    }
    //-- |||||||||||| ------------------------------------------
    void AddUIElement(SynqVarInfo synqVar) {
      // Debug.Log($"{svLogPrefix} AddUIElement for {synqVar.varName}");
      var attr = synqVar.attribute as SynqVarUIAttribute;
      // log error if attr is null
      if (attr==null) {
        // Debug.LogError($"{svLogPrefix} AddUIElement for {synqVar.varId} - <color=red>SynqVarUIAttribute is null</color>");
        // return;
      }
      if (attr?.theme!=null) {
        if (uiAttributes.ContainsKey(attr.theme)) {
          // make a deep copy 
          Debug.Log($"   ### 0 - deepCopy: {synqVar}");
          var themeAttr = uiAttributes[attr.theme];
          var currAttr  = attr.DeepCopy();
          attr.StompNonNullValuesUsing(themeAttr);
          attr.StompNonNullValuesUsing(currAttr);
        } else {
          Debug.LogError($"{svLogPrefix} AddUIElement for {synqVar.varId} - <color=red>theme not found</color>");
          return;
        }
      }

      var cloneMe = defaultUGuiToClone;
      if (attr?.clonePath != null) {
        // Debug.Log($"{svLogPrefix} $$$$ AddUIElement for {synqVar.varName} - uiPathToClone: {attr.clonePath}");
        attr.uiToClone = GameObject.Find(attr.clonePath);
        if (attr.uiToClone == null) {
          Debug.LogError($"{svLogPrefix} AddUIElement for {synqVar.varId} - <color=red>uiToClone is null</color>");
          return;
        } else {
          Debug.Log($"         ### AddUIElement for {synqVar.varName} - uiToClone: %gr%{attr.uiToClone.Path()}".TagColors());
        }
        cloneMe = attr.uiToClone;
      }
      if (cloneMe == null) {
        Debug.LogError($"{svLogPrefix} AddUIElement for {synqVar.varId} - no uGuiToClone");
        return;
      }
      cloneMe.SetActive(false);
      var clone = Instantiate(cloneMe);
      clone.SetActive(true);
      clone.transform.SetParent(cloneMe.transform.parent, false); // same parent
      clone.transform.SetSiblingIndex(cloneMe.transform.GetSiblingIndex() + 1); // after clone source
      // set name with var key
      clone.name = $"{cloneMe.name}_{synqVar.varName}";

      // Setup Text Component
      TMP_Text text;
      if (attr?.uGuiTxtName != null) text = clone.transform.Find(attr.uGuiTxtName).GetComponent<TMP_Text>();
      else text = clone.GetComponentInChildren<TMP_Text>();

      // Setup Image Component if specified
      Image imageComponent = null;
      if (!string.IsNullOrEmpty(attr?.imgCompPath)) {
        imageComponent = clone.transform.Find(attr.imgCompPath)?.GetComponent<Image>();
        if (imageComponent == null) Debug.LogWarning($"{svLogPrefix} Image component not found at path: {attr.imgCompPath}");
        else if (attr.defaultImg != null) imageComponent.sprite = attr.defaultImg;
      }

      string lblTxt = attr?.labelTxt;
      if (lblTxt == null) {
        var afterDotInVarName = synqVar.varName.Split('.').Last();
        if (attr!= null && attr.imgName == null) {
          attr.imgName = afterDotInVarName; // Both: (1) the image) ...
          Debug.Log($"   ### 777 - attr.imgName: {attr.imgName}");
        }
        lblTxt = afterDotInVarName.CapitalizeFirst(); //             ... and (2) the label
      }

      UpdateUI(text, imageComponent, synqVar, lblTxt, synqVar.LastValue.ToString());

      synqVar.onUICallback = (value) => UpdateUI(text, imageComponent, synqVar, lblTxt, value.ToString());
      // Debug.Log($"{svLogPrefix} $$$$$$ {synqVar.varName} onUICallback: {value}");
    }

    void UpdateUI(TMP_Text text, Image image, SynqVarInfo synqVar, string label, string value) {
      var attr = synqVar.attribute as SynqVarUIAttribute;
      
      // Update text if available
      if (text != null) {
        if (attr?.formatStr != null) text.text = attr.formatStr.Replace("{{key}}", label).Replace("{{value}}", value);
        else text.text = $"{label}   <color=#44ff44><b>{value}</b></color>";
      }

      // Update image if available
      if (image != null && !string.IsNullOrEmpty(attr?.imgRsrcPath)) {
        // Try to load sprite from Resources folder based on the value
        string imgName = attr.imgName ?? label;
        string spritePath = $"{attr.imgRsrcPath}/{imgName}";
        Sprite newSprite = Resources.Load<Sprite>(spritePath);
        if (newSprite != null) image.sprite = newSprite;
        else {
          Debug.LogWarning($"{svLogPrefix} Could not load sprite at path: {spritePath}");
          if (attr.defaultImg != null) image.sprite = attr.defaultImg;
        }
      }

    }

    //------ |||||| -------------------------------------------------------
    new void Update() { // block base.Update()
      //base.Update();
    }

  #endregion

  #region Messaging
    // //--------------------------- |||||||||||| --------------------------
    // new public (SynqVarInfo,bool) ReceiveAsMsg(string msg) {
    //   var (synqVar, valIsSame) = base.ReceiveAsMsg(msg); // <---- CALL BASE class!
    //   Debug.Log($"{svLogPrefix} ReceiveAsMsg: {synqVar.varName} = {synqVar.LastValue}");
    //   if ( ! valIsSame ) synqVar.onUICallback?.Invoke(synqVar.LastValue);
    //   return (synqVar, valIsSame);
    // } 
  #endregion

  #region Singleton
    private     static SynqVarUI_Mgr _Instance;
    new public  static SynqVarUI_Mgr I { // Usage:   SynqVarMgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
  #endregion
}

static public class UIToolkitExtensions {

  //   // static public VisualElement CloneVizEl(this Label lbl) => CloneVizEl(lbl as VisualElement);
  //   static public VisualElement CloneVizEl(this VisualElement original) {
  //     Debug.Log($"================= CloneVizEl: {original?.name}");
  //     VisualElement ve = new VisualElement();
  //     IList<string> classList = original.GetClasses() as IList<string>;
  //     int c = classList.Count;
  //     for (int i= 0; i < c; i++) {
  //       ve.AddToClassList(classList[i]);
  //       Debug.Log(classList[i]);
  //     }
  //     original.parent.Add(ve); // Add it where you want
  //     // recurse children
  //     // collect in list then add once collected



  // // InvalidOperationException: Collection was modified; enumeration operation may not execute.

  //     var children = new List<VisualElement>();
  //     foreach (var child in original.Children()) {
  //       Debug.Log($"================= CloneVizEl: {original?.name} children:{children.Count}");
  //       children.Add(child.CloneVizEl());
  //     }

  //     Debug.Log($"==================== CloneVizEl: {original?.name} children:{children.Count}");
  //     foreach (var child in children) ve.Add(child);

  //     return ve;
  //   }
}


} // END namespace Multisynq