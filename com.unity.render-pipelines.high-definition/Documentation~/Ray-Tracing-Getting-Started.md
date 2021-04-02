# Getting started with ray tracing

The High Definition Render Pipeline (HDRP) includes preview ray tracing support from Unity 2019.3. Ray tracing is a feature that allows you to access data that is not on screen. For example, you can use it to request position data, normal data, or lighting data, and then use this data to compute quantities that are hard to approximate using classic rasterization techniques.

While film production uses ray tracing extensively, its resource intensity has limited its use to offline rendering for a long time. Now, with recent advances in GPU hardware, you can make use of ray tracing effect in real time.

This document covers:

- [Hardware requirements](#HardwareRequirements).
- [Integrate ray tracing into your HDRP Project](#Integration).
- [HDRP effects that use ray tracing](#RayTracingEffectsOverview).

<a name="HardwareRequirements"></a>

## Hardware requirements

Full ray tracing hardware acceleration is available on following GPUs:
- NVIDIA GeForce 20 series:
  - RTX 2060
  - RTX 2060 Super
  - RTX 2070
  - RTX 2070 Super
  - RTX 2080
  - RTX 2080 Super
  - RTX 2080 Ti
  - NVIDIA TITAN RTX
- NVIDIA GeForce 30 series:
  - RTX 3060
  - RTX 3060Ti
  - RTX 3070
  - RTX 3080
  - RTX 3090
- NVIDIA Quadro:
  - RTX 3000 (laptop only)
  - RTX 4000
  - RTX 5000
  - RTX 6000
  - RTX 8000

NVIDIA also provides a ray tracing fallback for some previous generation graphics cards:
- NVIDIA GeForce GTX
  - Turing generation: GTX 1660 Super, GTX 1660 Ti
  - Pascal generation: GTX 1060 6GB, GTX 1070, GTX 1080, GTX 1080 Ti
- NVIDIA TITAN V
- NVIDIA Quadro: P4000, P5000, P6000, V100


If your computer has one of these graphics cards, it can run ray tracing in Unity.

Before you open Unity, make sure to update your NVIDIA drivers to the latest version, and also make sure your Windows version is at least 1809.

<a name="Integration"></a>

## Integrating ray tracing into your HDRP Project

Before you use ray tracing features in your HDRP Project, you need to set up your HDRP Project for ray tracing support. HDRP only supports ray tracing using the DirectX 12 API, so ray tracing only works in the Unity Editor or the Windows Unity Player when they render with DirectX 12. You need to change the default graphics API of your HDRP project from DirectX 11 to DirectX 12.

There are two ways to do this:

* [Use the Render Pipeline Wizard](#WizardSetup)

* [Manual setup](#ManualSetup)

Once you have completed one of these, move onto [Final setup](#final-setup).

<a name="WizardSetup"></a>

### Render Pipeline Wizard setup

You can use the [Render Pipeline Wizard](Render-Pipeline-Wizard.md) to set up ray tracing in your HDRP Project.

1. To open the Render Pipeline Wizard, go to Window > Render Pipeline and select HD Render Pipeline Wizard.
2. Select the HDRP + DXR tab.
3. Click the Fix All button.
4. (Optional) Enable the HDRP asset features that are required for the ray tracing effects.

Your HDRP Project now supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

<a name="ManualSetup"></a>

### Manual setup

To set up ray tracing manually, you need to:

1. [Make your HDRP project use DirectX 12](#ManualSetup-EnablingDX12).
2. [Disable static batching on your HDRP project](#ManualSetup-DisablingStaticBatching).
3. [Enable and configure ray tracing in your HDRP Asset](#ManualSetup-EnablingRayTracing).
4. [Ensure ray tracing resources are properly assigned](#ManualSetup-RayTracingResources).
5. (Optional) [Enable ray-traced effects in your HDRP Asset](#ManualSetup-EnableAssetFeatures).

<a name="ManualSetup-EnablingDX12"></a>

#### Upgrading to DirectX 12

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the Player tab.
2. Select the Other Settings fold-out, and in the Rendering section, disable Auto Graphics API for Windows. This exposes the Graphics APIs for Windows section.
3. In the Graphics APIs for Windows section, click the plus (+) button and select Direct3d12.
4. Unity uses Direct3d11 by default. To make Unity use Direct3d12, move Direct3d12 (Experimental) to the top of the list.
5. To apply the changes, you may need to restart the Unity Editor. If a window prompt appears telling you to restart the Editor, click **Restart Editor** in the window.

The Unity Editor window should now include the &lt;DX12&gt; tag in the title bar like so:

![](Images/RayTracingGettingStarted1.png)

<a name="ManualSetup-DisablingStaticBatching"></a>

#### Disabling static batching

Next, you need to disable static batching, because HDRP does not support this feature with ray tracing in **Play Mode**. To do this:

1. Open the Project Settings window (menu:  **Edit > Project Settings**), then select the **Player** tab.
2. Select the **Other Settings** fold-out, then in the **Rendering** section, disable **Static Batching**.

<a name="ManualSetup-EnablingRayTracing"></a>

#### HDRP Asset configuration

Now that Unity is running in DirectX 12, and you have disabled [static batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), enable and configure ray tracing in your [HDRP Asset](HDRP-Asset.md). The previous steps configured Unity to support ray tracing; the following step enables it in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Rendering section, enable Realtime Ray Tracing. This triggers a recompilation, which makes ray tracing available in your HDRP Project.

<a name="ManualSetup-RayTracingResources"></a>

#### Ray tracing resources

To verify that HDRP has properly assigned ray tracing resources:

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the HDRP Default Settings tab.
2. Make sure there is a Render Pipeline Resources Asset assigned to the Render Pipeline Resources field.

<a name="ManualSetup-EnableAssetFeatures"></a>

#### (Optional) Enable ray-traced effects in your HDRP Asset

HDRP uses ray tracing to replace certain rasterized effects. In order to use a ray tracing effect in your Project, you must first enable the rasterized version of the effect. The four effects that require you to modify your HDRP Asset  are:

* **Screen Space Shadows**
* **Screen Space Reflections**
*  **Transparent Screen Space Reflections**
* **Screen Space Global Illumination**

To enable the above effects in your HDRP Unity Project:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Lighting > Reflections** and enable **Screen Space Reflection**.
3. After enabling **Screen Space Reflections**, go to **Lighting > Reflections** and enable **Transparent Screen Space Reflection**.
4. Go to **Lighting > Shadows** and enable **Screen Space Shadows**.
5. Go to **Lighting > Lighting** and enable **Screen Space Global Illumination**.

Your HDRP Project now fully supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

### Final setup

Now that your HDRP Project supports ray tracing, there are a few steps you must complete in order to actually use it in your Scene.

1. [Frame Settings validation](#frame-settings)
2. [Build settings validation](#build-settings)
3. [Scene validation](#scene-validation)


#### Frame Settings

To make HDRP calculates ray tracing effects for [Cameras](HDRP-Camera.md) in your Scene, make sure your Cameras use [Frame Settings](Frame-Settings.md) that have ray tracing enabled. You can enable ray tracing for all Cameras by default, or you can enable ray tracing for specific Cameras in your Scene.

To enable ray tracing by default:

1. Open the Project Settings window (menu:  **Edit > Project Settings**), then select the HDRP Default Settings tab.
2. Select Camera from the Default Frame Settings For drop-down.
3. In the **Rendering** section, enable **Ray Tracing**.

To enable ray tracing for a specific Camera:

1. Click on the Camera in the Scene or Hierarchy to view it in the Inspector.
2. In the **General** section, enable **Custom Frame Settings**. This exposes Frame Settings just for this Camera.
3. in the **Rendering** section, enable **Ray Tracing**.

#### Build settings

To build your Project to a Unity Player, ray tracing requires that the build uses 64 bits architecture. To set your build to use 64 bits architecture:

1. Open the Build Settings window (menu: **File > Build Settings**).
2. From the **Architecture** drop-down, select **x86_64**.

#### Scene validation

To check whether it is possible to use ray tracing in a Scene, HDRP includes a menu option that validates each GameObject in the Scene. If you do not setup GameObjects correctly, this process throws warnings in the Console window. To use it:
1. Click **Edit > Rendering > Check Scene Content for HDRP Ray Tracing**.
2. In the Console window (menu: **Window > General > Console**), check if there are any warnings.

<a name="RayTracingEffectsOverview"></a>

## Ray tracing effects overview

HDRP uses ray tracing to replace some of its screen space effects, shadowing techniques, and Mesh rendering techniques.

- [Ray-Traced Ambient Occlusion](Ray-Traced-Ambient-Occlusion.md) replaces [screen space ambient occlusion](Override-Ambient-Occlusion.md) with a more accurate, ray-traced, ambient occlusion technique that can use off screen data.
- [Ray-Traced Contact Shadows](Ray-Traced-Contact-Shadows.md) replaces [contact shadows](Override-Contact-Shadows.md) with a more accurate, ray-traced, contact shadow technique that can use off screen data.
- [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.md) is an alternative to Light Probes and lightmaps in HDRP.
- [Ray-Traced Reflections](Ray-Traced-Reflections.md) is a replacement for [screen space reflection](Override-Screen-Space-Reflection.md) that uses a ray-traced reflection technique that can use off-screen data.
- [Ray-Traced Shadows](Ray-Traced-Shadows.md) replace shadow maps for Directional, Point, and Area [Lights](Light-Component.md).
- [Recursive Ray Tracing](Ray-Tracing-Recursive-Rendering.md) replaces the rendering pipeline for Meshes. Meshes that use this feature cast refraction and reflection rays recursively.
- [Ray-Traced Subsurface Scattering](Ray-Traced-Subsurface-Scattering.md) replaces [subsurface scattering](Subsurface-Scattering.md) with a more accurate, ray-traced, subsurface scattering technique that can use off screen data.

## Ray tracing mode

HDRP includes two ray tracing modes that define how it evaluates certain ray-traced effects. The modes are:

* **Performance**: This mode targets real-time applications. If you select this mode, ray-traced effects include presets that you can change to balance performance with quality.
* **Quality**: This mode targets technical demos and applications that want the best quality results.

Depending on which ray tracing mode you select, HDRP may expose difference properties for some ray-traced effects.

You can change which ray tracing mode HDRP uses on either a Project level or effect level. To change it for your entire Project:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Rendering section, select a ray tracing mode from the **Supported Ray Tracing Mode** drop-down.

If you select **Both**, you can change the ray tracing mode for each ray-traced effect. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component that includes a ray-traced effect.
2. In the Inspector for the ray-traced effect, change the **Mode** property to use the ray tracing mode you want the effect to use. This changes the properties available in the Inspector.


## Ray tracing project

You can find a small ray tracing project that contains all the effects mention above here:
https://github.com/Unity-Technologies/SmallOfficeRayTracing
This Project is already set up with ray tracing support.

## Limitations

This section contains information on the limitations of HDRP's ray tracing implementation. Mainly, this is a list of features that HDRP supports in its rasterized render pipeline, but not in its ray-traced render pipeline.

### Unsupported features of ray tracing

There is no support for ray tracing on platforms other than DX12 for now.

HDRP ray tracing in Unity 2020.2 has the following limitations:
- Does not support vertex animation.
- Does not supports decals.
- Does not support tessellation.
- Does not support per pixel displacement (parallax occlusion mapping, height map, depth offset).
- Does not support VFX and Terrain.
- Does not have accurate culling for shadows, you may experience missing shadows in the ray traced effects.
- Does not support MSAA.
- For renderers that have [LODs](https://docs.unity3d.com/2019.3/Documentation/Manual/LevelOfDetail.html), the ray tracing acceleration structure only includes the highest level LOD and ignores the lower LODs.
- Does not support [Graphics.DrawMesh](https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html).
- Ray tracing is not supported when rendering [Reflection Probes](Reflection-Probe.md).

### Unsupported shader graph nodes for ray tracing

When building your custom shaders using shader graph, some nodes are incompatible with ray tracing. You need either to avoid using them or provide an alternative behavior using the ray tracing shader node. Here is the list of the incompatible nodes:
- DDX, DDY and DDXY nodes.
- All the nodes under Inputs > Geometry (Position, View Direction, Normal, etc.) in View Space mode.
- Checkerboard node.

### Unsupported features of path tracing

For information about unsupported features of path tracing, see [Path tracing limitations](Ray-Tracing-Path-Tracing.md#limitations).
