#if (SHADERPASS == SHADERPASS_FORWARD)
float4 VFXGetPixelOutputForward(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData)
{
    float3 diffuseLighting;
    float3 specularLighting;

    SurfaceData surfaceData;
    BuiltinData builtinData;
    BSDFData bsdfData;
    PreLightData preLightData;

    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    VFXGetHDRPLitData(surfaceData,builtinData,bsdfData,preLightData,i,normalWS,uvData,tileIndex);

    float3 posRWS = VFXGetPositionRWS(i);
    PositionInputs posInput = GetPositionInput(i.VFX_VARYING_POSCS.xy, _ScreenSize.zw, i.VFX_VARYING_POSCS.z, i.VFX_VARYING_POSCS.w, posRWS, tileIndex);

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
    LightLoop(GetWorldSpaceNormalizeViewDir(posRWS), posInput, preLightData, bsdfData, builtinData, featureFlags, diffuseLighting, specularLighting);

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
    int bufferSize = int(_DebugViewMaterialArray[0]);
    // Loop through the whole buffer
    // Works because GetSurfaceDataDebug will do nothing if the index is not a known one
    for (int index = 1; index <= bufferSize; index++)
    {
        int indexMaterialProperty = int(_DebugViewMaterialArray[index]);
        if (indexMaterialProperty != 0)
        {
            float3 result = float3(1.0, 0.0, 1.0);
            bool needLinearToSRGB = false;

            GetPropertiesDataDebug(indexMaterialProperty, result, needLinearToSRGB);
            //GetVaryingsDataDebug(indexMaterialProperty, input, result, needLinearToSRGB);
            GetBuiltinDataDebug(indexMaterialProperty, builtinData, result, needLinearToSRGB);
            GetSurfaceDataDebug(indexMaterialProperty, surfaceData, result, needLinearToSRGB);
            GetBSDFDataDebug(indexMaterialProperty, bsdfData, result, needLinearToSRGB);

            // TEMP!
            // For now, the final blit in the backbuffer performs an sRGB write
            // So in the meantime we apply the inverse transform to linear data to compensate.
            if (!needLinearToSRGB)
                result = SRGBToLinear(max(0, result));

            outColor = float4(result, 1.0);
        }
    }
#endif

    return outColor;
}
#else
#define VFXComputePixelOutputToGBuffer(i,normalWS,uvData,outGBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    ENCODE_INTO_GBUFFER(surfaceData, builtinData, i.VFX_VARYING_POSCS, outGBuffer); \
}

#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), i.VFX_VARYING_POSCS, outNormalBuffer); \
}

#endif
