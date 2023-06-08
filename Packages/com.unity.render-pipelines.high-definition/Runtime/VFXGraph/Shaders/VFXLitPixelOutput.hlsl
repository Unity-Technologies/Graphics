#if (SHADERPASS == SHADERPASS_FORWARD)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

float4 VFXCalcPixelOutputForward(const SurfaceData surfaceData, const BuiltinData builtinData, const PreLightData preLightData, BSDFData bsdfData, const PositionInputs posInput, float3 posRWS)
{
    #if IS_OPAQUE_PARTICLE
    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
    #elif USE_ONLY_AMBIENT_LIGHTING
    uint featureFlags = LIGHTFEATUREFLAGS_ENV;
    #else
    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
    #endif

    #if HDRP_MATERIAL_TYPE_SIMPLE
    // If we are in the simple mode, we do not support area lights and some env lights
    featureFlags &= ~(LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_AREA);

    // If env light are not explicitly supported, skip them
    #ifndef HDRP_ENABLE_ENV_LIGHT
    featureFlags &= ~(LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SKY);
    #endif

    #endif

    #if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
    featureFlags &= ~(LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_SSREFLECTION);
    #endif

    float4 outColor = float4(0,0,0,0);
    #if IS_TRANSPARENT_PARTICLE
    if(_EnableBlendModePreserveSpecularLighting || builtinData.opacity > 0)
        #endif
    {
        LightLoopOutput lightLoopOutput;
        LightLoop(GetWorldSpaceNormalizeViewDir(posRWS), posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

        // Alias
        float3 diffuseLighting = lightLoopOutput.diffuseLighting;
        float3 specularLighting = lightLoopOutput.specularLighting;

        diffuseLighting *= GetCurrentExposureMultiplier();
        specularLighting *= GetCurrentExposureMultiplier();

        outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);
        outColor = EvaluateAtmosphericScattering(posInput, GetWorldSpaceNormalizeViewDir(posRWS), outColor);
    }

#ifdef DEBUG_DISPLAY
    float4 debugColor = 0;
    if (GetMaterialDebugColor(debugColor, builtinData, posInput, surfaceData, bsdfData))
    {
        outColor = debugColor;
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
        outColor = result;
    }
#endif

    return outColor;
}

#ifndef VFX_SHADERGRAPH

float4 VFXGetPixelOutputForward(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData, bool frontFace)
{
    SurfaceData surfaceData;
    BuiltinData builtinData;
    BSDFData bsdfData;
    PreLightData preLightData;

    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    VFXGetHDRPLitData(surfaceData,builtinData,bsdfData,preLightData,i,normalWS,uvData, frontFace, tileIndex);

    float3 posRWS = VFXGetPositionRWS(i);
    PositionInputs posInput = GetPositionInput(i.VFX_VARYING_POSCS.xy, _ScreenSize.zw, i.VFX_VARYING_POSCS.z, i.VFX_VARYING_POSCS.w, posRWS, tileIndex);

    return VFXCalcPixelOutputForward(surfaceData,builtinData,preLightData, bsdfData, posInput, posRWS);
}


#else


float4 VFXGetPixelOutputForwardShaderGraph(const VFX_VARYING_PS_INPUTS i, const SurfaceData surfaceData, float3 emissiveColor, float opacity)
{
    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, tileIndex);

    PreLightData preLightData = (PreLightData)0;
    BSDFData bsdfData = (BSDFData)0;
    bsdfData = ConvertSurfaceDataToBSDFData(posSS.xy, surfaceData);

    preLightData = GetPreLightData(GetWorldSpaceNormalizeViewDir(posRWS),posInput,bsdfData);
    preLightData.diffuseFGD = 1.0f;

    BuiltinData builtinData;
    InitBuiltinData(posInput, opacity, surfaceData.normalWS, -surfaceData.normalWS, (float4)0, (float4)0, builtinData);
    builtinData.emissiveColor = emissiveColor;
    PostInitBuiltinData(GetWorldSpaceNormalizeViewDir(posInput.positionWS), posInput,surfaceData, builtinData);

    return VFXCalcPixelOutputForward(surfaceData, builtinData, preLightData, bsdfData, posInput, posRWS);
}
#endif
#else


void VFXSetupBuiltinForGBuffer(const VFX_VARYING_PS_INPUTS i, const SurfaceData surface, float3 emissiveColor, float opacity, out BuiltinData builtin)
{
    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS);
    InitBuiltinData(posInput, opacity, surface.normalWS, -surface.normalWS, (float4)0, (float4)0, builtin);
    builtin.emissiveColor = emissiveColor;
    PostInitBuiltinData(GetWorldSpaceNormalizeViewDir(posInput.positionWS), posInput, surface, builtin);
}

#define VFXComputePixelOutputToGBuffer(i,normalWS,uvData,outGBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    ENCODE_INTO_GBUFFER(surfaceData, builtinData, i.VFX_VARYING_POSCS.xy, outGBuffer); \
}

#ifdef VFXComputePixelOutputToNormalBuffer
#undef VFXComputePixelOutputToNormalBuffer
#endif
#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer); \
}

#endif
