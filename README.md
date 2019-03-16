## NOTE: We have migrated reported issues to FogBugz. You can only log further issues via the Unity bug tracker. To see how, read [this](https://unity3d.com/unity/qa/bug-reporting).

# Unity Scriptable Render Pipeline
The Scriptable Render Pipeline (SRP) is a new Unity feature in active development. SRP has been designed to give artists and developers the tools they need to create modern, high-fidelity graphics in Unity. Including a built-in Lightweight Render Pipeline for use on all platforms, and a High Definition Render Pipeline (HDRP) for use on compute shader compatible platforms. These features are available in Unity 2018.1+.

We are committed to an open and transparent development process, and as such you are welcome to take a look around if you are really curious.

Detailed documentation is being added here: [Wiki](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki)

This feature is currently in preview. Some features may change or be removed before we move to a full release.  

[Lightweight Pipeline Blogpost](https://blogs.unity3d.com/2018/02/21/the-lightweight-render-pipeline-optimizing-real-time-performance/)

[High Definition Pipeline Blogpost](https://blogs.unity3d.com/2018/03/16/the-high-definition-render-pipeline-focused-on-visual-quality/)

[The High Definition Render Pipeline: Getting Started Guide for Artists](https://blogs.unity3d.com/2018/09/24/the-high-definition-render-pipeline-getting-started-guide-for-artists/)

## How to use the latest version
__Note: The Master branch is our current development branch and may not work on the latest publicly available version of Unity. To determine which version of SRP you should use with your version of Unity, go to Package Manager (Window > Package Manager > Show Preview Packages) to see what versions of SRP are available for your version of Unity Editor. Then you can search the Tags tab of the Branch dropdown in the SRP GitHub for that tag number.__
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

1. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right click in the Project window and select ___Create > Render Pipeline >  High Definition or Lightweight > Render Pipeline/Pipeline Asset.___
2. Navigate to ___Edit > Project Settings > Graphics___ and add the Render Pipeline Asset you created to the __Render Pipeline Settings__ field to use it in your project. 

Note: Always store your new Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your settings are not lost when merging new changes from the SRP repo.


## Using the High Definition Render Pipeline (HDRP) or the Lightweight Pipeline

### Using HDRP

To use HDRP you must edit your project’s __Player__ and __Graphics__ settings as follows:

1. Navigate to ___Edit > Project Settings > Player___ and set the color space of your project to Linear by selecting __Linear__ from the __Color Space__ dropdown. HDRP does not support Gamma lighting.
2. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right in click the Project window and select ___Create > Render Pipeline >  High Definition > Render Pipeline.___
3. Navigate to ___Edit > Project Settings > Graphics___ and add the High Definition Render Pipeline Asset you created to the __Render Pipeline Settings__ field.

Note: Always store your High Definition Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your HDRP settings are not lost when merging new changes from the SRP repo.

### Using Lightweight Pipeline
To use the Lightweight Pipeline you must edit your project’s __Graphics__ settings as follows:

1. In the Project window, navigate to a directory outside of the Scriptable Render Pipeline Folder, then right click in the Project window and select ___Create > Render Pipeline >  Lightweight > Pipeline Asset.___
2. Navigate to ___Edit > Project Settings > Graphics___ and add the Lightweight Render Pipeline Asset you created to the __Render Pipeline Settings__ field.

Note: Always store your new Render Pipeline Asset outside of the Scriptable Render Pipeline folder. This ensures that your Lightweight settings are not lost when merging new changes from the SRP repo.

## Sample Scenes in ScriptableRenderPipelineData

If you want some sample scenes to use with SRP, you can find them at the [ScriptableRenderPipelineData GitHub repository](https://github.com/Unity-Technologies/ScriptableRenderPipelineData).

Clone the repo into your project's Assets\ folder.

Previous iterations of the ScriptableRenderPipeline repo owned this sample scene data, in case you noticed it before, and wondered where it went.
