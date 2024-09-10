#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public class Parser {
  public string input;
  public int position;

  public Parser(string input) {
    this.input = input;
    this.position = 0;
  }

  public T Parse<T>(Func<Parser, T> rule) => rule(this);

  public T? TryParse<T>(Func<Parser, T?> rule) where T : class {
    int startPosition = position;
    try {
      return rule(this);
    }
    catch {
      position = startPosition;
      return null;
    }
  }

  public string Consume(params string[] patterns) {
    foreach (var pattern in patterns) {
      UnityEngine.Debug.Log($"B4:/{pattern}/ {input.Substring(position, 40)}...");
      var match = Regex.Match(input.Substring(position), $"^{pattern}");
      if (match.Success) {
        position += match.Length;
        UnityEngine.Debug.Log($"__:/{pattern}/ val='{match.Value}' {input.Substring(position, 40)}...");
        return match.Value;
      }
    }
    throw new Exception($"Expected one of '{string.Join("', '", patterns)}' at position {position}, found '{input.Substring(position, Math.Min(10, input.Length - position))}...'");
  }

  public void Skip(string pattern) => Consume(pattern);

  public void SkipWhitespace() => Skip(@"\s*");

  public string ConsumeUntil(string pattern) {
    var match = Regex.Match(input.Substring(position), pattern);
    if (match.Success) {
      string result = input.Substring(position, match.Index);
      position += match.Index;
      if (match.Index == 0) position++; // Ensure we always advance
      return result;
    }
    throw new Exception($"Expected pattern '{pattern}' not found from position {position}");
  }

  public Parser Keyword(string keyword) {
    SkipWhitespace();
    Consume(keyword);
    return this;
  }

  public string Identifier() {
    SkipWhitespace();
    return Consume(@"[a-zA-Z_]\w*");
  }

  public Parser LeftBrace() => Keyword("{");
  public Parser RightBrace() => Keyword("}");
  public Parser LeftParen() => Keyword("\\(");
  public Parser RightParen() => Keyword("\\)");

  public CodeBlock Block() {
    LeftBrace();
    int start = position;
    ConsumeUntil("}");
    int end = position;
    RightBrace();
    return new CodeBlock(start, end);
  }
}

public class CodeBlock {
  public int Start { get; }
  public int End { get; }

  public CodeBlock(int start, int end) {
    Start = start;
    End = end;
  }
}

public class JavaScriptParser {
  private Parser parser;
  private string content;

  public JavaScriptParser(string input) {
      this.content = input;
      this.parser = new Parser(input);
  }

  public T Parse<T>(Func<Parser, T> rule) => parser.Parse(rule);

  public CodeBlock? FindMethodInClass(string methodName, string? className = null, string? baseClassName = null) => Parse(p => {
    try {
      p.TryParse(_ => {
        if (className != null) {
          p.Keyword("class").Keyword(className);
        }
        else {
          p.Keyword("class").Identifier();
        }

        if (baseClassName != null) {
          p.Keyword("extends").Keyword(baseClassName);
        }

        p.LeftBrace();
        return FindMethodInClassInternal(p, methodName);
      });
      return null as CodeBlock;
    } catch (Exception e) {
      Console.WriteLine(e.Message);
      return null as CodeBlock;
    }
  });


  private CodeBlock? FindMethodInClassInternal(Parser p, string methodName) {
    int braceCount = 1; // We're already inside one level of braces
    while (braceCount > 0 && p.position < p.input.Length) {
      p.SkipWhitespace();
      if (p.TryParse(_ => _.Keyword(methodName)) != null) {
        if (p.TryParse(_ => _.LeftParen().RightParen()) != null) {
            return p.Block();
        }
      }
      
      if (p.TryParse(_ => _.LeftBrace()) != null) {
        braceCount++;
      } else if (p.TryParse(_ => _.RightBrace()) != null) {
        braceCount--;
      } else {
        // Consume until next significant token
        p.ConsumeUntil("[{}]|\\S");
      }
    }
    return null; // Method not found
  }

