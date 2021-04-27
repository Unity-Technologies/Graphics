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

#elif (SHADERPASS == SHADERPASS_DEPTHNORMALSONLY)

void VFXComputePixelOutputToNormalBuffer(float3 normalWS, out float4 outNormalBuffer)
{
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
    outNormalBuffer = float4(packedNormalWS, 0.0);
#else
    outNormalBuffer = float4(normalWS, 0.0);
#endif
}

#else

#ifndef VFX_SHADERGRAPH
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

#else
void VFXComputePixelOutputToGBufferShaderGraph(const VFX_VARYING_PS_INPUTS i, SurfaceData surfaceData, const float3 normalWS, out FragmentOutput gBuffer)
{
    uint2 tileIndex = uint2(i.VFX_VARYING_POSCS.xy) / GetTileSize();

    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, tileIndex);

    VFXUVData uvData = (VFXUVData)0;
    InputData inputData = VFXGetInputData(i, posInput, surfaceData, uvData, normalWS, 1.0f);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    gBuffer = BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
}

#endif
#endif
