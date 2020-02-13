PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

struct Targets
{
    float4  color   : SV_Target0;
#if USE_MASK
    float4  mask    : SV_Target1;
#endif

#if USE_NORMAL_MAP
#if USE_MASK
    float4  normal  : SV_Target2;
#else
    float4  normal  : SV_Target1;
#endif
#endif
};

Targets frag(PackedVaryings packedInput)
{
    Targets o = (Targets)0;

    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    float4 mainTex = unpacked.color * surfaceDescription.Color;
    mainTex.rgb *= mainTex.a;
    o.color = mainTex;

#if USE_MASK
    half4 maskTex = surfaceDescription.Mask;
    maskTex.a = mainTex.a;
    maskTex.rgb *= maskTex.a;
    o.mask = maskTex;
#endif

#if USE_NORMAL_MAP
    float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
    float4 normalVS = NormalsRenderingShared(mainTex, surfaceDescription.Normal, unpacked.tangentWS.xyz, bitangent, unpacked.normalWS);
    normalVS.rgb *= normalVS.a;
    o.normal = normalVS;
#endif

    return o;
}
