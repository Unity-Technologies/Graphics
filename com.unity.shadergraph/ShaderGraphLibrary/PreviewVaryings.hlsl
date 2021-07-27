Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

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

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionWS = positionWS;
#endif

#ifdef VARYINGS_NEED_POSITIONPREDISPLACEMENT_WS
    output.positionPredisplacementWS = positionWS;
#endif

#ifdef VARYINGS_NEED_NORMAL_WS
    output.normalWS = normalize(normalWS);
#endif

#ifdef VARYINGS_NEED_TANGENT_WS
    output.tangentWS = normalize(tangentWS);
#endif

    output.positionCS = TransformWorldToHClip(positionWS);

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

#if defined(VARYINGS_NEED_VERTEXID)
    output.vertexID = input.vertexID;
#endif

#ifdef VARYINGS_NEED_VIEWDIRECTION_WS
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - positionWS;
#endif

#ifdef VARYINGS_NEED_BITANGENT_WS
    output.bitangentWS = cross(normalWS, tangentWS.xyz) * tangentWS.w;
#endif

#ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = ComputeScreenPos(output.positionCS, _ProjectionParams.x);
#endif

    return output;
}
