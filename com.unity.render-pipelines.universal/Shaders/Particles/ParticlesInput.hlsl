#ifndef UNIVERSAL_PARTICLES_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct AttributesParticle
{
    float4 vertex : POSITION;
    half4 color : COLOR;

    #if defined(_FLIPBOOKBLENDING_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        float4 texcoords : TEXCOORD0;
        float texcoordBlend : TEXCOORD1;
    #else
        float2 texcoords : TEXCOORD0;
    #endif

    #if !defined(PARTICLES_EDITOR_META_PASS)
        float3 normal : NORMAL;
        float4 tangent : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsParticle
{
    float4 clipPos                  : SV_POSITION;
    float2 texcoord                 : TEXCOORD0;
    half4 color                     : COLOR;

    #if defined(_FLIPBOOKBLENDING_ON)
        float3 texcoord2AndBlend    : TEXCOORD5;
    #endif

    #if !defined(PARTICLES_EDITOR_META_PASS)
        float4 positionWS           : TEXCOORD1;

        #ifdef _NORMALMAP
            float4 normalWS         : TEXCOORD2;    // xyz: normal, w: viewDir.x
            float4 tangentWS        : TEXCOORD3;    // xyz: tangent, w: viewDir.y
            float4 bitangentWS      : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
        #else
            float3 normalWS         : TEXCOORD2;
            float3 viewDirWS        : TEXCOORD3;
        #endif

        #if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
            float4 projectedPosition: TEXCOORD6;
        #endif

        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            float4 shadowCoord      : TEXCOORD7;
        #endif

        float3 vertexSH             : TEXCOORD8; // SH
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // UNIVERSAL_PARTICLES_INPUT_INCLUDED
