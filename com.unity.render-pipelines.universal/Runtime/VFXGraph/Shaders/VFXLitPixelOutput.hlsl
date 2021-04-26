#ifndef SHADERPASS
#error SHADERPASS must be defined
#endif

#ifndef UNIVERSAL_SHADERPASS_INCLUDED
#error ShaderPass has to be included
#endif

uint GetTileSize() //TODOPAUL : Clean this
{
    return 1u;
}

#if (SHADERPASS == SHADERPASS_FORWARD)

float4 VFXCalcPixelOutputForward(const SurfaceData surfaceData, const InputData inputData)
{
    float4 outColor = UniversalFragmentPBR(inputData, surfaceData);
    //TODOPAUL : Check fog correctly applied afterwards
    return outColor;
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

#else //SHADERPASS_GBUFFER

void VFXComputePixelOutputToGBuffer(const VFX_VARYING_PS_INPUTS i, const float3 normalWS, const VFXUVData uvData, out FragmentOutput gBuffer)
{
    SurfaceData surfaceData;
    InputData inputData;
    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();
    VFXGetURPLitData(surfaceData, inputData, i, normalWS, uvData, tileIndex);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    gBuffer = BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
}


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
