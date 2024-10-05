using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Multisynq;



[InitializeOnLoad]
public static class SceneAndPlayWatcher {
  static PlayModeStateChange lastState = PlayModeStateChange.EnteredEditMode;

  // register event handlers when the class is initialized
  static SceneAndPlayWatcher() {
    // because this is rebuilt on Play, it turns out that we miss the ExitingEditMode event.
    // but we can detect whether the init is happening because of an imminent state change
    // https://gamedev.stackexchange.com/questions/157266/unity-why-does-playmodestatechanged-get-called-after-start
    EditorApplication.playModeStateChanged += HandlePlayModeState;
    // if (EditorApplication.isPlayingOrWillChangePlaymode)
    // {
    //     Mq_Builder.EnteringPlayMode();
    // }

    EditorSceneManager.activeSceneChangedInEditMode += HandleSceneChange;

    EditorApplication.quitting += EditorQuitting;
  }

  private static void HandlePlayModeState(PlayModeStateChange state) {
    lastState = state;
    // PlayModeStateChange.ExitingEditMode (i.e., before entering Play) - if needed - is handled above in the constructor
    if (state == PlayModeStateChange.EnteredEditMode) Mq_Builder.EnteredEditMode();
    // about to play
    // if (state == PlayModeStateChange.ExitingEditMode) { // About to enter Play
      // check if the platform is webgl
      // bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
      // if (isWebGL) { // UnPlay and show a dialog
      //     EditorApplication.isPlaying = false;
      //     string msg = "To Play in WebGL, you must build the app first.\n\nUse 'Multisynq => Build JS Now' to build the app.";
      //     EditorUtility.DisplayDialog("Multisynq", msg, "OK");
      //     EditorApplication.ExecuteMenuItem("File/Build Settings..."); // open the build settings window
      // }
    // }
  }

  private static void HandleSceneChange(Scene current, Scene next) {
    // for some reason, this can on occasion be triggered with a "next" scene that has no name, no path, and zero rootCount.
    // we feel justified in ignoring such changes.
    if (next.name != "") Mq_Builder.CacheSceneComponents(next);
  }

  private static void EditorQuitting() {
#if UNITY_EDITOR_OSX
    Mq_Builder.StopWatcher(); // if any
#endif
  }
}


public class MultisynqMenu {
  private const string BuildNowItem = "Multisynq/Build JS Now";
  private const string HarvestDefinitionsItem = "Multisynq/Harvest Scene Definitions Now";
  private const string BuildOnPlayItem = "Multisynq/Build JS on Play";

  private const string StarterItem = "Multisynq/Start JS Watcher";
  private const string StopperItemHere = "Multisynq/Stop JS Watcher (this app)";
  private const string StopperItemOther = "Multisynq/Stop JS Watcher (other app)";

  private const string InstallJSToolsItem = "Multisynq/Install JS Build Tools";

  private const string OpenDiscordItem = "Multisynq/Join Multisynq Discord...";
  private const string OpenPackageItem = "Multisynq/Open package on Github...";

  [MenuItem(BuildNowItem, false, 100)]
  public static async void BuildNow() {
    Debug.Log("<color=yellow>----------------   Building JS...  ----------------</color>");
    bool success = await Mq_Builder.EnsureJSToolsAvailable();
    if (!success) return;

    Mq_Builder.StartBuild(false); // false => no watcher
  }

  [MenuItem(BuildNowItem, true)]
  private static bool ValidateBuildNow() {
    // Debug.Log("Validate Build Now");
    // this item is not available if
    //   we don't know how to build for the current scene, or
    //   a watcher for any scene is running (MacOS only), or
    //   a build has been requested and hasn't finished yet
    if (!Mq_Builder.KnowHowToBuildJS()) return false;

#if UNITY_EDITOR_OSX
    if (Mq_Builder.RunningWatcherApp() == Mq_Builder.GetSceneBuildDetails().appName) return false;
#endif
    if (Mq_Builder.oneTimeBuildProcess != null) return false;
    return true;
  }

  [MenuItem(BuildOnPlayItem, false, 100)]
  private static void BuildOnPlayToggle() {
    Mq_Builder.BuildOnPlayEnabled = !Mq_Builder.BuildOnPlayEnabled;
  }

  [MenuItem(BuildOnPlayItem, true)]
  private static bool ValidateBuildOnPlayToggle() {
    if (!Mq_Builder.KnowHowToBuildJS()) return false;
#if UNITY_EDITOR_OSX
    if (Mq_Builder.RunningWatcherApp() == Mq_Builder.GetSceneBuildDetails().appName) return false;
#endif

    Menu.SetChecked(BuildOnPlayItem, Mq_Builder.BuildOnPlayEnabled);
    return true;
  }

#if UNITY_EDITOR_OSX
  [MenuItem(StarterItem, false, 100)]
  public static async void StartWatcher() {
    bool success = await Mq_Builder.EnsureJSToolsAvailable();
    if (!success) return;

    Mq_Builder.StartBuild(true); // true => start watcher
  }

