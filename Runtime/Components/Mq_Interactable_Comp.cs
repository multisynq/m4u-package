namespace Multisynq {

public class Mq_Interactable_Comp : Mq_Comp
{
    public override Mq_System croquetSystem { get; set; } = Mq_Interactable_System.Instance;

    public bool isInteractable = true;

    public string[] interactableLayers = new string[]{};


}

}