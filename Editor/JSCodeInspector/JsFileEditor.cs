using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
[CustomEditor(typeof(DefaultAsset))]
public class JsFileEditorInspector : Editor {
  private VisualElement root;
  private ScrollView codeScrollView;
  private Label codeLabel;
  private SliderInt fontSizeSlider;
  static public int fontSize = 16;
  private string cachedCode;

  public override VisualElement CreateInspectorGUI() {
    root = new VisualElement();

    // Load UXML
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/io.multisynq.multiplayer/Editor/JSCodeInspector/JsFileEditorInspector.uxml");
    visualTree.CloneTree(root);

    // Query elements
    codeScrollView = root.Q<ScrollView>("code-scroll-view");
    codeLabel = root.Q<Label>("code-label");
    fontSizeSlider = root.Q<SliderInt>("font-size-slider");

    // Set up font size slider
    fontSizeSlider.value = fontSize;
    fontSizeSlider.RegisterValueChangedCallback(evt => UpdateFontSize(evt.newValue));

    // Load and display code
    string path = AssetDatabase.GetAssetPath(target);
    if (IsValidFileType(path)) {
      LoadAndDisplayCode(path);
    }
    else {
      codeLabel.text = "Not a valid file type for this inspector.";
    }

    // Register callback for layout changes
    root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

    // Set initial size
    EditorApplication.delayCall += () => FitToInspector();

    return root;
  }

  private void LoadAndDisplayCode(string path) {
    if (string.IsNullOrEmpty(cachedCode)) {
      cachedCode = ApplySyntaxHighlighting(File.ReadAllText(path));
    }

    codeLabel.text = cachedCode;
    UpdateFontSize(fontSizeSlider.value);
  }

  private void UpdateFontSize(int _fontSize) {
    codeLabel.style.fontSize = _fontSize;
    fontSize = _fontSize;
    FitToInspector();
  }

  private void OnGeometryChanged(GeometryChangedEvent evt) {
    FitToInspector();
  }

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

  private bool IsValidFileType(string path) {
    string[] validExtensions = { ".js", ".ts", ".jsx", ".tsx", ".jslib" };
    return System.Array.Exists(validExtensions, ext => path.ToLower().EndsWith(ext));
  }
  
  private string ApplySyntaxHighlighting(string code) {
  string ColorWrap(string s, string color) => $"<color={color}>{s}</color>";
  string Yellow(string s) => ColorWrap(s, "#FFD702");
  string Tan(string s) => ColorWrap(s, "#E2E2AC");
  string Green(string s) => ColorWrap(s, "#6B9955");
  string Blue(string s) => ColorWrap(s, "#569CD6");
  string Orange(string s) => ColorWrap(s, "#FFA500");
  string White(string s) => ColorWrap(s, "white");
  string Mag(string s) => ColorWrap(s, "#DB70D6");
  string Cyan(string s) => ColorWrap(s, "#4EC9B0");

  code = Regex.Replace(code, @"color", "c0l0r"); // hide the word "color" from the syntax highlighter
  code = Regex.Replace(code, @"(\=)", m => Mag(m.Value));
  code = Regex.Replace(code, @"\.([\w_0-9]+)\(", m => Tan(m.Value)); // between a . and a ( is tan like this.foo()
  code = Regex.Replace(code, @"(\(|\))", m => Mag(m.Value));
  code = Regex.Replace(code, @"(\{|\})", m => Yellow(m.Value));
  code = Regex.Replace(code, @"(\;)", m => Blue(m.Value));
  code = Regex.Replace(code, @"(\,)", m => White(m.Value));
  code = Regex.Replace(code, @"(//.*)", m => Green(m.Value));
  code = Regex.Replace(code, "c0l0r", "color"); // restore the word "color"

  var keywordColors = new Dictionary<string, Func<string, string>> { { @"\bclass\s+(\w+)\b", Cyan }, { @"\b(if|else|for|while|return|class)\b", Orange }, { @"\b(from|import|export)\b", Mag }, { @"\b(function|var|let|const|extends)\b", Blue }
  };

  foreach (var kvp in keywordColors) {
      code = Regex.Replace(code, kvp.Key, m => kvp.Value(m.Value));
  }

  return White(code);
  }
}
