using UnityEngine.UIElements;

public abstract class BuiltOutput_SI {
  
  protected MultisynqBuildAssistantEW parentWindow;
  protected StatusSet statusSet;
  protected VisualElement statusImage;
  protected Label messageLabel;
  protected Button[] buttons;

  public BuiltOutput_SI(MultisynqBuildAssistantEW parent) {
    parentWindow = parent;
  }

  public abstract bool Check();
  
  public static void ShowVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(true, ve);
  }

  public static void HideVEs(params VisualElement[] ves) {
    foreach (var ve in ves) SetVEViz(false, ve);
  }

  public static void SetVEViz(bool seen, params VisualElement[] ves) {
    foreach (var ve in ves) ve.style.visibility = seen ? Visibility.Visible : Visibility.Hidden;
  }
}