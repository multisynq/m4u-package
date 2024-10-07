using UnityEditor;
using UnityEngine;

/// <summary>
/// Contains menu actions for creating Multisynq settings assets and adding Multisynq Bridge to the scene.
/// </summary>
public class Mq_ContextMenuActions
{
    /// <summary>
    /// Creates a new Multisynq settings asset.
    /// </summary>
    [MenuItem("Assets/Multisynq/New Mq_Settings", false, -1)]
    public static void CreateMyAsset()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            path = "Assets";
        }
        else
        {
            path += "/";
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "Mq_DefaultSettings.asset");
        Debug.Log("Loading Mq_Bridge prefab from path: Packages/com.multisynq.multiplayer/Prefabs/Mq_Bridge.prefab");

        Debug.Log($"Attempting to load Mq_DefaultSettings asset from path: /Packages/com.multisynq.multiplayer/Runtime/Settings/Mq_DefaultSettings.asset");

        //Find the Mq_DefaultSettings asset in the package
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        Mq_Settings settingsAsset = null;
        for (int i = 0; i < allAssetPaths.Length; ++i)
        {
            if (allAssetPaths[i].Contains("Mq_DefaultSettings.asset"))
                settingsAsset = AssetDatabase.LoadAssetAtPath<Mq_Settings>(allAssetPaths[i]);
        }

        if (settingsAsset == null)
        {
            Debug.LogError("Could not load Mq_DefaultSettings asset. Check the path is correct and the asset type matches Mq_Settings.");
        }
        else
        {
            Debug.Log("Mq_DefaultSettings asset loaded successfully.");
        }


        Mq_Settings instance = ScriptableObject.CreateInstance<Mq_Settings>();
        EditorUtility.CopySerialized(settingsAsset, instance);

        AssetDatabase.CreateAsset(instance, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = instance;
    }

    /// <summary>
    /// Validates the "New Multisynq Settings" menu item.
    /// </summary>
    /// <returns>True if the selected object is a folder in the Assets directory, false otherwise.</returns>
    // Validate the MenuItem
    [MenuItem("Assets/Multisynq/New Mq_Settings", true)]
    public static bool CreateMyAssetValidation()
    {
        // This returns true when the selected object is a folder in the Assets directory
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return AssetDatabase.IsValidFolder(path);
    }

    /// <summary>
    /// Adds the Multisynq Bridge prefab to the scene.
    /// </summary>
    [MenuItem("GameObject/Multisynq/Add Multisynq Bridge", false, -1)]
    static void AddMq_BridgeToScene()
    {
        // Load the Mq_Bridge prefab from the package
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        GameObject mqBridgePrefab = null;
        for (int i = 0; i < allAssetPaths.Length; ++i)
        {
            if (allAssetPaths[i].Contains("Mq_Bridge.prefab"))
                mqBridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(allAssetPaths[i]);
        }

        if (mqBridgePrefab == null)
        {
            Debug.LogError("Could not find Mq_Bridge prefab in the package.");
            return;
        }

        // Instantiate the prefab into the active scene
        GameObject mqBridgeInstance = PrefabUtility.InstantiatePrefab(mqBridgePrefab) as GameObject;

        if (mqBridgeInstance != null)
        {
            Selection.activeGameObject = mqBridgeInstance;
            SceneView.FrameLastActiveSceneView();
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(mqBridgeInstance, "Add Multisynq Bridge");
        }
        else
        {
            Debug.LogError("Failed to instantiate Mq_Bridge prefab.");
        }
    }

    // Updated method to find Mq_Bridge prefab dynamically
    [MenuItem("GameObject/Add Multisynq Bridge to Selected Object", false, 10)]
    private static void AddMq_BridgeToSelected(MenuCommand command)
    {
        Transform targetTransform = Selection.activeTransform;
        if (targetTransform == null)
        {
            Debug.LogError("No valid target selected.");
            return;
        }

        GameObject mqBridgePrefab = FindMq_BridgePrefab();
        if (mqBridgePrefab == null)
        {
            Debug.LogError("Could not find Mq_Bridge prefab in the project.");
            return;
        }

        GameObject mqBridgeInstance = PrefabUtility.InstantiatePrefab(mqBridgePrefab, targetTransform) as GameObject;
        if (mqBridgeInstance != null)
        {
            // If you want the prefab to be a sibling rather than a child, use the following line instead:
            // mqcroquetBridgeInstance.transform.SetParent(targetTransform.parent);

            mqBridgeInstance.transform.SetAsLastSibling(); // This places it as the last sibling in the hierarchy

            Selection.activeGameObject = mqBridgeInstance;
            Undo.RegisterCreatedObjectUndo(mqBridgeInstance, "Add Multisynq Bridge to Selected Object");
        }
        else
        {
            Debug.LogError("Failed to instantiate Mq_Bridge prefab.");
        }
    }

    [MenuItem("GameObject/Multisynq/Add Multisynq Bridge", true)]
    private static bool ValidateAddMq_BridgeToScene()
    {
        return FindMq_BridgePrefab() != null;
    }

    // Helper method to find the Mq_Bridge prefab dynamically
    private static GameObject FindMq_BridgePrefab()
    {
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        foreach (string assetPath in allAssetPaths)
        {
            if (assetPath.EndsWith("Mq_Bridge.prefab"))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
        }
        return null;
    }
}

