# Getting started with ray tracing

The High Definition Render Pipeline (HDRP) includes ray tracing support from Unity 2019.3. Ray tracing hardware acceleration is a feature that allows you to access data that is not on screen. For example, you can use it to request position data, normal data, or lighting data, and then use this data to compute quantities that are hard to approximate using classic rasterization techniques. 

While film production uses ray tracing extensively, its resource intensity has limited its use to offline rendering for a long time. Now, with recent advances in GPU hardware, you can make use of ray tracing effect in real time.

This document covers:

- [Hardware requirements](#HardwareRequirements).
- [Integrate ray tracing into your HDRP Project](#Integration).
- [HDRP effects that use ray tracing](#RayTracingEffectsOverview).

<a name="HardwareRequirements"></a>

## Hardware requirements

Ray tracing hardware acceleration is only available on certain graphics cards. The graphics cards with full support are:

- NVIDIA Volta (Titan X)
- NVIDIA Turing (2060, 2070, 2080, and their TI variants)

NVIDIA also provides a ray tracing fallback for some previous generation graphics cards:

- NVIDIA Pascal (1060, 1070, 1080 and their TI variants)

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

Your HDRP Project now supports ray tracing. For information on how to set up a ray tracing environment in your Scene, see [final setup](#FinalSetup).

<a name="ManualSetup"></a>

### Manual setup

To set up ray tracing manually, you need to:

1. [Make your HDRP project use DirectX 12](#ManualSetup-EnablingDX12).
2. [Enable and configure ray tracing in your HDRP Asset](#ManualSetup-EnablingRayTracing).
3. [Ensure ray tracing resources are properly assigned](#ManualSetup-RayTracingResources).

<a name="ManualSetup-EnablingDX12"></a>

#### Upgrading to DirectX 12

1. Open the Project Settings window (menu: Edit > Project Settings), then select the Player tab.
2. Select the Other Settings fold-out, and in the Rendering section, disable Auto Graphics API for Windows. This exposes the Graphics APIs for Windows section.
3. In the Graphics APIs for Windows section, click the plus (+) button and select Direct3d12.
4. Unity uses Direct3d11 by default. To make Unity use Direct3d12, move Direct3d12 (Experimental) to the top of the list.
5. Apply your changes.

The Unity Editor window should now include the <DX12> tag in the title bar like so:

<a name="ManualSetup-EnablingRayTracing"></a>

#### HDRP Asset configuration

Now that Unity is running in DirectX 12, and you have disabled static batching, enable and configure ray tracing in your [HDRP Asset](HDRP-Asset.html). The previous steps configured Unity to support ray tracing; the following step enables it in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Rendering section, enable Realtime Ray Tracing. This triggers a recompilation, which makes ray tracing available in your HDRP Project.
3. You can now configure ray tracing to suit your application. Click the Ray Tracing Tier drop-down and select the tier that matches your use case. See the table below for information on what each tier supports.

<a name="TierTable"></a>

| Tier       | Description                                                  |
| ---------- | ------------------------------------------------------------ |
| **Tier 1** | Balances performance with quality. Use this tier for games and other high-frame rate applications. |
| **Tier 2** | A ray tracing implementation that is significantly more resource-intensive than Tier 1. This allows for effects with higher image quality. Use this tier for automotive, production, or graphics demos. |
| **Tier 3** | This tier enables the path tracer which sends rays from the Camera. When a ray hits a reflective or refractive surface, it recurses the process until it reaches a light source. The series of rays from the Camera to the Light forms a "path". This is the most resource intensive ray tracing method in HDRP. Use this tier for automotive, production, or graphics demos. |

<a name="ManualSetup-RayTracingResources"></a>

#### Ray tracing resources

To verify that HDRP has properly assigned ray tracing resources:

1. Open the Project Settings window (menu: Edit > Project Settings), then select the HDRP Default Settings tab.
2. Make sure there is a Render Pipeline Resources Asset assigned to the Render Pipeline Resources field.

Your HDRP Project now supports ray tracing. For information on how to set up a ray tracing environment in your Scene, see [final setup](#FinalSetup).

<a name="FinalSetup"></a>

### Final setup

Now that your HDRP Project supports ray tracing, there are a few steps you must complete in order to actually use it in your Scene.

1. [Disable static batching](#FinalSetup-DisablingStaticBatching)
2. [ShaderConfig macro validation](#FinalSetup-Macros)
3. [Frame Settings validation](#FinalSetup-FrameSettings)
4. [Initialize a Ray Tracing Environment](#FinalSetup-RayTracingEnvironment)

<a name="FinalSetup-DisablingStaticBatching"></a>

#### Disabling static batching

Next, you need to disable static batching, because HDRP does not support this feature with ray tracing in Play mode. To do this:

1. Open the Project Settings window (menu: Edit > Project Settings), then select the Player tab.
2. Select the Other Settings fold-out, then in the Rendering section, disable Static Batching.

<a name="FinalSetup-Macros"></a>

#### ShaderConfig macro validation

HDRP has the package com.unity.render-pipelines.high-definition-config as a dependency. You can use it to configure certain settings in HDRP without changing the HDRP package itself. To enable ray tracing in HDRP, you need to change the value of a macro in the ShaderConfig.cs.hlsl file.

Open Packages > High Definition RP Config > Runtime > ShaderConfig.cs.hlsl and set the SHADEROPTIONS_RAYTRACING macro to 1.

<a name="FinalSetup-FrameSettings"></a>

#### Frame Settings

To make HDRP calculates ray tracing effects for [Cameras](HDRP-Camera.html) in your Scene, make sure your Cameras use [Frame Settings](Frame-Settings) that have ray tracing enabled. You can enable ray tracing for all Cameras by default, or you can enable ray tracing for specific Cameras in your Scene.

To enable ray tracing by default:

1. Open the Project Settings window (menu: Edit > Project Settings), then select the HDRP Default Settings tab.
2. Select Camera from the Default Frame Settings For drop-down.
3. In the Rendering section, enable Ray Tracing.

To enable ray tracing for a specific Camera:

1. Click on the Camera in the Scene or Hierarchy to view it in the Inspector.
2. In the General section, enable Custom Frame Settings. This exposes Frame Settings just for this Camera.
3. in the Rendering section, enable Ray Tracing.

<a name="FinalSetup-RayTracingEnvironment"></a>

#### Initializing a Ray Tracing Environment

Finally, to use ray tracing in your Scene, create a Ray Tracing Environment. To do this, select GameObject > Rendering > Ray Tracing Environment.

For information about what the Ray Tracing Environment does, and what each of its properties affects, see [Ray Tracing Environment](Ray-Tracing-Environment-Component.html).

<a name="RayTracingEffectsOverview"></a>

## Ray tracing effects overview

HDRP uses ray tracing to replace some of its screen space effects, shadowing techniques, and Mesh rendering techniques.

- [Ray-Traced Ambient Occlusion](Ray-Traced-Ambient-Occlusion.html) replaces [screen space ambient occlusion](Override-Ambient-Occlusion.html) with a more accurate, ray-traced, ambient occlusion technique that can use off screen data.
- [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.html) is an alternative to Light Probes and lightmaps in HDRP. It includes a different set of properties for [Tier 1](#TierTable) and [Tier 2](#TierTable) ray tracing.
- [Ray-Traced Reflections](Ray-Traced-Reflections.html) is a replacement for [screen space reflection](Override-Screen-Space-Reflection) that uses a ray-traced reflection technique that can use off-screen data.
- [Ray-Traced Shadows](Ray-Traced-Shadows.html) replace shadow maps for Directional, Point, and Area [Lights](Light-Component.html).
- [Recursive Ray Tracing](Ray-Tracing-Recursive-Rendering.html) replaces the rendering pipeline for Meshes. Meshes that use this feature cast refraction and reflection ray recursively.