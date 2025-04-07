#ifndef INPUT_CORE_2D_INCLUDED
#define INPUT_CORE_2D_INCLUDED

float3 UnityFlipSprite( in float3 pos, in float2 flip )
{
    return float3(pos.xy * flip, pos.z);
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

#endif
