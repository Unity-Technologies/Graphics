# Upgrading HDRP from 6.x to 7.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 6.x to 7.x.

## New Scene

The New Scene system in HDRP relies on a Prefab in your project. It also depends on default settings set for the Volumes. If you have already configured one with the Wizard, you need to update it regarding the **Default Volume Profile Asset** (in **Edit > Project Settings > HDRP Default Settings**).

If you use the default Prefab (the one created by the wizard) and rely on the default **Default Volume Profile Asset**, then your Prefab won't be in sync with the default volume profile and you must update it.

The easiest way is to recreate a new one using the Wizard:

1. Open the Wizard (**Window > Rendering > HDRP Wizard**)
2. Remove the Prefab set in **Default Scene Prefab**.
3. [*Optional*] Keep a copy of your previous Prefab if you have customized it, so you don't lose your version. To do so, rename the Prefab. It will prevent the Prefab being overridden.
4. Look at the **Configuration Checking** below for the line **Default Scene Prefab** and click on the **Fix** button.
5. [*Optional*] Report your custom change in the new created Prefab.

Then repeat this for **Default DXR Scene Prefab** if you were also using DXR.

<a name="ProceduralSky"></a>

## Procedural Sky

The [Procedural Sky](Override-Procedural-Sky.md) override is deprecated in 7.x. If your Project uses a procedural sky, you either need to install the override from the samples or switch to the new physically based sky.

To install the Procedural Sky override into a 7.x Project:

1. Open your Project in the Unity Editor.
2. Open the Package Manager (**Window > Package Manager**) and click on **High Definition RP**
3. In the **Samples** section, click on the **Import in project** button for the **Procedural Sky** entry. If you have a Scene open that uses the Procedural Sky,  the sky re-appears, but its settings are reset to the default values.
4. To recover your sky settings, you must quit Unity without saving and then re-open your Project.

The last step is important because, if you haven't installed the sample and load a Scene that uses a Procedural Sky, then the Procedural Sky data in the Volume is lost. Unity doesn't serialize this so you can close and re-open Unity to recover your settings.

## Sky Intensity Mode

In HDRP for 7.x, the way sky intensity is handled has been changed. Before the change, there were two parameters: **Exposure** and **Multiplier** that we both applied to change the sky intensity. Now, there is a new combo box for users to chose between one or the other. An update script is provided in **Edit > Render Pipeline > Upgrade Sky Intensity Mode**  to upgrade the existing sky components.

## Fog

HDRP has deprecated the Linear Fog, Exponential Fog, Volumetric Fog, and Volumetric Fog Quality overrides in 7.x and replaced them with a single [Fog](Override-Fog.md) override. This override acts as an exponential fog with a height component by default and allows you to add additional volumetric fog. To automatically update old fog overrides to the new system, select **Edit > Render Pipeline > Upgrade Fog Volume Components**. Note that it can not safely convert all cases so you may need to upgrade some manually.

## Shadow Maps

Before 7.x, each Light in HDRP exposed several options for the shadow map bias. From 7.x, HDRP has replaced every option, except for **Normal Bias**, with **Slope-Scale Depth Bias**. Introducing this property makes the shadow map bias setup fairly different to what it was. This means that, if the default values lead to unexpected results, you may need a new setup for the bias on each Light.

Also, before 7.x, PCSS had a different parametrization. Since 7.x, the shadow softness is controlled by angular diameter on directional lights and by shape radius for point and spot lights. To convert the previous shadow softness to a shape radius, an approximate function is: `0.333 * oldSoftness * (shadowResolution / 512)`.
Moreover, the *Minimum filter size* option is now called *Minimum Blur Intensity*; this is functionally equivalent but simply remaps the previous range [0 ... 0.0001] to [0 ... 1].

## Lights

From 7.x, the available Light types are Directional, Point, Spot, and Area so that they match the types in the Built-in Renderer. You can select the shape of Area lights (from Rectangle, Tube, and Disc) after your select Area as your Light type.

## Area Lights

Before 7.x, HDRP synchronized the width and height of an area [Light](Light-Component.md)'s **Emissive Mesh** with the [localScale](https://docs.unity3d.com/ScriptReference/Transform-localScale.html) of its Transform. From 7.x, HDRP uses the [lossyScale](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) to make the **Emissive Mesh** account for the scale of the parent Transforms. This means that you must resize every area Light in your Unity Project according to the scale of its parent.

## Cookie textures (Spot, Area and Directional lights) and Planar Reflection Probes

Before 7.x, we stored cookies of Spot, Area, and directional lights and planars into texture arrays. Due to the usage of these arrays, we were limited to use the same size for every element in one array. For cookie textures, a convertion code ensured that if a texture size wasn't exatcly the same as the size of the texture array (defined in the HDRP asset), then it was scaled to fit the size of the array.
Now that we're using an atlas we don't have this limitation anymore. It means that the cookie size you were using might differ now that we use the real size of the texture and could result in more sharp / pixelated cokies if your texture were too big or too small. If you encounter this kind of issue, we recommend fixing the images directly.
For Planar Reflection Probes it also means that you can use different resolution per probe.

