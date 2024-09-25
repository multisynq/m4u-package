using System;
using System.Collections.Generic;
using System.Linq;

namespace Multisynq {

//========== |||||||||||| ================
public class JsPluginCode {

  public string _klassName;
  public string _klassCode;
  public string _initModelCode;
  public bool codeIsGood = true;
  public CodeBlockForATag[] _taggedCodes = null;

  //Array of delegate methods to check the code for problems: (JsPluginCode jpc) => { return jpc._klassCode.Contains("export default class"); }
  public List<Func<JsPluginCode, bool>> _codeCheckers = new();

  //---------|||||||||||| --- constructor
  public     JsPluginCode( 
    string klassName, 
    string klassCode, 
    CodeBlockForATag[] taggedInits  = null,
    List<Func<JsPluginCode, bool>> codeCheckers = null
  ) { 
    if (string.IsNullOrWhiteSpace(klassName)) throw new ArgumentException("KlassName cannot be null or whitespace.", nameof(klassName));
    if (string.IsNullOrWhiteSpace(klassCode)) throw new ArgumentException("KlassCode cannot be null or whitespace.", nameof(klassCode));
    _klassName = klassName;
    _klassCode = klassCode;
    
    // tagged inits
    _taggedCodes = taggedInits ?? new CodeBlockForATag[0]; // empty array if null
    // if "ImportStatements" not present
    if (!_taggedCodes.Any( x=> x.tag == "ImportStatements")) {
      _taggedCodes = _taggedCodes.Append(new CodeBlockForATag(
        "ImportStatements", 
        $"import {{ {_klassName} }} from './{_klassName}'",
        0 // no indent for import
      ) ).ToArray();
    }

    if (codeCheckers != null) { _codeCheckers.AddRange(codeCheckers); }

    foreach (var checker in _codeCheckers) { // run code checkers and aggregate bools
      codeIsGood &= checker(this);
    }
    // Example checker delagate:
    // _codeCheckers.Add((JsPluginCode jpc) => { return jpc._klassCode.Contains("export default class"); });

  }
  //----------- |||||||||| -------------------------
  public string GetRelPath() { return $"plugins/{_klassName}.js"; }

}

} // namespace MultisynqNS