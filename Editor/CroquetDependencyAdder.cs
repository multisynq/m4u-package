using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class CroquetDependencyAdder
{
    static CroquetDependencyAdder()
    {
        EditorApplication.delayCall += AddDependencyAndCopyFolder;
    }

    static void AddDependencyAndCopyFolder()
    {
        EditorApplication.delayCall -= AddDependencyAndCopyFolder;

        AddDependency();
        // CroquetBuilder.CopyWebGLTemplatesFolder();
        SetWebGLTemplate();
    }

    static void AddDependency()
    {
        string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

        if (File.Exists(manifestPath))
        {
            string manifestJson = File.ReadAllText(manifestPath);
            var manifestDict = (Dictionary<string, object>)MiniJSON.Json.Deserialize(manifestJson);

            if (manifestDict.TryGetValue("dependencies", out object dependenciesObj))
            {
                var dependencies = (Dictionary<string, object>)dependenciesObj;

                string dependencyKey = "net.gree.unity-webview";
                string dependencyValue = "https://github.com/gree/unity-webview.git?path=/dist/package-nofragment";

                if (!dependencies.ContainsKey(dependencyKey))
                {
                    dependencies[dependencyKey] = dependencyValue;
                    string newManifestJson = MiniJSON.Json.Serialize(manifestDict);
                    File.WriteAllText(manifestPath, newManifestJson);

                    AssetDatabase.Refresh();

                    Debug.Log(dependencyKey + " dependency added to manifest.json");
                }
            }
        }
        else
        {
            Debug.LogError("Could not find the manifest.json file.");
        }
    }
    static void SetWebGLTemplate()
    {
        string templateName = "CroquetLoader"; // Replace with the name of your WebGL template folder
        string templatePath = Path.Combine("WebGLTemplates", templateName);

        Debug.Log("Setting WebGL template to: " + templateName + " at path: " + templatePath);

        if (Directory.Exists(Path.Combine(Application.dataPath, templatePath)))
        {
            PlayerSettings.WebGL.template = "PROJECT:"+templateName;//templatePath;
            Debug.Log("WebGL template set to: " + templateName);
        }
        else
        {
            Debug.LogError("WebGL template folder does not exist: " + Path.Combine(Application.dataPath, templatePath));
        }
    }
}
