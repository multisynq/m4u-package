using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

static class StringExtensions {
  //------------------ |||||||||| --------------------
  public static string LessIndent(this string str) {
    var lines = str.Split('\n');
    var minIndent =  lines.Where(l => l.Trim().Length > 0)
                          .Min(l => l.TakeWhile(char.IsWhiteSpace).Count());
    return  string.Join('\n', lines.Select(l => l.Length > minIndent ? l.Substring(minIndent) : l))
                  .Trim('\n');
  }
  //------------------ |||| --------------------
  public static string Join(this string[] strings, string separator="\n") {
    return string.Join(separator, strings);
  }
  //------------------ |||||||||||| --------------------
  public static string JoinIndented(this IEnumerable<string> strs, int inSpaces = 0, string separator = "\n") {
    var spaces = new string(' ', inSpaces);
    return string.Join(separator, strs.Select(s => spaces + s.Trim()));
  }

  static public string[] SplitAndTrimToArray(this string input, string delimiter = "\n") {
    return input.Split(delimiter).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
  }

  static public string SplitAndTrimToString(this string input, string delimiter = "\n") {
    return String.Join(delimiter, input.SplitAndTrimToArray(delimiter));
  }

  static public string CapitalizeFirst(this string input) {
    return input.Length > 0 ? char.ToUpper(input[0]) + input.Substring(1) : input;
  }

  static public string ConvertToJsonArray(this string[] stringArray) {
    return $"[{String.Join(",", stringArray.Select(s => $"\"{s}\"").ToArray())}]";
  }

}