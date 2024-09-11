using System.Linq;

public static class StringExtensions {
  public static string LessIndent(this string str) {
    var lines = str.Split('\n');
    var minIndent =  lines.Where(l => l.Trim().Length > 0)
                          .Min(l => l.Length - l.TrimStart().Length);
    return  string.Join('\n', lines.Select(l => l.Length > minIndent ? l.Substring(minIndent) : l))
                  .Trim('\n');
  }
}