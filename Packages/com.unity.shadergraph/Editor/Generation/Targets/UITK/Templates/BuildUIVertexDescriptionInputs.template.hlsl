// UITK variant of the shared BuildVertexDescriptionInputs template, included explicitly by
// PassUI.template (mirrors how TerrainPass uses BuildTerrainVertexDescriptionInputs). The only
// difference from the shared copy is the world-space position: UITK vertices are element-local, so
// the world read must apply the per-element transform (translation -> bone -> group -> world) rather
// than the group-only TransformObjectToWorld(input.positionOS). Object space is already element-local.

// World position for a UITK element vertex: group * bone * (local + translation). In the ShaderGraph
// preview there is no ElementInfo record bound, so fall back to the plain object->world transform to
// avoid sampling unbound tables.
float3 uie_world_from_attributes(Attributes input)
{
#if defined(SHADERGRAPH_PREVIEW)
    return TransformObjectToWorld(input.positionOS);
#else
    appdata_t uieInput = (appdata_t)0;
    uieInput.vertex    = float4(input.positionOS, 1.0f);
    uieInput.packedIds = input.uv4;

    uint transformId, opacityId;
    float2 translation;
    uie_load_element_info(uieInput, transformId, opacityId, translation);

    return TransformObjectToWorld(uie_local_to_world(uieInput.vertex, transformId, translation));
#endif
}

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    // Texture UV (static field) so the Element Texture UV node works in the vertex stage. The
    // fragment gets this from the uvClip varying that uie_std_vert writes; here we compute it
    // directly from the vertex inputs, including the dynamic-texture texel-size scaling.
    output.uvClip.xy = uie_element_texture_uv(input.uv0, input.uv4);

    // Resolved tint (static field) so the Element Color node works in the vertex stage; the fragment
    // reads the carried tint varying. Dead-code-eliminated when no node reads it.
    output.color = uie_element_tint(input.color, input.uv4);

    // Per-element opacity (static field) for the Element Color node's Opacity output in the vertex
    // stage. Dead-code-eliminated when no node reads it.
    output.opacity = uie_element_opacity(input.uv4);

    // Layout-rect UV (static field) so the Element Layout UV node works in the vertex stage. It is
    // packed into uv0.zw, the same source the fragment reads via the texCoord6 varying.
    output.layoutUV = input.uv0.zw;

    // Render type in typeTexSettings.x and texture slot in typeTexSettings.y (static field) so the
    // Render Type and Element Texture Size nodes work in the vertex stage; they read IN.typeTexSettings.x
    // and .y. The .z/.w components aren't read by any vertex node.
    output.typeTexSettings.x = uie_element_frag_type(input.uv4);
    output.typeTexSettings.y = uie_element_texture_slot(input.uv4);

    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ViewSpaceNormal:                           output.ViewSpaceNormal =                            TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:                        output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.ObjectSpaceTangent:                        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    $VertexDescriptionInputs.WorldSpaceTangent:                         output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
    $VertexDescriptionInputs.ViewSpaceTangent:                          output.ViewSpaceTangent =                           TransformWorldToViewDir(output.WorldSpaceTangent);
    $VertexDescriptionInputs.TangentSpaceTangent:                       output.TangentSpaceTangent =                        float3(1.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:                      output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
    $VertexDescriptionInputs.WorldSpaceBiTangent:                       output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
    $VertexDescriptionInputs.ViewSpaceBiTangent:                        output.ViewSpaceBiTangent =                         TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $VertexDescriptionInputs.TangentSpaceBiTangent:                     output.TangentSpaceBiTangent =                      float3(0.0f, 1.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         uie_world_from_attributes(input);
    $VertexDescriptionInputs.ViewSpacePosition:                         output.ViewSpacePosition =                          TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:                      output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         input.positionOS;
    $VertexDescriptionInputs.WorldSpacePositionPredisplacement:         output.WorldSpacePositionPredisplacement =          uie_world_from_attributes(input);
    $VertexDescriptionInputs.ViewSpacePositionPredisplacement:          output.ViewSpacePositionPredisplacement =           TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePositionPredisplacement:       output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement: output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.WorldSpaceViewDirection:                   output.WorldSpaceViewDirection =                    GetWorldSpaceNormalizeViewDir(output.WorldSpacePosition);
    $VertexDescriptionInputs.ObjectSpaceViewDirection:                  output.ObjectSpaceViewDirection =                   TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ViewSpaceViewDirection:                    output.ViewSpaceViewDirection =                     TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 float3x3 tangentSpaceTransform =                    float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 output.TangentSpaceViewDirection =                  TransformWorldToTangent(output.WorldSpaceViewDirection, tangentSpaceTransform);
    $VertexDescriptionInputs.ScreenPosition:                            output.ScreenPosition =                             ComputeScreenPos(TransformWorldToHClip(output.WorldSpacePosition), _ProjectionParams.x);
    $VertexDescriptionInputs.NDCPosition:                               output.NDCPosition =                                output.ScreenPosition.xy / output.ScreenPosition.w;
    $VertexDescriptionInputs.PixelPosition:                             output.PixelPosition =                              float2(output.NDCPosition.x, 1.0f - output.NDCPosition.y) * GetScaledScreenParams().xy;
    $VertexDescriptionInputs.ClipPosition:                              output.ClipPosition =                               TransformWorldToHClip(output.WorldSpacePosition).xy;
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        input.uv0;
    $VertexDescriptionInputs.uv1:                                       output.uv1 =                                        input.uv1;
    $VertexDescriptionInputs.uv2:                                       output.uv2 =                                        input.uv2;
    $VertexDescriptionInputs.uv3:                                       output.uv3 =                                        input.uv3;
    $VertexDescriptionInputs.uv4:                                       output.uv4 =                                        input.uv4;
    $VertexDescriptionInputs.uv5:                                       output.uv5 =                                        input.uv5;
    $VertexDescriptionInputs.uv6:                                       output.uv6 =                                        input.uv6;
    $VertexDescriptionInputs.uv7:                                       output.uv7 =                                        input.uv7;
    $VertexDescriptionInputs.VertexColor:                               output.VertexColor =                                input.color;
    $VertexDescriptionInputs.TimeParameters:                            output.TimeParameters =                             _TimeParameters.xyz;
    $VertexDescriptionInputs.BoneWeights:                               output.BoneWeights =                                input.weights;
    $VertexDescriptionInputs.BoneIndices:                               output.BoneIndices =                                input.indices;
    $VertexDescriptionInputs.VertexID:                                  output.VertexID =                                   input.vertexID;
#if UNITY_ANY_INSTANCING_ENABLED
    $VertexDescriptionInputs.InstanceID:                                output.InstanceID =                                 unity_InstanceID;
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
    $VertexDescriptionInputs.InstanceID:                                output.InstanceID =                                 input.instanceID;
#endif

    return output;
}

// Runs the shader-graph vertex stage and returns the (possibly graph-modified) element-local
// position; custom interpolators are forwarded into the Varyings. uie_custom_vert writes the result
// into the vertex, then uie_std_vert applies the element transform (translation -> bone -> group ->
// clip). This is only included when FEATURES_GRAPH_VERTEX is set, so the caller guards on it too.
float3 uie_build_vertex(Attributes input, inout Varyings varyings)
{
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
#if defined(CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC)
    CustomInterpolatorPassThroughFunc(varyings, vertexDescription);
#endif
    return vertexDescription.Position;
}
