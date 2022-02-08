#ifdef _ALPHATEST_ON
void ClipHoles(float2 uv)
{
    float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
    DoAlphaTest(hole, 0.5);
}
#endif

// Vertex height displacement
#ifdef HAVE_MESH_MODIFICATION

UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

float4 ConstructTerrainTangent(float3 normal, float3 positiveZ)
{
    // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateTangentToWorld function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    float3 tangent = cross(normal, positiveZ);
    return float4(tangent, -1);
}

AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
    , inout VaryingsMeshToPS varyings
#endif
    )
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = input.positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    input.positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    input.positionOS.y = height * _TerrainHeightmapScale.y;

    #ifdef ATTRIBUTES_NEED_NORMAL
        input.normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
        #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
            input.uv0 = sampleCoords;
        #else
            input.uv0 = sampleCoords * _TerrainHeightmapRecipSize.zw;
        #endif
    #endif
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    input.tangentOS = ConstructTerrainTangent(input.normalOS, float3(0, 0, 1));
#endif
    return input;
}

#endif // HAVE_MESH_MODIFICATION

FragInputs BuildFragInputs(VaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
    // to compute normals which are then passed on elsewhere to compute other values...
    output.tangentToWorld = k_identity3x3;
    output.positionSS = input.positionCS;       // input.positionCS is SV_Position

    $FragInputs.positionRWS:                    output.positionRWS =                input.positionRWS;
    $FragInputs.positionPixel:                  output.positionPixel =              input.positionCS.xy; // NOTE: this is not actually in clip space, it is the VPOS pixel coordinate value
    $FragInputs.positionPredisplacementRWS:     output.positionPredisplacementRWS = input.positionPredisplacementRWS;
    $FragInputs.tangentToWorld:                 output.tangentToWorld =             BuildTangentToWorld(input.tangentWS, input.normalWS);
    $FragInputs.texCoord0:                      output.texCoord0 =                  input.texCoord0;
    $FragInputs.texCoord1:                      output.texCoord1 =                  input.texCoord1;
    $FragInputs.texCoord2:                      output.texCoord2 =                  input.texCoord2;
    $FragInputs.texCoord3:                      output.texCoord3 =                  input.texCoord3;
    $FragInputs.color:                          output.color =                      input.color;

    // splice point to copy custom interpolator fields from varyings to frag inputs
    $splice(CustomInterpolatorVaryingsToFragInputs)

    return output;
}

// existing HDRP code uses the combined function to go directly from packed to frag inputs
FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
    return BuildFragInputs(unpacked);
}
