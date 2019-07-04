#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentvertex;
    GetCurrentIntersectionVertex(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersectionGbuffer.incidentDirection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersectionGbuffer.incidentDirection;

    // Make sure to add the additional travel distance
    float travelDistance = length(GetAbsolutePositionWS(fragInput.positionRWS) - rayIntersectionGbuffer.origin);
    rayIntersectionGbuffer.cone.width += travelDistance * rayIntersectionGbuffer.cone.spreadAngle;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentvertex, rayIntersectionGbuffer.cone, surfaceData, builtinData);

    // Sometimes, we only  want to use the diffuse when we compute the indirect diffuse
    #ifdef DIFFUSE_LIGHTING_ONLY
    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    builtinData.backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    #endif
    
    bool forwardOnly = false;
    RAYTRACING_ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, rayIntersectionGbuffer.gBufferData.gbuffer, forwardOnly);
    rayIntersectionGbuffer.t = forwardOnly ? -1 : travelDistance;
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentvertex;
    GetCurrentIntersectionVertex(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersectionGbuffer.incidentDirection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersectionGbuffer.incidentDirection;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentvertex, rayIntersectionGbuffer.cone, surfaceData, builtinData);

    // If this fella should be culled, then we cull it
    if(!isVisible)
    {
        IgnoreHit();
    }
}
