using System;
using System.Collections.Generic;
using System.Linq;

namespace Multisynq {

//========== |||||||||||| ================
public class JsPluginCode {

  public string   pluginName;
  public string   pluginCode;
  public string[] pluginExports;
  public string   initModelCode;
  public bool     codeIsGood = true;

  // Array of delegate methods to check the code for problems: 
  // i.e. (JsPluginCode jpc) => { return jpc.pluginCode.Contains("export class"); }
  // i.e. (JsPluginCode jpc) => { return jpc.pluginCode.MatchPatterns(new[] {"import.*Model", "export.*class"}); }
  public List<Func<JsPluginCode, bool>> codeCheckers = new();

  //---- |||||||||||| --- constructor
  public JsPluginCode( 
    string pluginName,
    string[] pluginExports,
    string pluginCode, 
    List<Func<JsPluginCode, bool>> codeCheckers = null
  ) { 
    if (string.IsNullOrWhiteSpace(pluginName)) throw new ArgumentException("pluginName cannot be null or whitespace.", nameof(pluginName));
    if (string.IsNullOrWhiteSpace(pluginCode)) throw new ArgumentException("pluginCode cannot be null or whitespace.", nameof(pluginCode));
    
    this.pluginName = pluginName;
    this.pluginCode = pluginCode;
    this.pluginExports = pluginExports;

    if (codeCheckers != null) { this.codeCheckers.AddRange(codeCheckers); }
    this.codeIsGood = this.codeCheckers.All(checker => checker(this)); // run code checkers and &= aggregate their bools
  }

}

} // namespace MultisynqNS