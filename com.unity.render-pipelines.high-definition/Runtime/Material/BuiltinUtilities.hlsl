#ifndef __BUILTINUTILITIES_HLSL__
#define __BUILTINUTILITIES_HLSL__

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"

// Calculate motion vector in Clip space [-1..1]
float2 CalculateMotionVector(float4 positionCS, float4 previousPositionCS)
{
    // This test on define is required to remove warning of divide by 0 when initializing empty struct
    // TODO: Add forward opaque MRT case...
#if (SHADERPASS == SHADERPASS_MOTION_VECTORS) || defined(_WRITE_TRANSPARENT_MOTION_VECTOR)
    // Encode motion vector
    positionCS.xy = positionCS.xy / positionCS.w;
    previousPositionCS.xy = previousPositionCS.xy / previousPositionCS.w;

    float2 motionVec = (positionCS.xy - previousPositionCS.xy);
#if UNITY_UV_STARTS_AT_TOP
    motionVec.y = -motionVec.y;
#endif
    return motionVec;

#else
    return float2(0.0, 0.0);
#endif
}

// For builtinData we want to allow the user to overwrite default GI in the surface shader / shader graph.
// So we perform the following order of operation:
// 1. InitBuiltinData - Init bakeDiffuseLighting and backBakeDiffuseLighting
// 2. User can overwrite these value in the surface shader / shader graph
// 3. PostInitBuiltinData - Handle debug mode + allow the current lighting model to update the data with ModifyBakedDiffuseLighting

// This method initialize BuiltinData usual values and after update of builtinData by the caller must be follow by PostInitBuiltinData
void InitBuiltinData(PositionInputs posInput, float alpha, float3 normalWS, float3 backNormalWS, float4 texCoord1, float4 texCoord2,
                        out BuiltinData builtinData)
{
    ZERO_INITIALIZE(BuiltinData, builtinData);

    builtinData.opacity = alpha;

    // Use uniform directly - The float need to be cast to uint (as unity don't support to set a uint as uniform)
    builtinData.renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;
    
    // We only want to read the screen space buffer that holds the indirect diffuse signal if this is not a transparent surface
#if RAYTRACING_ENABLED && ((SHADERPASS == SHADERPASS_GBUFFER) || (SHADERPASS == SHADERPASS_FORWARD)) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if (_UseIndirectDiffuse == RAY_TRACED_INDIRECT_DIFFUSE_FLAG)
    {
        #if SHADERPASS == SHADERPASS_GBUFFER
        // Incase we shall be using raytraced indirect diffuse, we want to make sure to not add the GBuffer because that will be happening later in the pipeline
        builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
        #endif

        #if SHADERPASS == SHADERPASS_FORWARD
        builtinData.bakeDiffuseLighting = LOAD_TEXTURE2D_X(_IndirectDiffuseTexture, posInput.positionSS).xyz;
        builtinData.bakeDiffuseLighting *= GetInverseCurrentExposureMultiplier();
        #endif
    }
    else
#endif
    {
        // Sample lightmap/probevolume/lightprobe/volume proxy
        builtinData.bakeDiffuseLighting = SampleBakedGI(posInput, normalWS, builtinData.renderingLayers, texCoord1.xy, texCoord2.xy);
    }

    // We also sample the back lighting in case we have transmission. If not use this will be optimize out by the compiler
    // For now simply recall the function with inverted normal, the compiler should be able to optimize the lightmap case to not resample the directional lightmap
    // however it may not optimize the lightprobe case due to the proxy volume relying on dynamic if (to verify), not a problem for SH9, but a problem for proxy volume.
    // TODO: optimize more this code.
    builtinData.backBakeDiffuseLighting = SampleBakedGI(posInput, backNormalWS, builtinData.renderingLayers, texCoord1.xy, texCoord2.xy);

#ifdef SHADOWS_SHADOWMASK
    float4 shadowMask = SampleShadowMask(posInput.positionWS, texCoord1.xy);
    builtinData.shadowMask0 = shadowMask.x;
    builtinData.shadowMask1 = shadowMask.y;
    builtinData.shadowMask2 = shadowMask.z;
    builtinData.shadowMask3 = shadowMask.w;
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BuiltinData
void ApplyDebugToBuiltinData(inout BuiltinData builtinData)
{
#ifdef DEBUG_DISPLAY
    bool overrideEmissiveColor = _DebugLightingEmissiveColor.x != 0.0f &&
        any(builtinData.emissiveColor != 0.0f);

    if (overrideEmissiveColor)
    {
        float3 overrideEmissiveColor = _DebugLightingEmissiveColor.yzw;
        builtinData.emissiveColor = overrideEmissiveColor;

    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // The lighting in SH or lightmap is assume to contain bounced light only (i.e no direct lighting),
        // and is divide by PI (i.e Lambert is apply), so multiply by PI here to get back the illuminance
        builtinData.bakeDiffuseLighting *= PI; // don't take into account backBakeDiffuseLighting
    }

#endif
}

#ifdef MODIFY_BAKED_DIFFUSE_LIGHTING
void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)
{
    // Since this is called early at PostInitBuiltinData and we need some fields from bsdfData and preLightData,
    // we get the whole structures redundantly earlier here - compiler should optimize out everything.
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);
    ModifyBakedDiffuseLighting(V, posInput, preLightData, bsdfData, builtinData);
}
#endif

// InitBuiltinData must be call before calling PostInitBuiltinData
void PostInitBuiltinData(   float3 V, PositionInputs posInput, SurfaceData surfaceData,
                            inout BuiltinData builtinData)
{
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
    if (IsUninitializedGI(builtinData.bakeDiffuseLighting))
        return;
#else
    // Apply control from the indirect lighting volume settings - This is apply here so we don't affect emissive
    // color in case of lit deferred for example and avoid material to have to deal with it

    // Note: We only apply indirect multiplier for Material pass mode, for lightloop mode, the multiplier will be apply in lightloop
    builtinData.bakeDiffuseLighting *= _IndirectLightingMultiplier.x;
    builtinData.backBakeDiffuseLighting *= _IndirectLightingMultiplier.x;
#endif

#ifdef MODIFY_BAKED_DIFFUSE_LIGHTING

#ifdef DEBUG_DISPLAY
    // When the lux meter is enabled, we don't want the albedo of the material to modify the diffuse baked lighting
    if (_DebugLightingMode != DEBUGLIGHTINGMODE_LUX_METER)
#endif
        ModifyBakedDiffuseLighting(V, posInput, surfaceData, builtinData);

#endif
    ApplyDebugToBuiltinData(builtinData);
}

#endif //__BUILTINUTILITIES_HLSL__
