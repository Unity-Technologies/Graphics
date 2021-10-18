VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#ifdef TERRAIN_ENABLED
        // Affects normal, position, tangent, bitangent, uv
    // TODO: Move terrain prop declaratoins, functions/math/etc. into include file once I find out where to add stuff to the include files.

    float3 terrainNormal;
#ifdef UNITY_INSTANCING_ENABLED
    // Affects normal, position, uv

    float2 patchVertex = input.positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    float3 terrainPositionOS;
    terrainPositionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    terrainPositionOS.y = height * _TerrainHeightmapScale.y;
    terrainNormal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        terrainPositionOS;
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         terrainPositionOS;
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        float4(sampleCoords, input.uv0.zw); // TODO HD assigns UV to sampleCoords without modification if perPixelNormal is not enabled. See if this works
#else
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        float4(sampleCoords * _TerrainHeightmapRecipSize.zw, input.uv0.zw);
#endif
#else
    terrainNormal = input.normalOS;
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         input.positionOS;
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        input.uv0;
#endif
    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          terrainNormal;
    float4 terrainTangentOS = ConstructTerrainTangent(terrainNormal, float3(0, 0, 1)); //float4(cross(terrainNormal, float3(0, 0, 1)), -1);
    $VertexDescriptionInputs.ObjectSpaceTangent:                        output.ObjectSpaceTangent =                         terrainTangentOS.xyz;
    $VertexDescriptionInputs.WorldSpaceTangent:                         output.WorldSpaceTangent =                          TransformObjectToWorldDir(output.ObjectSpaceTangent.xyz);
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(output.ObjectSpaceNormal);
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         TransformObjectToWorld(output.ObjectSpacePosition);
    $VertexDescriptionInputs.WorldSpacePositionPredisplacement:         output.WorldSpacePositionPredisplacement =          TransformObjectToWorld(output.ObjectSpacePositionPredisplacement);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(output.WorldSpacePosition.xyz);
    $VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement: output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(output.WorldSpacePositionPredisplacement.xyz);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:                      output.ObjectSpaceBiTangent =                       normalize(cross(output.ObjectSpaceNormal.xyz, output.ObjectSpaceTangent.xyz) * (terrainTangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
#else
    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ObjectSpaceTangent:                        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    $VertexDescriptionInputs.WorldSpaceTangent:                         output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:                      output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         input.positionOS;
    $VertexDescriptionInputs.WorldSpacePositionPredisplacement:         output.WorldSpacePositionPredisplacement =          TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement: output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        input.uv0;
#endif
    $VertexDescriptionInputs.ViewSpaceNormal:                           output.ViewSpaceNormal =                            TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:                        output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.ViewSpaceTangent:                          output.ViewSpaceTangent =                           TransformWorldToViewDir(output.WorldSpaceTangent);
    $VertexDescriptionInputs.TangentSpaceTangent:                       output.TangentSpaceTangent =                        float3(1.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.WorldSpaceBiTangent:                       output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
    $VertexDescriptionInputs.ViewSpaceBiTangent:                        output.ViewSpaceBiTangent =                         TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $VertexDescriptionInputs.TangentSpaceBiTangent:                     output.TangentSpaceBiTangent =                      float3(0.0f, 1.0f, 0.0f);
    $VertexDescriptionInputs.ViewSpacePosition:                         output.ViewSpacePosition =                          TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:                      output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.ViewSpacePositionPredisplacement:          output.ViewSpacePositionPredisplacement =           TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePositionPredisplacement:       output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.WorldSpaceViewDirection:                   output.WorldSpaceViewDirection =                    GetWorldSpaceNormalizeViewDir(output.WorldSpacePosition);
    $VertexDescriptionInputs.ObjectSpaceViewDirection:                  output.ObjectSpaceViewDirection =                   TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ViewSpaceViewDirection:                    output.ViewSpaceViewDirection =                     TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 float3x3 tangentSpaceTransform =                    float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 output.TangentSpaceViewDirection =                  TransformWorldToTangent(output.WorldSpaceViewDirection, tangentSpaceTransform);
    $VertexDescriptionInputs.ScreenPosition:                            output.ScreenPosition =                             ComputeScreenPos(TransformWorldToHClip(output.WorldSpacePosition), _ProjectionParams.x);
    $VertexDescriptionInputs.NDCPosition:                               output.NDCPosition =                                output.ScreenPosition.xy / output.ScreenPosition.w;
    $VertexDescriptionInputs.PixelPosition:                             output.PixelPosition =                              output.NDCPosition.xy * _ScreenParams.xy;
    $VertexDescriptionInputs.uv1:                                       output.uv1 =                                        input.uv1;
    $VertexDescriptionInputs.uv2:                                       output.uv2 =                                        input.uv2;
    $VertexDescriptionInputs.uv3:                                       output.uv3 =                                        input.uv3;
    $VertexDescriptionInputs.VertexColor:                               output.VertexColor =                                input.color;
    $VertexDescriptionInputs.TimeParameters:                            output.TimeParameters =                             _TimeParameters.xyz;
    $VertexDescriptionInputs.BoneWeights:                               output.BoneWeights =                                input.weights;
    $VertexDescriptionInputs.BoneIndices:                               output.BoneIndices =                                input.indices;
    $VertexDescriptionInputs.VertexID:                                  output.VertexID =                                   input.vertexID;

    return output;
}
