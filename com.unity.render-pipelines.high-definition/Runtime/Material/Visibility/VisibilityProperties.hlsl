
CBUFFER_START(UnityPerMaterial)
    float4 _DeferredMaterialInstanceData;
CBUFFER_END

#if defined(UNITY_DOTS_INSTANCING_ENABLED)

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _DeferredMaterialInstanceData)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _DeferredMaterialInstanceData UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _DeferredMaterialInstanceData)

#endif
