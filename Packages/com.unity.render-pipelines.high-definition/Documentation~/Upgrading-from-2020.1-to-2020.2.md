# Upgrading from HDRP 8.x to 10.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade the following features of HDRP from 8.x to 10.x:

* Lighting
* Shadows
* Volumetric Fog
* Shader Code
* Decals
* Constant Buffer API
* Custom Pass API
* Diffusion Profiles

For information about new, removed, or updated features, see [What's new in HDRP version 10 / Unity 2020.2](whats-new-10.md).

## Lighting

### Light cookies

HDRP might not have enough space on the current 2D atlas for Light cookies. This is because the **Cubemap cookie atlas** no longer exists in HDRP 10.x. If this happens, HDRP displays an error in the Console window. To fix this, increase the size of the 2D cookie atlas:

1. Select your [HDRP Asset](HDRP-Asset.md).
2. In the Inspector, go to **Lighting** > **Cookies**.
3. In the **2D Atlas Size** drop-down, select a  higher maximum size value.

### Emissive color space

From HDRP 10.x, the **EmissiveColorLDR** property is in sRGB color space instead of linear RGB color space. This might cause visual differences when you upgrade to 10.x if your project uses the **UseEmissiveIntensity** property in one of the following Materials:

* Decal
* Lit
* LayeredLit
* Unlit

If your project sets **EmissiveColor** from a custom script, you must update your script manually. To do this, use the [`Mathf.LinearToGammaSpace`](https://docs.unity3d.com/ScriptReference/Mathf.LinearToGammaSpace.html) function to convert color components.

### Debug Lens Attenuation

From 10.x, **Debug Lens Attenuation** no longer exists in the **Render Pipeline Debug** window. To set the lens attenuation:

1. Go to **Edit** > **Project Settings** > **HDRP Default Settings** > **Lens Attenuation Mode**.
2. Set **Lens Attenuation Mode** to **Perfect Lens** or **Imperfect Lens**.

## Shadows

### Filtering quality

From 10.x, you don’t need to change the [HDRP Config package](configure-a-project-using-the-hdrp-config-package.md) to set the shadow filtering quality for deferred rendering. Instead, change the filtering quality in the [HDRP Asset](HDRP-Asset.md#filtering-quality):

1. Open your HDRP Asset in the Inspector window.
2. Go to **Lighting** > **Shadows** > **Filtering Quality**.

**Note**: The automatic project upgrade process changes the shadow filtering quality to **Medium**. If you previously used a different shadow filtering quality, you need to manually change it back.

### OnEnable and OnDemand shadows

HDRP now stores **OnEnable** and **OnDemand** shadows in a separate atlas and more API is available to handle them. For more information, see [Shadows in HDRP](Shadows-in-HDRP.md).

### Sample shadow API

From 10.x, when you use the shader function `SampleShadow_PCSS`, you need to pass in an additional float2 parameter which contains the shadow atlas resolution in x and the inverse of the atlas resolution in y.

## Volumetric Fog

When you upgrade a project to 10.x, the quality of volumetric fog in your Scene might degrade. This is because of the new volumetric fog control modes in version 10.x. To make volumetric fog look the same as it did in 8.x, use one of the following methods:

* Use manual **Fog Control Mode** properties.

  1. In the Inspector, go to the [Fog Volume Override](fog-volume-override-reference.md).
  2. To expose additional properties, set **Fog Control Mode** to **Manual**.
  3. Set these properties to the same values you used in 8.x.

* Use the new performance-oriented properties.
  1. In the Inspector, go to the [Fog Volume Override](fog-volume-override-reference.md).
  2. Set **Fog Control Mode** to **Balance**.
  3. Use the new performance-oriented properties to define the quality of the volumetric fog.

## Shader code

### LightLoop

From 10.x, HDRP uses a new structure to output information from the LightLoop. It uses a custom `LightLoop()` struct instead of the `float3 diffuseLighting`, `float3 specularLighting` pair. This allows HDRP to export more information from the `LightLoop()` without breaking the API.

The following functions now pass the `LightLoop()` struct instead of the pair:

* `LightLoop()`, for both rasterization and ray tracing.
* `PostEvaluateBSDF()`
* `ApplyDebug()`
* `PostEvaluateBSDFDebugDisplay()`

To upgrade an existing shader for all the above functions:

1. Replace the declaration `float3 diffuseLighting; float3 specularLighting;` with `LightLoopOutput lightLoopOutput;` before the `LightLoop()` call.
2. Replace the argument pair `out float3 diffuseLighting, out float3 specularLighting` with `out LightLoopOutput lightLoopOutput`.

### ModifyBakedDiffuseLighting()

HDRP 10.x includesa new definition for `ModifyBakedDiffuseLighting()`  to prepare for future lighting features. This definition uses the former prototype definition and calls the new function prototype with the correct arguments. To update your custom shaders, in addition to the prototype update, remove the following lines:

```c#
BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);
```

### Multi-compile for Depth Prepass and Motion vector passes

HDRP 10.x introduces a new multi-compile for Depth Prepass and Motion vector passes to support the Decal Layers feature. To use multi-compile add `#pragma multi_compile _ WRITE_DECAL_BUFFER` to the `DepthForwardOnly` and `MotionVectors` passes of your custom shaders.

### Shader decal properties

HDRP 10.x changes shader decal properties to match a new set of `AffectXXX` properties. HDRP’s Material upgrade process automatically upgrades all the Decal Materials when you open your project, but it doesn’t automatically upgrade procedurally generated Decal Materials. If your project includes any C# scripts that create or manipulate a Decal Material, you must update the scripts to use the new properties. To find the new properties, see [What's new in HDRP version 10 / Unity 2021.2](whats-new-10.md#shader-decal-properties).

### Decal application

From 10.x, HDRP removes the shader code that relates to `HTile` optimization. This includes the `HTileMask` member in `DecalSurfaceData` and the `DBufferHTileBit` structure and the associated flag. To update your custom shaders:

* Remove the `DBUFFERHTILEBIT_DIFFUSE`, `DBUFFERHTILEBIT_NORMAL` and `DBUFFERHTILEBIT_MASK` defines.
* Check if the weight of individual attributes is non-neutral. For example, in your `ApplyDecalToSurfaceData()` function, replace the following lines:

```c#
   if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
      (...)
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
      (...)
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
        (...) ComputeFresnel0((decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE) ? (...));
    }
```

with

```c#
   if (decalSurfaceData.baseColor.w  < 1.0)
    {
      (...)
    }

    if (decalSurfaceData.normalWS.w < 1.0)
    {
      (...)
    }

    if (decalSurfaceData.MAOSBlend.x < 1.0 || decalSurfaceData.MAOSBlend.y < 1.0 || decalSurfaceData.mask.w)
    {
        (...) ComputeFresnel0((decalSurfaceData.baseColor.w  < 1.0) ? (...));
    }
```

For an example of how to apply decals to a material, see the ApplyDecalToSurfaceData() function in the `LitDecalData.hlsl` file.

### Planar Reflection Probes

From 10.x, HDRP includes a new optimization for [Planar Reflection Probes](Planar-Reflection-Probe.md). When a shader samples a probe's environment map, it samples from mip level 0 if you enable the `LightData.roughReflections` parameter by giving it a value of 1.0. If you have any custom shaders in your scene, multiply the mip level by `lightData.roughReflections` to take this into account. For example, 10.x updates the call in the Lit shader to:

`float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness) * lightData.roughReflections, lightData.rangeCompressionFactorCompensation, posInput.positionNDC);`

## Decals

### Depth Prepass

From 10.x, decals no longer require a full Depth Prepass. HDRP only renders Materials with **Receive Decals** enabled during the Depth Prepass, unless you enable the ***Depth Prepass within Deferred*** property in **Frame Settings** to force a Depth Prepass.

### Decal Layers system

HDRP 10.x adds the Decal Layers system. This system uses the **Rendering Layer Mask** property from a Mesh Renderer and Terrain. In earlier HDRP versions, the default **Rendering Layer Mask** value doesn’t include any Decal Layer flags. This means that when you enable this feature in 10.x, meshes don’t receive decals until you configure the meshes correctly. To configure existing meshes, go to **Edit** > **Render Pipeline/HD Render Pipeline** > **Upgrade from Previous Version** > **Add HDRP Decal Layer Default to Loaded Mesh Renderers and Terrains**. After you enable this setting, HDRP automatically enables **Decal Layer Default** on any Mesh Renderers or Terrains you create.

## Constant Buffer API

From 10.x, HDRP uses a new constant buffer API that it can use to set up uniforms during the frame and send them to the shader in a single transfer instead of multiple transfers. This means you can’t set up any of the global values individually using `CommandBuffer.SetVectorXXX()` or its related functions. This is because the global variables that HDRP declares individually in previous HDRP versions are now in the `ShaderVariablesGlobal` struct.

The only variables you can access in the `ShaderVariablesGlobal` struct are related to the Camera, and they’re only available in a [Custom Pass](Custom-Pass.md) through the following functions:

* `RenderFromCamera()`
* `RenderDepthFromCamera()`
* `RenderNormalFromCamera()`
* `RenderTangentFromCamera()`

## Custom pass API

From HDRP 10.x, the `Execute` function only takes a `CustomPassContext` as its input:

`void Execute(CustomPassContext ctx)`

The `CustomPassContext` now contains all the parameters of the old `Execute` function and all the available Render Textures and a `MaterialPropertyBlock` unique to the custom pass instance.

You can use the `CustomPassContex` to access the new [`CustomPassUtils`](ScriptRef:UnityEngine.Rendering.HighDefinition.CustomPassUtils) class which contains functions to speed up the development of your custom passes. For information on custom pass utilities, see the [CustomPassUtils API documentation](ScriptRef:UnityEngine.Rendering.HighDefinition.CustomPassUtils).

To upgrade your custom pass, replace the original execute function prototype with the new one. To do this:

1. Remove `protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult) { ... }`
2. Replace it with `protected override void Execute(CustomPassContext ctx) { ... }`

## Diffusion Profiles

HDRP 10.x moves the diffusion profile list to **Edit** > **Project Settings** > **HDRP Default Settings**.

This might affect your project if you had multiple HDRP assets set up in **Quality Settings**. If one or more of your HDRP assets in **Quality Settings** have a different diffusion profile than the one assigned in the **Graphics Settings**, this change forgets the diffusion profile lists in the HDRP Asset.

To save diffusion profiles before you upgrade to HDRP 10.x, move the diffusion profiles you use in your project into the HDRP Asset assigned in the **Graphics Settings**.
