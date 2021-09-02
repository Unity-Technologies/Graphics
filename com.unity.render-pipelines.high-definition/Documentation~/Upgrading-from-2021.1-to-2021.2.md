# Upgrading HDRP from 2021.1 to 2021.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 11.x to 12.x.

## Shader code

The following shader code behaviour has changed slightly for HDRP version 12.x

### Ambient occlusion for probe volume global illumination

From HDRP2021.2, when you apply ambient occlusion (AO) on a deferred Material to probe volume global illumination (GI) you need to define the following in the Material script:

* A `HAS_PAYLOAD_WITH_UNINIT_GI` constant
* A `float GetUninitializedGIPayload(SurfaceData surfaceData)` function that returns the AO factor that you want to apply.

You don't need to do anything differently for forward only Materials.

### New shader pass

HDRP 2021.2 includes the `ForwardEmissiveForDeferred` shader pass and the associated `SHADERPASS_FORWARD_EMISSIVE_FOR_DEFERRED` define for Materials that have a GBuffer pass. You can see this new pass in `Lit.shader`.

When you use the Deferred Lit shader mode, Unity uses `ForwardEmissiveForDeferred` to render the emissive contribution of a Material in a separate forward pass. Otherwise, Unity ignores `ForwardEmissiveForDeferred`.

### Decals

Decals in HDRP have changed in the following ways:

* HDRP Decals can now use a method based on surface gradient to disturb the normal of the affected GameObjects. To use this feature, enable it in the HDRP asset.

* When you create a custom decal shader, the accumulated normal value stored in the DBuffer now represents the surface gradient instead of the tangent space normal. You can find an example of this implementation in `DecalUtilities.hlsl`.

* When you write a shader for a surface that recieves decals, the normals should now be blended using the surface gradient framework. The prototype for the function `ApplyDecalToSurfaceData` has changed from: `void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)` to `void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData, inout float3 normalTS)`. You can refer to `LitData.hlsl` and `LitDecalData.hlsl` for an example implementation.

### Tessellation
HDRP 2021.2 has various tessellation shader code to enable tessellation support in [Master Stacks](master-stack-hdrp.md).  has changed the tessellation shader code in the following ways:

* The function `GetTessellationFactors()` has moved from `LitDataMeshModification.hlsl` to `TessellationShare.hlsl`. It calls a new function, `GetTessellationFactor()`, that is in the`LitDataMeshModification.hlsl`file.
* The prototype of `ApplyTessellationModification()` function has changed from:<br/> `void ApplyTessellationModification(VaryingsMeshToDS input, float3 normalWS, inout float3 positionRWS)`<br/>to:<br/>`VaryingsMeshToDS ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters)`.
* HDRP has improved support of motion vectors for tessellation. Only `previousPositionRWS` is part of the varyings. HDRP also added the `MotionVectorTessellation()` function. For more information, see the `MotionVectorVertexShaderCommon.hlsl` file.
* HDRP now evaluates the `tessellationFactor` in the vertex shader and passes it to the hull shader as an interpolator. For more information, see the `VaryingMesh.hlsl` and `VertMesh.hlsl` files.

## Density Volumes

Density Volumes are now known as **Local Volumetric Fog**.

If a Scene uses Density Volumes, HDRP automatically changes the GameObjects to use the new component name, with all the same properties set for the Density Volume.

However, if you reference a **Density Volume** through a C# script, a warning appears (**DensityVolume has been deprecated (UnityUpgradable) -> Local Volumetric Fog**) in the Console window. This warning may stop your Project from compiling in future versions of HDRP. To resolve this, change your code to target the new component.

## ClearFlag

HDRP 2021.2 includes the new `ClearFlag.Stencil` function. Use this to clear all flags from a stencil.

From HDRP 2021.2,  `ClearFlag.Depth` does not clear stencils.

## HDRP Global Settings

HDRP 2021.2 introduces a new HDRP Global Settings Asset which saves all settings that are unrelated to which HDRP Asset is active.

The HDRP Asset assigned in the Graphics Settings is no longer the default Asset for HDRP.

To ensure your build uses up to date data, the The HDRP Asset and the HDRP Global Settings Asset can cause a build error if they are not up to date when building.

If both assets are already included in your project (in QualitySettings or in GraphicsSettings), HDRP automatically upgrades the when you open the Unity Editor.

To upgrade these assets manually, include them in your project and build from the command line.

The Runtime Debug Display toggle has moved from the HDRP Asset to HDRP Global Settings Asset. This toggle uses the currently active HDRP Asset as the source.

## Materials

### Transparent Surface Type

From 2021.2, the range for **Sorting Priority** values has decreased from between -100 and 100, to between -50 and 50.

If you used transparent materials (**Surface Type** set to **Transparent**) with a sorting priority lower than -50 or greater than 50, you must remap them to within the new range.

 HDRP does not clamp the Sorting Priority to the new range until you edit the Sorting Priority property.

## RendererList API

From 2021.2, HDRP includes an updated `RendererList` API in the `UnityEngine.Rendering.RendererUtils` namespace. This API performs fewer operations than the previous version of the `RendererList` API when it submits the RendererList for drawing. You can use this new version to query if the list of visible objects is empty.

The previous version of the API in the `UnityEngine.Experimental.Rendering` namespace is still available for compatibility purposes but is now deprecated.

When the **Dynamic Render Pass Culling** option is enabled in the HDRP Global Settings, HDRP will use the new API to dynamically skip certain drawing passes based on the type of currently visible objects. For example if no objects with distortion are drawn, the Render Graph passes that draw the distortion effect (and their dependencies - like the color pyramid generation) will be skipped.

## Dynamic Resolution

From 2021.2, Bilinear and Lanczos upscale filters have been removed as they are mostly redundant with other better options. A project using Bilinear filter will migrate to use Catmull-Rom, if using Lanczos it will migrate to Contrast Adaptive Sharpening (CAS).  If your project was relying on those filters also consider the newly added filters TAA Upscale and FidelityFX Super Resolution 1.0.
