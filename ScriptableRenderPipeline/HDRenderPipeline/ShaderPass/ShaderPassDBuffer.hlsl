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
/*
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
}
