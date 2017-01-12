
// TODO: Here we will also have all the vertex deformation (GPU skinning, vertex animation, morph target...) or we will need to generate a compute shaders instead (better! but require work to deal with unpacking like fp16)
PackedVaryingsType Vert(Attributes input)
{
    VaryingsType output;

#if defined(TESSELLATION_ON)
    output.positionWS = TransformObjectToWorld(input.positionOS);
    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)  
#if defined(VARYING_DS_WANT_TANGENT_TO_WORLD) || defined(VARYING_DS_WANT_NORMAL)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
#if defined(VARYING_DS_WANT_TANGENT_TO_WORLD) || defined(VARYING_DS_WANT_TANGENT)
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif
#else
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    output.positionCS = TransformWorldToHClip(positionWS);
#ifdef VARYING_WANT_TANGENT_TO_WORLD
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    output.tangentToWorld[0] = tangentToWorld[0];
    output.tangentToWorld[1] = tangentToWorld[1];
    output.tangentToWorld[2] = tangentToWorld[2];
#endif
#endif

#if defined(VARYING_WANT_TEXCOORD0) || defined(VARYING_DS_WANT_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYING_WANT_TEXCOORD1) || defined(VARYING_DS_WANT_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYING_WANT_TEXCOORD2) || defined(VARYING_DS_WANT_TEXCOORD2)
    output.texCoord2 = input.uv2;
#endif
#if defined(VARYING_WANT_TEXCOORD3) || defined(VARYING_DS_WANT_TEXCOORD3)
    output.texCoord3 = input.uv3;
#endif
#if defined(VARYING_WANT_COLOR) || defined(VARYING_DS_WANT_COLOR)
    output.color = input.color;
#endif

    return PackVaryingsType(output);
}
