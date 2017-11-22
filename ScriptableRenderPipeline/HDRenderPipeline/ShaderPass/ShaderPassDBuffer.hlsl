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
//	float d = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, posInput.positionSS.xy), _ZBufferParams);
	float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, posInput.positionSS.xy);
	UpdatePositionInput(d, _InvViewProjMatrix, _ViewProjMatrix, posInput);
	posInput.positionWS.xyz += _WorldSpaceCameraPos;

	clip(posInput.positionWS.y < 0 ? -1 : 1);
	//clip(posInput.positionWS.y > 0.0001 ? -1 : 1);
	outDBuffer0.xyzw = float4(posInput.positionWS.xyz + float3(0.5, 0.0, 0.5), 1.0f);
/*
//	float d = Linear01Depth(SAMPLE_TEXTURE_2D(_CameraDepthTexture, sampler_CameraDepthTexture, posInput.positionSS.xy).z);
	float4 res;
	res = float4(posInput.positionSS.x,posInput.positionSS.y,0,0);
	res = float4(0,0,posInput.depthRaw,0);
	res = float4(0,0,d,0);
	res = float4(posInput.positionWS.xyz,1);
	res = mul(_WorldToDecal, res); 
	clip(res.y < 0.5 ? -1 : 1);
	clip(res.x < 0.5 ? -1 : 1);
	clip(res.z < 0.5 ? -1 : 1);
	clip(res.y > 0.5 ? -1 : 1);
	clip(res.x > 0.5 ? -1 : 1);
	clip(res.z > 0.5 ? -1 : 1);

//	clip(float3(1.0f, 1.0f, 1.0f) - res.xyz);
//	float4 res = packedInput.vmesh.positionCS;
	outDBuffer0.xyzw = float4(res.xyz, 1.0f);
*/
}
