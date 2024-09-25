using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DefaultAsset))]
public class JsFileEditor : Editor {
  private Vector2 scrollPosition;
  private GUIStyle richTextStyle;
  private string cachedCode;
  private Font monoSpaceFont;
  private int fontSize = 16; // Default font size

  public override void OnInspectorGUI() {
    var path = AssetDatabase.GetAssetPath(target);
    string[] validExtensions = { ".js", ".ts", ".jsx", ".tsx", ".jslib" };

    if (Array.Exists(validExtensions, ext => path.ToLower().EndsWith(ext))) {
      CodeInspectorGUI();
    }
    else {
      base.OnInspectorGUI();
    }
  }

  private void CodeInspectorGUI() {

    // Controls outside the scroll view
    EditorGUI.BeginChangeCheck();
    fontSize = EditorGUILayout.IntSlider("Font Size", fontSize, 8, 36);
    if (EditorGUI.EndChangeCheck()) {
      richTextStyle = null;
      Repaint();
    }

    EditorGUILayout.LabelField($"Current Font Size: {fontSize}");

    if (richTextStyle == null) {
      richTextStyle = new GUIStyle(EditorStyles.textArea) {
        richText = true,
        wordWrap = false,
        fontSize = fontSize,
        font = LoadCustomFont()
      };
    }

    if (target == null) {
      EditorGUILayout.LabelField("Target is null");
      return;
    }

    string path = AssetDatabase.GetAssetPath(target);

    if (!File.Exists(path)) {
      EditorGUILayout.LabelField("File not found at path: " + path);
      return;
    }

    // Load and highlight code if needed
    if (string.IsNullOrEmpty(cachedCode)) {
      cachedCode = ApplySyntaxHighlighting(File.ReadAllText(path));
    }

    // Scrollable code area
    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

    float textWidth = richTextStyle.CalcSize(new GUIContent(cachedCode)).x;
    float textHeight = richTextStyle.CalcHeight(new GUIContent(cachedCode), textWidth);
    Rect viewRect = GUILayoutUtility.GetRect(textWidth, textHeight);

    EditorGUI.DrawRect(viewRect, Color.black);
    cachedCode = EditorGUI.TextArea(viewRect, cachedCode, richTextStyle);

    EditorGUILayout.EndScrollView();
  }

  private Font LoadCustomFont() {
    if (monoSpaceFont != null) return monoSpaceFont;

    string[] potentialPaths = new string[] {
      "Packages/io.multisynq.multiplayer/Editor/JSCodeInspector/RobotoMono-Regular.ttf",
      "Packages/io.multisynq.multiplayer/Editor/JSCodeInspector/SpaceMono-Regular.ttf",
      "Assets/Fonts/SpaceMono-Regular.ttf",
      "Assets/SpaceMono-Regular.ttf"
    };

    foreach (string path in potentialPaths) {
      monoSpaceFont = AssetDatabase.LoadAssetAtPath<Font>(path);
      if (monoSpaceFont != null) {
        Debug.Log($"Loaded custom font from: {path}");
        return monoSpaceFont;
      }
    }

    Debug.LogWarning("Custom font not found. Falling back to default font.");
    return EditorStyles.label.font;  // Fallback to default font
  }

  private string ApplySyntaxHighlighting(string code) {
    string ColorWrap(string s, string color) => $"<color={color}>{s}</color>";
    string Yellow(string s) => ColorWrap(s, "#FFD702");
    string Tan(string s) => ColorWrap(s, "#DEDEAC");
    string Green(string s) => ColorWrap(s, "#6B9955");
    string Blue(string s) => ColorWrap(s, "#569CD6");
    string Orange(string s) => ColorWrap(s, "#FFA500");
    string White(string s) => ColorWrap(s, "white");
    string Mag(string s) => ColorWrap(s, "#DB70D6");
    string Cyan(string s) => ColorWrap(s, "#4EC9B0");

    code = Regex.Replace(code, @"color", "c0l0r");
    code = Regex.Replace(code, @"(\=)", m => Mag(m.Value));
    code = Regex.Replace(code, @"\.([\w_0-9]+)\(", m => Tan(m.Value)); // between a . and a ( is tan like this.foo()
    code = Regex.Replace(code, @"(\(|\))", m => Mag(m.Value));
    code = Regex.Replace(code, @"(\{|\})", m => Yellow(m.Value));
    code = Regex.Replace(code, @"(\;)", m => Blue(m.Value));
    code = Regex.Replace(code, @"(\,)", m => White(m.Value));
    code = Regex.Replace(code, @"(//.*)", m => Green(m.Value));

    var keywordColors = new Dictionary<string, Func<string, string>> { { @"\bclass\s+(\w+)\b", Cyan }, { @"\b(if|else|for|while|return|class)\b", Orange }, { @"\b(from|import|export)\b", Mag }, { @"\b(function|var|let|const|extends)\b", Blue }
  };

    foreach (var kvp in keywordColors) {
      code = Regex.Replace(code, kvp.Key, m => kvp.Value(m.Value));
    }

    return White(code);
  }
}