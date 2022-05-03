# Upgrading HDRP from 6.x to 7.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 6.x to 7.x.

## New Scene

The New Scene system in HDRP relies on a Prefab in your project. It also depends on default settings set for the Volumes. If you have already configured a Prefab with the HDRP Wizard, you need to update it in the **Default Volume Profile Asset** (in **Edit** > **Project Settings** > **HDRP Default Settings**).

If you use the default Prefab (the one created by the HDRP Wizard) and don't override the default settings in the **Default Volume Profile Asset**, your Prefab won't be in sync anymore with the default volume profile and you must create a new one.

To create a new Prefab with the HDRP Wizard:

1. Open the Wizard (Menu: **Window** > **Rendering** > **HDRP Wizard**)
2. Remove the Prefab set in **Default Scene Prefab**.
3. [Optional] Keep a copy of your previous Prefab if you have customized it, so you don't lose your version. To do this, rename the Prefab. It will prevent the new Prefab from overriding your customized Prefab.
4. Go to **Configuration Checking** > **Default Scene Prefab** and select **Fix**.
5. [Optional] Report your custom change in the newly created Prefab.

If your Project uses ray tracing, follow these additional steps:

1. Open the Wizard (Menu: **Window** > **Rendering** > **HDRP Wizard**)
2. Remove the Prefab set in **Default DXR Scene Prefab**.
3. [Optional] Keep a copy of your previous Prefab if you have customized it, so you don't lose your version. To do this, rename the Prefab. It will prevent the new Prefab from overriding your customized Prefab.
4. Go to **Configuration Checking** > **Default DXR Scene Prefab** and select **Fix**.
5. [Optional] Report your custom change in the newly created Prefab.

<a name="ProceduralSky"></a>

## Procedural Sky

The [Procedural Sky](Override-Procedural-Sky.md) override is deprecated in 7.x. If your Project uses a procedural sky, you need to do one of the following options:

* Install the Procedural Sky override into your 7.x Project.
* Switch to the new physically based sky.

To install the Procedural Sky override into a 7.x Project:

1. Open your Project in the Unity Editor.
2. Open the Package Manager. Go to **Window** > **Package Manager**.
3. Select **High Definition RP**.
4. In the **Samples** section, go to **Procedural Sky** and select **Import in project**. If you have a Scene open that uses the Procedural Sky, the sky re-appears, but its settings are reset to the default values.
5. To recover your sky settings, you must quit Unity without saving and then re-open your Project.

**Important**: If you haven't installed the sample and load a Scene that uses a Procedural Sky, HDRP loses the Procedural Sky data in the Volume. HDRP doesn't serialize this data. To recover your settings, close and re-open Unity.

## Sky Intensity Mode

In 7.x, the way HDRP handles sky intensity is different. Previously, there were two parameters: **Exposure** and **Multiplier** that HDRP applied to change the sky intensity. Now, there is a new combo box for you to chose between either **Exposure** or **Multiplier**. To upgrade existing sky components, go to **Edit** > **Render Pipeline** > **Upgrade Sky Intensity Mode**.

## Fog

HDRP has deprecated the Linear Fog, Exponential Fog, Volumetric Fog, and Volumetric Fog Quality overrides in 7.x and replaced them with a single [Fog](Override-Fog.md) override. This override acts as an exponential fog with a height component by default and allows you to add additional volumetric fog. To automatically update old fog overrides to the new system, go to **Edit** > **Render Pipeline** > **Upgrade Fog Volume Components**.

**Note**: HDRP can't convert all overrides automatically. You might need to upgrade some overrides manually.

## Shadow Maps

Before 7.x, each Light in HDRP exposed several options for the shadow map bias. From 7.x, HDRP has replaced every option, except for **Normal Bias**, with **Slope-Scale Depth Bias**. Introducing this property changes the shadow map bias setup. This means that, if the default values lead to unexpected results, you may need a new setup for the bias on each Light.

Also, before 7.x, PCSS had a different parametrization. From 7.x, HDRP controls shadow softness by angular diameter for directional lights and by shape radius for point and spot lights. To convert the previous shadow softness to a shape radius, an approximate function is: `0.333 * oldSoftness * (shadowResolution / 512)`.

**Minimum filter size** is now called **Minimum Blur Intensity**. **Minimun Blur Intensity** has the same effect as **Minimum filter size** but remaps the previous range from [0 ... 0.0001] to [0 ... 1].

## Lights

From 7.x, the available Light types are Directional, Point, Spot, and Area to match the Light types in the Built-in Render Pipeline. You can select the shape of Area lights (from Rectangle, Tube, and Disc) after you select Area as your Light type.

## Area Lights

Before 7.x, HDRP synchronized the width and height of an Area [Light](Light-Component.md)'s **Emissive Mesh** with the [`localScale`](https://docs.unity3d.com/ScriptReference/Transform-localScale.html) of its Transform. From 7.x, HDRP uses the [`lossyScale`](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) to make the **Emissive Mesh** account for the scale of the parent Transforms. This means that you must resize every Area Light in your Unity Project according to the scale of its parent.

## Cookie textures (Spot, Area and Directional lights) and Planar Reflection Probes

