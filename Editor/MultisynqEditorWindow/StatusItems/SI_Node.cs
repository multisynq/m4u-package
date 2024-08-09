using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SI_Node: StatusItem {

  Button GotoNodePath_Btn;
  Button TryAuto_Btn;

  DropdownField Node_Dropdown;

    public SI_Node(MultisynqBuildAssistantEW parent = null) : base(parent)
    {
    }

    override public void InitUI() {
    //Debug.Log("SI_Node.InitUI()");
    SetupVisElem("Node_Status_Img",      ref statusImage);
    SetupLabel(  "Node_Message_Lbl",     ref messageLabel);
    SetupButton( "GotoNodePath_Btn",     ref GotoNodePath_Btn,   Clk_GotoNodePath);
    SetupButton( "TryAuto_Btn",          ref TryAuto_Btn,        Clk_AutoSetupNode);
    Node_Dropdown = FindElement<DropdownField>("Node_Dropdown");
    Node_Dropdown.RegisterValueChangedCallback( (evt) => {
      string nodePath = evt.newValue.Replace(" ∕ ", "/");
      string nodeVer = TryNodePath(nodePath);
      if (nodeVer == null) MqWelcome_StatusSets.node.error.Set();
      else {
        MqWelcome_StatusSets.node.success.Set();
        // set the CroquetSetting.nodePath
        var cqStgs = CqFile.FindProjectCqSettings();
        cqStgs.pathToNode = nodePath;
      }
      edWin.CheckAllStatusForReady();
    });
    HideVEs(Node_Dropdown);
  }
  override public void InitText() {
    //Debug.Log("SI_Node.InitText()");

    string t_node = "<b><color=#417E37>Node</color></b>";
    MqWelcome_StatusSets.node = new StatusSet( messageLabel, statusImage,
      // (info, warning, error, success)
      $"{t_node} is ready to go!",
      $"{t_node} is not running",
      $"{t_node} needs your help getting set up.",
      $"{t_node} path configured!!! Well done!",
      "Node status"
    );
    statusSet = MqWelcome_StatusSets.node;
  }
  override public bool Check() {
    //Debug.Log("SI_Node.Check()");
    var cqStgs = CqFile.FindProjectCqSettings();
    if (cqStgs == null) {
      MqWelcome_StatusSets.node.error.Set();
      HideVEs(TryAuto_Btn);
      return false;
    }
    string nodeVer  = TryNodePath(cqStgs.pathToNode);
    bool nodeIsGood = (nodeVer != null);
    MqWelcome_StatusSets.node.SetIsGood(nodeIsGood);
    SetVEViz( nodeIsGood, GotoNodePath_Btn );
    SetVEViz(!nodeIsGood, TryAuto_Btn); // bad, so show the TryAuto button
    return nodeIsGood;
  }


  public string TryNodePath(string nodePath) {
    if (!File.Exists(nodePath)) {
      Notify("Could not find node path file:\n" + nodePath);
      return null;
    } else return GetNodeVersion(nodePath, "-v");
  }

  private string GetNodeVersion(string executable = "", string arguments = "") {
    string output = ShellHelp.RunShell(executable, arguments);
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
  



  public void Clk_GotoNodePath() { // NODE  ------------- Click
    edWin.siSettings.GotoSettings();
    // notify message
    var msg = "See Inspector.\n\nCroquet Settings\nwith node path\nselected in Project.";
    edWin.ShowNotification(new GUIContent(msg), 4);
  }

  List<string> FindAllNodeIntances() {

    List<string> nodePaths = new List<string>();
    // loop through possible node folders (by platform) and collect found ones in a List
    var nodeFolders = new List<string>();
    // fetch the home folder and expand any ~

    if (Application.platform == RuntimePlatform.OSXEditor) {
      string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      nodeFolders = new List<string>{
        "/usr/local/bin",
        "/opt/homebrew/bin",
        "/usr/bin",
        $"{home}/.nvm/versions/node/*/bin"
      };
    } else if (Application.platform == RuntimePlatform.WindowsEditor) {
      nodeFolders = new List<string>{
        "C:/Program Files/nodejs/node.exe"
      };
    }
    // loop through the subfolders and expanding any * wildcards
    // make sure to split any folders with * into parent and wildcard

    var foldersWithNode = nodeFolders
      .SelectMany(folder => {
        if (folder.Contains("*")) {
          var parts = folder.Split('*');
          // remove any trailing slashes
          var parent = parts[0].TrimEnd('/');
          var child = parts[1].TrimStart('/');
          var expanded   = Directory.GetDirectories(parent, "*");
          var candidates = expanded.Select( d => d + "/" + child );
          // Debug.Log(  "Parent: " + parent + " Child: " + child +  " Found: " + y.Aggregate("", (acc, f) => acc + f + "\n") );
          return candidates; // i.e ["/usr/local/bin", "/opt/homebrew/bin"]
        } else {
          // Debug.Log("Folder: " + folder);
          return new string[]{ folder };
        }
      }).Where( folder => File.Exists(folder + "/node") && File.Exists(folder + "/npm") ).ToList();

    Debug.Log($"FindAllNodeIntances().foldersWithNode[{foldersWithNode.Count}] = [\n{foldersWithNode.Aggregate("", (acc, f) => $"{acc}  {f}/node,\n")}]");
    return foldersWithNode;
  }

  private void Clk_AutoSetupNode() { // NODE  ------------- Click
    Debug.Log("Auto Setup Node!");
    switch (Application.platform) {
      case RuntimePlatform.OSXEditor:
        Debug.Log("OSX Editor Detected");
        var cqStgs = CqFile.FindProjectCqSettings();
        var nodePaths = FindAllNodeIntances();
        if (nodePaths==null || nodePaths.Count == 0) {
          NotifyAndLogError("Node not found on your system. To get it: https://nodejs.org/en/download/prebuilt-installer");
          MqWelcome_StatusSets.node.error.Set();
          return;
        } else cqStgs.pathToNode = nodePaths[0] + "/node";
        Check();
        break;
      case RuntimePlatform.WindowsEditor:
        Debug.Log("Windows Editor Detected");
        string nodeVer = GetNodeVersion("cmd.exe", $"/c runwebpack.bat ");
        break;
      // case RuntimePlatform.LinuxEditor:
      //   Debug.Log("Linux Editor Detected");
      // break;
      default:
        Debug.LogError("Unsupported platform: " + Application.platform);
        break;
    }
    edWin.CheckAllStatusForReady();
  }

  public void NodePathsToDropdownAndCheck() {
    var nps = FindAllNodeIntances().Select( f => (f+"/node").Replace("/"," ∕ ") ).ToList();
    Node_Dropdown.choices = nps;
    ShowVEs(Node_Dropdown);
    // compare to CroquetSettings
    var cqStgs = CqFile.FindProjectCqSettings();
    if (cqStgs != null) {
      string nodePath = cqStgs.pathToNode.Replace("/"," ∕ ");
      if (nps.Contains(nodePath)) {
        Node_Dropdown.SetValueWithoutNotify(nodePath);
        MqWelcome_StatusSets.node.success.Set();
      } else MqWelcome_StatusSets.node.error.Set();
    }
  }
}

