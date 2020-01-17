#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, rayIntersectionGbuffer.incidentDirection, fragInput);

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
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersectionGbuffer.cone, isVisible);

    // Sometimes, we only  want to use the diffuse when we compute the indirect diffuse
    #ifdef DIFFUSE_LIGHTING_ONLY
    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    builtinData.backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    #endif

    // First we pack the data into the standard bsdf data
    StandardBSDFData standardLitData;
    ZERO_INITIALIZE(StandardBSDFData, standardLitData);
    FitToStandardLit(surfaceData, builtinData, posInput.positionSS, standardLitData);
    
    // Then export it to the gbuffer
    ENCODE_TO_STANDARD_GBUFFER(standardLitData, rayIntersectionGbuffer.gBufferData.gbuffer);
    rayIntersectionGbuffer.t = standardLitData.isUnlit != 0 ? -1 : travelDistance;
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
#ifdef _SURFACE_TYPE_TRANSPARENT
    IgnoreHit();
#else
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, rayIntersectionGbuffer.incidentDirection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersectionGbuffer.incidentDirection;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersectionGbuffer.cone, isVisible);

    // If this fella should be culled, then we cull it
    if(!isVisible)
    {
        IgnoreHit();
    }
#endif
}
