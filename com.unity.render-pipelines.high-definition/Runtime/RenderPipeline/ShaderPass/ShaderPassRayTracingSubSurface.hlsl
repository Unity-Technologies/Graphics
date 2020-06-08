#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestSubSurface(inout RayIntersectionSubSurface rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // define the incident direction
    const float3 incidentDirection = WorldRayDirection();

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, incidentDirection, fragInput);
    
    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = rayIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -incidentDirection, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);

    // make sure we output the normal and the indirect diffuse lighting value
    rayIntersection.outNormal = fragInput.tangentToWorld[2];
    rayIntersection.outIndirectDiffuse = builtinData.bakeDiffuseLighting;
}
