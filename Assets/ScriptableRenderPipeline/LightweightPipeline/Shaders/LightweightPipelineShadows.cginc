#define MAX_SHADOW_CASCADES 4

sampler2D_float _ShadowMap;
float _PCFKernel[8];
float4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
float4 _DirShadowSplitSpheres[MAX_SHADOW_CASCADES];

inline half ShadowAttenuation(float3 shadowCoord)
{
    if (shadowCoord.x <= 0 || shadowCoord.x >= 1 || shadowCoord.y <= 0 || shadowCoord.y >= 1)
        return 1;

    float depth = tex2D(_ShadowMap, shadowCoord).r;
#if defined(UNITY_REVERSED_Z)
    return step(depth, shadowCoord.z);
#else
    return step(shadowCoord.z, depth);
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

inline half ComputeShadowAttenuation(v2f i, float bias)
{
#if _NORMALMAP
    float3 vertexNormal = float3(i.tangentToWorld0.z, i.tangentToWorld1.z, i.tangentToWorld2.z);
#else
    float3 vertexNormal = i.normal;
#endif
    float3 offset = vertexNormal * bias;

    float3 posWorldOffsetNormal = i.posWS + offset;
    int cascadeIndex = 0;

#ifdef _SHADOW_CASCADES
    cascadeIndex = ComputeCascadeIndex(i.posWS);
    if (cascadeIndex >= MAX_SHADOW_CASCADES)
        return 1.0;
#endif
    float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorldOffsetNormal, 1.0));
    shadowCoord.xyz /= shadowCoord.w;
    shadowCoord.z = saturate(shadowCoord.z);

#if defined(_SOFT_SHADOWS) || defined(_SOFT_SHADOWS_CASCADES)
    return ShadowPCF(shadowCoord.xyz);
#else
    return ShadowAttenuation(shadowCoord.xyz);
#endif
}
