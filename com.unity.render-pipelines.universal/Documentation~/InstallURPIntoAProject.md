# Installing the Universal Render Pipeline into an existing Project

You can download and install the latest version of the Universal Render Pipeline (URP) to your existing Project via the [Package Manager system](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html), and then install it into your Project. If you don’t have an existing Project, see documentation on [how to start a new URP Project from a Template](creating-a-new-project-with-urp.md).

## Before you begin

URP uses its own [integrated post-processing solution](integration-with-post-processing.md). If you have the Post Processing Version 2 package installed in your Project, you need to delete the Post Processing Version 2 package before you install URP into your Project. When you have installed URP, you can then recreate your post-processing effects.

URP does not currently support custom post-processing effects. If your Project uses custom post-processing effects, these cannot currently be recreated in URP. Custom post-processing effects will be supported in a forthcoming release of URP.

## Installing URP

1. Open a Unity project.
2. Open the __Package Manager__ window (__Window > Package Manager__).
3. In the __Package Manager__ window, in the **Packages** field, select **Unity Registry**.
4. Select **Universal RP** from the list of packages.
5. In the bottom right corner of the Package Manager window, select __Install__. Unity installs URP into your Project.

## Configuring URP

Before you can start using URP, you need to configure it. To do this, you need to create a Scriptable Render Pipeline Asset and adjust your Graphics settings.

### Creating the Universal Render Pipeline Asset

The [Universal Render Pipeline Asset](universalrp-asset.md) controls the global rendering and quality settings of your Project, and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.

To create a Universal Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create > Rendering > Universal Render Pipeline > Pipeline Asset__. Alternatively, navigate to the menu bar at the top, and select __Assets > Create > Rendering > Universal Render Pipeline > Pipeline Asset__.

You can either leave the default name for the new Universal Render Pipeline Asset, or type a new one.


### Adding the Asset to your Graphics settings

To use URP, you need to add the newly created Universal Render Pipeline Asset to your Graphics settings in Unity. If you don't, Unity still tries to use the Built-in render pipeline.

To add the Universal Render Pipeline Asset to your Graphics settings:


1. Navigate to __Edit > Project Settings... > Graphics__.
2. In the __Scriptable Render Pipeline Settings__ field, add the Universal Render Pipeline Asset you created earlier. When you add the Universal Render Pipeline Asset, the available Graphics settings immediately change. Your Project is now using URP.

## Upgrading your shaders

If your project uses the prebuilt [Standard Shader](https://docs.unity3d.com/Manual/shader-StandardShader.html), or custom Unity shaders made for the Built-in Render Pipeline, you must convert them to URP-compatible Unity shaders. For more information on this topic, see [Upgrading your Shaders](upgrading-your-shaders.md).
