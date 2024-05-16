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
        CopyWebGLTemplatesFolder();
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

    static void CopyWebGLTemplatesFolder()
    {
        string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
        if (File.Exists(manifestPath))
        {
            string manifestJson = File.ReadAllText(manifestPath);
            var manifestDict = (Dictionary<string, object>)MiniJSON.Json.Deserialize(manifestJson);

            if (manifestDict.TryGetValue("dependencies", out object dependenciesObj))
            {
                var dependencies = (Dictionary<string, object>)dependenciesObj;

                if (dependencies.TryGetValue("io.croquet.multiplayer", out object packagePathObj))
                {
                    string packagePath = packagePathObj.ToString();

                    if (packagePath.StartsWith("file:"))
                    {
                        packagePath = packagePath.Substring(5);
                    }
                    else
                    {
                        string packageCachePath = Path.Combine(Application.dataPath, "..", "Library", "PackageCache");
                        string[] directories = Directory.GetDirectories(packageCachePath, "io.croquet.multiplayer@*");

                        if (directories.Length > 0)
                        {
                            packagePath = directories[0];
                        }
                        else
                        {
                            Debug.LogError("Could not find the package in the PackageCache.");
                            return;
                        }
                    }

                    string sourcePath = Path.Combine(packagePath, "Runtime", "WebGLTemplates");
                    string destinationPath = Path.Combine(Application.dataPath, "WebGLTemplates");

                    if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, destinationPath);
                        AssetDatabase.Refresh();
                        Debug.Log("WebGLTemplates folder copied to Assets/");
                    }
                    else
                    {
                        Debug.LogError("Could not find the WebGLTemplates folder in the package.");
                    }
                }
                else
                {
                    Debug.LogError("Could not find the io.croquet.multiplayer dependency in the manifest.");
                }
            }
        }
        else
        {
            Debug.LogError("Could not find the manifest.json file.");
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string destDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, destDir);
        }
    }
}
