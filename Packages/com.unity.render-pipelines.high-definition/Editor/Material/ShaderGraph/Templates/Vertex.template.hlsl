
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

VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
#ifdef HAVE_VFX_MODIFICATION
    , AttributesElement element
#endif
)
{
    // build graph inputs
    VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
    // Override time parameters with used one (This is required to correctly handle motion vectors for vertex animation based on time)
    $VertexDescriptionInputs.TimeParameters: vertexDescriptionInputs.TimeParameters = timeParameters;

    // evaluate vertex graph
#ifdef HAVE_VFX_MODIFICATION
    GraphProperties properties;
    ZERO_INITIALIZE(GraphProperties, properties);

    // Fetch the vertex graph properties for the particle instance.
    GetElementVertexProperties(element, properties);

    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
#else
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
#endif
    return vertexDescription;

}

AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
    #ifdef TESSELLATION_ON
    , inout VaryingsMeshToDS varyings
    #else
    , inout VaryingsMeshToPS varyings
    #endif
#endif
#ifdef HAVE_VFX_MODIFICATION
        , AttributesElement element
#endif
    )
{
    VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
#ifdef HAVE_VFX_MODIFICATION
        , element
#endif
    );

    // copy graph output to the results
    $VertexDescription.Position: input.positionOS = vertexDescription.Position;
    $VertexDescription.Normal:   input.normalOS = vertexDescription.Normal;
    $VertexDescription.Tangent:  input.tangentOS.xyz = vertexDescription.Tangent;
    $VertexDescription.uv0:      input.uv0 = vertexDescription.uv0;
    $VertexDescription.uv1:      input.uv1 = vertexDescription.uv1;
    $VertexDescription.uv2:      input.uv2 = vertexDescription.uv2;
    $VertexDescription.uv3:      input.uv3 = vertexDescription.uv3;

    $splice(CustomInterpolatorVertMeshCustomInterpolation)

    return input;
}

#if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
// Return precomputed Velocity in object space
float3 GetCustomVelocity(AttributesMesh input
#ifdef HAVE_VFX_MODIFICATION
    , AttributesElement element
#endif
)
{
    VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
#ifdef HAVE_VFX_MODIFICATION
        , element
#endif
    );
    return vertexDescription.CustomVelocity;
}
#endif

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

#ifdef HAVE_VFX_MODIFICATION
    // FragInputs from VFX come from two places: Interpolator or CBuffer.
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    $splice(VFXLoadGraphValues)
#endif

    $splice(VFXSetFragInputs)

    $FragInputs.elementToWorld:                 BuildElementToWorld(input);
    $FragInputs.worldToElement:                 BuildWorldToElement(input);
#endif

    // splice point to copy custom interpolator fields from varyings to frag inputs
    $splice(CustomInterpolatorVaryingsToFragInputs)

    return output;
}

// existing HDRP code uses the combined function to go directly from packed to frag inputs
FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
    unity_InstanceID = input.instanceID;
#endif
    VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
    return BuildFragInputs(unpacked);
}
