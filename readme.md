# Croquet for Unity
Croquet for Unity is a Multiplayer Package that allows you to build flawlessly synchronized, bit-identical simulations with JavaScript. Deploy effortlessly everywhere without the hassle of server management, complex netcode, or rollback. Author how something behaves **once**, and it will behave that way for everyone playing your game.


## Unity Package Repo
This repo contains all Croquet for Unity functionality to be added from the Unity Package Manager.
This repo is the starting point to create your own project.

For more examples please see our tutorials or other demo repos:
- [Tutorials](https://github.com/croquet/croquet-for-unity-tutorials)
- [Demolition](https://github.com/croquet/croquet-for-unity-demolition)
- [Guardians](https://github.com/croquet/croquet-for-unity-guardians)


## Questions
Ask questions on our [discord](https://croquet.io/discord)!


## Setup
*Let's Get Started!*
Overall, you will need to create a Unity project and repo, set up all the dependencies, and create a basic JavaScript model to drive your game. The concepts are covered in more detail in Tutorial 1 of our tutorials repo.

For a visual representation of this information please see our [getting started guide](https://docs.google.com/document/d/1XXBRe3H6pRdbKw7pfVStnIfaOzQd3d1A7DseA7kEobI).

### Unity Project
Croquet for Unity has been built with and tested on projects using Unity editor version `2021.3.19f1`. The easiest way to get started is to use the same version - but feel free to try a new version and tell us how it goes!

All Unity versions are available for download [here](https://unity.com/releases/editor/archive).

Create a new Unity Project via the Unity Hub Application.

Select a path to save your Unity project.

### git setup
Be sure to have a system level installation of git that is in your path variable. Unity will use this to resolve git repo based packages. Installation Instructions for git can be found at: https://git-scm.com/book/en/v2/Getting-Started-Installing-Git

Here are suggested .gitignore and .gitattributes files that we use in our Guardians demonstration project:
- [Guardians root gitignore](https://github.com/croquet/croquet-for-unity-guardians/blob/release/.gitignore)
- [Guardians Unity gitignore](https://github.com/croquet/croquet-for-unity-guardians/blob/release/unity/.gitignore)
- [Guardians Root gitattributes](https://github.com/croquet/croquet-for-unity-guardians/blob/release/.gitattributes)


### Included Dependencies

The Croquet for Unity package now includes essential networking dependencies, ensuring seamless integration and setup.

#### WebSocket

The package comes pre-integrated with the `WebSocketSharp-netstandard` library to facilitate the C# to JavaScript bridge. This removes the need for manual downloads or setup. The `websocket-sharp.dll`, compatible with `netstandard2.0`, is automatically placed in the correct directory within your Unity project, typically under `Assets/Plugins`, ensuring immediate functionality.

#### WebView

For platforms other than Windows, including any deployed applications, the package automatically incorporates a WebView component. This is essential for running the Croquet JavaScript code across various environments. The `unity-webview` package by GREE, Inc. is included out-of-the-box, negating the need for manual addition through the Unity Package Manager. This inclusion ensures that Croquet for Unity operates  across all supported platforms without additional configuration steps.


#### Croquet for Unity

Now that all dependencies are in place, add the `Croquet Multiplayer` package using this git URL:
```
https://github.com/croquet/croquet-for-unity-package.git
```

#### Install the Tools
As part of the installation of the C4U package, the Unity editor will have been given a `Croquet` menu.
On this menu, now invoke the option `Install JS Build Tools`.
That option will create a "MultisynqJS" folder that has the following application structure.

```
- (unity project root)
    - /Assets
        - /MultisynqJS
            .gitignore
            .eslintrc
            package.json
            ...
            - /_Runtime
                - /Packages
                    - /game-models
                    - /unity-bridge
                - /Platforms
                    - /Node
                    - /WebGL
                    - /WebView
            - /(your_app_name_1)
                - index.js
            - /(your_app_name_2)
                - index.js
    - Packages
    - etc
```
The `MultisynqJS` folder itself has various tools for building the JavaScript part of you app, while the `_Runtime` folder has mostly source files needed for building.

The `your_app_name` subdirectories can be used for independent apps - for example, in our `croquet-for-unity-tutorials` repository, there are independent directories for nine introductory apps.


### Create a Default Addressable Assets Group
C4U expects to find a default addressable-assets group, which is how we associate particular assets across the bridge for spawning. Unity's Addressables are great system to use for asset naming and management.

Clicking `Window => Asset Management => Group => "Create Asset Group"`
will create the group; an `AddressableAssetsData` folder will appear in your project.

Add tags that correspond with the scene names you will use each prefab in (Croquet will only load what is needed for each scene), _or_ add the "default" tag if the asset should be loaded for every scene.


### Create and fill in a Mq_Settings asset
Find the `Mq_DefaultSettings` asset within the C4U package, by going to `Packages/Croquet Multiplayer/Scripts/Runtime/Settings`. Copy the settings into your project - for example, into an `Assets/Settings` directory.

The most important field to set up in the settings asset is the **Api Key**, which is a token of around 40 characters that you can create for yourself at https://croquet.io/account. It provides access to the Croquet infrastructure.

The **App Prefix** is the way of identifying with your organization the Croquet apps that you develop and run.  The combination of this prefix and the App Name provided on the Croquet Bridge component in each scene (see below) is a full App ID - for example, `io.croquet.worldcore.guardians`.  When you are running our demonstration projects (`tutorials`, `guardians` etc), it is fine to leave this prefix as is, but when you develop your own apps you must change the prefix so that the App ID is a globally unique identifier. The ID must follow the Android reverse domain naming convention - i.e., each dot-separated segment must start with a letter, and only letters, digits, and underscores are allowed.

**For MacOS only:** Find the Path to your Node executable, by going to a terminal and running
```
which node
```
On the Settings asset, fill in the **Path to Node** field with the path.

### Create a Unity Scene

Create a new scene _(note: a scene's name is used in our package to tie the scene to its assets and other build aspects; these features have not yet been tested with names containing white space, punctuation etc)_

From the `Croquet Multiplayer` package's `Prefabs` folder drag a `Mq_Bridge` object to your scene. Configure the bridge object as follows:

Associate the **App Properties** field with the `Mq_Settings` object that you created in the last step.

Set the **App Name** to the `your_app_name` part of the path, illustrated above, to the directory holding the JavaScript source that belongs with this scene. For example, a name `myGame` would connect this scene to the code inside `Assets/MultisynqJS/myGame`.

### Create Your App's JavaScript Code and Unity-side Entities

#### Create a Top-Level JavaScript File

In the app's directory create a file called `index.js`, that will be responsible for importing both the model- and view-side code that your app requires.  Here is an example:

```javascript
import { StartSession, GameViewRoot } from "@croquet/unity-bridge";
import { MyModelRoot } from "./Models";

StartSession(MyModelRoot, GameViewRoot);
```
#### Provide the JavaScript Model Code
Create the file that implements the JavaScript model behavior for your app. The `index.js` above expects a file called `Models.js`, that exports a `MyModelRoot` class.

To get started, you can copy any Models file from under the `MultisynqJS` folder of one of our demonstration repositories.  Here is a sample, copied from Tutorial 1 of our Tutorials repository:

```javascript
import { Actor, mix, AM_Spatial } from "@croquet/worldcore-kernel";
import { GameModelRoot } from "@croquet/game-models";

class TestActor extends mix(Actor).with(AM_Spatial) {
    get gamePawnType() { return "basicCube" }

    init(options) {
        super.init(options);
        this.subscribe("input", "zDown", this.moveLeft);
        this.subscribe("input", "xDown", this.moveRight);
    }

    moveLeft() {
        const translation = this.translation;
        translation[0] += -0.1;
        this.set({translation});
    }

    moveRight() {
        const translation = this.translation;
        translation[0] += 0.1;
        this.set({translation});

    }
}
TestActor.register('TestActor');

export class MyModelRoot extends GameModelRoot {

    init(options) {
        super.init(options);
        console.log("Start model root!");
        this.test = TestActor.create({translation:[0,0,0]});
    }

}
MyModelRoot.register("MyModelRoot");
```

#### Enable the Input Handler
We provide a basic keypress and pointer forwarding template that uses Unity's new input system.
See `Multisynq/Runtime/UserInputActions` (lightning bolt icon).
Select it and click "Make this the active input map".

This allows most keypresses and pointer events to be forwarded. Skip this step if you want to use your own completely custom set of input events.

#### Create the Necessary Prefabs
The model code above expects that its `TestActor` will be represented in Unity by a game pawn of type "basicCube".  To make that association across the Croquet Bridge, you will need to make a corresponding prefab. This Prefab must have a Croquet "Actor Manifest" Component, with its Pawn Type field set to "basicCube" to match the `gamePawnType` used in the model.

Each of the various pawn prefabs used by your app must be copied into the Addressable Assets Group that you created earlier, and labeled there either with the names of specific scenes for which that prefab is needed, or with the label "default" to mean that it is available in every scene.

### Run and Test
You should now run the app. A basicCube will spawn in the scene, and you will be able to control the cube's movement with the Z and X keys.

## Croquet Menu Items
_Within the package we have provided a Croquet Menu which gives developers the ability to quickly perform various useful operations._

### Build JS Now
_Manually initiate a build of the JavaScript code for the Croquet session that synchronizes this app._

This and the other JS build items are available when the open scene has a Croquet Bridge object with an appropriately set App Name (and there is a corresponding app source directory under MultisynqJS).

This item triggers a bundling of that source along with the libraries that are currently installed as part of the "JS build tools". The bundling is required for the Croquet session to run.

We usually recommend setting "Build JS on Play", which will cause bundling to be done automatically as part of the switch into play mode, thus ensuring that the latest code is being used. Manually triggering the build is a quick way to test whether the JavaScript will in fact bundle successfully, without having to wait through the other aspects of play-mode initialization.

Where a JS Watcher is available (see below), that is an even quicker way to incorporate changes in the JavaScript code.

### Build JS on Play (toggle)
_Whether or not to initiate a build of the JavaScript code every time you hit play._

As noted above, we recommend setting this during development of the JavaScript code (if not using a Watcher), so that the latest code is always in use. If you are not making changes to the JavaScript, disabling this option (once the code has been built) will speed up the entry to play mode.

### Start/Stop a JS Watcher on the Scene
_Currently only offered on MacOS.  Starts a Webpack watcher that instantly re-bundles the JavaScript when any source file is changed._

The webpack watcher is optimized for fast rebuilds (on a small project that takes multiple seconds to start up and complete a one-time build, the watcher may achieve a rebuild in 20-50ms). Running a watcher is therefore a way to speed up the iterative cycle of changing the JavaScript and testing it in Unity play mode.

We only support a single watcher for a project, tied to the app specified by the active scene when you launched the watcher. If your project involves different apps used by different scenes, you would need to stop the watcher that is running for one app before starting it to run on another.

### Harvest Scene Definitions
_Sweeps through all scenes included in Build Settings, producing scene-definition files that the Croquet code can read when switching to a scene, instead of having to query a live Unity participant on the fly._

To play a Unity scene, Croquet needs information about how that scene functions - including the prefabs that are available for use as pawns, and the published events that Unity scripts want to subscribe to. In addition, a scene can include an arbitrary spatial arrangement of game objects defining the initial state of the Croquet-synchronized objects in that scene.

Harvesting scene definitions is optional. However, if scenes are not harvested, each entry to a new scene during play will require the details for that scene to be transmitted over the Croquet network to all participants. In the case of a scene that includes hundreds or thousands of pre-laid-out objects, this could introduce a multi-second delay that the presence of the scene-definition file would avoid.

A scene's definition is written to a file in the JavaScript source directory of the app named on the scene's Croquet Bridge object. If multiple scenes use the same app, their definitions are included in a single file for that app.

### Install JS Build Tools
_Extract from the Croquet Multiplayer package the tools and libraries needed to bundle JavaScript code._

In addition to C# scripts, the package defines the JavaScript environment needed to build your Croquet code. This includes dependencies on the Croquet library and its Worldcore game features, and on the webpack tool that is used for bundling.

The first time you attempt to build JavaScript in your project - whether manually, or triggered by pressing play - Croquet for Unity will automatically invoke this installation mechanism.

You will only need to invoke it manually if a new version of the Croquet Multiplayer package itself is released (and is detected by your Unity Package Manager, perhaps as a result of explicitly selecting "Update"), and you would like to take advantage of the new version.


## Contribution
Contributions to the package are welcome as these projects are open source and we encourage community involvement.

1. Base your `feature/my-feature-name` or `bugfix/descriptor` branch off of `develop` branch
2. Make your changes
3. Open a PR against the `develop` branch
4. Discuss and Review the PR with the team
5. Changes will be merged into `develop` after PR approval

## Local Package Development

When changing and adding to the code of the package, you will want to set up a local way to experience the edits you make to the package dynamically. To do so, you will need to point to your local copy of the "io.croquet.multiplayer" package instead of the one on github.
In your testing project folder, you will want to make an edit to the Package/manifest.json file:

For example in the file:

`croquet-for-unity-tutorials/Tutorials/Packages/manifest.json`

Change the line:

`"io.croquet.multiplayer": "https://github.com/croquet/croquet-for-unity-package.git#v0.9.3",`

To:

`"io.croquet.multiplayer": "file:../../../croquet-for-unity-package",`

Of course, this folder path assumes you have a particular file structure with sibling project and package folders. Your path may vary a bit. Let the Unity Editor console log messages be your guide to get this wired up correctly.

With that edit to the manifest correctly set, just switching back to the Unity editor and it will load from your local folder instead of the github one. Code edits to the package code will be immediately compiled when focus is returned to the Unity Editor just like project code.