  public static string? FindFunctionParamN(string content, string funcName, int nthParam) {
    return new JavaScriptParser(content).Parse(p => {
      int maxIterations = 1000000; // Adjust as needed
      while (maxIterations-- > 0) {
        if (p.TryParse(_ => p.Keyword(funcName).LeftParen()) != null) {
          for (int i = 1; i < nthParam; i++) {
            p.ConsumeUntil(",");
            p.Keyword(",");
          }
          return p.Identifier();
        }
        if (p.position >= p.input.Length) return null;
        p.ConsumeUntil($"{funcName}|$");
      }
      throw new Exception("Maximum iterations reached in FindFunctionParamN");
    });
  }

  public string? FindFileOfImported(string importedName) {
      return Parse(p => {
          while (true) {
              if (p.TryParse(_ => _.Keyword("import")) != null) {
                  string importStatement = p.ConsumeUntil("from").Trim();
                  p.Keyword("from");
                  p.SkipWhitespace();
                  
                  // Handle double quotes, single quotes, and template literals in one go
                  string fileName = p.Consume("\"[^\"]+\"", "'[^']+'", "`[^`]+`").Trim('"', '\'', '`');

                  if (Regex.IsMatch(importStatement, $@"\b{Regex.Escape(importedName)}\b")) {
                      return fileName;
                  }
              }
              if (p.position >= p.input.Length) return null;
              p.ConsumeUntil("import|$");
          }
      });
  }

  public string InsertCodeIntoMethod(CodeBlock method, string codeToInsert) {
    return content.Substring(0, method.End) + codeToInsert + content.Substring(method.End);
  }

  public string AppendSyncVarActorClass() {
    string syncVarActorClass = @"

class SyncVarActor extends Actor {
  get gamePawnType() { return '' }
  init(options) {
    super.init(options)
    this.subscribe('SyncVar', 'set1', this.syncVarChange)
  }
  syncVarChange(msg) {
    this.publish('SyncVar', 'set2', msg)
  }
}
SyncVarActor.register('SyncVarActor')
";
    return content + syncVarActorClass;
  }
}


public class ExampleProgram {
  public static void Main() {
    string indexJsPath = CqFile.AppIndexJs().longPath;
    string indexContent = File.ReadAllText(indexJsPath);

    string? modelClassName = JavaScriptParser.FindFunctionParamN(indexContent, "StartSession", 1);

    if (string.IsNullOrEmpty(modelClassName)) {
      Console.WriteLine("Could not find model class name in index.js");
      return;
    }

    var indexParser = new JavaScriptParser(indexContent);
    string? modelJsName = indexParser.FindFileOfImported(modelClassName);
    string modelJsPath = modelJsName != null
        ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(indexJsPath) ?? "", modelJsName))
        : indexJsPath;  // If not found in imports, assume it's in the index.js file

    string modelContent = File.ReadAllText(modelJsPath);
    var modelParser = new JavaScriptParser(modelContent);
    CodeBlock? initMethod = modelParser.FindMethodInClass("init", null, "GameModelRoot");

    if (initMethod != null) {
      string codeToInsert = "    this.syncer = SyncVarActor.create({});\n";
      string modifiedContent = modelParser.InsertCodeIntoMethod(initMethod, codeToInsert);
      UnityEngine.Debug.Log($"Modified content: {modifiedContent.Trim()}");
      File.WriteAllText(modelJsPath, modifiedContent);
    }
    else {
      Console.WriteLine($"Could not find init method in class {modelClassName}");
    }
  }
}


    /* 
      Given file "index.js" with:
        import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
        import { MyModelRoot } from "./Models";
        StartSession(MyModelRoot, GameViewRoot);

      and file: "./Models" with:
        export class MyModelRoot extends GameModelRoot {
          init() {
            super.init();
          }
        }

      (1) Finds the first parameter of the StartSession() function, which is the model class name (here "MyModelRoot")
      (2) Finds the file that imports the model class (here "./Models")
      (3) If not found in imports, assumes it's here in the "index.js" file
      (4) Reads the content of the model class file
      (5) Finds the init method in a class that extends GameModelRoot, and
      (6) inserts the initCode into the init() method
    */
