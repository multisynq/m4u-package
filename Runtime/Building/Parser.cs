using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

public struct Token
{
    public int DelimIdx;
    public int[] DelimDepths;
    public string Txt;
}

[Serializable]
public class DelimPair
{
    public string Start;
    public string End;
}

public static class Parser
{
    public static string[] DeriveDelims(DelimPair[] delimPairs)
    {
        return delimPairs.SelectMany(pair => new[] { pair.Start, pair.End }).Distinct().ToArray();
    }

    public static Token[] ParseTokensWithPairs(
        string input,
        DelimPair[] delimPairs,
        Func<Token, int[], Token> onNonDelimToken)
    {
        string[] delims = DeriveDelims(delimPairs);
        var tokens = ParseTokens(input, delims);
        var delimDepths = new int[delimPairs.Length];
        var result = new List<Token>();
        var stack = new Stack<(int PairIndex, int TokenIndex)>();

        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.DelimIdx != -1)
            {
                int pairIndex = Array.FindIndex(delimPairs, pair => 
                    pair.Start == delims[token.DelimIdx] || pair.End == delims[token.DelimIdx]);
                
                if (pairIndex != -1)
                {
                    bool isStart = delimPairs[pairIndex].Start == delims[token.DelimIdx];
                    bool isEqualPair = delimPairs[pairIndex].Start == delimPairs[pairIndex].End;

                    if (isEqualPair)
                    {
                        // For equal pairs, toggle depth based on parity
                        if (delimDepths[pairIndex] % 2 == 0)
                        {
                            delimDepths[pairIndex]++;
                            stack.Push((pairIndex, i));
                        }
                        else
                        {
                            delimDepths[pairIndex]--;
                            if (stack.Count > 0 && stack.Peek().PairIndex == pairIndex)
                            {
                                stack.Pop();
                            }
                        }
                    }
                    else if (isStart)
                    {
                        delimDepths[pairIndex]++;
                        stack.Push((pairIndex, i));
                    }
                    else if (stack.Count > 0 && stack.Peek().PairIndex == pairIndex)
                    {
                        delimDepths[pairIndex]--;
                        stack.Pop();
                    }
                }
            }

            result.Add(token.DelimIdx == -1
                ? onNonDelimToken(new Token { DelimIdx = -1, DelimDepths = delimDepths.ToArray(), Txt = token.Txt }, delimDepths)
                : new Token { DelimIdx = token.DelimIdx, DelimDepths = delimDepths.ToArray(), Txt = token.Txt });
        }

        // Ensure the last token has DelimDepths of [0,0,0]
        if (result.Count > 0)
        {
            var lastToken = result[result.Count - 1];
            lastToken.DelimDepths = new int[delimPairs.Length];
            result[result.Count - 1] = lastToken;
        }

        return result.ToArray();
    }

    private static Token[] ParseTokens(string input, string[] delims)
    {
        var pattern = string.Join("|", delims.Select(Regex.Escape));
        UnityEngine.Debug.Log($"pattern: {pattern}");
        return Regex.Split(input, $"({pattern})")
            .Where(part => !string.IsNullOrEmpty(part))
            .Select((part, index) => new Token { DelimIdx = Array.IndexOf(delims, part), Txt = part })
            .ToArray();
    }
}