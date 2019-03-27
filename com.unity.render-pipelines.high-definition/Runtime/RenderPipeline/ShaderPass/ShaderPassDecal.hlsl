#if (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_MESH)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"


void MeshDecalsPositionZBias(inout VaryingsToPS input)
{
#if defined(UNITY_REVERSED_Z)
	input.vmesh.positionCS.z -= _DecalMeshDepthBias;
#else
	input.vmesh.positionCS.z += _DecalMeshDepthBias;
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
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    OUTPUT_DBUFFER(outDBuffer)
#else
    out float4 outEmissive : SV_Target0
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    DecalSurfaceData surfaceData;
    bool isClipped = false;

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
	float depth = LoadCameraDepth(input.positionSS.xy);
	PositionInputs posInput = GetPositionInput_Stereo(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);
    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5f, 0.5);
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);

    if ((all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
    {

        input.texCoord0.xy = positionDS.xz;
        input.texCoord1.xy = positionDS.xz;
        input.texCoord2.xy = positionDS.xz;
        input.texCoord3.xy = positionDS.xz;

        float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        GetSurfaceData(input, V, posInput, surfaceData);
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

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
    }
    else
    {
        isClipped = true;
        clip(-1);
    }
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    uint2 htileCoord = input.positionSS.xy / 8;
    uint mask = surfaceData.HTileMask;
#if (DECALS_HTILE_SUPPORT)
#ifdef SUPPORTS_WAVE_INTRINSICS
    uint tileCoord1d = (htileCoord.y << 16) | (htileCoord.x);
    // Loop over up to 4 tiles.
    for (int i = 0; ; i++)
    {
        // Select the 1st tile with active lanes.
        uint minTileCoord1d = WaveActiveMin(tileCoord1d);

        // Make sure we still have tiles to process.
        if (minTileCoord1d == -1)
            break;

        // Mask lanes corresponding to the min tile.
        // Mask clipped lanes.
        if ((tileCoord1d == minTileCoord1d) && (!isClipped))
        {
            // Process one tile.
            mask = WaveActiveBitOr(surfaceData.HTileMask);

            uint laneID = WaveGetLaneIndex();

            // Is it the first active lane?
            if (laneID == WaveReadLaneFirst(laneID))
            {
                htileCoord.x = minTileCoord1d & 0xffff;
                htileCoord.y = (minTileCoord1d >> 16) & 0xffff;
                InterlockedOr(_DecalHTile[COORD_TEXTURE2D_X(htileCoord)], mask);
            }

            // Mark tile as processed.
            tileCoord1d = -1;
        }
    }
#else // SUPPORTS_WAVE_INTRINSICS
    InterlockedOr(_DecalHTile[COORD_TEXTURE2D_X(htileCoord)], mask);
#endif // SUPPORTS_WAVE_INTRINSICS
#endif // DECALS_HTILE_SUPPORT
#endif // (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#else
    outEmissive = surfaceData.emissive;
#endif
}
