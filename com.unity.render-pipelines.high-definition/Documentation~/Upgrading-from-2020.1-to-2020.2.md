# Upgrading HDRP from Unity 2020.1 to Unity 2020.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2020.1 to 2020.2.

## Shadows

From Unity 2020.2, it is not necessary to change the [HDRP Config package](HDRP-Config-Package.html) in order to set the [Shadows filtering quality](HDRP-Asset.html#FilteringQualities) for Deferred rendering. Instead the filtering quality can be simply set on the [HDRP Asset](HDRP-Asset.html#FilteringQualities) similarly to what was previously setting only the quality for Forward. Note that if previously the Shadow filtering quality wasn't setup on medium on the HDRP Asset you will experience a change of shadow quality as now it will be taken into account.

Starting from 2020.2, HDRP now stores OnEnable and OnDemand shadows in a separate atlas and more API is available to handle them. For more information, see [Shadows in HDRP](Shadows-in-HDRP.md).

From Unity 2020.2, the shader function `SampleShadow_PCSS` now requires you to pass in an additional float2 parameter which contains the shadow atlas resolution in x and the inverse of the atlas resolution in y.

## Shader code

A new structure is use to output the information of the LightLoop. LightLoop struct is use instead of the pair (float3 diffuseLighting, float3 specularLighting). This is to allow to export more information from the LightLoop in the future without breaking the API. The function LightLoop() - For rasterization and raytracing - PostEvaluateBSDF(), ApplyDebug() and PostEvaluateBSDFDebugDisplay now pass this structure instead of the Pair. The function LightLoop() will initialize this structure to zero. To upgrade existing shader, replace the declaration "float3 diffuseLighting; float3 specularLighting;" by "LightLoopOutput lightLoopOutput;" before call of LightLoop  and repalce the argument pair "out float3 diffuseLighting, out float3 specularLighting" by "out LightLoopOutput lightLoopOutput" in all the function mention.

The prototype of the function ModifyBakedDiffuseLighting() in the various material have change from "void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)" to "void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)". There is a new ModifyBakedDiffuseLighting using the former prototype added in the file BuiltinUtilities.hlsl which will call the new function prototype with the correct arguments. The purpose of this change it to prepare for future lighting features. To update code, in addition of the prototype update it is required to remove those line "BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData); PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);" as it is now perform by the common code from BuiltinUtilities.hlsl.
