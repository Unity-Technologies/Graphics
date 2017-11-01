#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LightweightLighting.cginc"

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) tex2D(_MetallicGlossMap, uv)
#endif

half4 _Color;
sampler2D _MainTex; half4 _MainTex_ST;
half _Cutoff;
half _Glossiness;
half _GlossMapScale;
half _SmoothnessTextureChannel;
half _Metallic;
sampler2D _MetallicGlossMap;
half4 _SpecColor;
sampler2D _SpecGlossMap;
half _BumpScale;
sampler2D _BumpMap;
half _OcclusionStrength;
sampler2D _OcclusionMap;
half4 _EmissionColor;
sampler2D _EmissionMap;
half _Shininess;

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LightweightVertexOutput
{
    float4 uv01                     : TEXCOORD0; // xy: main UV, zw: lightmap UV (directional / non-directional)
    float3 posWS                    : TEXCOORD1;
#if _NORMALMAP
    half3 tangent                   : TEXCOORD2;
    half3 binormal                  : TEXCOORD3;
    half3 normal                    : TEXCOORD4;
#else
    half3 normal                    : TEXCOORD2;
#endif
    half3 viewDir                   : TEXCOORD5;
    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
#if defined(EVALUATE_SH_VERTEX) || defined(EVALUATE_SH_MIXED)
    half4 vertexSH                  : TEXCOORD7;
#endif
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

inline half Alpha(half albedoAlpha)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    half alpha = _Color.a;
#else
    half alpha = albedoAlpha * _Color.a;
#endif

#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

    return alpha;
}

half3 Normal(float2 uv)
{
#if _NORMALMAP
    return UnpackNormalScale(tex2D(_BumpMap, uv), _BumpScale);
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

half SpecularGloss(half2 uv, half alpha)
{
    half4 specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv);
    specularGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
#elif defined(_SPECULAR_COLOR)
    specularGloss = _SpecColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
    return specularGloss;
}

half4 MetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = specGloss = SAMPLE_METALLICSPECULAR(uv);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _GlossMapScale;
#else
    specGloss.a *= _GlossMapScale;
#endif

#else // _METALLICSPECGLOSSMAP
#if _SPECULAR_SETUP
    specGloss.rgb = _SpecColor.rgb;
#else
    specGloss.rgb = _Metallic.rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _GlossMapScale;
#else
    specGloss.a = _Glossiness;
#endif
#endif

    return specGloss;
}

half Occlusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
#if (SHADER_TARGET < 30)
    // SM20: instruction count limitation
    // SM20: simpler occlusion
    return tex2D(_OcclusionMap, uv).g;
#else
    half occ = tex2D(_OcclusionMap, uv).g;
    return _LerpOneTo(occ, _OcclusionStrength);
#endif
#else
    return 1.0;
#endif
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
    return LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, uv).rgb) * _EmissionColor.rgb;
#endif
}

inline void InitializeStandardLitSurfaceData(LightweightVertexOutput IN, out SurfaceData outSurfaceData)
{
    float2 uv = IN.uv01.xy;
    half4 albedoAlpha = tex2D(_MainTex, uv);

    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha);
    outSurfaceData.albedo = LIGHTWEIGHT_GAMMA_TO_LINEAR(albedoAlpha.rgb) * _Color.rgb;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normal = Normal(uv);
    outSurfaceData.occlusion = Occlusion(uv);
    outSurfaceData.emission = Emission(uv);
    outSurfaceData.alpha = Alpha(albedoAlpha.a);
}

LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
    o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

    float3 positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
    half3 viewDirectionWS = SafeNormalize(_WorldSpaceCameraPos - positionWS);

#if _NORMALMAP
    OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
    o.normal = UnityObjectToWorldNormal(v.normal);
#endif

    float4 clipPos = UnityObjectToClipPos(v.vertex);

#if defined(EVALUATE_SH_VERTEX) || defined(EVALUATE_SH_MIXED)
    o.vertexSH = half4(EvaluateSHPerVertex(o.normal), 0.0);
#endif

    o.posWS = positionWS;
    o.viewDir = viewDirectionWS;
    o.fogFactorAndVertexLight.yzw = VertexLighting(positionWS, o.normal);
    o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
    o.clipPos = clipPos;

    return o;
}

half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN, surfaceData);

#if _NORMALMAP
    half3 normalWS = TangentToWorldNormal(surfaceData.normal, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

#if LIGHTMAP_ON
    half3 indirectDiffuse = SampleLightmap(IN.uv01.zw, normalWS);
#else
    half3 indirectDiffuse = EvaluateSHPerPixel(normalWS, IN.vertexSH);
#endif

    float fogFactor = IN.fogFactorAndVertexLight.x;
    return LightweightFragmentPBR(IN.posWS, normalWS, IN.viewDir, fogFactor, indirectDiffuse, IN.fogFactorAndVertexLight.yzw, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
}

half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    float2 uv = IN.uv01.xy;
    float2 lightmapUV = IN.uv01.zw;

    half4 diffuseAlpha = tex2D(_MainTex, uv);
    half3 diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(diffuseAlpha.rgb) * _Color.rgb;

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    half alpha = _Color.a;
#else
    half alpha = diffuseAlpha.a * _Color.a;
#endif

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif

#if _NORMALMAP
    half3 normalTangent = Normal(uv);
    half3 normalWS = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWS = normalize(IN.normal);
#endif

    half3 emission = Emission(uv);

    half3 viewDirectionWS = SafeNormalize(IN.viewDir.xyz);
    float3 positionWS = IN.posWS.xyz;

#if defined(LIGHTMAP_ON)
    half3 diffuseGI = SampleLightmap(lightmapUV, normalWS);
#else
    half3 diffuseGI = EvaluateSHPerPixel(normalWS, IN.vertexSH);
#endif

#if _VERTEX_LIGHTS
    diffuseGI += IN.fogFactorAndVertexLight.yzw;
#endif

    half shininess = _Shininess * 128.0h;
    half fogFactor = IN.fogFactorAndVertexLight.x;

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half4 specularGloss = SpecularGloss(uv, alpha);
    return LightweightFragmentBlinnPhong(positionWS, normalWS, viewDirectionWS, fogFactor, diffuseGI, diffuse, specularGloss, shininess, emission, alpha);
#else
    return LightweightFragmentLambert(positionWS, normalWS, viewDirectionWS, fogFactor, diffuseGI, diffuse, emission, alpha);
#endif
};

#endif
