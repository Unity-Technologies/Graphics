# Upgrading HDRP from Unity 2019.2 to Unity 2019.3

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2019.2 to 2019.3.

<a name="ProceduralSky"></a>

## Procedural Sky

The [Procedural Sky](Override-Procedural-Sky.html) override is deprecated in Unity 2019.3. If your Project uses a procedural sky, you either need to install the override from the samples or switch to the new physically based sky.

To install the Procedural Sky override into a 2019.3 Project:

1. Open your Project in the Unity Editor.
2. Open the Package Manager (**Window > Package Manager**) and click on **High Definition RP**
3. In the **Samples** section, click on the **Import in project** button for the **Procedural Sky** entry. If you have a Scene open that uses the Procedural Sky,  the sky re-appears, but its settings are reset to the default values.
4. To recover your sky settings, you must quit Unity without saving and then re-open your Project.

The last step is important because, if you haven't installed the sample and load a Scene that uses a Procedural Sky, then the Procedural Sky data in the Volume is lost. Unity does not serialize this so you can close and re-open Unity to recover your settings.

## Sky Intensity Mode

In HDRP for 2019.3, the way sky intensity is handled has been changed. Before the change, there were two parameters: **Exposure** and **Multiplier** that we both applied to change the sky intensity. Now, there is a new combo box for users to chose between one or the other. An update script is provided in **Edit > Render Pipeline > Upgrade Sky Intensity Mode**  to upgrade the existing sky components.

## Fog

HDRP has deprecated the Linear Fog, Exponential Fog, Volumetric Fog, and Volumetric Fog Quality overrides in 2019.3 and replaced them with a single [Fog](Override-Fog.html) override. This override acts as an exponential fog with a height component by default and allows you to add additional volumetric fog. To automatically update old fog overrides to the new system, select **Edit > Render Pipeline > Upgrade Fog Volume Components**. Note that it can not safely convert all cases so you may need to upgrade some manually.

## Shadow Maps

Before Unity 2019.3, each Light in HDRP exposed several options for the shadow map bias. From 2019.3, HDRP has replaced every option, except for **Normal Bias**, with **Slope-Scale Depth Bias**. Introducing this property makes the shadow map bias setup fairly different to what it was. This means that, if the default values lead to unexpected results, you may need a new setup for the bias on each Light. 

Also, before Unity 2019.3, PCSS had a different parametrization. Since 2019.3, the shadow softness is controlled by angular diameter on directional lights and by shape radius for point and spot lights. To convert the previous shadow softness to a shape radius, an approximate function is: `0.333 * oldSoftness * (shadowResolution / 512)`. 
Moreover, the *Minimum filter size* option is now called *Minimum Blur Intensity*; this is functionally equivalent but simply remaps the previous range [0 ... 0.0001] to [0 ... 1]. 

## Lights

From Unity 2019.3, the available Light types are Directional, Point, Spot, and Area so that they match the types in the Built-in Renderer. You can select the shape of Area lights (from Rectangle, Tube, and Disc) after your select Area as your Light type.

## Area Lights

Before Unity 2019.3, HDRP synchronized the width and height of an area [Light](Light-Component.html)'s **Emissive Mesh** with the [localScale](https://docs.unity3d.com/ScriptReference/Transform-localScale.html) of its Transform. From Unity 2019.3, HDRP uses the [lossyScale](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) to make the **Emissive Mesh** account for the scale of the parent Transforms. This means that you must resize every area Light in your Unity Project according to the scale of its parent.

## Max Smoothness, Emission Radius, Bake Shadows Radius and Bake Shadows Angle 

Before Unity 2019.3, Max Smoothness, Emission Radius, and Bake Shadows Radius were separate controls for Point and Spot Lights. From Unity 2019.3, the UI displays a single property, called **Radius** that controls all of the properties mentioned above.
Also, Max Smoothness, Angular Diameter, and Bake Shadows Angle were separate controls for Directional Lights. The UI now displays a single property, called **Angular Diameter**, that controls all the mentioned above.

When upgrading, a slight shift of highlight shape or shadow penumbra size can occur. This happens if you set the original properties to values that do not match what the automatic conversion from "Radius" or "Angular Diameter" results in.

## Realtime GI Enlighten

From Unity 2019.3, HDRP no longer supports realtime GI for new Projects. However, HDRP still supports realtime GI for Projects that you previously created for 2019.3 LTS only.

## Custom Shaders

Unity 2019.3 introduces a change to Reflection Probes which allows you to compress the range that Unity uses when rendering the probe's content. This comes with a small change to the Shader framework, the function `SampleEnv()` now requires an additional parameter, being the factor to apply to the probe data to compensate the range compression done at probe rendering time. This value is in the data structure `EnvLightData` under the name of `rangeCompressionFactorCompensation`. 

## HDRP Asset and Default Settings

From Unity 2019.3, a lot of HDRP’s settings have moved. Specifically, most of the information that was previously in the HDRP Asset is now mirrored in Project Settings.  
There are two separate categories: 
* Default Settings (**Project Settings > HDRP Default Settings**), where you can serve up default Frame Andy Volume Settings.
* Quality Settings (**Project Settings > Quality > HDRP**), where you can manage settings for multiple Render Pipeline Assets. 

From Unity 2019.3, Baking sky / Static lighting Sky Components are deprecated. Settings are now in (**Lighting Window > Environment lighting**) and use a Volume Profile Asset and a drop-down to select the sky to use for baking. You can remove the Static Lighting Sky component from your Scenes.

## Shorcut for volume settings

From Unity 2019.3, there is no Render Settings shorcut in the context menu to create pre-set Volume settings. HDRP uses Default Settings (**Project Settings > HDRP Default Settings**) instead. HDRP now includes shortcuts for local Volumes of different shapes to ease the process of setting up your Volumes. In addition, HDRP also includes a shortcut which creates a Volume with Sky and Fog overrides as this is a commonly used Volume type.

## HDRP Configuration Package

From Unity 2019.3, HDRP has an additional small package as a dependency. This is the HDRP-Config package. You can change properties in this package to disable or tweak features that you cannot control dynamically in HDRP’s UI. Currently, this include:
* Selecting the Shadow Filtering quality when in Deferred Mode.
* Camera Relative Rendering.
* Raytracing.
* XR Maximum views.
* Area Lights.

## Material Upgrader

When you upgrade your **High Definition RP** package, Unity now upgrades each of your Materials. This means adds a version number the first time it happens so, if a HDRP Shader change occurs, Unity is able to fix your Material so that it still works correctly with the new changes.

To do this, Unity opens a prompt when you begin the upgrade, asking if you want to save your Project. It keeps attempting to upgrade old files until you agree to save your Project.

## Missing Script for GameObject SceneIDMap

For scene with baked probes authored prior to 2019.3, you may ran into a warning concerning a missing script for a GameObject named SceneIDMap when entering play mode.
To fix it, you can load the scene in the editor and click on "Edit/Render Pipeline/Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes".

