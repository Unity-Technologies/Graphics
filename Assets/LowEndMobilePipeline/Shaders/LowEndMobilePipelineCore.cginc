#define DEBUG_CASCADES 0
#define MAX_SHADOW_CASCADES 4
#define MAX_LIGHTS 8

#define INITIALIZE_LIGHT(light, lightIndex) \
                            light.pos = globalLightPos[lightIndex]; \
                            light.color = globalLightColor[lightIndex]; \
                            light.atten = globalLightAtten[lightIndex]; \
                            light.spotDir = globalLightSpotDir[lightIndex]

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

struct LightInput
{
    half4 pos;
    half4 color;
    half4 atten;
    half4 spotDir;
};

struct LowendVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float3 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
};

struct v2f
{
    float4 uv01 : TEXCOORD0; // uv01.xy: uv0, uv01.zw: uv1
    float3 posWS : TEXCOORD1;
#if _NORMALMAP
    half3 tangentToWorld0 : TEXCOORD2; // tangentToWorld matrix
    half3 tangentToWorld1 : TEXCOORD3; // tangentToWorld matrix
    half3 tangentToWorld2 : TEXCOORD4; // tangentToWorld matrix
#else
    half3 normal : TEXCOORD2;
#endif
    half4 viewDir : TEXCOORD5; // xyz: viewDir
    UNITY_FOG_COORDS_PACKED(6, half4) // x: fogCoord, yzw: vertexColor
#ifndef _SHADOW_CASCADES
    float4 shadowCoord : TEXCOORD7;
#endif
    float4 hpos : SV_POSITION;
};

// The variables are very similar to built-in unity_LightColor, unity_LightPosition,
// unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
// we use world space positions instead of view space.
half4 globalLightColor[MAX_LIGHTS];
float4 globalLightPos[MAX_LIGHTS];
half4 globalLightSpotDir[MAX_LIGHTS];
half4 globalLightAtten[MAX_LIGHTS];
int4  globalLightCount; // x: pixelLightCount, y = totalLightCount (pixel + vert)

sampler2D_float _ShadowMap;
float _PCFKernel[8];

half4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
float4 _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];
half _Shininess;
samplerCUBE _Cube;
half4 _ReflectColor;

inline void NormalMap(v2f i, out half3 normal)
{
#if _NORMALMAP
    half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

    // glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
    // i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance.
    // The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
    normal = half3(dot(normalmap, i.tangentToWorld0), dot(normalmap, i.tangentToWorld1), dot(normalmap, i.tangentToWorld2));
#else
    normal = normalize(i.normal);
#endif
}

inline void SpecularGloss(half2 uv, half3 diffuse, half alpha, out half4 specularGloss)
{
#ifdef _SPECGLOSSMAP
    specularGloss = tex2D(_SpecGlossMap, uv) * _SpecColor;
#elif defined(_SPECGLOSSMAP_BASE_ALPHA)
    specularGloss = tex2D(_SpecGlossMap, uv) * _SpecColor;
    specularGloss.a = alpha;
#else
    specularGloss = _SpecColor;
#endif
}

inline void Emission(v2f i, out half3 emission)
{
#ifdef _EMISSION_MAP
    emission = tex2D(_EmissionMap, i.uv01.xy) * _EmissionColor;
#else
    emission = _EmissionColor;
#endif
}

inline void Indirect(v2f i, half3 diffuse, half3 normal, half glossiness, out half3 indirect)
{
#ifdef LIGHTMAP_ON
    indirect = (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) + i.fogCoord.yzw) * diffuse;
#else
    indirect = i.fogCoord.yzw * diffuse;
#endif

    // TODO: we can use reflect vec to compute specular instead of half when computing cubemap reflection
#ifdef _CUBEMAP_REFLECTION
    half3 reflectVec = reflect(-i.viewDir.xyz, normal);
    half3 indirectSpecular = texCUBE(_Cube, reflectVec) * _ReflectColor * glossiness;
    indirect += indirectSpecular;
#endif
}

half4 OutputColor(half3 color, half alpha)
{
#ifdef _ALPHABLEND_ON
    return half4(color, alpha);
#else
    return half4(color, 1);
#endif
}

inline half ComputeCascadeIndex(float3 wpos)
{
    float3 fromCenter0 = wpos.xyz - _DirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - _DirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - _DirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - _DirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 vDirShadowSplitSphereSqRadii;
    vDirShadowSplitSphereSqRadii.x = _DirShadowSplitSpheres[0].w;
    vDirShadowSplitSphereSqRadii.y = _DirShadowSplitSpheres[1].w;
    vDirShadowSplitSphereSqRadii.z = _DirShadowSplitSpheres[2].w;
    vDirShadowSplitSphereSqRadii.w = _DirShadowSplitSpheres[3].w;
    fixed4 weights = fixed4(distances2 < vDirShadowSplitSphereSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return 4 - dot(weights, fixed4(4, 3, 2, 1));
}

inline half ShadowAttenuation(half3 shadowCoord)
{
    if (shadowCoord.x <= 0 || shadowCoord.x >= 1 || shadowCoord.y <= 0 || shadowCoord.y >= 1)
        return 1;

    half depth = tex2D(_ShadowMap, shadowCoord).r;
#if defined(UNITY_REVERSED_Z)
    return step(depth, shadowCoord.z);
#else
    return step(shadowCoord.z, depth);
#endif
}

inline half ShadowPCF(half3 shadowCoord)
{
    // TODO: simulate textureGatherOffset not available, simulate it
    half2 offset = half2(0, 0);
    half attenuation = ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[0], _PCFKernel[1]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[2], _PCFKernel[3]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[4], _PCFKernel[5]) + offset, shadowCoord.z)) +
        ShadowAttenuation(half3(shadowCoord.xy + half2(_PCFKernel[6], _PCFKernel[7]) + offset, shadowCoord.z));
    return attenuation * 0.25;
}

inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float3 posWorld, half3 viewDir)
{
    float3 posToLight = lightInput.pos.xyz;
    posToLight -= posWorld * lightInput.pos.w;

    float distanceSqr = max(dot(posToLight, posToLight), 0.001);
    float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

    half3 lightDir = posToLight * rsqrt(distanceSqr);
    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
    lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

    half cutoff = step(distanceSqr, lightInput.atten.w);
    lightAtten *= cutoff;

    half NdotL = saturate(dot(normal, lightDir));

    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));

    half3 lightColor = lightInput.color.rgb * lightAtten;
    half3 diffuse = diffuseColor * lightColor * NdotL;

#if defined(_SHARED_SPECULAR_DIFFUSE) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 specular = specularGloss.rgb * lightColor * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return diffuse + specular;
#else
    return diffuse;
#endif
}

inline half ComputeShadowAttenuation(v2f i)
{
#ifndef _SHADOW_CASCADES
    half4 shadowCoord;
    shadowCoord = i.shadowCoord;
#else
    half4 shadowCoord;
    int cascadeIndex = ComputeCascadeIndex(i.posWS);
    if (cascadeIndex < 4)
        shadowCoord = mul(_WorldToShadow[cascadeIndex], half4(i.posWS, 1.0));
    else
        return 1.0;
#endif

    shadowCoord.xyz /= shadowCoord.w;
    shadowCoord.z = saturate(shadowCoord.z);

#if defined(_SOFT_SHADOWS) || defined(_SOFT_SHADOWS_CASCADES)
    return ShadowPCF(shadowCoord.xyz);
#else
    return ShadowAttenuation(shadowCoord.xyz);
#endif
}
