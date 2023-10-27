# Upgrading from HDRP 11.x to 12.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade the following features of HDRP from 11.x to 12.x:

* HDRP Global Settings
* Materials
* Shader code
* Density Volumes
* Ambient Mode

For information about new, removed, or updated features, see [What's new in HDRP version 12 / Unity 2021.2](whats-new-12.md).

## HDRP Global Settings

HDRP 12.x introduces a new HDRP Global Settings Asset which saves all settings that are unrelated to which HDRP Asset is active.

The HDRP Asset assigned in the Graphics Settings is no longer the default Asset for HDRP.

The HDRP Asset and the HDRP Global Settings Asset cause a build error if they're not up to date when building. You can either update them automatically or manually:

* If the HDRP Asset and HDRP Global Settings Asset are both included in your Project Settings before you upgrade to 12.x, either under **Graphics** > **HDRP Global Settings** or **Quality** > **HDRP**, HDRP automatically upgrades them when you open the Unity Editor.
* To upgrade the assets manually:

    1. Include both assets in your Project Settings.
    2. Build your project from the command line:
        1. Open your command line application.
        2. Enter the command: `/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectPath <pathname>` (substitute `<pathname>` for the path to your project).

### Decals

To upgrade an existing project you must enable decals in your HDRP Asset. To do this:

1. Select your HDRP Asset to open it in the Inspector.
2. Go to **Rendering** > **Decal**.
3. Select **Enable**.

When you write a shader for a surface that receives decals, set HDRP to blend the normals that use the surface gradient framework.

## Materials

### Transparent Surface Type

In 12.x, HDRP changes the **Sorting Priority** value range from between -100 and 100, to between -50 and 50. If you use transparent materials (**Surface Type** set to **Transparent**) with a sorting priority lower than -50 or greater than 50, you must change this value in the Sorting Priority property to be within this new range. To do this:

1. Select your material to open it in the Inspector.
2. Go to **Surface Options** > **Sorting Priority**.
3. Change the value of the **Sorting Priority** property to be between -50 and 50.

HDRP doesn't clamp the Sorting Priority to the new range until you edit the Sorting Priority property.

## Shader Code

You need to upgrade the shader code for certain features in  HDRP version 12.x:

### Decals

If your project uses the following function:

`void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)`

Change it to:

`void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData, inout float3 normalTS)`.

This is because the behavior of the function has changed and the `inout float3 normalTS` parameter has been added to the function.

You can refer to `LitData.hlsl` and `LitDecalData.hlsl` for an example implementation.

### Tessellation

HDRP version 12.x changes tessellation shader code. You must change the following function:

`ApplyTessellationModification()`

from:

`void ApplyTessellationModification(VaryingsMeshToDS input, float3 normalWS, input float3 positionRWS)`

to:

`VaryingsMeshToDS ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters)`.

### Ambient Occlusion and Specular Occlusion

When you upgrade to 12.x, HDRP changes the algorithm that calculates specular occlusion from bent normals and ambient occlusion to improve visual results.

HDRP uses the new algorithm by default, so if you want to use the old algorithm, replace function calls to `GetSpecularOcclusionFromBentAO` with calls to `GetSpecularOcclusionFromBentAO_ConeCone`.

## Density Volumes

### Local Volumetric Fog

HDRP 12.X renames Density Volumes to **Local Volumetric Fog**.

If your scene uses Density Volumes, HDRP automatically changes the GameObjects to use the new component name  and keeps the property values you  set for the Density Volume.

However, if your project references a **Density Volume** through a C# script, you need to change the following line in your script to target the new Local Volumetric Fog component:

`DensityVolume component = gameObject.GetComponent<DensityVolume>();`

to

`LocalVolumetricFog component = gameObject.GetComponent<LocalVolumetricFog>();`

If you don’t change your code, a warning appears (`DensityVolume has been deprecated (UnityUpgradable) -> Local Volumetric Fog`) in the Console window. This warning might stop your Project from compiling in future versions of HDRP.

### 3DTexture

The sampling axis of **3DTexture** in the **Density Volume** component has been corrected to match Unity's axis convention. To accommodate this change:

1. Open your **3DTexture** in your image editing software.
2. Mirror the **3DTexture** along its **Z axis**. For example, if it's a sliced texture, invert the order of the slices.

## Ambient Mode

From 12.x, HDRP changes the default value of the **Ambient Mode** parameter in the **Visual Environment** volume component from **Static** to **Dynamic**. If your project previously used the Static default value, you can change it back by doing the following:

1. Go to **Edit** > **Project Settings** > **Graphics** > **HDRP Global Settings** > **Volume Profiles** > **Add Override**.
2. Select **Visual Environment**.
3. Enable **Ambient Mode** and set it to **Static**.

## Dynamic Resolution

HDRP 12.x doesn't include Bilinear and Lanczos upscale filters:
* If your project uses a Bilinear filter, HDRP migrates it to Catmull-Rom.
* If your project uses a Lanczos filter, HDRP migrates it to Contrast Adaptive Sharpening (CAS).

If your project relies on those filters you can use the TAA Upscale and FidelityFX Super Resolution 1.0 filters. For more information about these upscale filters, see [Dynamic Resolution](Dynamic-Resolution.md).