You may also encounter this error in the console: `No more space in the 2D Cookie Texture Atlas. To solve this issue, increase the resolution of the cookie atlas in the HDRP settings.` This means that there is no space left in the Cookie atlas because there are too many of them in the view or the cookie textures are too big. To solve this issue you can either lower the resolution of the cookie textures or increase the atlas resolution by going to **Edit** > **Project Settings** > **Quality** > **HDRP** > **DefaultHDRPAsset** > **Lighting** > **Cookies** > **2D Atlas Size**.

## Max Smoothness, Emission Radius, Bake Shadows Radius and Bake Shadows Angle

Before 7.x, Max Smoothness, Emission Radius, and Bake Shadows Radius were separate controls for Point and Spot Lights. From 7.x, the UI displays a single property, called **Radius** that controls all of the properties mentioned above.
Also, Max Smoothness, Angular Diameter, and Bake Shadows Angle were separate controls for Directional Lights. The UI now displays a single property, called **Angular Diameter**, that controls all the mentioned above.

When upgrading, a slight shift of highlight shape or shadow penumbra size can occur. This happens if you set the original properties to values that don't match what the automatic conversion from "Radius" or "Angular Diameter" results in.

## Realtime GI Enlighten

From 7.x, HDRP no longer supports realtime GI for new Projects. However, HDRP still supports realtime GI for Projects that you previously created for 7.x LTS only.

## Custom Shaders

7.x introduces a change to Reflection Probes which allows you to compress the range that Unity uses when rendering the probe's content. This comes with a small change to the Shader framework, the function `SampleEnv()` now requires an additional parameter, being the factor to apply to the probe data to compensate the range compression done at probe rendering time. This value is in the data structure `EnvLightData` under the name of `rangeCompressionFactorCompensation`.

## HDRP Asset and Default Settings

From 7.x, a lot of HDRP’s settings have moved. Specifically, most of the information that was previously in the HDRP Asset is now mirrored in Project Settings.
There are two separate categories:
* Default Settings (**Project Settings > HDRP Default Settings**), where you can serve up default Frame Andy Volume Settings.
* Quality Settings (**Project Settings > Quality > HDRP**), where you can manage settings for multiple Render Pipeline Assets.

From 7.x, Baking sky / Static lighting Sky Components are deprecated. Settings are now in (**Lighting Window > Environment lighting**) and use a Volume Profile Asset and a drop-down to select the sky to use for baking. You can remove the Static Lighting Sky component from your Scenes.

## Shorcut for volume settings

From 7.x, there is no Render Settings shorcut in the context menu to create pre-set Volume settings. HDRP uses Default Settings (**Project Settings > HDRP Default Settings**) instead. HDRP now includes shortcuts for local Volumes of different shapes to ease the process of setting up your Volumes. In addition, HDRP also includes a shortcut which creates a Volume with Sky and Fog overrides as this is a commonly used Volume type.

## HDRP Configuration Package

From 7.x, HDRP has an additional small package as a dependency. This is the HDRP-Config package. You can change properties in this package to disable or tweak features that you can't control dynamically in HDRP’s UI. Currently, this include:
* Selecting the Shadow Filtering quality when in Deferred Mode.
* Camera Relative Rendering.
* Raytracing.
* XR Maximum views.
* Area Lights.

## Material Upgrader

When you upgrade your **High Definition RP** package, Unity now upgrades each of your Materials. This means adds a version number the first time it happens so, if a HDRP Shader change occurs, Unity is able to fix your Material so that it still works correctly with the new changes.

To do this, Unity opens a prompt when you begin the upgrade, asking if you want to save your Project. It keeps attempting to upgrade old files until you agree to save your Project.

## Missing Script for GameObject SceneIDMap

When you enter Play Mode in a Scene with baked Probes authored prior to 7.x, you may encounter a warning about a missing Script for a GameObject named **SceneIDMap**.
To fix this, load the Scene in the Unity Editor and select **Edit > Render Pipeline > Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes**.

## Light Intensity and Sky Exposure versus HDRP Default Settings

By default, HDRP uses physically correct intensities for Lights. Because of this, the exposure of the default HDRI sky present in HDRP is set to **11** to match a Directional Light intensity of **10000**. For reference, you can find similar values in the template Project.
When the HDRP Wizard has been set up correctly, if you create a new Scene, Unity automatically creates GameObjects with the correct intensities so that everything is coherent. However, if the HDRP Wizard isn't set up correctly, or if you create Directional Lights from scratch, the intensity isn't physically correct. The consequence is that the Light doesn't match the default sky exposure, so any GameObject in the Scene looks black because of the automatic exposure compensating for the overly bright sky.
To avoid this, make sure that you use coherent values for light intensity compared to the current sky exposure.

## Iridescence color space

Previously, HDRP used the wrong color space to calculate iridescence. From 7.x, HDRP uses the correct color space. This results in a more vibrant / saturated color when you use the iridescence effect.