  [MenuItem(StarterItem, true)]
  private static bool ValidateStartWatcher() {
    if (!Mq_Builder.KnowHowToBuildJS()) return false;

    // Debug.Log($"Mq_Builder has process: {Mq_Builder.builderProcess != null}");
    return Mq_Builder.RunningWatcherApp() == "";
  }

  [MenuItem(StopperItemHere, false, 100)]
  private static void StopWatcherHere() {
    Mq_Builder.StopWatcher();
  }

  [MenuItem(StopperItemHere, true)]
  private static bool ValidateStopWatcherHere() {
    if (!Mq_Builder.KnowHowToBuildJS()) return false;

    return Mq_Builder.RunningWatcherApp() == Mq_Builder.GetSceneBuildDetails().appName;
  }

  [MenuItem(StopperItemOther, false, 100)]
  private static void StopWatcherOther() {
    Mq_Builder.StopWatcher();
  }

  [MenuItem(StopperItemOther, true)]
  private static bool ValidateStopWatcherOther() {
    if (!Mq_Builder.KnowHowToBuildJS()) return false;

    string appName = Mq_Builder.RunningWatcherApp();
    return appName != "" && appName != Mq_Builder.GetSceneBuildDetails().appName;
  }
#endif

  [MenuItem(HarvestDefinitionsItem, false, 100)]
  private static void HarvestNow() {
    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

    // before entering play mode, go through all scenes that will be included in a build and
    // make a list of the scenes and the app associated with each scene.
    // store the list in an EditorPref using the format
    //   scene1:appName1,scene2:appName2...
    List<string> scenesAndApps = new List<string>();
    Scene activeScene = EditorSceneManager.GetActiveScene();
    string previousScenePath = activeScene.path;
    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
      if (scene.enabled) {
        EditorSceneManager.OpenScene(scene.path);
        Mq_Bridge[] allObjects = Resources.FindObjectsOfTypeAll<Mq_Bridge>();
        foreach (Mq_Bridge obj in allObjects) {
          // the collection will contain components from the scene and from any known prefab.
          // filter out the latter.
          if (string.IsNullOrEmpty(obj.gameObject.scene.name)) continue; // prefab
          if (obj.launchViaMenuIntoScene != "" || string.IsNullOrEmpty(obj.appName)) continue; // not relevant

          string sceneName = Path.GetFileNameWithoutExtension(scene.path);
          scenesAndApps.Add($"{sceneName}:{obj.appName}");
        }
      }
    }
    // return to the scene where we started
    EditorSceneManager.OpenScene(previousScenePath);

    if (scenesAndApps.Count == 0) {
      Debug.LogError("Found no scenes to harvest from.  Are all desired scenes included in Build Settings, and does each have a Multisynq object that specifies its App Name?");
      Mq_Builder.HarvestSceneList = "";
    }
    else {
      string harvestString = string.Join(',', scenesAndApps.ToArray());
      Mq_Builder.HarvestSceneList = harvestString;
      EditorApplication.EnterPlaymode();
    }
  }

  [MenuItem(InstallJSToolsItem, false, 200)]
  public static async void InstallJSTools() {
    bool success = await Mq_Builder.InstallJSTools();
    if (success) {
      Debug.Log("JS Build Tools successfully installed");
    }
    else {
      Debug.LogError("Could not install JS Build Tools");
    }
  }

  [MenuItem(InstallJSToolsItem, true)]
  private static bool ValidateInstallJSTools() {
#if UNITY_EDITOR_OSX
    if (Mq_Builder.RunningWatcherApp() != "") return false;
#endif
    return true;
  }

  [MenuItem(OpenDiscordItem, false, 300)]
  private static void OpenDiscord() {
    Application.OpenURL("https://multisynq.io/discord");
  }

  [MenuItem(OpenPackageItem, false, 300)]
  private static void OpenPackage() {
    Application.OpenURL("https://github.com/multisynq/m4u-package");
  }
}


