# Upgrading HDRP from Unity 2020.1 to Unity 2020.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2020.1 to 2020.2.

## Constant Buffer API

From Unity 2020.2, HDRP uses a new constant buffer API that allows it to set up uniforms during the frame and send them to the shader in a single transfer instead of multiple transfers. To do this, the global variables that were declared individually are now all within the `ShaderVariablesGlobal ` struct. The consequence of this is that its no longer possible to setup any of the global values individually using `CommandBuffer.SetVectorXXX()` or its related functions. Instead, to change a global variable, you need to update the struct in its entirety.

Currently, the only publicly accessible variables in the `ShaderVariablesGlobal` struct are camera related and only available within [Custom Passes](Custom-Pass.md) via the following functions:

* `RenderFromCamera()`
* `RenderDepthFromCamera()`
* `RenderNormalFromCamera()`
* `RenderTangentFromCamera()`


## Frame Settings

From Unity 2020.2, if you create a new [HDRP Asset](HDRP-Asset.md), the **MSAA Within Forward** Frame Setting is enabled by default.

## Lighting

From Unity 2020.2, if you disable the sky override used as the **Static Lighting Sky** in the **Lighting** window, the sky no longer affects the baked lighting. Previously, the sky affected the baked lighting even when it was disabled.

From Unity 2020.2, HDRP has removed the Cubemap Array for Point [Light](Light-Component.md) cookies and now uses octahedral projection with a regular 2D-Cookie atlas. This is to allow for a single path for light cookies and IES, but it may produce visual artifacts when using a low-resolution cube-cookie. For example, projecting pixel art data.

As the **Cubemap cookie atlas no longer exists**, it is possible that HDRP does not have enough space on the current 2D atlas for the cookies. If this is the case, HDRP displays an error in the Console window. To fix this, increase the size of the 2D cookie atlas. To do this:
Select your [HDRP Asset](HDRP-Asset.md).
In the Inspector, go to Lighting > Cookies.
In the 2D Atlas Size drop-down, select a larger cookie resolution.

## Shadows

From Unity 2020.2, it is no longer necessary to change the [HDRP Config package](HDRP-Config-Package.md) to set the [shadow filtering quality](HDRP-Asset.md#FilteringQualities) for deferred rendering. Instead, you can now change the filtering quality directly on the [HDRP Asset](HDRP-Asset.md#FilteringQualities). Note if you previously had not set the shadow filtering quality to **Medium** on the HDRP Asset, the automatic project upgrade process changes the shadow quality which means you may need to manually change it back to its original value.

HDRP now stores OnEnable and OnDemand shadows in a separate atlas and more API is available to handle them. For more information, see [Shadows in HDRP](Shadows-in-HDRP.md).

The shader function `SampleShadow_PCSS` now requires you to pass in an additional float2 parameter which contains the shadow atlas resolution in x and the inverse of the atlas resolution in y.

## Shader code

From Unity 2020.2, HDRP uses a new structure to output information from the LightLoop. It now uses a custom LightLoop struct instead of the `float3 diffuseLighting`, `float3 specularLighting` pair. This is to allow HDRP to export more information from the LightLoop in the future without breaking the API.

The following functions now pass this structure instead of the pair:

* LightLoop(), for both rasterization and raytracing.
* PostEvaluateBSDF()
* ApplyDebug()
* PostEvaluateBSDFDebugDisplay()

To upgrade an existing shader, for all the above functions:

1. Replace the declaration `float3 diffuseLighting; float3 specularLighting;` with `LightLoopOutput lightLoopOutput;` before the LightLoop call.
2. Replace the argument pair `out float3 diffuseLighting, out float3 specularLighting` with `out LightLoopOutput lightLoopOutput`.



The prototype for the function `ModifyBakedDiffuseLighting()` in the various materials has changed from:
`void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)`
to:
 `void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)`

There is also a new definition for `ModifyBakedDiffuseLighting()` that uses the former prototype definition and calls the new function prototype with the correct arguments. The purpose of this change it to prepare for future lighting features. To update your custom shaders, in addition of the prototype update, you must remove the following lines:
```
BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);
```

