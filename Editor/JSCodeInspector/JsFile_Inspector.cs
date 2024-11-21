using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
//========== |||||||||||||||| ====================================================
[CustomEditor(typeof(DefaultAsset))]
public class JsFile_Inspector : Editor { //====================
  private VisualElement root;
  private ScrollView codeScrollView;
  private Label codeLabel;
  private Label Filename_Lbl;
  private SliderInt fontSizeSlider;
  static public int fontSize = 16;
  private string cachedCode;
  readonly int MAX_CODE = 15000;
  string path = null;

  public static HashSet<JsFile_Inspector> activeEditors = new();
  void OnEnable(){ activeEditors.Add(this); }
  void OnDisable(){ activeEditors.Remove(this); }
  static public void RepaintActiveEditors() {
    UnityEngine.Debug.Log("Repainting active JsFile_Inspectors");
    foreach (var ae in activeEditors) {
      ae.cachedCode = null;
      ae.LoadAndDisplayCode();
      EditorUtility.SetDirty(ae);
    }
  }

  //--------------------------- |||||||||||||||||| --------------------------
  public override VisualElement CreateInspectorGUI() {

    path = AssetDatabase.GetAssetPath(target);
    if (path!=null && IsValidFileType(path)) {

      root = new VisualElement();
      // Load UXML
      var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/io.multisynq.multiplayer/Editor/JSCodeInspector/JsFileEditorInspector.uxml");
      visualTree.CloneTree(root);

      // Query elements
      codeScrollView = root.Q<ScrollView>("code-scroll-view");
      codeLabel = root.Q<Label>("code-label");
      fontSizeSlider = root.Q<SliderInt>("font-size-slider");
      Filename_Lbl = root.Q<Label>("Filename_Lbl");
      Filename_Lbl.text = Path.GetFileName(path);

      // Set up font size slider
      fontSizeSlider.value = fontSize;
      fontSizeSlider.RegisterValueChangedCallback(evt => UpdateFontSize(evt.newValue));

      // handlers
      root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged); // Register callback for layout changes
      EditorApplication.delayCall += () => FitToInspector(); // Set initial size

      LoadAndDisplayCode();

      return root;

    } else {
      return base.CreateInspectorGUI();
    }
  }
  // --------- |||||||||||||||||| --------------------------
  private void LoadAndDisplayCode() {
    if (string.IsNullOrEmpty(cachedCode)) {
      string code = File.ReadAllText(path);
      if (code.Length > MAX_CODE) code = code[..MAX_CODE] + "\n...";
      cachedCode = ApplySyntaxHighlighting(code);
    }
    codeLabel.text = cachedCode;
    UpdateFontSize(fontSizeSlider.value);
  }
  // --------- |||||||||||||| -------------------------------
  private void UpdateFontSize(int _fontSize) {
    codeLabel.style.fontSize = _fontSize;
    fontSize = _fontSize;
    FitToInspector();
  }
  // --------- ||||||||||||||||| -------------------------------
  private void OnGeometryChanged(GeometryChangedEvent evt) {
    FitToInspector();
  }
  // --------- |||||||||||||| -------------------------------
  private void FitToInspector() {
    if (root == null) return;

    // Get the current inspector window
    EditorWindow inspectorWindow = EditorWindow.focusedWindow;
    if (inspectorWindow == null || inspectorWindow.GetType().Name != "InspectorWindow") {
      inspectorWindow = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"));
    }

    // Calculate available height
    float availableHeight = inspectorWindow.position.height - EditorGUIUtility.singleLineHeight * 9; // Subtracting space for the font size slider and some padding

    // Set the height of the scroll view
    codeScrollView.style.height = availableHeight;

    // Set the width of the code label to enable horizontal scrolling if needed
    codeLabel.style.width = new StyleLength(StyleKeyword.Auto);
    codeLabel.style.flexShrink = 0;
  }
  // --------- ||||||||||||||| -------------------------------
  private bool IsValidFileType(string path) {
    string[] validExtensions = { ".js", ".ts", ".jsx", ".tsx", ".jslib" };
    return System.Array.Exists(validExtensions, ext => path.ToLower().EndsWith(ext));
  }
  
  // ----------- ||||||||||||||||||||||| -------------------------------
  private string ApplySyntaxHighlighting(string code) {
    string ColorWrap(string s, string color) => $"<color={color}>{s}</color>";
    string Yellow(string s) => ColorWrap(s, "#FFD702");
    string Tan(   string s) => ColorWrap(s, "#E8E890");
    string Green( string s) => ColorWrap(s, "#6B9955");
    string Blue(  string s) => ColorWrap(s, "#569CD6");
    string Orange(string s) => ColorWrap(s, "#FFA500");
    string White( string s) => ColorWrap(s, "white");
    string Cyan(  string s) => ColorWrap(s, "#83D7C5");
    string Pink(  string s) => ColorWrap(s, "#E394DD");
    string Red(   string s) => ColorWrap(s, "#C1808A");
    // string Mag(   string s) => ColorWrap(s, "#DB70D6");

    code = Regex.Replace(code, @"color", "c0l0r"); // hide the word "color" from the syntax highlighter
    // code = Regex.Replace(code, @"(\=)[^>|==]",     m => Mag(   m.Value)); // =
    code = Regex.Replace(code, @"\.([\w_0-9]+)\(", m => Tan(   m.Value)); // between a "." and a "(" i.e. "foo" in "this.foo()"
    code = Regex.Replace(code, @"(\(|\))",         m => Blue(  m.Value)); // ( )
    code = Regex.Replace(code, @"(\{|\})",         m => Yellow(m.Value)); // { }
    code = Regex.Replace(code, @"(\[|\])",         m => Yellow(m.Value)); // [ ]
    code = Regex.Replace(code, @"(\;)",            m => Blue(  m.Value)); // ;
    code = Regex.Replace(code, @"(\,)",            m => White( m.Value)); // ,
    code = Regex.Replace(code, @"(//.*)",          m => Green( m.Value)); // //xxx
    code = Regex.Replace(code, @"('[^'\n]*')",     m => Pink(  m.Value)); // 'xxx'
    code = Regex.Replace(code, @"(""[^""\n]*"")",  m => Pink(  m.Value)); // "xxx"
    code = Regex.Replace(code, @"(this).",         m => Red(   m.Value)); // +xxx
    code = Regex.Replace(code, "c0l0r", "color"); // restore the word "color"

    var keywordColors = new Dictionary<string, Func<string, string>> { 
      { @"\bclass\s+(\w+)\b", Blue }, 
      { @"\b(if|else|for|while|return|class)\b", Orange }, 
      { @"\b(from|import|export|extends|new)\b", Cyan }, 
      { @"\b(function|var|let|const)\b", Blue }
    };

    foreach (var kvp in keywordColors) {
      code = Regex.Replace(code, kvp.Key, m => kvp.Value(m.Value));
    }

    return White(code);
  }

}
