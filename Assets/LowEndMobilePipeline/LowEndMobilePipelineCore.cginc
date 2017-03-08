#define DEBUG_CASCADES 0
#define MAX_SHADOW_CASCADES 4
#define MAX_LIGHTS 8

#define INITIALIZE_LIGHT(light, lightIndex) \
							light.pos = globalLightPos[lightIndex]; \
							light.color = globalLightColor[lightIndex]; \
							light.atten = globalLightAtten[lightIndex]; \
							light.spotDir = globalLightSpotDir[lightIndex]

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
    float4 posWS : TEXCOORD1; // xyz: posWorld, w: eyeZ
#if _NORMALMAP
    half3 tangentToWorld[3] : TEXCOORD2; // tangentToWorld matrix
#else
    half3 normal : TEXCOORD2;
#endif
    half4 viewDir : TEXCOORD5; // xyz: viewDir
    UNITY_FOG_COORDS_PACKED(6, half4) // x: fogCoord, yzw: vertexColor
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
float4 g_vDirShadowSplitSpheres[MAX_SHADOW_CASCADES];
half4 _PSSMDistancesAndShadowResolution; // xyz: PSSM Distance for 4 cascades, w: 1 / shadowmap resolution. Used for filtering
half _SpecularStrength;


inline void NormalMap(v2f i, out half3 normal)
{
#if _NORMALMAP
    half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

    // glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
    // i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance.
    // The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
    normal = half3(dot(normalmap, i.tangentToWorld[0]), dot(normalmap, i.tangentToWorld[1]), dot(normalmap, i.tangentToWorld[2]));
#else
    normal = normalize(i.normal);
#endif
}

inline void SpecularGloss(half3 diffuse, half alpha, out half4 specularGloss)
{
#ifdef _SHARED_SPECULAR_DIFFUSE
    specularGloss.rgb = diffuse;
    specularGloss.a = alpha;
#elif defined(_SHARED_SPECULAR_DIFFUSE)
    #if _GLOSSINESS_FROM_BASE_ALPHA
        specularGloss.rgb = tex2D(_SpecGlossMap, i.uv01.xy) * _SpecColor;
        specularGloss.a = alpha;
    #else
        specularGloss = tex2D(_SpecGlossMap, i.uv01.xy) * _SpecColor;
    #endif
#else
    #if _GLOSSINESS_FROM_BASE_ALPHA
        specularGloss.rgb = _SpecColor;
        specularGloss.a = alpha;
    #else
        specularGloss = _SpecColor;
    #endif
#endif
}

inline void IndirectDiffuse(v2f i, half3 diffuse, out half3 indirectDiffuse)
{
#ifdef LIGHTMAP_ON
    indirectDiffuse = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) * diffuse;
#else
    indirectDiffuse = i.fogCoord.yzw * diffuse;
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
    float3 fromCenter0 = wpos.xyz - g_vDirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - g_vDirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - g_vDirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - g_vDirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 vDirShadowSplitSphereSqRadii;
    vDirShadowSplitSphereSqRadii.x = g_vDirShadowSplitSpheres[0].w;
    vDirShadowSplitSphereSqRadii.y = g_vDirShadowSplitSpheres[1].w;
    vDirShadowSplitSphereSqRadii.z = g_vDirShadowSplitSpheres[2].w;
    vDirShadowSplitSphereSqRadii.w = g_vDirShadowSplitSpheres[3].w;
    fixed4 weights = float4(distances2 < vDirShadowSplitSphereSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return 4 - dot(weights, float4(4, 3, 2, 1));
}

inline half ShadowAttenuation(half2 shadowCoord, half shadowCoordDepth)
{
    if (shadowCoord.x <= 0 || shadowCoord.x >= 1 || shadowCoord.y <= 0 || shadowCoord.y >= 1)
        return 1;

    half depth = tex2D(_ShadowMap, shadowCoord).r;
#if defined(UNITY_REVERSED_Z)
    return step(depth, shadowCoordDepth);
#else
    return step(shadowCoordDepth, depth);
#endif
}

inline half ShadowPCF(half4 shadowCoord)
{
    // TODO: simulate textureGatherOffset not available, simulate it
    half2 offset = half2(0, 0);
    half attenuation = ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[0], _PCFKernel[1]) + offset, shadowCoord.z) +
        ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[2], _PCFKernel[3]) + offset, shadowCoord.z) +
        ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[4], _PCFKernel[5]) + offset, shadowCoord.z) +
        ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[6], _PCFKernel[7]) + offset, shadowCoord.z);
    return attenuation * 0.25;
}

inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float3 posWorld, half3 viewDir)
{
    float3 posToLight = lightInput.pos.xyz;
    posToLight -= posWorld * lightInput.pos.w;

    float distanceSqr = max(dot(posToLight, posToLight), 0.001);
    float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

    float3 lightDir = posToLight * rsqrt(distanceSqr);
    float SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
    lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

    float cutoff = step(distanceSqr, lightInput.atten.w);
    lightAtten *= cutoff;

    float NdotL = saturate(dot(normal, lightDir));

    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));

    half3 lightColor = lightInput.color.rgb * lightAtten;
    half3 diffuse = diffuseColor * lightColor * NdotL;

#if defined(_SHARED_SPECULAR_DIFFUSE) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 specular = specularGloss.rgb * lightColor * pow(NdotH, 256.0 - _SpecularStrength) * specularGloss.a;
    return diffuse + specular;
#else
    return diffuse;
#endif
}

inline half3 EvaluateMainLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float4 posWorld, half3 viewDir)
{
    half3 color = EvaluateOneLight(lightInput, diffuseColor, specularGloss, normal, posWorld, viewDir);

#if defined(HARD_SHADOWS) || defined(SOFT_SHADOWS)
    int cascadeIndex = ComputeCascadeIndex(posWorld);

    half shadowAttenuation = 1.0;
    if (cascadeIndex < 4)
    {
        float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorld.xyz, 1.0));
        shadowCoord.z = saturate(shadowCoord.z);

#ifdef SOFT_SHADOWS
    shadowAttenuation = ShadowPCF(shadowCoord);
#else
    shadowAttenuation = ShadowAttenuation(shadowCoord.xy, shadowCoord.z);
#endif
    }

#if DEBUG_CASCADES
    half3 cascadeColors[MAX_SHADOW_CASCADES] = {half3(1.0, 0.0, 0.0), half3(0.0, 1.0, 0.0),  half3(0.0, 0.0, 1.0),  half3(1.0, 0.0, 1.0)};
    return cascadeColors[cascadeIndex] * diffuseColor * max(shadowAttenuation, 0.5);
#endif

    return color * shadowAttenuation;
#else
    return color;
#endif
}
