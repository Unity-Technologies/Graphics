#ifndef INPUT_CORE_2D_INCLUDED
#define INPUT_CORE_2D_INCLUDED

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
