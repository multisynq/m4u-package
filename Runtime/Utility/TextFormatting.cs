using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

static class TextFormatting {
    // Reference for Unity text colors: https://docs.unity3d.com/Manual/StyledText.html

    public static Dictionary<string, string> unityEditorColorMap = new Dictionary<string, string>() {
        {"white", "white"}, {"wht", "white"},
        {"blue", "#59F"},
        {"red", "red"},
        {"yellow", "yellow"}, {"ylw", "yellow"},
        {"green", "#7F5"}, {"grn", "#7F5"}, {"gn", "#7F5"},
        {"cyan", "cyan"}, {"cyn", "cyan"}, {"cn", "cyan"},
        {"magenta", "#ED88FD"}, {"mg", "#ED88FD"},
        {"grey", "#C4C4C4"}, {"gray", "#C4C4C4"}, {"gry", "#C4C4C4"}, {"gy", "#C4C4C4"},
        {"black", "black"}, {"blk", "black"},
    };

    public static Dictionary<string, string> consoleColorEscapeCodes = new Dictionary<string, string>() {
        {"red", "\x1b[1m\x1b[31m"},
        {"green", "\x1b[1m\x1b[32m"}, {"grn", "\x1b[1m\x1b[32m"}, {"gn", "\x1b[1m\x1b[32m"},
        {"yellow", "\x1b[1m\x1b[33m"}, {"ylw", "\x1b[1m\x1b[33m"},
        {"blue", "\x1b[1m\x1b[34m"},
        {"magenta", "\x1b[1m\x1b[35m"}, {"mg", "\x1b[1m\x1b[35m"},
        {"cyan", "\x1b[1m\x1b[36m"}, {"cyn", "\x1b[1m\x1b[36m"}, {"cn", "\x1b[1m\x1b[36m"},
        {"white", "\x1b[1m\x1b[37m"}, {"wht", "\x1b[1m\x1b[37m"},
        {"grey", "\x1b[1m\x1b[30m"}, {"gray", "\x1b[1m\x1b[30m"}, {"gry", "\x1b[1m\x1b[30m"}, {"gy", "\x1b[1m\x1b[30m"},
        {"black", "\x1b[1m\x1b[30m"}, {"blk", "\x1b[1m\x1b[30m"},
        
        {"reset", "\x1b[0m"},
        {"bold", "\x1b[1m"},
        {"dim", "\x1b[2m"},
        {"underline", "\x1b[4m"},
        {"blink", "\x1b[5m"},
        {"inverse", "\x1b[7m"},
        {"hidden", "\x1b[8m"},
        
        {"dark_blue", "\x1b[34m"},
        {"dark_red", "\x1b[31m"},
        {"dark_green", "\x1b[32m"}, {"dark_grn", "\x1b[32m"},
        {"dark_yellow", "\x1b[33m"}, {"dark_ylw", "\x1b[33m"},
        {"dark_magenta", "\x1b[35m"},
        {"dark_cyan", "\x1b[36m"}, {"dark_cyn", "\x1b[36m"},
        {"dark_black", "\x1b[30m"}, {"dark_blk", "\x1b[30m"},
        {"dark_white", "\x1b[37m"}, {"dark_wht", "\x1b[37m"},
        {"dark_grey", "\x1b[30m"}, {"dark_gry", "\x1b[30m"},
        
        {"bg_bold", "\x1b[1m"},
        {"bg_dim", "\x1b[2m"},
        {"bg_underline", "\x1b[4m"},
        {"bg_blink", "\x1b[5m"},
        {"bg_inverse", "\x1b[7m"},
        {"bg_hidden", "\x1b[8m"},
        
        {"bg_blue", "\x1b[44m"},
        {"bg_red", "\x1b[41m"},
        {"bg_green", "\x1b[42m"}, {"bg_grn", "\x1b[42m"},
        {"bg_yellow", "\x1b[43m"}, {"bg_ylw", "\x1b[43m"},
        {"bg_magenta", "\x1b[45m"},
        {"bg_cyan", "\x1b[46m"}, {"bg_cyn", "\x1b[46m"},
        {"bg_white", "\x1b[47m"}, {"bg_wht", "\x1b[47m"},
        {"bg_grey", "\x1b[40m"}, {"bg_gry", "\x1b[40m"},
        {"bg_black", "\x1b[40m"}, {"bg_blk", "\x1b[40m"},
        
        {"bg_dark_blue", "\x1b[44m"},
        {"bg_dark_red", "\x1b[41m"},
        {"bg_dark_green", "\x1b[42m"}, {"bg_dark_grn", "\x1b[42m"},
        {"bg_dark_yellow", "\x1b[43m"}, {"bg_dark_ylw", "\x1b[43m"},
        {"bg_dark_magenta", "\x1b[45m"},
        {"bg_dark_cyan", "\x1b[46m"}, {"bg_dark_cyn", "\x1b[46m"},
        {"bg_dark_white", "\x1b[47m"}, {"bg_dark_wht", "\x1b[47m"},
        {"bg_dark_grey", "\x1b[40m"}, {"bg_dark_gry", "\x1b[40m"},
        {"bg_dark_black", "\x1b[40m"}, {"bg_dark_blk", "\x1b[40m"},
        
        {"bg_reset", "\x1b[0m"},
    };

    public static string GetColorCode(string colorPrefix, Dictionary<string, string> colorMap) {
        bool isUnityEditor = (colorMap == unityEditorColorMap);
        string defaultColor = isUnityEditor ? "white" : consoleColorEscapeCodes["white"];

        foreach (KeyValuePair<string, string> colorEntry in colorMap) {
            if (colorEntry.Key.StartsWith(colorPrefix)) {
                defaultColor = colorEntry.Value;
                break;
            }
        }
        return isUnityEditor ? $"<color={defaultColor}>" : defaultColor;
    }

    // Apply color formatting to strings with patterns like:
    // $"%cy%FunctionName()%wh%.%yel%paramName=%blu%{paramValue}%wh%.%yel%anotherParam=%blu%{anotherValue}"
    static public string TagColors(this string inputText) {
        inputText = " " + inputText;
        var colorMap = Application.isEditor ? TextFormatting.unityEditorColorMap : TextFormatting.consoleColorEscapeCodes;
        var colorEndTag = Application.isEditor ? "</color>" : TextFormatting.consoleColorEscapeCodes["reset"];
        string formattedText = inputText;

        string[] segments = inputText.Split('%').ToArray();
        int colorSegmentStart = inputText.StartsWith("%") ? 0 : 1;
        for (int i = 0; i < segments.Length; i++) {
            string segment = segments[i];
            if (i % 2 == colorSegmentStart) {
                string colorCode = GetColorCode(segment.Trim(), colorMap);
                segments[i] = colorCode;
            } else {
                segments[i] = segment + ((i > 0) ? colorEndTag : "");
            }
        }
        var result = string.Join("", segments);
        if (!result.EndsWith(colorEndTag)) result += colorEndTag;
        return result;
    }

    static public string[] SplitAndTrimToArray(this string input, string delimiter = "\n") {
        return input.Split(delimiter).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
    }

    static public string SplitAndTrimToString(this string input, string delimiter = "\n") {
        return String.Join(delimiter, input.SplitAndTrimToArray(delimiter));
    }

    static public string ConvertToJsonArray(this string[] stringArray) {
        return $"[{String.Join(",", stringArray.Select(s => $"\"{s}\"").ToArray())}]";
    }

    static public string CombinePathsWithSingleSlash(this string path1, string path2) {
        return $"{path1.TrimEnd('/')}/{path2.TrimStart('/')}";
    }
}