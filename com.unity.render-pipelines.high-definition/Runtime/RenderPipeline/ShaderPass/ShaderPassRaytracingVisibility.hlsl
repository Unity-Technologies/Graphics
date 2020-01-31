#include "Packages/com.unity.render-pipelines.core/ShaderLibrary\CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	// The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentvertex;
    GetCurrentIntersectionVertex(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersection.incidentDirection;

    // Compute the distance of the ray
    float travelDistance = length(GetAbsolutePositionWS(fragInput.positionRWS) - rayIntersection.origin);
    rayIntersection.t = travelDistance;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentvertex, rayIntersection.cone, surfaceData, builtinData);

    // If this fella is not opaque, then we ignore this hit
    if(!isVisible)
    {
        IgnoreHit();
    }
    else
    {
        // If this fella is opaque, then we need to stop
        rayIntersection.color = float3(0.0, 0.0, 0.0);
        AcceptHitAndEndSearch();
    }
}
