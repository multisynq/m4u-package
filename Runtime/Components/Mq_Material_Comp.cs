using UnityEngine;

namespace Multisynq {


[AddComponentMenu("Multisynq/Mq_Material_Comp")]
public class  Mq_Material_Comp : Mq_Comp {
  public bool mq_Material_Comp;  // Helps tools resolve "missing Script" problems
  public Color color;
  public override Mq_System croquetSystem { get; set; } = Mq_Material_System.Instance;
}

}