
VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ViewSpaceNormal:                           output.ViewSpaceNormal =                            TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:                        output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.ObjectSpaceTangent:                        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    $VertexDescriptionInputs.WorldSpaceTangent:                         output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
    $VertexDescriptionInputs.ViewSpaceTangent:                          output.ViewSpaceTangent =                           TransformWorldToViewDir(output.WorldSpaceTangent);
    $VertexDescriptionInputs.TangentSpaceTangent:                       output.TangentSpaceTangent =                        float3(1.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:                      output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS.xyz, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
    $VertexDescriptionInputs.WorldSpaceBiTangent:                       output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
    $VertexDescriptionInputs.ViewSpaceBiTangent:                        output.ViewSpaceBiTangent =                         TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $VertexDescriptionInputs.TangentSpaceBiTangent:                     output.TangentSpaceBiTangent =                      float3(0.0f, 1.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.ViewSpacePosition:                         output.ViewSpacePosition =                          TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:                      output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS).xyz);
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         input.positionOS;
    $VertexDescriptionInputs.WorldSpacePositionPredisplacement:         output.WorldSpacePositionPredisplacement =          TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.ViewSpacePositionPredisplacement:          output.ViewSpacePositionPredisplacement =           TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePositionPredisplacement:       output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement: output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS).xyz);
    $VertexDescriptionInputs.WorldSpaceViewDirection:                   output.WorldSpaceViewDirection =                    GetWorldSpaceNormalizeViewDir(output.WorldSpacePosition);
    $VertexDescriptionInputs.ObjectSpaceViewDirection:                  output.ObjectSpaceViewDirection =                   TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ViewSpaceViewDirection:                    output.ViewSpaceViewDirection =                     TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 float3x3 tangentSpaceTransform =                    float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 output.TangentSpaceViewDirection =                  TransformWorldToTangent(output.WorldSpaceViewDirection, tangentSpaceTransform);
    $VertexDescriptionInputs.ScreenPosition:                            output.ScreenPosition =                             ComputeScreenPos(TransformWorldToHClip(output.WorldSpacePosition), _ProjectionParams.x);
    $VertexDescriptionInputs.NDCPosition:                               output.NDCPosition =                                output.ScreenPosition.xy / output.ScreenPosition.w;
    $VertexDescriptionInputs.PixelPosition:                             output.PixelPosition =                              float2(output.NDCPosition.x, 1.0f - output.NDCPosition.y) * _ScreenParams.xy;
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        input.uv0;
    $VertexDescriptionInputs.uv1:                                       output.uv1 =                                        input.uv1;
    $VertexDescriptionInputs.uv2:                                       output.uv2 =                                        input.uv2;
    $VertexDescriptionInputs.uv3:                                       output.uv3 =                                        input.uv3;
    $VertexDescriptionInputs.VertexColor:                               output.VertexColor =                                input.color;
    $VertexDescriptionInputs.TimeParameters:                            output.TimeParameters =                             _TimeParameters.xyz; // Note: in case of animation this will be overwrite (allow to handle motion vector)
    $VertexDescriptionInputs.BoneWeights:                               output.BoneWeights =                                input.weights;
    $VertexDescriptionInputs.BoneIndices:                               output.BoneIndices =                                input.indices;
    $VertexDescriptionInputs.VertexID:                                  output.VertexID =                                   input.vertexID;

    return output;
}

VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters)
{
    // build graph inputs
    VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
    // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
    $VertexDescriptionInputs.TimeParameters: vertexDescriptionInputs.TimeParameters = timeParameters;

    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    return vertexDescription;

}

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

    #if defined(ATTRIBUTES_NEED_NORMAL) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    input.normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords.xy, 0)).rgb * 2.0 - 1.0;
    #endif

    #ifdef ATTRIBUTES_NEED_TEXCOORD0
        #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
        input.uv0.xy = sampleCoords;
        #else
        input.uv0.xy = sampleCoords * _TerrainHeightmapRecipSize.zw;
        #endif
    #endif
#endif

    VertexDescription vertexDescription = GetVertexDescription(input, timeParameters);

    // copy graph output to the results
    $VertexDescription.Position: input.positionOS = vertexDescription.Position;
    $VertexDescription.Normal:   input.normalOS = vertexDescription.Normal;
    $VertexDescription.uv0:      input.uv0 = vertexDescription.uv0;

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

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    // it will be reconstructed in fragment stage
    $FragInputs.tangentToWorld: float3x3 tangentToWorld = k_identity3x3;
#else
    $FragInputs.tangentToWorld: float3 normalWS = input.normalWS;
    $FragInputs.tangentToWorld: float3 normalOS = TransformWorldToObjectNormal(normalWS);
    $FragInputs.tangentToWorld: float4 tangentOS = ConstructTerrainTangent(normalOS, float3(0.0, 0.0, 1.0));
    $FragInputs.tangentToWorld: float4 tangentWS = float4(TransformObjectToWorldNormal(tangentOS.xyz), 0.0);
    $FragInputs.tangentToWorld: float3x3 tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
#endif

    $FragInputs.positionRWS:                    output.positionRWS =                input.positionRWS;
    $FragInputs.positionPixel:                  output.positionPixel =              input.positionCS.xy; // NOTE: this is not actually in clip space, it is the VPOS pixel coordinate value
    $FragInputs.positionPredisplacementRWS:     output.positionPredisplacementRWS = input.positionPredisplacementRWS;
    $FragInputs.tangentToWorld:                 output.tangentToWorld =             tangentToWorld;
    $FragInputs.texCoord0:                      output.texCoord0 =                  input.texCoord0;
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
