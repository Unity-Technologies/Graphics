#if (SHADERPASS == SHADERPASS_SHADOWCASTER)
    float3 _LightDirection;
#endif

Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(FEATURES_GRAPH_VERTEX)
    // Evaluate Vertex Graph
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    // Assign modified vertex attributes
    input.positionOS = vertexDescription.VertexPosition;
    #if defined(VARYINGS_NEED_NORMAL_WS)
        input.normalOS = vertexDescription.VertexNormal;
    #endif //FEATURES_GRAPH_NORMAL
    #if defined(VARYINGS_NEED_TANGENT_WS)
        input.tangentOS.xyz = vertexDescription.VertexTangent.xyz;
    #endif //FEATURES GRAPH TANGENT
#endif //FEATURES_GRAPH_VERTEX

    // TODO: Avoid path via VertexPositionInputs (Universal)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // Returns the camera relative position (if enabled)
    float3 positionWS = TransformObjectToWorld(input.positionOS);

#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#else
    // Required to compile ApplyVertexModification that doesn't use normal.
    float3 normalWS = float3(0.0, 0.0, 0.0);
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

    // TODO: Change to inline ifdef
    // Do vertex modification in camera relative space (if enabled)
#if defined(HAVE_VERTEX_MODIFICATION)
    ApplyVertexModification(input, normalWS, positionWS, _TimeParameters.xyz);
#endif

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS = positionWS;
#endif

#ifdef VARYINGS_NEED_NORMAL_WS
    output.normalWS = normalWS;			// normalized in TransformObjectToWorldNormal()
#endif

#ifdef VARYINGS_NEED_TANGENT_WS
    output.tangentWS = tangentWS;		// normalized in TransformObjectToWorldDir()
#endif

#if (SHADERPASS == SHADERPASS_SHADOWCASTER)
    // Define shadow pass specific clip position for Universal
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
    #if UNITY_REVERSED_Z
        output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
#elif (SHADERPASS == SHADERPASS_META)
    output.positionCS = MetaVertexPosition(float4(input.positionOS, 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
    output.texCoord2 = input.uv2;
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
    output.texCoord3 = input.uv3;
#endif

#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.color = input.color;
#endif

#ifdef VARYINGS_NEED_VIEWDIRECTION_WS
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - positionWS;
#endif

#ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = ComputeScreenPos(output.positionCS, _ProjectionParams.x);
#endif

#if (SHADERPASS == SHADERPASS_FORWARD)
    OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(normalWS, output.sh);
#endif

#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    half3 vertexLight = VertexLighting(positionWS, normalWS);
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}
