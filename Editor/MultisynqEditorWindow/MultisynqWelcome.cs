using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TemporaryMenu {
  [MenuItem("Croquet/Copy UI to Cq package (TODO: DEL THIS)", false, 100)]
  private static void CopyUiToPackage() {
    string localUiFolder = "Assets/Scripts/Editor/UI_Only_MultisynqEditorWindow/";
    string packageFolder = "Packages/io.croquet.multiplayer/Editor/MultisynqEditorWindow/";
    string[] files = new string[] {
      // "MultisynqWelcome.cs",
      "MultisynqWelcome.uxml",
      // "MultisynqWelcome.uss",
      // "CroquetSettings.cs",
      // "CroquetSettings_Template.asset",
      // "Images/MultiSynq_Icon.png"
    };
    foreach (string file in files) {
      string src  = localUiFolder + file;
      string dest = packageFolder + file;
      Debug.Log("Copying " + src + " to " + dest);
      File.Copy(src, dest, true);
    }
    // Now reload that package
    AssetDatabase.Refresh();
  }
}

public class Colz {
  public Color green;
  public Color red;
  public Color yellow;
  public Color blue;
  public Color lime;
  public Color white;
  public Color c_node;

  public Colz() {
    green  = GetColor("#BFFFC5");
    red    = GetColor("#FFBFBF");
    yellow = GetColor("#FFFFBF");
    blue   = GetColor("#006AFF");
    lime   = GetColor("#00FF00");
    white  = GetColor("#FFFFFF");
    c_node = GetColor("#417E37");
  }

  public static Color GetColor(string hex) {
    Color color;
    ColorUtility.TryParseHtmlString(hex, out color);
    return color;
  }
};

public class MultisynqWelcomeEW : EditorWindow {
  //=============================================================================
  public class Status {
    public string message;
    public Color color;
    public Label label;
    public VisualElement img;
    public void Set() {
      label.text = message;
      img.style.unityBackgroundImageTintColor = color;
    }
    public Status(Label label, VisualElement img, string message, Color color) {
      this.message = message;
      this.color = color;
      this.label = label;
      this.img = img;
    }
  }
  
  public class StatusSet {
    public Status ready;
    public Status warning;
    public Status error;
    public Status success;
    public Status blank;
    public Label label;
    public VisualElement img;
    public StatusSet(Label label, VisualElement img, string info, string warning, string error, string success) {
      this.ready   = new Status(label, img, info,    colz.green);
      this.warning = new Status(label, img, warning, colz.yellow);
      this.error   = new Status(label, img, error,   colz.red);
      this.success = new Status(label, img, success, colz.lime);
      this.blank   = new Status(label, img, "--",    colz.white); 
    }
  }
  static public class Statuses {
    static public StatusSet ready;
    static public StatusSet node;
    static public StatusSet key;
    static public StatusSet jsBuild;
    static public StatusSet settings;
  }

  private static Colz colz;

  // public StatusSet statuses_ready;
  // public StatusSet statuses_node;
  // public StatusSet statuses_key;
  // public StatusSet statuses_jsBuild;
  //=============================================================================

  private Button Awesome_Btn;        
  private Button TakeMeToSetting_Btn;
  private Button TryAuto_Btn;
  private Button SignUpApi_Btn;      
  private Button ToggleJSBuild_Btn;
  private Button Top_Ready_Docs_Btn;
  private Button CheckIfReady_Btn;
  private Button SettingsSelect_Btn;
  private Button SettingsCreate_Btn;


  private Label Ready_Message_Lbl;
  private Label Node_Message_Lbl;
  private Label Key_Message_Lbl;
  private Label JSBuild_Message_Lbl;
  private Label Settings_Message_Lbl;

  private VisualElement Ready_Status_Img;
  private VisualElement Node_Status_Img;
  private VisualElement Key_Status_Img;
  private VisualElement JSBuild_Status_Img;
  private VisualElement Settings_Status_Img;

  //=============================================================================

  // private const string ewFolder = "Assets/Scripts/Editor/MultisynqEditorWindow/";
  private const string ewFolder = "Packages/io.croquet.multiplayer/Editor/MultisynqEditorWindow/";
  private const string cqSettingsAssetOutputPath = "Assets/Settings/CroquetSettings_XXXXXXXX.asset";

  //=============================================================================

  [MenuItem("Window/==Multisynq Welcome (from Package!)")]
  public static void ShowMultisynqWelcome() {
    var ceWindow = GetWindow<MultisynqWelcomeEW>();
    // Assets/Scripts/Editor/MultisynqEditorWindow/Images/MultiSynq_Icon.png
    var icon = AssetDatabase.LoadAssetAtPath<Texture>(ewFolder + "Images/MultiSynq_Icon.png");
    ceWindow.titleContent = new GUIContent("Multisynq Welcome", icon);
  }

