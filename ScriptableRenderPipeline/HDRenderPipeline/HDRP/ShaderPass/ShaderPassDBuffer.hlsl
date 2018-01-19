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
    output.tangentWS = float4(decalRotation[2], 0.0);

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

	float depth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.positionSS).x;
	UpdatePositionInput(depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP, posInput);

    // Transform from world space to decal space (DS) to clip the decal.
    // For this we must use absolute position.
    // There is no lose of precision here as it doesn't involve the camera matrix
	float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);
	float3 positionDS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1.0)).xyz;
	positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.0f, 0.5);
	clip(positionDS);       // clip negative value
	clip(1.0 - positionDS); // Clip value above one

    DecalSurfaceData surfaceData;
	float3x3 decalToWorld;
	// using the interpolators directly, because UnpackVaryingsMeshToFragInputs does some tangent space manipulations
	decalToWorld[0] = packedInput.vmesh.interpolators0;
	decalToWorld[1] = packedInput.vmesh.interpolators1;
	decalToWorld[2] = packedInput.vmesh.interpolators2.xyz;

    GetSurfaceData(positionDS.xz, decalToWorld, surfaceData);

	ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
