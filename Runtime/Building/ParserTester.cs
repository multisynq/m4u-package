using UnityEngine;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

[Serializable]
public class SnipForATag {
  public string tag;
  public string code;
  public string plugin;

  public SnipForATag
    (string tag, string code, string plugin) => 
    (  this.tag,   this.code,   this.plugin) = 
    (       tag,        code,        plugin);
}

public class ParserTester : MonoBehaviour {
  [TextArea(15, 20)]
  public string input = @"
    // ### imports
    [[Import: import %%CODE%% from './%%PLUGIN%%';]]
    // ### imports end
  ".LessIndent();

  public SnipForATag[] snips = new[] {
    new SnipForATag("Import", "{ Cat, Dog }",    "animals"),
    new SnipForATag("Import", "{ Ant, Beetle }", "animals"),
  };

  DelimPair[] delimPairs = new DelimPair[] {
    new DelimPair { Start = "[[", End = "]]" },
    new DelimPair { Start = ":", End = "]]" },
    new DelimPair { Start = "%%", End = "%%" }
  };

  [TextArea(15, 20)]
  public string output;

  [TextArea(15, 20)]
  public string tokensReport;



  private void Start() {
    ParseInput();
  }

  private void ParseInput() {
    var tokens = Parser.ParseTokensWithPairs(input, delimPairs, ProcessNonDelimToken);

    GenerateTokensReport(tokens);
    GenerateOutput(tokens);
  }

  private Token ProcessNonDelimToken(Token token, int[] depths) {
    // This is where you can process non-delimiter tokens if needed
    return token;
  }

  private void GenerateTokensReport(Token[] tokens) {
    var report = new StringBuilder();
    foreach (var token in tokens) {
      report.AppendLine($"Token: '{token.Txt}'    DelimIdx: {token.DelimIdx}");
      report.AppendLine($"   DelimDepths: [{string.Join(", ", token.DelimDepths)}]");
      report.AppendLine();
    }
    tokensReport = report.ToString();
  }

  private void GenerateOutput(Token[] tokens) {
    output = SwapInManySnips(tokens, snips);
  }
  
  static private string SwapInManySnips(Token[] tokens, SnipForATag[] snips) {
    Debug.Log("<color=#4444ff>============= SWAP IN MANY SNIPS =============</color>");
    var result = new StringBuilder();
    var code = new StringBuilder();
    List<string> merged = new();
    SnipForATag[] currTagSnips = new SnipForATag[0];
    bool pendingMergedCode = false;

    // helper inline function to AddToAllMerged()
    void AddToAllMerged( Func<int, string> txtFunc ) {
      for (int i = 0; i < merged.Count; i++) {
        merged[i] += txtFunc(i);
      }
    }
    foreach (var token in tokens) {
      bool HasDelimDepths( params int[] depths ) {
        return token.DelimDepths.SequenceEqual( depths );
      }
      // delims are all zero
      bool outsideAllDelims = HasDelimDepths( 0, 0, 0 ); 

      if (token.DelimIdx > -1 & !outsideAllDelims) continue; // skip the delimiters themselves since they are encoded in the DelimDepths

      if (outsideAllDelims) { // outside tags "HERE [[xxx:xxx %xxx% xxx;]] OR HERE"
        Debug.Log($"<color=white>{token.Txt.Replace("\n","\\n")}</color> outside pendingMergedCode={pendingMergedCode}"); 
        if (pendingMergedCode) {
          code.Append( string.Join("\n", merged) );
          merged.Clear();
          currTagSnips = new SnipForATag[0];
          pendingMergedCode = false;
        }
        code.Append(token.Txt);
        Debug.Log($"<color=cyan>{token.Txt.Replace("\n","\\n")}</color> Append");
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 0, 0 })) { // "[[HERE: xxx %xx%% xxx %%xx%% xxx;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[HERE: xxx %xx%% xxx %%xx%% xxx;]]"); 
        pendingMergedCode = true;
        currTagSnips = snips.Where(s => s.tag == token.Txt).ToArray();
        merged.AddRange(Enumerable.Repeat("", currTagSnips.Count())); // blank merged of same length
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 1, 0 })) { // "[[xxx:HERE %xxx% OR HERE;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[xxx:HERE %xxx% OR HERE;]]");
        AddToAllMerged( (i) => token.Txt );
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 1, 1 })) { // "[[xxx:xxxx %HERE% xxx %OR_HERE% xxxx;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[xxx:xxxx %%HERE% xxx %OR_HERE% xxxx;]]"); 
        if (token.Txt == "CODE") {
          AddToAllMerged( (i) => currTagSnips.ElementAt(i).code );
        }
        else if (token.Txt == "PLUGIN") {
          AddToAllMerged( (i) => currTagSnips.ElementAt(i).plugin );
        }
      }
    } // end foreach

    return code.ToString().Trim();
  }

  [ContextMenu("Parse Input")]
  private void ParseInputMenu() {
    ParseInput();
    Debug.Log("Parsing complete. Check Inspector for results.");
  }
}