#ifndef _NORMALS_2D_COMMON
#define _NORMALS_2D_COMMON

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

Varyings CommonNormalsVertex(Attributes input)
{
    Varyings o = (Varyings) 0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.uv = input.uv;
    o.normalWS = TransformObjectToWorldDir(input.normal);
    o.tangentWS = TransformObjectToWorldDir(input.tangent.xyz);
    o.bitangentWS = cross(o.normalWS, o.tangentWS) * input.tangent.w;
    return o;
}

half4 CommonNormalsFragment(Varyings input, half4 color)
{
    const half4 mainTex = color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

    return NormalsRenderingShared(mainTex, normalTS, input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
}

#endif // _NORMALS_2D_COMMON
