## NOTE: We have migrated reported issues to FogBugz. You can only log further issues via the Unity bug tracker. To see how, read [this](https://unity3d.com/unity/qa/bug-reporting).

# Unity Scriptable Render Pipeline
The Scriptable Render Pipeline (SRP) is a Unity feature designed to give artists and developers the tools they need to create modern, high-fidelity graphics in Unity. Unity provides two pre-built Scriptable Render Pipelines:

* The Universal Render Pipeline (URP) for use on all platforms.
* The High Definition Render Pipeline (HDRP) for use on compute shader compatible platforms.

Unity is committed to an open and transparent development process for SRP and the pre-built Render Pipelines. This means that so you can browse this repository to see what features are currently in development.

For more information about the packages in this repository, see the following:

* [Scriptable Render Pipeline Core](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html)
* [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html)
* [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html)
* [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html)
* [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html)

## How to use the latest version
__Note: The Master branch is our current development branch and may not work on the latest publicly available version of Unity. To determine which version of SRP you should use with your version of Unity, go to Package Manager (Window > Package Manager > Show Preview Packages) to see what versions of SRP are available for your version of Unity Editor. Then you can search the Tags tab of the Branch dropdown in the SRP GitHub for that tag number.__

__Regarding package number, we have adopted those numbers
Unity binaries 2019.1 is compatible with 5.x version
Unity binaries 2019.2 is compatible with 6.x version
Unity binaries 2019.3 is compatible with 7.x version
Unity binaries 2020.1 is compatible with 8.x version__

To use the latest version of the SRP, follow the instructions below:

This repository consists of a folder that should be cloned outside the Assets\ folder of your Unity project. We recommend creating a new project to test SRP. Do not clone this repo into an existing project unless you want to break it, or unless you are updating to a newer version of the SRP repo.

After cloning you will need to edit your project's `packages.json` file (in either `UnityPackageManager/` or `Packages/`) to point to the SRP submodules you wish to use. See: https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/HDRP_Tests/Packages/manifest.json

This will link your project to the specific version of SRP you have cloned.

You can use the GitHub desktop app to clone the latest version of the SRP repo or you can use GitHub console commands.

### To clone the repo using the GitHub Desktop App:
1. Open the GitHub Desktop App and click __Clone a Repository__.
2. Click the __URL__ tab in the __Clone a Repository__ window
3. Enter the following URL: https://github.com/Unity-Technologies/ScriptableRenderPipeline
4. Click the __Choose…__ button to navigate to your project’s base folder.
5. Click the __Clone__ button.

After the repo has been cloned you will need to run the following console commands from the ScriptableRenderPipeline folder:

```
> git checkout Unity-2018.1.0b2 (or the latest tag)
```
### To download the repo using console commands:
Enter the following commands in your console application of choice:  

```
> cd <Path to your Unity project>
> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline
> cd ScriptableRenderPipeline
> git checkout Unity-2018.1.0b2 (or the latest tag)
```
## Scriptable Render Pipeline Assets
The Scriptable Render Pipeline Asset controls the global rendering quality settings of your project and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render loop implementation.

You can create multiple Pipeline Assets to store settings for different built platforms or for different testing environments. 

To create a Render Pipeline Asset: 

1. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right click in the Project window and select ___Create > Render Pipeline > Rendering > High Definition or Universal Render Pipeline Asset.___
2. Navigate to ___Edit > Project Settings > Graphics___ and add the Render Pipeline Asset you created to the __Render Pipeline Settings__ field to use it in your project. 

Note: Always store your new Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your settings are not lost when merging new changes from the SRP repo.


## Using the High Definition Render Pipeline (HDRP) or the Universal Render Pipeline (URP)

### Using HDRP

To use HDRP you must edit your project’s __Player__ and __Graphics__ settings as follows:

1. Navigate to ___Edit > Project Settings > Player___ and set the color space of your project to Linear by selecting __Linear__ from the __Color Space__ dropdown. HDRP does not support Gamma lighting.
2. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right in click the Project window and select ___Create >  Rendering > High Definition Render Pipeline Asset.___
3. Navigate to ___Edit > Project Settings > Graphics___ and add the High Definition Render Pipeline Asset you created to the __Render Pipeline Settings__ field.

Note: Always store your High Definition Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your HDRP settings are not lost when merging new changes from the SRP repo.

### Using URP
To use the Universal Pipeline you must edit your project’s __Graphics__ settings as follows:

1. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right click in the Project window and select ___Create > Rendering > Universal Render Pipeline Asset.___
2. Navigate to ___Edit > Project Settings > Graphics___ and add the Universal Render Pipeline Asset you created to the __Render Pipeline Settings__ field.

Note: Always store your new Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your Universal settings are not lost when merging new changes from the SRP repo.

## Sample Scenes in ScriptableRenderPipelineData

If you want some sample scenes to use with SRP, you can find them at the [ScriptableRenderPipelineData GitHub repository](https://github.com/Unity-Technologies/ScriptableRenderPipelineData).

Clone the repo into your project's Assets\ folder.

Previous iterations of the ScriptableRenderPipeline repo owned this sample scene data, in case you noticed it before, and wondered where it went.
