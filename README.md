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
com.unity.render-pipelines.core | [![](https://badge-proxy.cds.internal.unity3d.com/7068273a-d16d-45d9-bb84-7cdc68ba0580)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/de196ba3-6ab9-440b-905e-1dadc025583a)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/74b65e22-f1c3-4b3a-a6e9-6c1528314bc4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/820e3703-f2a9-42bc-9548-73492135a540)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/90be70c3-cd3c-4275-940c-8ca0262fb711) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/73c999ed-fd64-4df1-a6b8-77df8cbfe50f)
com.unity.render-pipelines.universal | [![](https://badge-proxy.cds.internal.unity3d.com/76a51820-0a3b-46cc-859a-fe88f7d0ac8b)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/60561f65-d5aa-4b6a-96de-35f4960ac0d5)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/2eaeea22-a937-4476-ac4b-6071378be1ba)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/4efa2cae-2666-4bc3-877b-47c7bd4142d6)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5a632a87-cc88-4414-be12-394dfeb934df) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/28dfd57b-54d1-45ca-80d3-94d96dbbcfd0)
com.unity.render-pipelines.high-definition | [![](https://badge-proxy.cds.internal.unity3d.com/a68dae85-ce0f-46e6-95bf-aa04f2a845d9)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/42c38313-bf0b-42a4-96d7-3dccf39d92b8)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/d3ed9e4b-d9c4-4401-b952-ed5808aafe44)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/31437d42-85cb-428d-b718-921dc971b8a9)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/b7d3bcae-9ad8-4375-a683-1b907828137f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/1ef3d7d0-cea1-4955-9276-e34c0952afbb)
com.unity.render-pipelines.high-definition-config | [![](https://badge-proxy.cds.internal.unity3d.com/89664583-2f3c-4a61-a1fa-a9daea037b2e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/3ed117a7-740c-4ef1-a280-c97221742a1e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/ab12a6a1-17e5-478f-9916-7cfe77f2dbbb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/2421fdd2-bda0-492f-bcdf-ce764b64d58e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59fd14b1-3fc2-49e4-bf24-950f1482323f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/d0fb96fc-6ff8-45a8-a317-ec19f30894cc)
com.unity.shadergraph | [![](https://badge-proxy.cds.internal.unity3d.com/ad6f7b2b-97ec-46c5-8539-9b70e8c30bb5)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/067b8f44-3f3a-4925-8462-996ffbe41662)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/7e1ee3c6-0477-4076-a2af-3376ead10421)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/9ce9cc97-b89d-4a2a-98c2-d1a1d2d0277e)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/e2171d56-50c8-4803-964c-a63dcc728355) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/30fe71f1-5838-4bf9-84eb-26a42320e4a2)
com.unity.visualeffectgraph | [![](https://badge-proxy.cds.internal.unity3d.com/0fbfa6fc-2faf-4689-a3e7-fca736ab23cb)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/6606630d-31a9-4af5-b63c-25272411c381)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/c10f50c2-2a79-4d0a-a763-54dcb40d027f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/823df233-071e-4ceb-a39f-b810d7fe6fe1)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59b6ec9b-c477-4767-82ba-d2390e70cede) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/ae2fb4f5-43dc-4ad2-8c94-7190dbcdc132)
com.unity.render-pipelines.lightweight | [![](https://badge-proxy.cds.internal.unity3d.com/dabba5ea-621a-45b4-98e5-eecd6e3026a8)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/3af4fced-c82d-4737-b37f-654c3d960b76)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/7e4aae95-2a9a-471c-a5f8-e8faf3675454)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/87242c39-da1e-49df-bcd5-c3aa8665b9f4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/679931b4-d19f-4788-90af-be45f40f3a11) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/a11f872a-60e4-4a16-a3f7-4ac888bcd879)

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