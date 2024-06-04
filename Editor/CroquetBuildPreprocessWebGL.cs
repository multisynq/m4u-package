// using System.Threading.Tasks;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEngine;

// class CroquetBuildPreprocessWebGL : IPreprocessBuildWithReport
// {
//     #if UNITY_WEBGL
//     public int callbackOrder { get { return 0; } }
    
//     public async void OnPreprocessBuild(BuildReport report)
//     {
//         // Execute the actions in ValidateBuildNow and BuildNow in sequence
//         await ValidateBuildNow();
//         await Task.Delay(1000); // Wait for the build to finish before starting the next build
//         await BuildNow();
        
//         // The rest of your existing pre-build logic goes here...
//     }

//     private async Task ValidateBuildNow()
//     {
//         Debug.Log("Validate Build Now");
//         // this item is not available if
//         //   we don't know how to build for the current scene, or
//         //   a watcher for any scene is running (MacOS only), or
//         //   a build has been requested and hasn't finished yet
//         if (!CroquetBuilder.KnowHowToBuildJS()) return;

//         #if !UNITY_EDITOR_WIN
//         if (CroquetBuilder.RunningWatcherApp() == CroquetBuilder.GetSceneBuildDetails().appName) return;
//         #endif
//         if (CroquetBuilder.oneTimeBuildProcess != null) return;
//     }

//     private async Task BuildNow()
//     {
//         Debug.Log("Building JS...");
//         bool success = await CroquetBuilder.EnsureJSToolsAvailable();
//         if (success)
//         {
//             CroquetBuilder.StartBuild(false); // false => no watcher
//         }
//         else
//         {
//             Debug.LogError("Failed to build JS. Missing JS build tools.");
//         }
//     }
//     #endif
// }
