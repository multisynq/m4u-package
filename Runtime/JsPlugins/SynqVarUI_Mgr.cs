using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multisynq {


#region Attribute
  //========== ||||||||| |||||||| ================
  //========| [SynqVarUI] | ======================
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public class SynqVarUIAttribute : SynqVarAttribute { // C# Attribute
    // Usage options: 
    // [SynqVarUI] 
    // [SynqVarUI(labelTxt = "O2")]
    // [SynqVarUI(valueTxtFunc = (string val, object env)=>$"{(val/100f).ToString(1)}%")]
    public string labelTxt { get; set; } // Custom name for the variable, useful for shortening to reduce message size
    public Func<string, object, string> valueTxtFunc { get; set; } // Method to make text for value
  }
#endregion

//========== ||||||||||||| ===================================== ||||||||||||| ================
public class SynqVarUI_Mgr : SynqVar_Mgr { // <<<<<<<<<<<< class SynqVarUI_Mgr <<<<<<<<<<<<

  #region Fields
    [SerializeField] public UIDocument uiDoc;
    VisualElement scoreTemplate;
  #endregion
  
  #region Start/Update
    //------------------ ||||| ------------------------------------------
    override public void Start() { // SynqVarUI_Mgr.Start()
      base.Start();
      svLogPrefix = "<color=#5555FF>[SynqVarUI]</color> ";
      Debug.Log($"{svLogPrefix} Start()");
      scoreTemplate = uiDoc.rootVisualElement.Q<VisualElement>("Score");
      foreach(SynqVarInfo sv in syncVarsArr) AddUIElement(sv);
    }
    //-- |||||||||||| ------------------------------------------
    void AddUIElement(SynqVarInfo synqVar) {
      Debug.Log($"{svLogPrefix} AddUIElement(1) for {synqVar.varId}");
      var newScore = scoreTemplate.CloneVizEl();
      var valTxt   = newScore.Q<Label>("Value");
      var keyTxt   = newScore.Q<Label>("Key");
      synqVar.onUICallback = (value) => valTxt.text = value.ToString();
    }
    //------ |||||| -------------------------------------------------------
    // new void Update() {
    //   base.Update();
    // }
  #endregion

  #region Messaging
    //------------- |||||||||||| --------------------------
    new public (SynqVarInfo,bool) ReceiveAsMsg(string msg) {
      var (synqVar, valIsSame) = base.ReceiveAsMsg(msg); // <---- CALL BASE class!
      if ( ! valIsSame ) synqVar.onUICallback?.Invoke(synqVar.LastValue);
      return (synqVar, valIsSame);
    } 
  #endregion
  #region Singleton
    private     static SynqVarUI_Mgr _Instance;
    new public  static SynqVarUI_Mgr I { // Usage:   SynqVarMgr.I.JsPluginFileName();
      get { return _Instance = Singletoner.EnsureInst(_Instance); }
    }
  #endregion
}

static public class UIToolkitExtensions {

    // static public VisualElement CloneVizEl(this Label lbl) => CloneVizEl(lbl as VisualElement);
    static public VisualElement CloneVizEl(this VisualElement original) {
      Debug.Log($"================= CloneVizEl: {original?.name}");
      VisualElement ve = new VisualElement();
      IList<string> classList = original.GetClasses() as IList<string>;
      int c = classList.Count;
      for (int i= 0; i < c; i++) {
        ve.AddToClassList(classList[i]);
        Debug.Log(classList[i]);
      }
      original.parent.Add(ve); // Add it where you want
      // recurse children
      // collect in list then add once collected



  // InvalidOperationException: Collection was modified; enumeration operation may not execute.

      var children = new List<VisualElement>();
      foreach (var child in original.Children()) {
        Debug.Log($"================= CloneVizEl: {original?.name} children:{children.Count}");
        children.Add(child.CloneVizEl());
      }

      Debug.Log($"==================== CloneVizEl: {original?.name} children:{children.Count}");
      foreach (var child in children) ve.Add(child);

      return ve;
    }
}


} // END namespace Multisynq