  void AllStatusToBlank() {
    Statuses.ready.blank.Set();
    Statuses.node.blank.Set();
    Statuses.key.blank.Set();
    Statuses.jsBuild.blank.Set();
  }

  public void CreateGUI() {
    colz = new();

    // Each editor window contains a root VisualElement object
    var root = rootVisualElement;

    // Import UXML
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ewFolder + "MultisynqWelcome.uxml");
    var labelFromUXML = visualTree.Instantiate();
    root.Add(labelFromUXML);

    // A stylesheet can be added to a VisualElement.
    // The style will be applied to the VisualElement and all of its children.
    // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CustomEditor.uss");
    // root.styleSheets.Add(styleSheet);

    SetupUI();
    SetupStatuses();

    Statuses.node.error.Set();
    Statuses.key.warning.Set();
    Statuses.jsBuild.success.Set();
    Statuses.ready.ready.Set();

    AllStatusToBlank();
    // // Ready_Status_Img blue!
  }
  
  //=============================================================================
  //=============================================================================
  
  private void SetupUI() {
    // Getting the UI element by type needs to be filtered inorder to get what we want
    // _textField = rootVisualElement.Query<TextField>().First();
    // Getting the UI element by type and name
    SetupButton("Awesome_Btn",         ref Awesome_Btn,         Clk_BeAwesome      );
    SetupButton("TakeMeToSetting_Btn", ref TakeMeToSetting_Btn, Clk_TakeMeToSetting);
    SetupButton("TryAuto_Btn",         ref TryAuto_Btn,         Clk_AutoSetupNode  );
    SetupButton("SignUpApi_Btn",       ref SignUpApi_Btn,       Clk_SignUpApi      );
    SetupButton("ToggleJSBuild_Btn",   ref ToggleJSBuild_Btn,   Clk_ToggleJSBuild  ); // Start JS Build Watcher
    SetupButton("Top_Ready_Docs_Btn",  ref Top_Ready_Docs_Btn,  Clk_Top_Ready_Docs );
    SetupButton("CheckIfReady_Btn",    ref CheckIfReady_Btn,    Clk_CheckIfReady   );
    SetupButton("SettingsSelect_Btn",  ref SettingsSelect_Btn,  Clk_SettingsSelect );
    SetupButton("SettingsCreate_Btn",  ref SettingsCreate_Btn,  Clk_SettingsCreate );

    SetupLabel("Ready_Message_Lbl",    ref Ready_Message_Lbl   ); 
    SetupLabel("Node_Message_Lbl",     ref Node_Message_Lbl   );
    SetupLabel("Key_Message_Lbl",      ref Key_Message_Lbl    );
    SetupLabel("JSBuild_Message_Lbl",  ref JSBuild_Message_Lbl);
    SetupLabel("Settings_Message_Lbl", ref Settings_Message_Lbl);

    // SetupImage("Ready_Status_Img",     ref Ready_Status_Img  );
    // SetupImage("Node_Status_Img",      ref Node_Status_Img   );
    // SetupImage("Key_Status_Img",       ref Key_Status_Img    );
    // SetupImage("JSBuild_Status_Img",   ref JSBuild_Status_Img);

    SetupVisElem("Ready_Status_Img",     ref Ready_Status_Img  );
    SetupVisElem("Node_Status_Img",      ref Node_Status_Img   );
    SetupVisElem("Key_Status_Img",       ref Key_Status_Img    );
    SetupVisElem("JSBuild_Status_Img",   ref JSBuild_Status_Img);
    SetupVisElem("Settings_Status_Img",  ref Settings_Status_Img);
  }

  private void SetupStatuses() {
    string t_synq = "<b><color=#006AFF>Synq</color></b>";
    string t_key  = "<b><color=#006AFF>API Key</color></b>";
    string t_node = "<b><color=#417E37>Node</color></b>";
    string t_jsb  = "<b><color=#E5DB1C>JS Build</color></b>";

    Statuses.ready = new StatusSet( Ready_Message_Lbl, Ready_Status_Img,
      // (info, warning, error, success)
      $"You are <b><size=+1><color=#77ff77>Ready to </color>{t_synq}</b></size><color=#888>      All green lights below.",
      $"Warn 00000",
      $"Look below in \"Help Getting Set Up\" for what's not ready...",
      $"W00t!!! You are ready to {t_synq}!" // displays for 5 seconds, then switches to the .ready message
    );
    Statuses.node = new StatusSet( Node_Message_Lbl, Node_Status_Img,
      // (info, warning, error, success)
      $"{t_node} is ready to go!",
      $"{t_node} is not running",
      $"{t_node} needs your help getting set up.",
      $"{t_node} path configured!!! Well done!"
    );
    Statuses.key = new StatusSet( Key_Message_Lbl, Key_Status_Img,
      // ... info, warning, error, success)
      $"The {t_key} is ready to go!",
      $"The {t_key} is not set",
      $"Let's get you a free {t_key}. It's easy.",
      $"The {t_key} is configured!!! Well done!"
    );
    Statuses.jsBuild = new StatusSet( JSBuild_Message_Lbl, JSBuild_Status_Img,
      // ... info, warning, error, success)
      $"{t_jsb} is ready to go!",
      $"{t_jsb} is not ready",
      $"{t_jsb} needs your help getting set up.",
      $"{t_jsb} path configured!!! Well done!"
    );
    Statuses.settings = new StatusSet( Settings_Message_Lbl, Settings_Status_Img,
      // ... info, warning, error, success)
      $"Settings are ready to go!",
      $"Settings are not set",
      $"Let's get you a free {t_key}. It's easy.",
      $"Settings are configured!!! Well done!"
    );
  }

  //=============================================================================
  //=============================================================================

  private void Clk_SettingsSelect() {
    Clk_TakeMeToSetting();
  }
  private void Clk_SettingsCreate() {
    // CroquetSettings in scene
    var cqStgs = EnsureSettingsFile();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create CroquetSettings file");
      Statuses.ready.error.Set();
    } else {
      Statuses.ready.success.Set();
    }
    Clk_TakeMeToSetting();
  }

  private void Clk_CheckIfReady() {
    // CroquetSettings in scene
    var cqStgs = EnsureSettingsFile();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create CroquetSettings file");
      Statuses.ready.error.Set();
    } else {

    }
  }

  private void Clk_SignUpApi() {
    Clk_TakeMeToSetting();
    Application.OpenURL("https://croquet.io/account/");
  }
  private void Clk_Top_Ready_Docs() {
    Application.OpenURL("https://multisynq.io/docs/unity/");
  }

  private void Clk_BeAwesome() {
    Debug.Log("Be Awesome!!!!");
    // Application.OpenURL("https://www.youtube.com/watch?v=dQw4w9WgXcQ"); // Copilot thinks you should go to this url. You know you want to. =]
    Application.OpenURL("https://giphy.com/search/everything-is-awesome");
  }
  private async void Clk_ToggleJSBuild() {
    if (ToggleJSBuild_Btn.text == "Start JS Build Watcher") {
      bool success = await CroquetBuilder.EnsureJSToolsAvailable();
      if (!success) return;
      CroquetBuilder.StartBuild(true); // true => start watcher
      Debug.Log("Started JS Build Watcher");
      ToggleJSBuild_Btn.text = "Stop JS Build Watcher";
    } else {
      ToggleJSBuild_Btn.text = "Start JS Build Watcher";
      CroquetBuilder.StopWatcher();
    }
  }

  private void Clk_TakeMeToSetting() {
    // Select the file in Project pane of the Editor so it shows up in the Inspector
    var cqStgs = EnsureSettingsFile();
    if (cqStgs == null) {
      Debug.LogError("Could not find or create CroquetSettings file");
      return;
    } else {
      Debug.Log("Found CroquetSettings file");
      Selection.activeObject = cqStgs;
    }
  }


  private void Clk_AutoSetupNode() {
    Debug.Log("Auto Setup Node!");
    if (!Application.isEditor) {
      Debug.LogError("This feature is only available in the Editor.");
      return;
    }

    string nodeVer;

    switch (Application.platform) {
      case RuntimePlatform.OSXEditor:
        Debug.Log("OSX Editor Detected");
        nodeVer = GetNodeVersion("/usr/local/bin/node", "-v");
        break;
      case RuntimePlatform.WindowsEditor:
        Debug.Log("Windows Editor Detected");
        nodeVer = GetNodeVersion("cmd.exe", $"/c runwebpack.bat ");
        break;
      // case RuntimePlatform.LinuxEditor:
      // Debug.Log("Linux Editor Detected");
      // break;
      default:
        Debug.LogError("Unsupported platform: " + Application.platform);
        break;
    }
  }

  //=============================================================================
  //=============================================================================

  private void SetupButton(string buttonName, ref Button button, Action buttonAction) {
    button = rootVisualElement.Query<Button>(buttonName).First();
    if (button == null) {
      Debug.LogError("Could not find button: " + buttonName);
      return;
    }
    button.clicked += buttonAction;
  }

  private void SetupLabel(string labelName, ref Label label) {
    label = rootVisualElement.Query<Label>(labelName).First();
    if (label == null) {
      Debug.LogError("Could not find label: " + labelName);
    }
  }

  private void SetupVisElem(string visElemName, ref VisualElement visElem) {
    visElem = rootVisualElement.Query<VisualElement>(visElemName).First();
    if (visElem == null) {
      Debug.LogError("Could not find VisualElement: " + visElemName);
    }
  }

  //=============================================================================
  
  private CroquetSettings EnsureSettingsFile() {
    CroquetSettings cqSettings = null;
    // Check if the file is there
    string[] guids = AssetDatabase.FindAssets("t:CroquetSettings");
    if (guids.Length > 0) {
      cqSettings = AssetDatabase.LoadAssetAtPath<CroquetSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
      if (guids.Length > 1) {
        Debug.LogWarning("Found more than one CroquetSettings file. You should only have one.");
        // Print out path of all files found
        int i = 1;
        foreach (string guid in guids) {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          // Make them select when click each log
          Debug.LogWarning( i++ + ". " + path, AssetDatabase.LoadAssetAtPath<CroquetSettings>(path));
        }
      }
    }

    // If not, copy file from ./resources/CroquetSettings_Template.asset
    // into Assets/Settings/CroquetSettings.asset
    if (cqSettings == null) {
      string path = ewFolder + "resources/CroquetSettings_Template.asset";
      AssetDatabase.CopyAsset(path, cqSettingsAssetOutputPath);
      cqSettings = AssetDatabase.LoadAssetAtPath<CroquetSettings>(cqSettingsAssetOutputPath);
    }
    return cqSettings;
  }
  
  //=============================================================================

  private void StartShScript(string scriptPath) {
    string path = EdWinPath(scriptPath);
    StartScript(path);
  }

  private string EdWinPath(string path) {
    string ewPath = Path.GetFullPath(Path.Combine(ewFolder, path));
    if (!File.Exists(ewPath)) {
      Debug.LogError("Could not find file: " + ewPath);
      return null;
    }
    return ewPath;
  }
  
  private string TryNodePath(string nodePath) {
    if (!File.Exists(nodePath)) {
      Debug.LogError("Could not find file: " + nodePath);
      return null;
    } else {
      return GetNodeVersion(nodePath, "-v");
    }
  }
  private string GetNodeVersion(string executable = "", string arguments = "") {
    string output = RunShell(executable, arguments);
    string[] stdoutLines = output.Split('\n');

    // If output is a string that starts with v and then has a number,
    // then it's the version number, otherwise its an error
    foreach (string line in stdoutLines) {
      if (line.StartsWith("v") && float.TryParse(line.Substring(1,3), out float version)) {
        Debug.Log("Node version: " + line);
        return line;
      }
    }
    Debug.LogError("Node not found");
    return null;
  }

  private string RunShell(string executable = "", string arguments = "", int logLevel = 2) {

    System.Diagnostics.Process pcs = new();
    pcs.StartInfo.UseShellExecute = false;
    pcs.StartInfo.RedirectStandardOutput = true;
    pcs.StartInfo.RedirectStandardError = true;
    pcs.StartInfo.CreateNoWindow = true;
    pcs.StartInfo.WorkingDirectory = Path.GetFullPath(ewFolder);
    pcs.StartInfo.FileName = executable;
    pcs.StartInfo.Arguments = arguments;
    pcs.Start();

    string output = pcs.StandardOutput.ReadToEnd();
    string errors = pcs.StandardError.ReadToEnd();
    pcs.WaitForExit();

    if (output.Length > 0 && logLevel > 1) Debug.Log(output);
    if (errors.Length > 0 && logLevel > 0) Debug.LogError(errors);

    return output;
  }

  private void StartScript(string scriptPath, string arguments = "", Action callback = null) {
    Debug.Log("Running: " + scriptPath + " " + arguments);
    System.Diagnostics.Process pcs = new();
    pcs.StartInfo.UseShellExecute = false;
    pcs.StartInfo.RedirectStandardOutput = true;
    pcs.StartInfo.RedirectStandardError = true;
    pcs.StartInfo.CreateNoWindow = false;
    pcs.StartInfo.WorkingDirectory = Path.GetFullPath(ewFolder);
    pcs.StartInfo.FileName = "/bin/bash";
    pcs.StartInfo.Arguments = scriptPath;
    pcs.Start();

    // if (callback != null) {
      pcs.Exited += (sender, e) => {
        // callback(sender, e);
        Debug.Log("Exited with code: " + pcs.ExitCode);
      };
    // }

    string output = pcs.StandardOutput.ReadToEnd();
    string errors = pcs.StandardError.ReadToEnd();
    pcs.WaitForExit();
    if (output.Length > 0) Debug.Log(output);
    if (errors.Length > 0) Debug.LogError(errors);


    pcs.Close();
  }
}