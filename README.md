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
com.unity.render-pipelines.core | [![](https://badge-proxy.cds.internal.unity3d.com/fbe7884e-7b48-4d94-b0d2-910d05aa1ac6)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/2103163e-f7f2-4e51-821d-ea45d551f4aa)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/74b65e22-f1c3-4b3a-a6e9-6c1528314bc4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/76b3ef71-d57a-402a-be1e-9401f872f65e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/90be70c3-cd3c-4275-940c-8ca0262fb711) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/73c999ed-fd64-4df1-a6b8-77df8cbfe50f)
com.unity.render-pipelines.universal | [![](https://badge-proxy.cds.internal.unity3d.com/0d7c6f45-6b13-4cad-ab86-4c3d19900cf6)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/633a2b67-0fd8-4cf7-98d8-0892eec36b36)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/2eaeea22-a937-4476-ac4b-6071378be1ba)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/55998229-8e1e-43dc-8828-5ae6e60a7e61)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5a632a87-cc88-4414-be12-394dfeb934df) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/28dfd57b-54d1-45ca-80d3-94d96dbbcfd0)
com.unity.render-pipelines.high-definition | [![](https://badge-proxy.cds.internal.unity3d.com/cf26af5b-4cfa-41b6-964e-5fd0a04ddecb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/76d79cfa-43fa-43e9-9d25-aa45065278d8)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/d3ed9e4b-d9c4-4401-b952-ed5808aafe44)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/68ea9caa-f95a-4136-bfd2-34535b1108e3)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/b7d3bcae-9ad8-4375-a683-1b907828137f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/1ef3d7d0-cea1-4955-9276-e34c0952afbb)
com.unity.render-pipelines.high-definition-config | [![](https://badge-proxy.cds.internal.unity3d.com/974ca994-62bf-4626-a2be-fea7e84b1ab2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/faf341f0-a584-4705-91c2-bbf106a164f1)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/ab12a6a1-17e5-478f-9916-7cfe77f2dbbb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/5dae58be-38ac-4e96-8dcf-33e1183fc547)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59fd14b1-3fc2-49e4-bf24-950f1482323f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/d0fb96fc-6ff8-45a8-a317-ec19f30894cc)
com.unity.shadergraph | [![](https://badge-proxy.cds.internal.unity3d.com/4619e388-f247-4ec0-8ac3-83744581e687)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/e3f12551-15c9-41bf-94b7-62be90f95e4a)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e1ee3c6-0477-4076-a2af-3376ead10421)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/4085e508-0443-4b46-b309-59dca2ea4b7f)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/e2171d56-50c8-4803-964c-a63dcc728355) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/30fe71f1-5838-4bf9-84eb-26a42320e4a2)
com.unity.visualeffectgraph | [![](https://badge-proxy.cds.internal.unity3d.com/e4e3b028-c988-4a74-b948-f8860a772f6c)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/0bafe0f1-264d-48db-a4b0-4aa76c9d48fb)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/c10f50c2-2a79-4d0a-a763-54dcb40d027f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/fe51d994-d07c-4e63-b5db-472c0e64095f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59b6ec9b-c477-4767-82ba-d2390e70cede) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/ae2fb4f5-43dc-4ad2-8c94-7190dbcdc132)
com.unity.render-pipelines.lightweight | [![](https://badge-proxy.cds.internal.unity3d.com/359b0f86-810b-4dbe-910d-bd068d515282)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/build-info?branch=8.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/d9108f37-5b8c-4897-bb84-492b02118a78)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependencies-info?branch=8.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e4aae95-2a9a-471c-a5f8-e8faf3675454)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/71c28de8-a86b-4b64-8bc1-e0a09d182c39)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/warnings-info?branch=8.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/679931b4-d19f-4788-90af-be45f40f3a11) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/a11f872a-60e4-4a16-a3f7-4ac888bcd879)

## Using the latest version

This repository uses the **master** branch for main development. Development on this branch is based on the latest internal version of Unity so it may not work on the latest publicly available version of Unity. The following list contains Unity version/major SRP version pairs which you can use as a guideline as to which major SRP version you can use in your Unity Project:

- **Unity 2019.1 is compatible with SRP version 5.x**
- **Unity 2019.2 is compatible with SRP version 6.x**
- **Unity 2019.3 is compatible with SRP version 7.x**
- **Unity 2020.1 is compatible with SRP version 8.x**

The above list is a guideline for major versions of SRP, but there are often multiple minor versions that you can use for a certain version of Unity. To determine which minor versions of SRP you can use:

1. In your Unity Project, open the Package Manager window (menu: **Window > Package Manager**).
2. In the list of packages, find **Core RP Library**. To find this package in older versions of Unity, you may need to expose preview packages. To do this, click the **Advanced** button at the top of the window then, in the context menu, click **Show preview packages**.
3. Click the drop-down arrow to the left of the package entry then click **See all versions**. This shows a list that contains every package version compatible with your version of Unity.

After you decide which version of SRP to use:

1. Go to the [Scriptable Render Pipeline repository](https://github.com/Unity-Technologies/ScriptableRenderPipeline).
2. Click the **Branch** drop-down then click the **Tags** tab.
3. Find the tag that corresponds to the version of SRP you want to use. When you clone the repository, you use this tag to check out the correct branch.

To clone the repository, you can use a visual client, like [GitHub Desktop](#GitHubDesktop), or use [console commands](#ConsoleCommands). When you clone the repository, make sure to clone it outside of your Unity Project's Asset folder. 

After you clone the repository, you can install the package into your Unity Project. To do this, see [Installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html).

<a name="GitHubDesktop"></a>

### Cloning the repository using the GitHub Desktop App:

1. Open the GitHub Desktop App and click **File > Clone repository**.
2. Click the **URL** tab and enter the following URL: https://github.com/Unity-Technologies/ScriptableRenderPipeline.
3. Click the **Choose…** button and navigate to your Unity Project’s base folder.
4. Click the **Clone** button.

After you clone the repository, open your console application of choice in the ScriptableRenderPipeline folder and run the following console command:

`\> git checkout v7.1.8 (or the latest tag)`

<a name="ConsoleCommands"></a>

### Cloning the repository using console commands:

Open your console application of choice and run the following console commands:

```
\> cd <Path to your Unity project>

\> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline

\> cd ScriptableRenderPipeline

\>  git checkout v7.1.8 (or the latest tag)
```

## Sample Scenes in ScriptableRenderPipelineData

Unity provides sample Scenes to use with SRP. You can find these Scenes in the [ScriptableRenderPipelineData GitHub repository](https://github.com/Unity-Technologies/ScriptableRenderPipelineData). To add the Scenes to your Project, clone the repository into your Project's Assets folder.