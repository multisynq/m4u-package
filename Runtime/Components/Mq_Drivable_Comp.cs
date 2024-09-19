using UnityEngine;

namespace Multisynq {


[AddComponentMenu("Multisynq/DrivableComponent")]
public class Mq_Drivable_Comp : Mq_Comp
{
    public override Mq_System croquetSystem { get; set; } = Mq_Drivable_System.Instance;

    public bool isDrivenByThisView;
}

}