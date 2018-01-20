#if SHADERPASS != SHADERPASS_DBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "VertMesh.hlsl"

float3 RemoveScale(float3 val)
{
	return normalize(val / length(val));
}

VaryingsMeshType Transform(AttributesMesh input)
{
    VaryingsMeshType output;
	
	UNITY_SETUP_INSTANCE_ID(input)
	UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
	positionWS = GetCameraRelativePositionWS(positionWS);
	output.positionCS = TransformWorldToHClip(positionWS);

	float3x3 decalRotation;
	decalRotation[0] = RemoveScale(float3(UNITY_MATRIX_M[0][0], UNITY_MATRIX_M[0][1], UNITY_MATRIX_M[0][2]));
	decalRotation[2] = RemoveScale(float3(UNITY_MATRIX_M[1][0], UNITY_MATRIX_M[1][1], UNITY_MATRIX_M[1][2]));
	decalRotation[1] = RemoveScale(float3(UNITY_MATRIX_M[2][0], UNITY_MATRIX_M[2][1], UNITY_MATRIX_M[2][2]));

	decalRotation = transpose(decalRotation);

	output.positionWS = decalRotation[0];
    output.normalWS = decalRotation[1];
    output.tangentWS = float4(decalRotation[2], 0); 

    return output;
}

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = Transform(inputMesh);
    return PackVaryingsType(varyingsType);
}

void Frag(  PackedVaryingsToPS packedInput,
            OUTPUT_DBUFFER(outDBuffer)            
            )
{	

    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw);

	float d = LOAD_TEXTURE2D(_MainDepthTexture, posInput.positionSS).x;
	UpdatePositionInput(d, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP, posInput);

	// calling absolute position only to go in decal space, so there is no loss of precision here
	float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);
	float3 positionDS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1.0f)).xyz;
	positionDS = positionDS * float3(1.0f, -1.0f, 1.0f) + float3(0.5f, 0.0f, 0.5f);
	clip(positionDS < 0 ? -1 : 1);
	clip(positionDS > 1 ? -1 : 1);

    DecalSurfaceData surfaceData;
	float3x3 decalToWorld = (float3x3)UNITY_ACCESS_INSTANCED_PROP(matrix, normalToWorld);
	// using the interpolators directly, because UnpackVaryingsMeshToFragInputs does some tangent space manipulations
//	decalToWorld[0] = packedInput.vmesh.interpolators0;
//	decalToWorld[1] = packedInput.vmesh.interpolators1;
//	decalToWorld[2] = packedInput.vmesh.interpolators2.xyz;

    GetSurfaceData(positionDS.xz, decalToWorld, surfaceData);

	ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
