#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}


void Frag(  PackedVaryingsToPS packedInput,
            OUTPUT_DBUFFER(outDBuffer)            
            )
{
	
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
//    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
/*
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);

    ENCODE_INTO_GBUFFER(surfaceData, bakeDiffuseLighting, outGBuffer);
    ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outVelocityBuffer);
*/
	float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, posInput.positionSS.xy);
	UpdatePositionInput(d, _InvViewProjMatrix, _ViewProjMatrix, posInput);
	float3 positionWS = posInput.positionWS;
	float3 positionDS = mul(_WorldToDecal, float4(positionWS, 1.0f)).xyz;
	clip(positionDS < 0 ? -1 : 1);
	clip(positionDS > 1 ? -1 : 1);
	outDBuffer0.xyzw = float4(positionDS, 1.0f);
}
