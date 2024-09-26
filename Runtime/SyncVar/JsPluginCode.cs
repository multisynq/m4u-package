using System;
using System.Collections.Generic;
using System.Linq;

namespace Multisynq {

//========== |||||||||||| ================
public class JsPluginCode {

  public string _pluginName;
  public string _pluginCode;
  public string _initModelCode;
  public bool codeIsGood = true;
  public CodeBlockForATag[] _taggedCodes = null;

  //Array of delegate methods to check the code for problems: (JsPluginCode jpc) => { return jpc._pluginCode.Contains("export default class"); }
  public List<Func<JsPluginCode, bool>> _codeCheckers = new();

  //---------|||||||||||| --- constructor
  public     JsPluginCode( 
    string pluginName, 
    string pluginCode, 
    CodeBlockForATag[] taggedBlocks  = null,
    List<Func<JsPluginCode, bool>> codeCheckers = null
  ) { 
    if (string.IsNullOrWhiteSpace(pluginName)) throw new ArgumentException("pluginName cannot be null or whitespace.", nameof(pluginName));
    if (string.IsNullOrWhiteSpace(pluginCode)) throw new ArgumentException("pluginCode cannot be null or whitespace.", nameof(pluginCode));
    _pluginName = pluginName;
    _pluginCode = pluginCode;
    
    // tagged inits
    _taggedCodes = taggedBlocks ?? new CodeBlockForATag[0]; // empty array if null
    // if "ImportStatements" not present
    if (!_taggedCodes.Any( x=> x.tag == "ImportStatements")) {
      _taggedCodes = _taggedCodes.Append(new CodeBlockForATag(
        "ImportStatements", 
        $"import {{ {_pluginName} }} from './{_pluginName}'",
        0 // no indent for import
      ) ).ToArray();
    }

    if (codeCheckers != null) { _codeCheckers.AddRange(codeCheckers); }

    foreach (var checker in _codeCheckers) { // run code checkers and aggregate bools
      codeIsGood &= checker(this);
    }
    // Example checker delagate:
    // _codeCheckers.Add((JsPluginCode jpc) => { return jpc._pluginCode.Contains("export default class"); });

  }
  //----------- |||||||||| -------------------------
  public string GetRelPath() { return $"plugins/{_pluginName}.js"; }

}

} // namespace MultisynqNS