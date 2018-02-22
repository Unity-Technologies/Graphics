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

    float depth = LOAD_TEXTURE2D(_MainDepthTexture, input.positionSS.xy).x;
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP);

    // Transform from world space to decal space (DS) to clip the decal.
    // For this we must use absolute position.
    // There is no lose of precision here as it doesn't involve the camera matrix
	float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);
	float3 positionDS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1.0)).xyz;
	positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.0f, 0.5);
	clip(positionDS);       // clip negative value
	clip(1.0 - positionDS); // Clip value above one

    DecalSurfaceData surfaceData;
	float4x4 decalToWorld = UNITY_ACCESS_INSTANCED_PROP(matrix, _NormalToWorld);
    GetSurfaceData(positionDS.xz, decalToWorld, surfaceData);

	// have to do explicit test since compiler behavior is not defined for RW resources and discard instructions
	if((all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
	{
		uint oldVal = UnpackByte(_DecalHTile[posInput.positionSS.xy / 8]);
		oldVal |= surfaceData.HTileMask;
		_DecalHTile[posInput.positionSS.xy / 8] = PackByte(oldVal);
	}
	ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
