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

float4 ApplyBlendMode(float3 diffuseLighting, float3 specularLighting, float opacity)
{
    return float4(diffuseLighting + specularLighting, opacity);
}

// ref: http://advances.realtimerendering.com/other/2016/naughty_dog/index.html
// Lit transparent object should have reflection and tramission.
// Transmission when not using "rough refraction mode" (with fetch in preblured background) is handled with blend mode.
// However reflection should not be affected by blend mode. For example a glass should still display reflection and not lose the highlight when blend
// This is the purpose of following function, "Cancel" the blend mode effect on the specular lighting but not on the diffuse lighting
float4 ApplyBlendModeAccurateLighting(float3 diffuseLighting, float3 specularLighting, float opacity)
{
#ifdef _BLENDMODE_ALPHA
    return float4(diffuseLighting + (specularLighting / max(opacity, 0.01)), opacity);
#elif defined(_BLENDMODE_ADD) || defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting * opacity + specularLighting, opacity);
#else
    return ApplyBlendMode(diffuseLighting, specularLighting, opacity);
#endif
}

void Frag(PackedVaryingsToPS packedInput,
          out float4 outColor : SV_Target0
      #ifdef _DEPTHOFFSET_ON
          , out float outputDepth : SV_Depth
      #endif
          )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw, uint2(input.unPositionSS.xy) / GetTileSize());
    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    outColor = float4(0.0, 0.0, 0.0, 0.0);

    // We need to skip lighting when doing debug pass because the debug pass is done before lighting so some buffers may not be properly initialized potentially causing crashes on PS4.
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode != DEBUGLIGHTINGMODE_NONE)
#endif
    {
        uint featureFlags = 0xFFFFFFFF;
        float3 diffuseLighting;
        float3 specularLighting;
        float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);
        LightLoop(V, posInput, preLightData, bsdfData, bakeDiffuseLighting, featureFlags, diffuseLighting, specularLighting);

        outColor = ApplyBlendModeAccurateLighting(diffuseLighting, specularLighting, builtinData.opacity);
        outColor = EvaluateAtmosphericScattering(posInput, outColor);
    }

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.depthRaw;
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
        GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB); // TODO: This required to initialize all field from BSDFData...

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate.
        if (!needLinearToSRGB)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
    }
#endif
}
