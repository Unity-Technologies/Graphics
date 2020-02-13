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

### Package CI Summary

Package Name | Latest CI Status
------------ | ---------
com.unity.render-pipelines.core | [![](https://badge-proxy.cds.internal.unity3d.com/658ed6e2-cb73-4c17-909e-1c558e402f15)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/d97b521a-469f-4c39-9176-efba794011d2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/74b65e22-f1c3-4b3a-a6e9-6c1528314bc4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependants-info)[![](https://badge-proxy.cds.internal.unity3d.com/ce5c4776-4467-468a-9251-ce9f232b3fdd)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/90be70c3-cd3c-4275-940c-8ca0262fb711) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/73c999ed-fd64-4df1-a6b8-77df8cbfe50f)
com.unity.render-pipelines.universal | [![](https://badge-proxy.cds.internal.unity3d.com/83da995f-a4e5-46d7-8965-4dd38cc6d0a2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/4758e59a-2dcb-40ee-8e41-6b779340a25b)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/2eaeea22-a937-4476-ac4b-6071378be1ba)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/b15d41d5-df2b-4411-9413-7f8c8ea369be)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5a632a87-cc88-4414-be12-394dfeb934df) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/28dfd57b-54d1-45ca-80d3-94d96dbbcfd0)
com.unity.render-pipelines.high-definition | [![](https://badge-proxy.cds.internal.unity3d.com/a8b1403f-68dd-4d9e-8198-39931007b1d2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/1038f4d6-9ace-4e6e-aa5f-1793f222716d)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/d3ed9e4b-d9c4-4401-b952-ed5808aafe44)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/7e6d655c-ce18-4546-8f2e-6ee85583f244)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/b7d3bcae-9ad8-4375-a683-1b907828137f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/1ef3d7d0-cea1-4955-9276-e34c0952afbb)
com.unity.render-pipelines.high-definition-config | [![](https://badge-proxy.cds.internal.unity3d.com/25b27fae-b4c9-4ef9-84c2-8ca7f38c2262)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation)[![](https://badge-proxy.cds.internal.unity3d.com/2abd0987-57c9-41a4-bc89-04037fe17057)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/ab12a6a1-17e5-478f-9916-7cfe77f2dbbb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/b5ba525c-426a-4fa8-a783-e6b0001d430c)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59fd14b1-3fc2-49e4-bf24-950f1482323f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/d0fb96fc-6ff8-45a8-a317-ec19f30894cc)
com.unity.shadergraph | [![](https://badge-proxy.cds.internal.unity3d.com/5f2ebc29-a76f-40e3-8f2c-9b3f19e382ce)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/f20fc57c-45d3-4990-8f1f-d311c79c0824)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e1ee3c6-0477-4076-a2af-3376ead10421)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/dc92bffa-83bc-49af-b432-eaf1376b8935)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/e2171d56-50c8-4803-964c-a63dcc728355) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/30fe71f1-5838-4bf9-84eb-26a42320e4a2)
com.unity.visualeffectgraph | [![](https://badge-proxy.cds.internal.unity3d.com/bbc6e3c6-5113-451d-bab1-71c2f14ae9ef)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/77e84168-aec9-44d6-94ce-c9404d3aebcc)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/c10f50c2-2a79-4d0a-a763-54dcb40d027f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/82a3dcaa-f077-43ed-9573-6b19327024ec)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59b6ec9b-c477-4767-82ba-d2390e70cede) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/ae2fb4f5-43dc-4ad2-8c94-7190dbcdc132)
com.unity.render-pipelines.lightweight | [![](https://badge-proxy.cds.internal.unity3d.com/9e82bdfb-323e-4053-857d-53ae40105738)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/c9df9d2b-7132-405a-8437-f89a74192067)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e4aae95-2a9a-471c-a5f8-e8faf3675454)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/af483d19-8862-4ccf-ae50-48f1605cce3e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/679931b4-d19f-4788-90af-be45f40f3a11) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/a11f872a-60e4-4a16-a3f7-4ac888bcd879)


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
