using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class CroquetBuildPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    
    public async void OnPreprocessBuild(BuildReport report)
    {
        // Add your pre-build actions here
        await ReCopyBuildItems();
        await Task.Delay(2000); // Delay for 2 seconds (adjust as needed)
        await BuildJSNow();
        
        // The rest of your existing pre-build logic goes here...
    }

    private async Task ReCopyBuildItems()
    {
        bool success = await CroquetBuilder.ReCopyBuildItems();
        if (success)
        {
            Debug.Log("Build items re-copied successfully.");
        }
        else
        {
            Debug.LogError("Failed to re-copy build items.");
        }
    }

    private async Task BuildJSNow()
    {
        bool success = await CroquetBuilder.EnsureJSToolsAvailable();
        if (success)
        {
            CroquetBuilder.StartBuild(false); // false => no watcher
        }
        else
        {
            Debug.LogError("Failed to build JS. Missing JS build tools.");
        }
    }
}