Before 7.x, HDRP stored cookies of Spot, Area, and directional lights and planars into texture arrays. HDRP converted cookie texture sizes to be the same in any given array. From 7.x, HDRP uses an atlas, which can store the real size of the cookie textures. For Planar Reflection Probes it also means that you can use different resolution per probe. This change might result in sharper or pixelated cookies if your textures were too big or too small. If you encounter this issue, fix the images directly.

You may also encounter this error in the console: `No more space in the 2D Cookie Texture Atlas. To solve this issue, increase the resolution of the cookie atlas in the HDRP settings.` This means that there is no space left in the Cookie atlas because there are too many of them in the view or the cookie textures are too big. To solve this issue, use one of the following options:

* Lower the resolution of the cookie textures
* Increase the atlas resolution in the HDRP settings.

## Max Smoothness, Emission Radius, Bake Shadows Radius and Bake Shadows Angle

Before 7.x, Max Smoothness, Emission Radius, and Bake Shadows Radius were separate controls for Point, Spot, and directional Lights. From 7.x, the UI displays a single property, called **Radius** for Point and Spot Lights and **Angular Diameter** for directional Lights, that controls all of the properties mentioned above.

When you upgrade, a slight shift of highlight shape or shadow penumbra size might occur. This happens if you set the original properties to values that don't match what the automatic conversion from **Radius** or **Angular Diameter** results in.

## Realtime GI Enlighten

From 7.x, HDRP no longer supports realtime GI for new Projects. However, HDRP still supports realtime GI for Projects that you previously created for 7.x LTS only.

## Custom Shaders

7.x introduces a change to Reflection Probes which allows you to compress the range that Unity uses when rendering the probe's content. This comes with a small change to the Shader framework, the function `SampleEnv()` now requires an additional parameter, being the factor to apply to the probe data to compensate the range compression done at probe rendering time. This value is in the data structure `EnvLightData` under the name of `rangeCompressionFactorCompensation`.

## HDRP Asset and Default Settings

From 7.x, many of HDRP’s settings have moved. Specifically, most of the information that was in the HDRP Asset is now mirrored in Project Settings.
There are two categories:

* Default Settings (menu: **Project Settings** > **HDRP Default Settings**), where you can configure default Frame and Volume settings.
* Quality Settings (menu: **Project Settings** > **Quality** > **HDRP**), where you can manage settings for multiple Render Pipeline Assets.

From 7.x, the Baking Sky and Static Lighting Sky Components are deprecated. These settings are now in **Lighting Window** > **Environment lighting**. Use a Volume Profile Asset and a drop-down to select the sky to use for baking. You can remove the Static Lighting Sky component from your Scenes.

## Shorcut for volume settings

From 7.x, there is no Render Settings shorcut in the context menu to create pre-set Volume settings. HDRP uses Default Settings (menu: **Project Settings** > **HDRP Default Settings**) instead.

HDRP now includes shortcuts for local Volumes of different shapes to ease the process of setting up your Volumes. HDRP also includes a shortcut to create a Volume with Sky and Fog overrides as this is a commonly used Volume type.

## HDRP Configuration Package

From 7.x, HDRP has an additional small package as a dependency. This is the HDRP-Config package. You can change properties in this package to disable or tweak features that you can't control dynamically in HDRP’s UI. This includes:

* Selecting the Shadow Filtering quality when in Deferred Mode.
* Camera Relative Rendering.
* Raytracing.
* XR Maximum views.
* Area Lights.

## Material Upgrader

When you upgrade your **High Definition RP** package, Unity now upgrades each of your Materials. This adds a version number the first time it happens so, if a HDRP Shader change occurs, Unity is able to fix your Material so that it still works correctly with the new changes.

To do this, Unity opens a prompt when you begin the upgrade, asking if you want to save your Project. It keeps attempting to upgrade old files until you agree to save your Project.

## Missing Script for GameObject SceneIDMap

When you enter Play Mode in a Scene with baked Probes authored before 7.x, you might encounter a warning about a missing Script for a GameObject named **SceneIDMap**. To fix this:

1. Load the Scene in the Unity Editor.
2. Go to **Edit** > **Render Pipeline** > **Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes**.

## Light Intensity and Sky Exposure versus HDRP Default Settings

By default, HDRP uses physically correct intensities for Lights. Because of this, the exposure of the default HDRI sky present in HDRP is set to **11** to match a Directional Light intensity of **10000**. For reference, you can find similar values in the template Project.

When the HDRP Wizard has been set up correctly, if you create a new Scene, Unity automatically creates GameObjects with the correct intensities so that everything is coherent. However, if the HDRP Wizard isn't set up correctly, or if you create Directional Lights from scratch, the intensity isn't physically correct. The consequence is that the Light doesn't match the default sky exposure, so any GameObject in the Scene looks black because of the automatic exposure compensating for the overly bright sky.
To avoid this, make sure that you use coherent values for light intensity compared to the current sky exposure.

## Iridescence color space

Previously, HDRP used the wrong color space to calculate iridescence. From 7.x, HDRP uses the correct color space. This results in a more vibrant and saturated color when you use the iridescence effect.
