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

uint GetTileSize() //TODOPAUL : Clean this
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
float4 VFXGetPixelOutputForwardShaderGraph(const VFX_VARYING_PS_INPUTS i, SurfaceData surfaceData, float3 normalWS)
{
    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();

    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, tileIndex);

    VFXUVData uvData = (VFXUVData)0;
    InputData inputData = VFXGetInputData(i, posInput, surfaceData, uvData, normalWS, 1.0f /* TODOPAUL remove opacity */);

    return VFXCalcPixelOutputForward(surfaceData, inputData);
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
