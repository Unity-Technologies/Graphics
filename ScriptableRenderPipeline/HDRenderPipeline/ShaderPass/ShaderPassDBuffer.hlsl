#if SHADERPASS != SHADERPASS_DBUFFER
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

	float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
//    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
/*
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

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
	outDBuffer0 = surfaceData.baseColor;
	outDBuffer1 = surfaceData.normalWS;
//	outDBuffer0.xyzw = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, positionDS.xz); // float4(positionDS, 1.0f);
//	texCoord.uv = positionDS.xz;
//	outDBuffer1.xyz = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, positionDS.xz).xyz; //SAMPLE_UVMAPPING_NORMALMAP(_NormalMap, sampler_NormalMap, texCoord, 1); //SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, positionDS.xz).xyz;
//	outDBuffer1.w = outDBuffer0.w;
}
