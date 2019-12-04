## NOTE: We have migrated reported issues to FogBugz. You can only log further issues via the Unity bug tracker. To see how, read [this](https://unity3d.com/unity/qa/bug-reporting).

# Unity Scriptable Render Pipeline
The Scriptable Render Pipeline (SRP) is a new Unity feature in active development. SRP has been designed to give artists and developers the tools they need to create modern, high-fidelity graphics in Unity. Including a built-in Lightweight Render Pipeline for use on all platforms, and a High Definition Render Pipeline (HDRP) for use on compute shader compatible platforms. These features are available in Unity 2018.1+.

We are committed to an open and transparent development process, and as such you are welcome to take a look around if you are really curious.

Detailed documentation is being added here: [Wiki](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki)

This feature is currently in preview. Some features may change or be removed before we move to a full release.  

[Lightweight Pipeline Blogpost](https://blogs.unity3d.com/2018/02/21/the-lightweight-render-pipeline-optimizing-real-time-performance/)

[High Definition Pipeline Blogpost](https://blogs.unity3d.com/2018/03/16/the-high-definition-render-pipeline-focused-on-visual-quality/)

[The High Definition Render Pipeline: Getting Started Guide for Artists](https://blogs.unity3d.com/2018/09/24/the-high-definition-render-pipeline-getting-started-guide-for-artists/)

### Package CI Summary

Package Name | Latest CI Status
------------ | ---------
com.unity.render-pipelines.core | [![](https://badge-proxy.cds.internal.unity3d.com/7068273a-d16d-45d9-bb84-7cdc68ba0580)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/de196ba3-6ab9-440b-905e-1dadc025583a)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/74b65e22-f1c3-4b3a-a6e9-6c1528314bc4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/820e3703-f2a9-42bc-9548-73492135a540)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/90be70c3-cd3c-4275-940c-8ca0262fb711) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/73c999ed-fd64-4df1-a6b8-77df8cbfe50f)
com.unity.render-pipelines.universal | [![](https://badge-proxy.cds.internal.unity3d.com/76a51820-0a3b-46cc-859a-fe88f7d0ac8b)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/60561f65-d5aa-4b6a-96de-35f4960ac0d5)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/2eaeea22-a937-4476-ac4b-6071378be1ba)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/4efa2cae-2666-4bc3-877b-47c7bd4142d6)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5a632a87-cc88-4414-be12-394dfeb934df) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/28dfd57b-54d1-45ca-80d3-94d96dbbcfd0)
com.unity.render-pipelines.high-definition | [![](https://badge-proxy.cds.internal.unity3d.com/a68dae85-ce0f-46e6-95bf-aa04f2a845d9)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/42c38313-bf0b-42a4-96d7-3dccf39d92b8)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/d3ed9e4b-d9c4-4401-b952-ed5808aafe44)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/31437d42-85cb-428d-b718-921dc971b8a9)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/b7d3bcae-9ad8-4375-a683-1b907828137f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/1ef3d7d0-cea1-4955-9276-e34c0952afbb)
com.unity.render-pipelines.high-definition-config | [![](https://badge-proxy.cds.internal.unity3d.com/89664583-2f3c-4a61-a1fa-a9daea037b2e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/3ed117a7-740c-4ef1-a280-c97221742a1e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/ab12a6a1-17e5-478f-9916-7cfe77f2dbbb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/2421fdd2-bda0-492f-bcdf-ce764b64d58e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59fd14b1-3fc2-49e4-bf24-950f1482323f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/d0fb96fc-6ff8-45a8-a317-ec19f30894cc)
com.unity.shadergraph | [![](https://badge-proxy.cds.internal.unity3d.com/ad6f7b2b-97ec-46c5-8539-9b70e8c30bb5)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/067b8f44-3f3a-4925-8462-996ffbe41662)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/7e1ee3c6-0477-4076-a2af-3376ead10421)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/9ce9cc97-b89d-4a2a-98c2-d1a1d2d0277e)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/e2171d56-50c8-4803-964c-a63dcc728355) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/30fe71f1-5838-4bf9-84eb-26a42320e4a2)
com.unity.visualeffectgraph | [![](https://badge-proxy.cds.internal.unity3d.com/0fbfa6fc-2faf-4689-a3e7-fca736ab23cb)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/6606630d-31a9-4af5-b63c-25272411c381)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/c10f50c2-2a79-4d0a-a763-54dcb40d027f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/823df233-071e-4ceb-a39f-b810d7fe6fe1)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59b6ec9b-c477-4767-82ba-d2390e70cede) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/ae2fb4f5-43dc-4ad2-8c94-7190dbcdc132)
com.unity.render-pipelines.lightweight | [![](https://badge-proxy.cds.internal.unity3d.com/dabba5ea-621a-45b4-98e5-eecd6e3026a8)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/3af4fced-c82d-4737-b37f-654c3d960b76)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/7e4aae95-2a9a-471c-a5f8-e8faf3675454)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/87242c39-da1e-49df-bcd5-c3aa8665b9f4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/679931b4-d19f-4788-90af-be45f40f3a11) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/a11f872a-60e4-4a16-a3f7-4ac888bcd879)

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
