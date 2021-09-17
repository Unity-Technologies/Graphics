
// TODO: I think we can get rid of this Varyings struct
Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    // TODO: VR support
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

// #if defined(FEATURES_GRAPH_VERTEX)
//     // Evaluate Vertex Graph
//     VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
//     VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

//     // Assign modified vertex attributes
//     input.positionOS = vertexDescription.Position;
//     #if defined(VARYINGS_NEED_NORMAL_WS)
//         input.normalOS = vertexDescription.Normal;
//     #endif //FEATURES_GRAPH_NORMAL
//     #if defined(VARYINGS_NEED_TANGENT_WS)
//         input.tangentOS.xyz = vertexDescription.Tangent.xyz;
//     #endif //FEATURES GRAPH TANGENT
// #endif //FEATURES_GRAPH_VERTEX

    // TODO: Avoid path via VertexPositionInputs (BuiltIn)
    // VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // Returns the camera relative position (if enabled)
    // float3 positionWS = TransformObjectToWorld(input.positionOS);

// #ifdef ATTRIBUTES_NEED_NORMAL
//     float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
// #else
//     // Required to compile ApplyVertexModification that doesn't use normal.
//     float3 normalWS = float3(0.0, 0.0, 0.0);
// #endif

// #ifdef ATTRIBUTES_NEED_TANGENT
//     float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
// #endif

    // TODO: Change to inline ifdef
    // Do vertex modification in camera relative space (if enabled)
// #if defined(HAVE_VERTEX_MODIFICATION)
//     ApplyVertexModification(input, normalWS, positionWS, _TimeParameters.xyz);
// #endif

// #ifdef VARYINGS_NEED_POSITION_WS
//     output.positionWS = positionWS;
// #endif

// #ifdef VARYINGS_NEED_NORMAL_WS
//     output.normalWS = normalWS;         // normalized in TransformObjectToWorldNormal()
// #endif

// #ifdef VARYINGS_NEED_TANGENT_WS
//     output.tangentWS = tangentWS;       // normalized in TransformObjectToWorldDir()
// #endif

// // Handled by the legacy pipeline
// #ifndef BUILTIN_TARGET_API
// #if (SHADERPASS == SHADERPASS_SHADOWCASTER)
//     // Define shadow pass specific clip position for BuiltIn
//     #if _CASTING_PUNCTUAL_LIGHT_SHADOW
//         float3 lightDirectionWS = normalize(_LightPosition - positionWS);
//     #else
//         float3 lightDirectionWS = _LightDirection;
//     #endif
//     output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
//     #if UNITY_REVERSED_Z
//         output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
//     #else
//         output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
//     #endif
// #elif (SHADERPASS == SHADERPASS_META)
//     output.positionCS = MetaVertexPosition(float4(input.positionOS, 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
// #else
    // output.positionCS = TransformWorldToHClip(positionWS);
// #endif
// #else

    // TODO: support Blit as well
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
// #endif

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = output.positionCS * 0.5 + 0.5;
#endif
// #if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
//     output.texCoord1 = input.uv1;
// #endif
// #if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
//     output.texCoord2 = input.uv2;
// #endif
// #if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
//     output.texCoord3 = input.uv3;
// #endif

// #if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
//     output.color = input.color;
// #endif

// #ifdef VARYINGS_NEED_VIEWDIRECTION_WS
//     output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);
// #endif

#ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = output.texCoord1;
#endif

    return output;
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

// TODO: Add depth as optional target
float4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);

    // TODO: VR
    // UNITY_SETUP_INSTANCE_ID(unpacked);
    // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    return surfaceDescription.Color;
}
