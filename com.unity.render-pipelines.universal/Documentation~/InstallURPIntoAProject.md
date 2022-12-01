# Installing the Universal Render Pipeline into an existing Project

You can download and install the latest version of the Universal Render Pipeline (URP) to your existing Project via the [Package Manager system](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html), and then install it into your Project. If you don’t have an existing Project, see documentation on [how to start a new URP Project from a Template](creating-a-new-project-with-urp.md).

## Before you begin

URP uses its own [integrated post-processing solution](integration-with-post-processing.md). If you have the Post Processing Version 2 package installed in your Project, you need to delete the Post Processing Version 2 package before you install URP into your Project. When you have installed URP, you can then recreate your post-processing effects.

URP does not currently support custom post-processing effects. If your Project uses custom post-processing effects, these cannot currently be recreated in URP. Custom post-processing effects will be supported in a forthcoming release of URP.

## Installing URP

1. In Unity, open your Project.
2. In the top navigation bar, select __Window > Package Manager__ to open the __Package Manager__ window.
3. In the __Packages__ menu, select **Unity Registry**. This shows the list of available packages for the version of Unity that you are currently running.
4. Select **Universal RP** from the list of packages.
5. In the bottom right corner of the Package Manager window, select __Install__. Unity installs URP directly into your Project.

## Configuring URP

Before you can start using URP, you need to configure it. To do this, you need to create a Scriptable Render Pipeline Asset and adjust your Graphics settings.

### Creating the Universal Render Pipeline Asset

The [Universal Render Pipeline Asset](universalrp-asset.md) (URP Asset) contains the global rendering and quality settings of your project, and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.

To create a Universal Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create > Rendering > URP Asset__. Alternatively, navigate to the menu bar at the top, and select __Assets > Create > Rendering > URP Asset__.

You can either leave the default name for the new Universal Render Pipeline Asset, or type a new one.

### <a name="set-urp-active"></a>Set URP as the active render pipeline

To set URP as the active render pipeline:

1. In your project, locate the Render Pipeline Asset that you want to use.<br/>**Tip**: to find all URP Assets in a project, use the following query in the search field: `t:universalrenderpipelineasset`.

1. Select **Edit** > **Project Settings** > **Graphics**.

2. In the **Scriptable Render Pipeline Settings** field, select the URP Asset. When you select the URP Asset, the available Graphics settings change immediately.

**Optional**:

Set an override URP Assets for different quality levels:

1. Select **Edit** > **Project Settings** > **Quality**.

2. Select a quality level. In the **Render Pipeline Asset** field, select the Render Pipeline Asset.

## Upgrading your shaders

If your project uses the prebuilt [Standard Shader](https://docs.unity3d.com/Manual/shader-StandardShader.html), or custom Unity shaders made for the Built-in Render Pipeline, you must convert them to URP-compatible Unity shaders. For more information on this topic, see [Upgrading your Shaders](upgrading-your-shaders.md).
