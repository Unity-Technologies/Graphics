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
    PositionInputs posInput = GetPositionInput(packedInput.vmesh.positionCS, _ScreenSize.zw);

	float d = LOAD_TEXTURE2D(_MainDepthTexture, posInput.positionSS).x;
	UpdatePositionInput(d, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP, posInput);

	float3 positionWS = posInput.positionWS;
	float3 positionDS = mul(_WorldToDecal, float4(positionWS, 1.0f)).xyz;
	clip(positionDS < 0 ? -1 : 1);
	clip(positionDS > 1 ? -1 : 1);

    DecalSurfaceData surfaceData;
    GetSurfaceData(positionDS.xz, surfaceData);

	ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
