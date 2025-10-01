#ifndef INPUT_CORE_2D_INCLUDED
#define INPUT_CORE_2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#if defined(DEBUG_DISPLAY)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
#endif

// Lit
#define COMMON_2D_LIT_OUTPUTS           \
        COMMON_2D_OUTPUTS               \
        half2 lightingUV  : TEXCOORD1;

// Unlit
#define COMMON_2D_INPUTS                 \
        float3 positionOS   : POSITION;  \
        float2 uv           : TEXCOORD0; \
        float3 normal       : NORMAL;    \
        UNITY_VERTEX_INPUT_INSTANCE_ID

#define COMMON_2D_OUTPUTS_SHARED              \
        float4 positionCS      : SV_POSITION; \
        float2 uv              : TEXCOORD0;   \
        UNITY_VERTEX_OUTPUT_STEREO

#if defined(DEBUG_DISPLAY)
    #define COMMON_2D_OUTPUTS                \
            COMMON_2D_OUTPUTS_SHARED         \
            float3 positionWS  : TEXCOORD2;  \
            half3  normalWS    : TEXCOORD3;
#else
    #define COMMON_2D_OUTPUTS                \
            COMMON_2D_OUTPUTS_SHARED             
#endif

// Normals
#define COMMON_2D_NORMALS_INPUTS       \
        COMMON_2D_INPUTS               \
        float4 tangent      : TANGENT; \

#define COMMON_2D_NORMALS_OUTPUTS          \
        COMMON_2D_OUTPUTS_SHARED           \
        half3 normalWS        : TEXCOORD1; \
        half3 tangentWS       : TEXCOORD2; \
        half3 bitangentWS     : TEXCOORD3;

#if defined(SKINNED_SPRITE)

    #define UNITY_SKINNED_VERTEX_INPUTS         float4 weights : BLENDWEIGHTS; uint4 indices : BLENDINDICES;
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)     x.positionOS = UnitySkinSprite(x.positionOS, x.indices, x.weights, unity_SpriteProps.z, 1.0f);

#else

    #define UNITY_SKINNED_VERTEX_INPUTS
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)

#endif

uniform StructuredBuffer<float4x4> _SpriteBoneTransforms;

float3 UnityFlipSprite( in float3 pos, in float2 flip )
{
    return float3(pos.xy * flip, pos.z);
}

float3 UnitySkinSprite( in float3 inputData, in uint4 blendIndices, in float4 blendWeights, in float offset, in float w )
{
    float4 outputData = float4(inputData, w);

#if defined(SKINNED_SPRITE) && !defined(SHADERGRAPH_PREVIEW)
    UNITY_BRANCH
    if (offset >= 0)
    {
        outputData =
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.x], outputData) * blendWeights.x +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.y], outputData) * blendWeights.y +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.z], outputData) * blendWeights.z +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.w], outputData) * blendWeights.w;
    }
#endif // SKINNED_SPRITE

    return outputData.xyz;
}

#ifdef UNITY_INSTANCING_ENABLED
    UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
        // SpriteRenderer.Color while Non-Batched/Instanced.
        UNITY_DEFINE_INSTANCED_PROP(float4, unity_SpriteRendererColorArray)
        // this could be smaller but that's how bit each entry is regardless of type
        UNITY_DEFINE_INSTANCED_PROP(float2, unity_SpriteFlipArray)            
    UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

    #define unity_SpriteColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
    #define unity_SpriteFlip   UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)
#endif // instancing

void SetUpSpriteInstanceProperties()
{
#ifdef UNITY_INSTANCING_ENABLED
    unity_SpriteProps.xy = unity_SpriteFlip;
#endif
}

#endif
