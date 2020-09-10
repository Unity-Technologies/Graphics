struct Attributes
{
    float4 positionOS       : POSITION;
    float2 uv               : TEXCOORD0;
    float2 lightmapUV       : TEXCOORD1;
    float3 normalOS         : NORMAL;
    float4 tangentOS        : TANGENT;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float3 uv0AndFogCoord           : TEXCOORD0; // xy: uv0, z: fogCoord
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
    half3 normalWS                  : TEXCOORD2;
#if defined(_NORMALMAP)
    half4 tangentWS                 : TEXCOORD3;
#endif
    float4 vertex : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.vertex = vertexInput.positionCS;
    output.uv0AndFogCoord.xy = TRANSFORM_TEX(input.uv, _BaseMap);
    output.uv0AndFogCoord.z = ComputeFogFactor(vertexInput.positionCS.z);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
#if defined(_NORMALMAP)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.vertexSH);

    return output;
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uv0AndFogCoord.xy;
    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 color = texColor.rgb * _BaseColor.rgb;
    half alpha = texColor.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#if defined(_PRESERVE_SPECULAR)
    #if defined(_ALPHAPREMULTIPLY_ON)
    // NOTE: src color has alpha multiplied.
    #else
        color *= alpha;
    #endif
#endif

#if defined(_ALPHAMODULATE_ON)
    color = lerp(1, color, alpha);
#endif

#if defined(_NORMALMAP)
    half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap)).xyz;
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS));
#else
    half3 normalWS = input.normalWS;
#endif
    normalWS = NormalizeNormalPerPixel(normalWS);
    color *= SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
    #if defined(_SCREEN_SPACE_OCCLUSION)
        float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.vertex);
        color *= SampleAmbientOcclusion(normalizedScreenSpaceUV);
    #endif
    color = MixFog(color, input.uv0AndFogCoord.z);
    alpha = OutputAlpha(alpha, _Surface);

    return half4(color, alpha);
}
