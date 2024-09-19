using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;


namespace Multisynq {


[InitializeOnLoad]
public class Mq_DependencyAdder {
  static Mq_DependencyAdder() {
    EditorApplication.delayCall += AddDependencyAndCopyFolder;
  }

  static void AddDependencyAndCopyFolder() {
    EditorApplication.delayCall -= AddDependencyAndCopyFolder;

    AddDependency();
    // Mq_Builder.CopyWebGLTemplatesFolder();
    SetWebGLTemplate();
  }

  static void AddDependency() {
    string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

    if (File.Exists(manifestPath)) {
      string manifestJson = File.ReadAllText(manifestPath);
      var manifestDict = (Dictionary<string, object>)MiniJSON.Json.Deserialize(manifestJson);

      if (manifestDict.TryGetValue("dependencies", out object dependenciesObj)) {
        var dependencies = (Dictionary<string, object>)dependenciesObj;

        string dependencyKey = "net.gree.unity-webview";
        string dependencyValue = "https://github.com/gree/unity-webview.git?path=/dist/package-nofragment";

        if (!dependencies.ContainsKey(dependencyKey)) {
          dependencies[dependencyKey] = dependencyValue;
          string newManifestJson = MiniJSON.Json.Serialize(manifestDict);
          File.WriteAllText(manifestPath, newManifestJson);

          AssetDatabase.Refresh();

          Debug.Log(dependencyKey + " dependency added to manifest.json");
        }
      }
    }
    else {
      Debug.LogError("Could not find the manifest.json file.");
    }
  }
  static void SetWebGLTemplate() {
    string templateName = "MultisynqLoader"; // our WebGL template folder
    string templatePath = Path.Combine("WebGLTemplates", templateName);

    if (!Directory.Exists(Path.Combine(Application.dataPath, templatePath))) {
      Debug.LogError("WebGL template folder does not exist: " + Path.Combine(Application.dataPath, templatePath));
    }
    else {
      string template = "PROJECT:" + templateName;
      if (PlayerSettings.WebGL.template != template) {
        PlayerSettings.WebGL.template = template;
        Debug.Log("WebGL template set to: " + templateName);
      }
    }
  }
}

}