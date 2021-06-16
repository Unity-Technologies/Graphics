#if (SHADERPASS == SHADERPASS_FORWARD)


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

    LightLoopOutput lightLoopOutput;
    LightLoop(GetWorldSpaceNormalizeViewDir(posRWS), posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    // Alias
    float3 diffuseLighting = lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();

    float4 outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);
    outColor = EvaluateAtmosphericScattering(posInput, GetWorldSpaceNormalizeViewDir(posRWS), outColor);

#ifdef DEBUG_DISPLAY
    // Same code in ShaderPassForward.shader
    // Reminder: _DebugViewMaterialArray[i]
    //   i==0 -> the size used in the buffer
    //   i>0  -> the index used (0 value means nothing)
    // The index stored in this buffer could either be
    //   - a gBufferIndex (always stored in _DebugViewMaterialArray[1] as only one supported)
    //   - a property index which is different for each kind of material even if reflecting the same thing (see MaterialSharedProperty)
    int bufferSize = _DebugViewMaterialArray[0].x;
    if (bufferSize != 0)
    {
        float3 result = float3(1.0, 0.0, 1.0);
        bool needLinearToSRGB = false;
                
        // Loop through the whole buffer
        // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
        for (int index = 1; index <= bufferSize; index++)
        {
            int indexMaterialProperty = _DebugViewMaterialArray[index].x;
            if (indexMaterialProperty != 0)
            {
                GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
                //GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
                GetBuiltinDataDebug(indexMaterialProperty, builtinData, posInput, result, needLinearToSRGB);
                GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
                GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);
            }
        }
        
        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate, unless we output to AOVs.
        if (!needLinearToSRGB && _DebugAOVOutput == 0)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
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

float4 VFXGetPixelOutputForward(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData)
{
    SurfaceData surfaceData;
    BuiltinData builtinData;
    BSDFData bsdfData;
    PreLightData preLightData;

    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    VFXGetHDRPLitData(surfaceData,builtinData,bsdfData,preLightData,i,normalWS,uvData,tileIndex);

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
    uint2 tileIndex = uint2(0,0);
    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, tileIndex);
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

#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer); \
}

#endif
