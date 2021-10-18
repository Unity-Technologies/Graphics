#ifndef VISIBILITY_PASS_HLSL
#define VISIBILITY_PASS_HLSL


#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/Visibility.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GeometryPool/Resources/GeometryPool.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _VisBufferInstanceData;
CBUFFER_END

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _VisBufferInstanceData)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _VisBufferInstanceData UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _VisBufferInstanceData)

#endif

struct GeoPoolInput
{
    uint vertId : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VisibilityVtoP
{
    float4 pos : SV_Position;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct VisibilityDrawInput
{
    uint vertexIndex : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VisibilityVtoP Vert(VisibilityDrawInput input)
{
    VisibilityVtoP v2p;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, v2p);

    GeoPoolMetadataEntry metadata = _GeoPoolGlobalMetadataBuffer[(int)_VisBufferInstanceData.x];

    GeoPoolVertex vertexData;
    GeometryPool::LoadVertex(input.vertexIndex, metadata, vertexData);

    float3 worldPos = TransformObjectToWorld(vertexData.pos);
    v2p.pos = TransformWorldToHClip(worldPos);
    return v2p;
}

void Frag(
    VisibilityVtoP packedInput,
    uint primitiveID : SV_PrimitiveID,
    out uint outVisibility : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    UNITY_SETUP_INSTANCE_ID(packedInput);
    #ifdef DOTS_INSTANCING_ON
        VisibilityData visData;
        visData.DOTSInstanceIndex = GetDOTSInstanceIndex();
        visData.primitiveID = primitiveID;
        outVisibility = packVisibilityData(visData);
    #else
        outVisibility = InvalidVisibilityData;
    #endif
}

#endif
