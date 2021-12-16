#ifndef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
};

Varyings ShadowPassVertex
(
#ifdef BRG_DRAW_PROCEDURAL
    uint vertexID : SV_VertexID
#else
    Attributes input
#endif
)
{
    Varyings output = (Varyings)0;

#ifdef BRG_DRAW_PROCEDURAL
    float3 positionOS = LoadBRGProcedural_Position(vertexID);
    float3 normalOS = LoadBRGProcedural_Normal(vertexID);
    float2 uv0 = LoadBRGProcedural_UV0(vertexID);
#else
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float2 uv0 = input.texcoord;
#endif

    float3 positionWS = TransformObjectToWorld(positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    output.uv = TRANSFORM_TEX(uv0, _BaseMap);
    output.positionCS = positionCS;
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    return 0;
}

#endif
