# Upgrading HDRP from Unity 2020.1 to Unity 2020.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2020.1 to 2020.2.

## Constant Buffer API

From Unity 2020.2, HDRP is using a new constant buffer API that allow to setup uniform used during the Frame and sent to the shader in a single transfer instead of multiple one. The consequence is that it is no longer possible to setup any of the values declare in ShaderVariablesGlobal.cs individualy with cmd.SetVectorXXX() or related function. It is now required to update the value of ShaderVariablesGlobal to be able to update the values use in the shaders.

## FrameSettings

From Unity 2020.2, "MSAA Within Forward" Camera Frame Setting is enabled by default when new Render Pipeline asset is created.

## Lighting

From Unity 2020.2, when the Sky component affected to the volume profile used for Static Lighting Sky in Environment settings of the Lighting panel is disabled. It now don't affect the bake lighting. Previously the Sky was still affecting the bake lighting even if disabled.

## Shadows

From Unity 2020.2, it is not necessary to change the [HDRP Config package](HDRP-Config-Package.html) in order to set the [Shadows filtering quality](HDRP-Asset.html#FilteringQualities) for Deferred rendering. Instead the filtering quality can be simply set on the [HDRP Asset](HDRP-Asset.html#FilteringQualities) similarly to what was previously setting only the quality for Forward. Note that if previously the Shadow filtering quality wasn't setup on medium on the HDRP Asset you will experience a change of shadow quality as now it will be taken into account.

Starting from 2020.2, HDRP now stores OnEnable and OnDemand shadows in a separate atlas and more API is available to handle them. For more information, see [Shadows in HDRP](Shadows-in-HDRP.md).

From Unity 2020.2, the shader function `SampleShadow_PCSS` now requires you to pass in an additional float2 parameter which contains the shadow atlas resolution in x and the inverse of the atlas resolution in y.

## Shader code

A new structure is use to output the information of the LightLoop. LightLoop struct is use instead of the pair (float3 diffuseLighting, float3 specularLighting). This is to allow to export more information from the LightLoop in the future without breaking the API. The function LightLoop() - For rasterization and raytracing - PostEvaluateBSDF(), ApplyDebug() and PostEvaluateBSDFDebugDisplay now pass this structure instead of the Pair. The function LightLoop() will initialize this structure to zero. To upgrade existing shader, replace the declaration "float3 diffuseLighting; float3 specularLighting;" by "LightLoopOutput lightLoopOutput;" before call of LightLoop  and repalce the argument pair "out float3 diffuseLighting, out float3 specularLighting" by "out LightLoopOutput lightLoopOutput" in all the function mention.

The prototype of the function ModifyBakedDiffuseLighting() in the various material have change from "void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)" to "void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)". There is a new ModifyBakedDiffuseLighting using the former prototype added in the file BuiltinUtilities.hlsl which will call the new function prototype with the correct arguments. The purpose of this change it to prepare for future lighting features. To update code, in addition of the prototype update it is required to remove those line "BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData); PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);" as it is now perform by the common code from BuiltinUtilities.hlsl.
