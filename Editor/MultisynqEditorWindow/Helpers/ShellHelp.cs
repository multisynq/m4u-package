using System.IO;
using UnityEngine;

static public class ShellHelp {

  static public string RunShell(string executable = "", string arguments = "", int logLevel = 2, bool shellExec = false) {
    System.Diagnostics.Process pcs       = new();
    pcs.StartInfo.UseShellExecute        = shellExec;
    pcs.StartInfo.RedirectStandardOutput = true;
    pcs.StartInfo.RedirectStandardError  = true;
    pcs.StartInfo.CreateNoWindow         = true;
    pcs.StartInfo.WorkingDirectory       = Path.GetFullPath(CqFile.ewFolder);
    pcs.StartInfo.FileName               = executable;
    pcs.StartInfo.Arguments              = arguments;
    pcs.StartInfo.UserName               = "root";
    pcs.Start();

    string output = pcs.StandardOutput.ReadToEnd();
    string errors = pcs.StandardError.ReadToEnd();
    pcs.WaitForExit();
    string exeAsJustFile = Path.GetFileName(executable);

    if (output.Length > 0 && logLevel > 1) {
      Debug.Log(     $"RunShell({exeAsJustFile} {arguments}).output = '{output.Trim()}'");
    }
    if (errors.Length > 0 && logLevel > 0) {
      Debug.LogError($"RunShell({exeAsJustFile} {arguments}).errors = '{errors.Trim()}'");
    }

    return output;
  }
}