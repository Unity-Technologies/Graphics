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
    return UnpackNormal(tex2D(_BumpMap, uv));
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

inline void SpecularGloss(half2 uv, half alpha, out half4 specularGloss)
{
    specularGloss = half4(0, 0, 0, 1);
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv);
    specularGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
#elif defined(_SPECULAR_COLOR)
    specularGloss = _SpecColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularGloss.a = alpha;
#endif
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
#if _SPECUALR_SETUP
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

#if _METALLIC_SETUP
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#else
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
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

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.posWS = worldPos;

    half3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    o.viewDir = viewDir;

#if _NORMALMAP
    OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
    o.normal = normalize(UnityObjectToWorldNormal(v.normal));
#endif

#if defined(EVALUATE_SH_VERTEX) || defined(EVALUATE_SH_MIXED)
    o.vertexSH = half4(EvaluateSHPerVertex(o.normal), 0.0);
#endif

    o.fogFactorAndVertexLight.yzw = half3(0.0h, 0.0h, 0.0h);
#if defined(_VERTEX_LIGHTS)
    half3 diffuse = half3(1.0, 1.0, 1.0);
    int vertexLightStart = _AdditionalLightCount.x;
    int vertexLightEnd = min(_AdditionalLightCount.y, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
    {
        LightInput lightData;
        INITIALIZE_LIGHT(lightData, lightIter);

        half3 lightDirection;
        half atten = ComputeVertexLightAttenuation(lightData, o.normal, worldPos, lightDirection);
        o.fogFactorAndVertexLight.yzw += LightingLambert(diffuse, lightDirection, o.normal, atten) * lightData.color;
    }
#endif

    float4 clipPos = UnityObjectToClipPos(v.vertex);
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
    half3 diffuseGI = SampleLightmap(IN.uv01.zw, normalWS);
#else
    half3 diffuseGI = EvaluateSHPerPixel(normalWS, IN.vertexSH);
#endif

#if _VERTEX_LIGHTS
    diffuseGI += IN.fogFactorAndVertexLight.yzw;
#endif

    float fogFactor = IN.fogFactorAndVertexLight.x;
    return LightweightFragmentPBR(IN.posWS, normalWS, IN.viewDir, fogFactor, diffuseGI, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
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
    half3 normalWorld = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWorld = normalize(IN.normal);
#endif

    half4 specularGloss;
    SpecularGloss(uv, alpha, specularGloss);

    half3 viewDir = IN.viewDir.xyz;
    float3 worldPos = IN.posWS.xyz;

    half3 lightDirection;

#if defined(LIGHTMAP_ON)
    half3 color = SampleLightmap(lightmapUV, normalWorld) * diffuse;
#else
    half3 color = EvaluateSHPerPixel(normalWorld, IN.vertexSH) * diffuse;
#endif

    half shininess = _Shininess * 128.0h;

    LightInput lightInput;
    INITIALIZE_MAIN_LIGHT(lightInput);
    half lightAtten = ComputeMainLightAttenuation(lightInput, normalWorld, worldPos, lightDirection);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(worldPos, normalize(IN.normal), _ShadowLightDirection.xyz);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normalWorld, viewDir, lightAtten, shininess) * lightInput.color;
#else
    color += LightingLambert(diffuse, lightDirection, normalWorld, lightAtten) * lightInput.color;
#endif

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput lightData;
        INITIALIZE_LIGHT(lightData, lightIter);
        half lightAtten = ComputePixelLightAttenuation(lightData, normalWorld, worldPos, lightDirection);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
        color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normalWorld, viewDir, lightAtten, shininess) * lightData.color;
#else
        color += LightingLambert(diffuse, lightDirection, normalWorld, lightAtten) * lightData.color;
#endif
    }

#endif // _ADDITIONAL_LIGHTS

    color += Emission(uv);
    color += IN.fogFactorAndVertexLight.yzw;

    // Computes Fog Factor per vextex
    ApplyFog(color, IN.fogFactorAndVertexLight.x);
    return OutputColor(color, alpha);
};

#endif
