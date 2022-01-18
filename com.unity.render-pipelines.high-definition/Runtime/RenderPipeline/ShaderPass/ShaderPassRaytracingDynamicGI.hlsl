#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitDynGI(inout RayIntersectionDynGI rayIntersectionDynGI : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Evaluate the incident direction
    const float3 incidentDir = WorldRayDirection();

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    RayCone cone;
    cone.width = 0.0;
    cone.spreadAngle = 0.0;
    GetSurfaceAndBuiltinData(fragInput, -incidentDir, posInput, surfaceData, builtinData, currentVertex, cone, isVisible);

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    rayIntersectionDynGI.normalWS = bsdfData.normalWS;
    rayIntersectionDynGI.albedo = bsdfData.diffuseColor;
    rayIntersectionDynGI.t = RayTCurrent();
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitDynGI(inout RayIntersectionDynGI rayIntersectionDynGI : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    IgnoreHit();
}
