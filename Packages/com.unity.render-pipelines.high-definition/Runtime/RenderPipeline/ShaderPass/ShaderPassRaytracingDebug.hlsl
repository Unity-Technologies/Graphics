#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitDebug(inout RayIntersectionDebug rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    rayIntersection.t = RayTCurrent();
    rayIntersection.barycentrics = attributeData.barycentrics;
    rayIntersection.primitiveIndex = PrimitiveIndex();
    rayIntersection.instanceIndex = InstanceIndex();
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitDebug(inout RayIntersectionDebug rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    // Debug data
    rayIntersection.t = RayTCurrent();
    rayIntersection.barycentrics = attributeData.barycentrics;
    rayIntersection.primitiveIndex = PrimitiveIndex();
    rayIntersection.instanceIndex = InstanceIndex();
}
