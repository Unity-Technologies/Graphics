#if SHADERPASS != SHADERPASS_FORWARD
#error SHADERPASS_is_not_correctly_define
#endif

#include "VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "TessellationShare.hlsl"

#endif // TESSELLATION_ON

void Frag(PackedVaryingsToPS packedInput,
        #ifdef OUTPUT_SPLIT_LIGHTING
            out float4 outColor : SV_Target0,  // outSpecularLighting
            out float4 outDiffuseLighting : SV_Target1,
            OUTPUT_SSSBUFFER(outSSSBuffer)
        #else
            out float4 outColor : SV_Target0
        #endif
        #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
        #endif
          )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionWS.xyz, uint2(input.positionSS.xy) / GetTileSize());

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
#else
    float3 V = 0; // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    outColor = float4(0.0, 0.0, 0.0, 0.0);

    // We need to skip lighting when doing debug pass because the debug pass is done before lighting so some buffers may not be properly initialized potentially causing crashes on PS4.
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode != DEBUGLIGHTINGMODE_NONE || _DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
#endif
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
#else
        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
#endif
        float3 diffuseLighting;
        float3 specularLighting;
        BakeLightingData bakeLightingData;
        bakeLightingData.bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);
#ifdef SHADOWS_SHADOWMASK
        bakeLightingData.bakeShadowMask = float4(builtinData.shadowMask0, builtinData.shadowMask1, builtinData.shadowMask2, builtinData.shadowMask3);
#endif
        LightLoop(V, posInput, preLightData, bsdfData, bakeLightingData, featureFlags, diffuseLighting, specularLighting);

#ifdef OUTPUT_SPLIT_LIGHTING
        if (_EnableSubsurfaceScattering != 0 && PixelHasSubsurfaceScattering(bsdfData))
        {
            outColor = float4(specularLighting, 1.0);
            outDiffuseLighting = float4(TagLightingForSSS(diffuseLighting), 1.0);
        }
        else
        {
            outColor = float4(diffuseLighting + specularLighting, 1.0);
            outDiffuseLighting = 0;
        }
        ENCODE_INTO_SSSBUFFER(surfaceData, posInput.positionSS, outSSSBuffer);
#else
        outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);
        outColor = EvaluateAtmosphericScattering(posInput, outColor);
#endif
    }

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif

#ifdef DEBUG_DISPLAY
    // Same code in ShaderPassForwardUnlit.shader
    if (_DebugViewMaterial != 0)
    {
        float3 result = float3(1.0, 0.0, 1.0);

        bool needLinearToSRGB = false;

        GetPropertiesDataDebug(_DebugViewMaterial, result, needLinearToSRGB);
        GetVaryingsDataDebug(_DebugViewMaterial, input, result, needLinearToSRGB);
        GetBuiltinDataDebug(_DebugViewMaterial, builtinData, result, needLinearToSRGB);
        GetSurfaceDataDebug(_DebugViewMaterial, surfaceData, result, needLinearToSRGB);
        GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB);

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate.
        if (!needLinearToSRGB)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
    }
#endif
}
