// Visibility function required for the intersection shader
bool AABBPrimitiveIsVisible(RayTracingProceduralData rtProceduralData, float2 uv)
{
    //In shadergraph, alpha clipping is handled in the any-hit
    return true;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/RayTracingProcedural.hlsl"

[shader("intersection")]
void IntersectionShader()
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    InternalAttributesElement attributes;
    ZERO_INITIALIZE(InternalAttributesElement, attributes);

    // Index needs to be available in the context for the attribute load to work
    uint index, instanceIndex, instanceActiveIndex;
    GetVFXInstancingIndices(index, instanceIndex, instanceActiveIndex);
#if VFX_USE_GRAPH_VALUES
    $splice(VFXLoadGraphValues)
#endif

    // Load the VFX attributes that we need for this
    $splice(VFXLoadAttribute)
    $splice(VFXProcessBlocks)
    float3 size3 = GetElementSizeRT(attributes
#if VFX_USE_GRAPH_VALUES
        , graphValues
#endif
    );

    // Build the ray tracing procedural data
    RayTracingProceduralData rtProceduralData = BuildRayTracingProceduralData(attributes, size3);

    // Execute the matching intersection code
    IntersectPrimitive(rtProceduralData);
}
