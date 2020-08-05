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
- NVIDIA GeForce RTX 2060, RTX 2080 Super, RTX 2070, RTX 2070 Super, RTX 2080, RTX 2080 Super, RTX 2080 Ti
NVIDIA TITAN RTX
- NVIDIA Quadro RTX 3000 (laptop only), RTX 4000, RTX 5000, RTX 6000, RTX 8000

NVIDIA also provides a ray tracing fallback for some previous generation graphics cards:
- NVIDIA GeForce GTX
  - Turing generation: GTX 1650, GTX 1660 Super, GTX 1660 Ti
  - Pascal generation: GTX 1060, GTX 1070, GTX 1080, GTX 1080 Ti
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

Once you have completed one of these, move onto [Final setup](#FinalSetup).

<a name="WizardSetup"></a>

### Render Pipeline Wizard setup

You can use the [Render Pipeline Wizard](Render-Pipeline-Wizard.html) to set up ray tracing in your HDRP Project.

1. To open the Render Pipeline Wizard, go to Window > Render Pipeline and select HD Render Pipeline Wizard.
2. Select the HDRP + DXR tab.
3. Click the Fix All button.

Your HDRP Project now supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#FinalSetup).

<a name="ManualSetup"></a>

### Manual setup

To set up ray tracing manually, you need to:

1. [Make your HDRP project use DirectX 12](#ManualSetup-EnablingDX12).
2. [Disable static batching on your HDRP project](#ManualSetup-DisablingStaticBatching).
3. [Enable and configure ray tracing in your HDRP Asset](#ManualSetup-EnablingRayTracing).
4. [Ensure ray tracing resources are properly assigned](#ManualSetup-RayTracingResources).
5. [Make sure you have a local HDRP-config package that enables ray tracing](#ManualSetup-LocalHDRPConfig).
6. [Enable Screen Space Shadows and Screen Space Reflections in your HDRP Asset](#ManualSetup-EnableSSRandShadows).

<a name="ManualSetup-EnablingDX12"></a>

#### Upgrading to DirectX 12

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the Player tab.
2. Select the Other Settings fold-out, and in the Rendering section, disable Auto Graphics API for Windows. This exposes the Graphics APIs for Windows section.
3. In the Graphics APIs for Windows section, click the plus (+) button and select Direct3d12.
4. Unity uses Direct3d11 by default. To make Unity use Direct3d12, move Direct3d12 (Experimental) to the top of the list.
5. Apply your changes.

The Unity Editor window should now include the <DX12> tag in the title bar like so:

<a name="ManualSetup-DisablingStaticBatching"></a>

#### Disabling static batching

Next, you need to disable static batching, because HDRP does not support this feature with ray tracing in **Play Mode**. To do this:

1. Open the Project Settings window (menu:  **Edit > Project Settings**), then select the **Player** tab.
2. Select the **Other Settings** fold-out, then in the **Rendering** section, disable **Static Batching**.

<a name="ManualSetup-EnablingRayTracing"></a>

#### HDRP Asset configuration

Now that Unity is running in DirectX 12, and you have disabled [static batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), enable and configure ray tracing in your [HDRP Asset](HDRP-Asset.html). The previous steps configured Unity to support ray tracing; the following step enables it in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Rendering section, enable Realtime Ray Tracing. This triggers a recompilation, which makes ray tracing available in your HDRP Project.

<a name="ManualSetup-RayTracingResources"></a>

#### Ray tracing resources

To verify that HDRP has properly assigned ray tracing resources:

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the HDRP Default Settings tab.
2. Make sure there is a Render Pipeline Resources Asset assigned to the Render Pipeline Resources field.

<a name="ManualSetup-LocalHDRPConfig"></a>

#### Local HDRP config and ShaderConfig macro validation

The High Definition Render Pipeline (HDRP) uses a separate [package](https://docs.unity3d.com/Manual/Packages.html) to control the availability of some of its features. You can use it to configure certain settings in HDRP without changing the HDRP package itself.  First, create a local copy of this package in your Project and change the manifest.json file to point to it. For information on how to do this, see the [HDRP Config documentation](HDRP-Config-Package.html). Then, to enable ray tracing in HDRP, change the value of a macro in the ShaderConfig.cs.hlsl file.

Open **Packages > High Definition RP Config > Runtime > ShaderConfig.cs.hlsl** and set the **SHADEROPTIONS_RAYTRACING** macro to **1**.

<a name="ManualSetup-EnableSSRandShadows"></a>

#### Enable Screen Space Shadows and Screen Space Reflections in your HDRP Asset

HDRP uses ray tracing to replace certain rasterized effects. In order to use a ray tracing effect in your Project, you must first enable the rasterized version of the effect. The two effects that require you to modify your HDRP Asset  **Screen Space Shadows** and **Screen Space Reflections**. The following step enables them in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Lighting > Reflections** and enable **Screen Space Reflection**.
3. Go to **Lighting > Reflections** and enable **Screen Space Shadows**.
3. Set the value for **Maximum** to be the maximum number of screen space shadows you want to evaluate each frame. If there are more than this number of Lights in your Scene, HDRP only ray casts shadows for this number of them, then uses a shadow map for the rest.

Your HDRP Project now fully supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#FinalSetup).

<a name="FinalSetup"></a>

### Final setup

Now that your HDRP Project supports ray tracing, there are a few steps you must complete in order to actually use it in your Scene.

1. [Frame Settings validation](#FinalSetup-FrameSettings)

<a name="FinalSetup-FrameSettings"></a>

#### Frame Settings

To make HDRP calculates ray tracing effects for [Cameras](HDRP-Camera.html) in your Scene, make sure your Cameras use [Frame Settings](Frame-Settings.html) that have ray tracing enabled. You can enable ray tracing for all Cameras by default, or you can enable ray tracing for specific Cameras in your Scene.

To enable ray tracing by default:

1. Open the Project Settings window (menu:  **Edit > Project Settings**), then select the HDRP Default Settings tab.
2. Select Camera from the Default Frame Settings For drop-down.
3. In the **Rendering** section, enable **Ray Tracing**.

To enable ray tracing for a specific Camera:

1. Click on the Camera in the Scene or Hierarchy to view it in the Inspector.
2. In the **General** section, enable **Custom Frame Settings**. This exposes Frame Settings just for this Camera.
3. in the **Rendering** section, enable **Ray Tracing**.

<a name="RayTracingEffectsOverview"></a>

## Ray tracing effects overview

HDRP uses ray tracing to replace some of its screen space effects, shadowing techniques, and Mesh rendering techniques.

- [Ray-Traced Ambient Occlusion](Ray-Traced-Ambient-Occlusion.html) replaces [screen space ambient occlusion](Override-Ambient-Occlusion.html) with a more accurate, ray-traced, ambient occlusion technique that can use off screen data.
- [Ray-Traced Contact Shadows](Ray-Traced-Contact-Shadows.html) replaces [contact shadows](Override-Contact-Shadows.html) with a more accurate, ray-traced, contact shadow technique that can use off screen data.
- [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.html) is an alternative to Light Probes and lightmaps in HDRP.
- [Ray-Traced Reflections](Ray-Traced-Reflections.html) is a replacement for [screen space reflection](Override-Screen-Space-Reflection.html) that uses a ray-traced reflection technique that can use off-screen data.
- [Ray-Traced Shadows](Ray-Traced-Shadows.html) replace shadow maps for Directional, Point, and Area [Lights](Light-Component.html).
- [Recursive Ray Tracing](Ray-Tracing-Recursive-Rendering.html) replaces the rendering pipeline for Meshes. Meshes that use this feature cast refraction and reflection rays recursively.

## Ray tracing project

You can find a small ray tracing project that contains all the effects mention above here:
https://github.com/Unity-Technologies/SmallOfficeRayTracing
This Project is already set up with ray tracing support.

## Advice and supported feature of preview ray tracing

DX12 and DXR are currently in preview and are thus missing some functionnality. 
When you enable DX12, Unity shows this error message:
d3d12: generating mipmaps for array textures is not yet supported.

There is no support for ray tracing on other platform than DX12 for now.

HDRP ray tracing in Unity 2019.3 has the following limitations:
- Does not support deformers (skinning, blend shape, alembic, vertex animation).
- Does not support tessellation
- Does not support per pixel displacement (parallax occlusion mapping, height map, depth offset)
- Does not support VFX and Terrain.
- Does not support several of HDRP's Materials. This includes Hair, StackLit, Eye, and AxF Materials.
- Does not have correct culling for shadows. It uses frustum culling instead.
HDRP ray tracing in Unity 2020.1 and above add support for skinning, blend shapes and alembic.
