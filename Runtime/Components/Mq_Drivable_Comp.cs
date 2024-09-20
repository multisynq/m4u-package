using UnityEngine;

namespace Multisynq {


[AddComponentMenu("Multisynq/Mq_Drivable_Comp")]
public class  Mq_Drivable_Comp : Mq_Comp {
  public bool mq_Drivable_Comp;  // Helps tools resolve "missing Script" problems
  
  public override Mq_System croquetSystem { get; set; } = Mq_Drivable_System.Instance;

  public bool isDrivenByThisView;
}

}