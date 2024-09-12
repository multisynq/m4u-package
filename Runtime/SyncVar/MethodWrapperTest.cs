// using UnityEngine;
// using System.Reflection;

// public class DemoMethodWrapper
// {
//     public string KeyPress(string keyId)
//     {
//         Debug.Log("Simple log " + keyId);
//         return "Pressed " + keyId;
//     }

//     public static void MultiLog_ToWrapBeforeKeypress(string msg)
//     {
//         Debug.Log("Multi log: " + msg + msg + msg + msg);
//     }
// }

// public class MethodWrapperTest : MonoBehaviour
// {
//     private DemoMethodWrapper demo;
//     private bool methodsWrapped = false;

//     void Start()
//     {
//         demo = new DemoMethodWrapper();
//         // Initialize the MethodWrapper with the assembly containing DemoMethodWrapper
//         MethodWrapper.Initialize(typeof(DemoMethodWrapper).Assembly);
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.X))
//         {
//             string result = demo.KeyPress("X");
//             Debug.Log("Result: " + result);
//         }
//         else if (Input.GetKeyDown(KeyCode.Y))
//         {
//             string result = demo.KeyPress("Y");
//             Debug.Log("Result: " + result);
//         }
//         else if (Input.GetKeyDown(KeyCode.Z))
//         {
//             string result = demo.KeyPress("Z");
//             Debug.Log("Result: " + result);
//         }
//         else if (Input.GetKeyDown(KeyCode.W) && !methodsWrapped)
//         {
//             WrapMethods();
//             methodsWrapped = true;
//             Debug.Log("Methods wrapped! Press X, Y, or Z to test.");
//         }
//     }

//     void WrapMethods()
//     {
//         // Get MethodInfo for the method to replace and the inject method
//         MethodInfo methodToReplace = typeof(DemoMethodWrapper).GetMethod("KeyPress");
//         MethodInfo injectMethod = typeof(DemoMethodWrapper).GetMethod("MultiLog_ToWrapBeforeKeypress", BindingFlags.Public | BindingFlags.Static);

//         // Wrap the KeyPress method
//         MethodWrapper.ReplaceMethod(methodToReplace, injectMethod);

//         // Apply the changes
//         MethodWrapper.ApplyChanges();
//     }

//     void OnDestroy()
//     {
//         // Ensure we clean up if the script is destroyed before ApplyChanges is called
//         if (methodsWrapped)
//         {
//             MethodWrapper.ApplyChanges();
//         }
//     }
// }
//             // MethodWrapper.PrependMethCallToMeth(methodToReplace, injectMethod);