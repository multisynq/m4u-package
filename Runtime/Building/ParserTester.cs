using UnityEngine;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SnipForATag {
  public string Tag { get; set; }
  public string Code { get; set; }
  public string Plugin { get; set; }

  public SnipForATag(string tag, string code, string plugin) {
    Tag = tag;
    Code = code;
    Plugin = plugin;
  }
}

public class ParserTester : MonoBehaviour {
  [TextArea(15, 20)]
  public string input = @"
  woot [[Import: import %%CODE%% from './%%PLUGIN%%';]] boot
";

  [TextArea(15, 20)]
  public string tokensReport;

  [TextArea(15, 20)]
  public string output;

  private DelimPair[] delimPairs = new DelimPair[] {
  new DelimPair { Start = "[[", End = "]]" },
  new DelimPair { Start = ":", End = "]]" },
  new DelimPair { Start = "%%", End = "%%" }
  };
  /*
  Token: 'woot '
  DelimIdx: -1
  DelimDepths: [0, 0, 0]

  Token: '[['
  DelimIdx: 0
  DelimDepths: [1, 0, 0]

  Token: 'Import'
  DelimIdx: -1
  DelimDepths: [1, 0, 0]

  Token: ':'
  DelimIdx: 2
  DelimDepths: [1, 1, 0]

  Token: ' import '
  DelimIdx: -1
  DelimDepths: [1, 1, 0]

  Token: '%%'
  DelimIdx: 3
  DelimDepths: [1, 1, 1]

  Token: 'CODE'
  DelimIdx: -1
  DelimDepths: [1, 1, 1]

  Token: '%%'
  DelimIdx: 3
  DelimDepths: [1, 1, 0]

  Token: ' from './'
  DelimIdx: -1
  DelimDepths: [1, 1, 0]

  Token: '%%'
  DelimIdx: 3
  DelimDepths: [1, 1, 1]

  Token: 'PLUGIN'
  DelimIdx: -1
  DelimDepths: [1, 1, 1]

  Token: '%%'
  DelimIdx: 3
  DelimDepths: [1, 1, 0]

  Token: '';'
  DelimIdx: -1
  DelimDepths: [1, 1, 0]

  Token: ']]'
  DelimIdx: 1
  DelimDepths: [1, 1, 0]

  Token: ' boot'
  DelimIdx: -1
  DelimDepths: [0, 0, 0]


  */
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
    var snips = new[] {
      new SnipForATag("Import", "{ Cat, Dog }",    "animals"),
      new SnipForATag("Import", "{ Ant, Beetle }", "animals"),
  };

    output = SwapInManySnips(tokens, snips);
  }
  
  private string SwapInManySnips(Token[] tokens, SnipForATag[] snips) {
    var result = new StringBuilder();
    var code = new StringBuilder();
    List<string> merged = new();
    SnipForATag[] currTagSnips = new SnipForATag[0];
    bool insideTag = false;

    foreach (var token in tokens) {

      if (token.DelimIdx > -1) continue; // skip the delimiters themselves since they are encoded in the DelimDepths

      if (token.DelimDepths.SequenceEqual(new[] { 0, 0, 0 })) { // outside tags "HERE [[xxx:xxx %xxx% xxx;]] OR HERE"
        if (!insideTag) {
          code.Append(token.Txt);
        } else {
          // we should have merged to add to the code
          for (int i = 0; i < merged.Count; i++) {
            code.Append(merged[i]+"\n");
          }
          // clear merged and currTagSnips
          merged.Clear();
          currTagSnips = new SnipForATag[0];
          insideTag = false;
        }
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 0, 0 })) { // "[[HERE: xxx %xx%% xxx %%xx%% xxx;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[HERE: xxx %xx%% xxx %%xx%% xxx;]]"); 
        insideTag = true;
        currTagSnips = snips.Where(s => s.Tag == token.Txt).ToArray();
        merged.AddRange(Enumerable.Repeat("", currTagSnips.Count())); // blank merged of same length
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 1, 0 })) { // "[[xxx:HERE %xxx% OR HERE;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[xxx:HERE %xxx% OR HERE;]]");
        for (int i = 0; i < merged.Count; i++) {
          merged[i] += token.Txt;
        }
      }
      else if (token.DelimDepths.SequenceEqual(new[] { 1, 1, 1 })) { // "[[xxx:xxxx %HERE% xxx %OR_HERE% xxxx;]]"
        Debug.Log($"<color=cyan>{token.Txt}</color> - [[xxx:xxxx %%HERE% xxx %OR_HERE% xxxx;]]"); 
        if (token.Txt == "CODE") {
          for (int i = 0; i < merged.Count; i++) {
            merged[i] += currTagSnips.ElementAt(i).Code;
          }
        }
        else if (token.Txt == "PLUGIN") {
          for (int i = 0; i < merged.Count; i++) {
            merged[i] += currTagSnips.ElementAt(i).Plugin;
          }
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