class Mq_BuildPreprocess : IPreprocessBuildWithReport {
  public int callbackOrder { get { return 0; } }
  public void OnPreprocessBuild(BuildReport report) {
    BuildTarget target = report.summary.platform;
    bool isWindowsBuild = target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;
    string jsTarget = isWindowsBuild ? "node" : (target == BuildTarget.WebGL ? "webgl" : "webview");

    Debug.LogWarning($"Building for target {jsTarget}");

    Scene activeScene = EditorSceneManager.GetActiveScene();
    if (!Mq_Builder.PrepareSceneForBuildTarget(activeScene, isWindowsBuild)) {
      // reason for refusal will already have been logged
      throw new BuildFailedException("You must fix some settings (see warnings above) before building");
    }

    bool readyToBuild = true;
    string failureMessage = "Missing JS build tools";
    string state = Mq_Builder.StateOfJSBuildTools(); // ok, needsRefresh, needsInstall, unavailable
    if (state == "unavailable") readyToBuild = false; // explanatory error will already have been logged
    else if (state == "needsInstall") {
      Debug.LogError("No JS build tools found.  Use menu 'Multisynq > Open Build Assistant'");
      readyToBuild = false;
    }

    if (readyToBuild) { // ok so far
      // find all the appNames that are going into the build
      HashSet<string> appNames = new HashSet<string>();
      string previousScenePath = activeScene.path;
      foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
        if (scene.enabled) {
          EditorSceneManager.OpenScene(scene.path);
          Mq_Bridge[] allObjects = Resources.FindObjectsOfTypeAll<Mq_Bridge>();
          foreach (Mq_Bridge obj in allObjects) {
            // the collection will contain components from the scene and from all known prefabs.
            // filter out the latter.
            if (string.IsNullOrEmpty(obj.gameObject.scene.name)) continue;

            if (obj.gameObject.activeSelf && !String.IsNullOrEmpty(obj.appName)) appNames.Add(obj.appName);
          }
        }
      }
      // put it back to the scene where we started
      EditorSceneManager.OpenScene(previousScenePath);

      // for each appName, check its build directory to ensure that we
      // have an up-to-date build for the current installed level of the JS build tools.
      foreach (string appName in appNames) {
        if (!Mq_Builder.CheckJSBuildState(appName, jsTarget)) {
          Debug.LogError($"Check Menu: Multisynq > Open Build Assistant > [Check if Ready] Failed to find up-to-date build for \"{appName}\", target \"{jsTarget}\"");
          failureMessage = "Missing up-to-date JS build(s)";
          readyToBuild = false;
        }
      }
    }

    if (!readyToBuild) throw new BuildFailedException(failureMessage);

    // everything seems fine.  copy the tools record into the StreamableAssets folder
    CopyJSToolsRecord();
    //
    // and on Windows, copy our pre-supplied node.exe too,
    // as well as the node_datachannel.node library
    if (isWindowsBuild) {
      CopyNodeExe();
      CopyNodeDataChannelLib();
    } else {
      // on other platforms, delete the the library to save space
      FileUtil.DeleteFileOrDirectory(Mq_Builder.NodeDataChannelLibInBuild);
    }
  }

  private void CopyJSToolsRecord() {
    string src = Mq_Builder.JSToolsRecordInEditor;
    string dest = Mq_Builder.JSToolsRecordInBuild;
    string destDir = Path.GetDirectoryName(dest);
    Directory.CreateDirectory(destDir);
    FileUtil.ReplaceFile(src, dest);
  }

  private void CopyNodeExe() {
    string src = Mq_Builder.NodeExeInPackage;
    string dest = Mq_Builder.NodeExeInBuild;
    string destDir = Path.GetDirectoryName(dest);
    Directory.CreateDirectory(destDir);
    FileUtil.ReplaceFile(src, dest);
  }
  public static void CopyNodeDataChannelLib() {
    // copy node_datachannel.node from node_modules to StreamingAssets
    // so that the relative link "../build/Release/node_datachannel.node"
    // works (relative to the bundled StreamingAssets/<app>/node-main.js)
    string src = Mq_Builder.NodeDataChannelLibInNodeModules;
    string dest = Mq_Builder.NodeDataChannelLibInBuild;
    string destDir = Path.GetDirectoryName(dest);
    Directory.CreateDirectory(destDir);
    FileUtil.ReplaceFile(src, dest);
  }
}

class Mq_BuildPostprocess : IPostprocessBuildWithReport {
  public int callbackOrder { get { return 0; } }
  public void OnPostprocessBuild(BuildReport report) {
    // if we temporarily copied node.exe (see above), remove it again
    BuildTarget target = report.summary.platform;
    if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
      string dest = Mq_Builder.NodeExeInBuild;
      FileUtil.DeleteFileOrDirectory(dest);
      FileUtil.DeleteFileOrDirectory(dest + ".meta");
    } else {
      // if we temporarily deleted node_datachannel.node (see above), restore it
      Mq_BuildPreprocess.CopyNodeDataChannelLib();
    }
  }
}

