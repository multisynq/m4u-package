using UnityEngine;

namespace Multisynq {


[AddComponentMenu("Multisynq/MaterialComponent")]
public class Mq_Material_Comp : Mq_Comp
{
    public Color color;
    public override Mq_System croquetSystem { get; set; } = Mq_Material_System.Instance;
}

}