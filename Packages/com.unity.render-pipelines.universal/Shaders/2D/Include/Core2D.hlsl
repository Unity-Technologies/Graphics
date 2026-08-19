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
// unityVertexID (SV_VertexID) is needed by the Unified per-sprite deformation path so the
// vertex shader can index _UnifiedDeformedVerts at unity_SpriteProps.w + vid * 12.
// System values cost no vertex stream slot, so this is safe to add unconditionally.
#define COMMON_2D_INPUTS                       \
        float3 positionOS   : POSITION;        \
        float2 uv           : TEXCOORD0;       \
        float3 normal       : NORMAL;          \
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

// The 2D Animation package writes the raw byte offset into _UnifiedDeformedVerts to unity_SpriteProps.w
// for sprites it owns; -1 means "not Unified-managed"
// (same sentinel convention as unity_SpriteProps.z for bone transforms) and the shader falls through
// to the legacy SKINNED_SPRITE / mesh-stream paths Buffer stride is 12 (pos.xyz). Address = w + vid * 12.
// spriteProps.x first 10 bits dictate vertex stride to make it per sprite configurable.
ByteAddressBuffer _UnifiedDeformedVerts;
uniform StructuredBuffer<float4x4> _SpriteBoneTransforms;

// Per-vertex deform dispatch. Priority:
//   1) unity_SpriteProps.w >= 0 → Unified path: read pre-deformed pos from _UnifiedDeformedVerts
//   2) legacy vertex-shader LBS via _SpriteBoneTransforms
//   3) otherwise no-op (positionOS used as-is, e.g. CPU pre-deformed via SetDeformableBuffer)
// The Unified branch is only compiled if SKINNED_SPRITE is enabled. Requires GPU-Skinning + SRP-Batcher on.

#if defined(SKINNED_SPRITE) && !defined(SHADERGRAPH_PREVIEW)

    #define UNITY_SKINNED_VERTEX_INPUTS         float4 weights : BLENDWEIGHTS; uint4 indices : BLENDINDICES; uint vertexID : SV_VertexID;
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)                                                                                                                     \
                                                {                                                                                                               \
                                                    int unifiedDeformOffset = asint(unity_SpriteProps.w);                                                       \
                                                    UNITY_BRANCH                                                                                                \
                                                    if (unifiedDeformOffset >= 0)                                                                               \
                                                    {                                                                                                           \
                                                        (x).positionOS = UnifiedSpriteDeform((x).positionOS, (x).vertexID, unity_SpriteProps, 1.0f);            \
                                                    }                                                                                                           \
                                                    else                                                                                                        \
                                                    {                                                                                                           \
                                                        (x).positionOS = UnitySkinSprite((x).positionOS, (x).indices, (x).weights, unity_SpriteProps, 1.0f);    \
                                                    }                                                                                                           \
                                                }

#else

    #define UNITY_SKINNED_VERTEX_INPUTS
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)

#endif

float3 UnityFlipSprite(in float3 pos, in float2 flip)
{
    return float3(pos.xy * flip, pos.z);
}

float3 UnifiedSpriteDeform(in float3 inputData, uint vertexId, in float4 spriteProps, in float w)
{
    float4 outputData = float4(inputData, w);

#if defined(SKINNED_SPRITE) && !defined(SHADERGRAPH_PREVIEW)
    uint _ofst = asint(spriteProps.w);
    uint _addr = _ofst + vertexId * 12; // Only pos,
    outputData.xyz = asfloat(_UnifiedDeformedVerts.Load3(_addr));
#endif // SKINNED_SPRITE

    return outputData.xyz;
}

float3 UnitySkinSprite(in float3 inputData, in uint4 blendIndices, in float4 blendWeights, in float4 spriteProps, in float w)
{
    float4 outputData = float4(inputData, w);

#if defined(SKINNED_SPRITE) && !defined(SHADERGRAPH_PREVIEW)
    int boneTransformOffset = asint(spriteProps.z);
    UNITY_BRANCH
    if (boneTransformOffset >= 0)
    {
        outputData =
            mul(_SpriteBoneTransforms[boneTransformOffset + blendIndices.x], outputData) * blendWeights.x +
            mul(_SpriteBoneTransforms[boneTransformOffset + blendIndices.y], outputData) * blendWeights.y +
            mul(_SpriteBoneTransforms[boneTransformOffset + blendIndices.z], outputData) * blendWeights.z +
            mul(_SpriteBoneTransforms[boneTransformOffset + blendIndices.w], outputData) * blendWeights.w;
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
        // To support RSUV in Instanced code-path
        UNITY_DEFINE_INSTANCED_PROP(float, unity_RendererUserValueArray)
    UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

    #define unity_SpriteColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
    #define unity_SpriteFlip   UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)
    #define unity_RendererUserValue asuint(UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_RendererUserValueArray).x)
#endif // instancing

void SetUpSpriteInstanceProperties()
{
#if defined(UNITY_INSTANCING_ENABLED) && !defined(HAVE_VFX_MODIFICATION)
    unity_SpriteProps = float4(unity_SpriteFlip.x, unity_SpriteFlip.y, -1.0f, -1.0f);
#endif
}

#endif
