using UnityEngine;

public class HandyColors {
  public Color green;
  public Color red;
  public Color yellow;
  public Color blue;
  public Color lime;
  public Color white;
  public Color grey;
  public Color c_node;

  public HandyColors() {
    green  = GetColor("#BFFFC5");
    red    = GetColor("#FF9090");
    yellow = GetColor("#FFFFBF");
    blue   = GetColor("#006AFF");
    lime   = GetColor("#00FF00");
    white  = GetColor("#FFFFFF");
    grey   = GetColor("#888888");
    c_node = GetColor("#417E37");
  }

  public static Color GetColor(string hex) {
    Color color;
    ColorUtility.TryParseHtmlString(hex, out color);
    return color;
  }
};
