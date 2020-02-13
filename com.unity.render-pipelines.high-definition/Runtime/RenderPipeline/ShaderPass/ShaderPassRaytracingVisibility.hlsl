#include "Packages/com.unity.render-pipelines.core/ShaderLibrary\CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitVisibility(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, rayIntersection.incidentDirection, fragInput);

    // Compute the distance of the ray
    rayIntersection.t = length(GetAbsolutePositionWS(fragInput.positionRWS) - rayIntersection.origin);

    // Compute the velocity of the itnersection
    float3 previousPositionWS = TransformPreviousObjectToWorld(currentVertex.positionOS);
    rayIntersection.velocity = saturate(length(previousPositionWS - fragInput.positionRWS));
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitVisibility(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	// The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, rayIntersection.incidentDirection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersection.incidentDirection;

    // Compute the distance of the ray
    float travelDistance = length(GetAbsolutePositionWS(fragInput.positionRWS) - rayIntersection.origin);
    rayIntersection.t = travelDistance;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = rayIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);
#if defined(TRANSPARENT_COLOR_SHADOW) && defined(_SURFACE_TYPE_TRANSPARENT)
    #if HAS_REFRACTION
        rayIntersection.color *= lerp(surfaceData.transmittanceColor, float3(0.0, 0.0, 0.0), 1.0 - surfaceData.transmittanceMask);
    #else
        rayIntersection.color *= (1.0 - builtinData.opacity);
    #endif
    IgnoreHit();
#else
    // If this fella is not opaque, then we ignore this hit
    if (!isVisible)
    {
        IgnoreHit();
    }
    else
    {
        // If this fella is opaque, then we need to stop
        rayIntersection.color = float3(0.0, 0.0, 0.0);
        AcceptHitAndEndSearch();
    }
#endif
}
