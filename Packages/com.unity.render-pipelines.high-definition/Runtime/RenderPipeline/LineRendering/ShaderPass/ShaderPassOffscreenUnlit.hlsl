#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
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
        int endRow = startRow + (uint)_ShadingSampleVisibilityCount / OffscreenAtlasWidth;
        float2 minMaxRowsUV = 1.0 - (float2(startRow, endRow) / float(OffscreenAtlasHeight));
        uvScreen.y = clamp(uvScreen.y, minMaxRowsUV.y - 2.0 / (float)OffscreenAtlasHeight, minMaxRowsUV.x);
        varyingsType.vmesh.positionCS = float4(uvScreen * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
    }

    return PackVaryingsType(varyingsType);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

float GetDeExposureMultiplier()
{
#if defined(DISABLE_UNLIT_DEEXPOSURE)
    return 1.0;
#else
    return _DeExposureMultiplier;
#endif
}

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    OffscreenShadingFillFragInputs(packedInput.vmesh.positionCS.xy, input);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Not lit here (but emissive is allowed)
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    // If this is a shadow matte, then we want the AO to affect the base color (the AO being correct if the surface is flagged shadow matte).
#if defined(_ENABLE_SHADOW_MATTE)
    bsdfData.color *= GetScreenSpaceAmbientOcclusion(input.positionSS.xy);
#endif

#ifdef DEBUG_DISPLAY
    // Handle debug lighting mode here as there is no lightloop for unlit.
    // For unlit we let all unlit object appear
    if (_DebugLightingMode >= DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING && _DebugLightingMode <= DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
    {
        if (_DebugLightingMode != DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
        {
            builtinData.emissiveColor = 0.0;
        }
        else
        {
            bsdfData.color = 0.0;
        }
    }
#endif

    // Note: we must not access bsdfData in shader pass, but for unlit we make an exception and assume it should have a color field
    float4 outResult = ApplyBlendMode(bsdfData.color * GetDeExposureMultiplier() + builtinData.emissiveColor * GetCurrentExposureMultiplier(), builtinData.opacity);
    outResult = EvaluateAtmosphericScattering(posInput, V, outResult);

#ifdef DEBUG_DISPLAY
    float4 debugColor = 0;
    if (GetMaterialDebugColor(debugColor, input, builtinData, posInput, surfaceData, bsdfData))
    {
        outResult = debugColor;
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        float4 result = _DebugTransparencyOverdrawWeight * float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_A);
        outResult = result;
    }
#endif

    outColor = outResult;
}
