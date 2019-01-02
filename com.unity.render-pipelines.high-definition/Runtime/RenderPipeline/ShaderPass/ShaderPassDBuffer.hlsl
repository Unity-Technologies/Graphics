#if (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"


void MeshDecalsPositionZBias(inout VaryingsToPS input)
{
#if defined(UNITY_REVERSED_Z)
	input.vmesh.positionCS.z -= _DecalMeshDepthBias * input.vmesh.positionCS.w;
#else
	input.vmesh.positionCS.z += _DecalMeshDepthBias * input.vmesh.positionCS.w;
#endif
}

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
#if (SHADERPASS == SHADERPASS_DBUFFER_MESH)
	MeshDecalsPositionZBias(varyingsType);
#endif
    return PackVaryingsType(varyingsType);
}

void Frag(  PackedVaryingsToPS packedInput,
            OUTPUT_DBUFFER(outDBuffer)
            )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    DecalSurfaceData surfaceData;

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
	float depth = LOAD_TEXTURE2D(_CameraDepthTexture, input.positionSS.xy).x;
	PositionInputs posInput = GetPositionInput_Stereo(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);
    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5f, 0.5);
    clip(positionDS);       // clip negative value
    clip(1.0 - positionDS); // Clip value above one

    input.texCoord0.xy = positionDS.xz;
    input.texCoord1.xy = positionDS.xz;
    input.texCoord2.xy = positionDS.xz;
    input.texCoord3.xy = positionDS.xz;

    float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
    GetSurfaceData(input, V, posInput, surfaceData);

	// have to do explicit test since compiler behavior is not defined for RW resources and discard instructions
	if ((all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
    {
#else // Decal mesh

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput_Stereo(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0), unity_StereoEyeIndex);

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif
    GetSurfaceData(input, V, posInput, surfaceData);

#endif        

        uint oldVal = UnpackByte(_DecalHTile[input.positionSS.xy / 8]);
        oldVal |= surfaceData.HTileMask;
        _DecalHTile[input.positionSS.xy / 8] = PackByte(oldVal);

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
    }
#endif

    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
}
