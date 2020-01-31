#if (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_MESH) && (SHADERPASS != SHADERPASS_FORWARD_PREVIEW)
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
#elif (SHADERPASS == SHADERPASS_FORWARD_PREVIEW) // Only used for preview in shader graph
    out float4 outColor : SV_Target0
#else
    out float4 outEmissive : SV_Target0
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    DecalSurfaceData surfaceData;
    float clipValue = 1.0;

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)    

	float depth = LoadCameraDepth(input.positionSS.xy);
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);
    if (!(all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
    {
        clipValue = -1.0; // helper lanes will be clipped
        #ifndef SHADER_API_METAL
        clip(clipValue); // call clip as early as possible
        #endif
    }

    input.texCoord0.xy = positionDS.xz;
    input.texCoord1.xy = positionDS.xz;
    input.texCoord2.xy = positionDS.xz;
    input.texCoord3.xy = positionDS.xz;

    float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
#else // Decal mesh
    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0));

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif
#endif

    GetSurfaceData(input, V, posInput, surfaceData);

// Perform HTile optimization only on platform that support it
#if ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)) && defined(PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER)
    uint2 htileCoord = input.positionSS.xy / 8;
    int stride = (_ScreenSize.x + 7) / 8;
    uint mask = surfaceData.HTileMask;
    uint tileCoord1d = htileCoord.y * stride + htileCoord.x;
#ifdef PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // This is an optimization to reduce the number of atomatic operation executed.
    // smallest tile index in the wave
    uint minTileCoord1d = WaveActiveMin(tileCoord1d);
    while (minTileCoord1d != -1)
    {
        if ((minTileCoord1d == tileCoord1d) && (clipValue > 0.0))// if this is the current tile and not a helper lane
        {
            // calculate the mask across the current tile
            mask = WaveActiveBitOr(surfaceData.HTileMask);

            // Is it the first active lane?
            if(WaveIsFirstLane())
            {
                // recalculate tileCoord1d, because on Xbox the register holding its value gets overwritten
                if (tileCoord1d != -1)
                {
                    tileCoord1d = htileCoord.y * stride + htileCoord.x;
                }
                InterlockedOr(_DecalPropertyMaskBuffer[tileCoord1d], mask);
            }
            // mark this tile as processed
            tileCoord1d = -1;
        }
        // recalculate tileCoord1d, because on Xbox the register holding its value gets overwritten
        if (tileCoord1d != -1)
        {
            tileCoord1d = htileCoord.y * stride + htileCoord.x;
        }
        // get the next tile with smallest index
        minTileCoord1d = WaveActiveMin(tileCoord1d);
    }
#else // PLATFORM_SUPPORTS_WAVE_INTRINSICS
    InterlockedOr(_DecalPropertyMaskBuffer[tileCoord1d], mask);
#endif // PLATFORM_SUPPORTS_WAVE_INTRINSICS

#endif // ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)) && defined(PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER)


#if ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)) && defined(SHADER_API_METAL)
    // Metal Shading Language declares that fragment discard invalidates
    // derivatives for the rest of the quad, so we need to reorder when
    // we discard during decal projection, or we get artifacts along the
    // edges of the projection(any partial quads get bad partial derivatives
    //regardless of whether they are computed implicitly or explicitly).
    clip(clipValue);
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#elif (SHADERPASS == SHADERPASS_FORWARD_PREVIEW) // Only used for preview in shader graph
    outColor = 0;
    // Evaluate directional light from the preview
    uint i;
    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        DirectionalLightData light = _DirectionalLightDatas[i];
        outColor.rgb += surfaceData.baseColor.rgb * light.color * saturate(dot(surfaceData.normalWS.xyz, -light.forward.xyz));
    }

    outColor.rgb += surfaceData.emissive;
    outColor.w = 1.0;
#else
    // Emissive need to be pre-exposed
    outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
    outEmissive.a = 1.0;
#endif
}
