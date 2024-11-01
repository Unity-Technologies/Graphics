#ifndef INPUT_CORE_2D_INCLUDED
#define INPUT_CORE_2D_INCLUDED

#if defined(SKINNED_SPRITE)

    #define UNITY_SKINNED_VERTEX_INPUTS         float4 blendWeights : BLENDWEIGHTS; uint4  blendIndices : BLENDINDICES;
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)     x.positionOS = UnitySkinSprite(x.positionOS, x.blendIndices, x.blendWeights, unity_SpriteProps.z);

#else

    #define UNITY_SKINNED_VERTEX_INPUTS
    #define UNITY_SKINNED_VERTEX_COMPUTE(x)

#endif

uniform StructuredBuffer<float4x4> _SpriteBoneTransforms;

float3 UnityFlipSprite( in float3 pos, in float2 flip )
{
    return float3(pos.xy * flip, pos.z);
}

float3 UnitySkinSprite( in float3 positionOS, in uint4 blendIndices, in float4 blendWeights, in float offset )
{
    float4 vertex = float4(positionOS, 1.0);

#if defined(SKINNED_SPRITE)
    UNITY_BRANCH
    if (offset >= 0)
    {
        vertex =
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.x], vertex) * blendWeights.x +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.y], vertex) * blendWeights.y +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.z], vertex) * blendWeights.z +
            mul(_SpriteBoneTransforms[uint(offset) + blendIndices.w], vertex) * blendWeights.w;
    }
#endif // SKINNED_SPRITE

    return vertex.xyz;
}

#endif
