#ifndef SHADERPASS
#error SHADERPASS must be defined
#endif

#if (SHADERPASS == SHADERPASS_FORWARD)

float4 VFXCalcPixelOutputForward(const SurfaceData surfaceData, const InputData inputData)
{
    float4 outColor = UniversalFragmentPBR(inputData, surfaceData);
    //TODOPAUL : Check fog correctly applied afterwards
    return outColor;
}

uint GetTileSize()
{
    return 1u;
}

#ifndef VFX_SHADERGRAPH
float4 VFXGetPixelOutputForward(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData)
{
    SurfaceData surfaceData;
    InputData inputData;

    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    VFXGetURPLitData(surfaceData, inputData, i, normalWS, uvData, tileIndex);

    return VFXCalcPixelOutputForward(surfaceData, inputData);
}

#else

//TODOPAUL : SG case
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


/*
TODOPAUL : Use the same design for URP GBuffer

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

#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    SurfaceData surfaceData; \
    BuiltinData builtinData; \
    VFXGetHDRPLitData(surfaceData,builtinData,i,normalWS,uvData); \
 \
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer); \
}
*/
#endif
