#if SHADERPASS != SHADERPASS_FORWARD
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/LineRendering/ShaderPass/LineRenderingOffscreenShading.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

ByteAddressBuffer _CounterBuffer;


PackedVaryingsType Vert(uint vertexID : SV_VertexID)
{
    VaryingsType varyingsType;

    ZERO_INITIALIZE(VaryingsType, varyingsType);
    {
        int idxs[] = { 0, 1, 2, 2, 1, 3 };
        int vertId = idxs[vertexID % 6];
        float2 uvScreen = float2(vertId & 0x1, (vertId >> 1) & 0x1);

        int startRow = 0;
        int endRow = startRow + _ShadingSampleVisibilityCount / OffscreenAtlasWidth;
        float2 minMaxRowsUV = 1.0 - (float2(startRow, endRow) / float(OffscreenAtlasHeight));
        uvScreen.y = clamp(uvScreen.y, minMaxRowsUV.y - 2.0 / (float)OffscreenAtlasHeight, minMaxRowsUV.x);
        varyingsType.vmesh.positionCS = float4(uvScreen * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
    }

    return PackVaryingsType(varyingsType);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    OffscreenShadingFillFragInputs(packedInput.vmesh.positionCS.xy, input);

    // We need to readapt the SS position as our screen space positions are for a low res buffer, but we try to access a full res buffer.
    input.positionSS.xy = _OffScreenRendering > 0 ? (uint2)round(input.positionSS.xy * _OffScreenDownsampleFactor) : input.positionSS.xy;

    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    outColor = float4(0.0, 0.0, 0.0, 0.0);

    // We need to skip lighting when doing debug pass because the debug pass is done before lighting so some buffers may not be properly initialized potentially causing crashes on PS4.

#ifdef DEBUG_DISPLAY
    bool viewMaterial = GetMaterialDebugColor(outColor, input, builtinData, posInput, surfaceData, bsdfData);

    if (!viewMaterial)
    {
        if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR || _DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
        {
            float3 result = float3(0.0, 0.0, 0.0);

            GetPBRValidatorDebug(surfaceData, result);

            outColor = float4(result, 1.0f);
        }
        else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
        {
            float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
            outColor = result;
        }
        else
#endif
        {
            uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;

            LightLoopOutput lightLoopOutput;
            LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

            // Alias
            float3 diffuseLighting = lightLoopOutput.diffuseLighting;
            float3 specularLighting = lightLoopOutput.specularLighting;

            diffuseLighting *= GetCurrentExposureMultiplier();
            specularLighting *= GetCurrentExposureMultiplier();

            outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);

            #ifdef _ENABLE_FOG_ON_TRANSPARENT
            {
                float3 volColor, volOpacity;
                EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity); // Premultiplied alpha
                outColor.rgb = outColor.rgb * (1 - volOpacity) + volColor;
            }
            #endif
        }

#ifdef DEBUG_DISPLAY
    }
#endif
}
