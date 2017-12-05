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
	float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, posInput.positionSS.xy);
	UpdatePositionInput(d, _InvViewProjMatrix, _ViewProjMatrix, posInput);

	float3 positionWS = posInput.positionWS;
	float3 positionDS = mul(_WorldToDecal, float4(positionWS, 1.0f)).xyz;
	clip(positionDS < 0 ? -1 : 1);
	clip(positionDS > 1 ? -1 : 1);

    DecalSurfaceData surfaceData;
    GetSurfaceData(positionDS.xz, surfaceData);

	ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
