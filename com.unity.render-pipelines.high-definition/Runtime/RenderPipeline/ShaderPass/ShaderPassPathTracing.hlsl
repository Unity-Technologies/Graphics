// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"

// Path tracing includes
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitPathTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/SubSurface.hlsl"
#endif

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Compute the viewv ector
    float3 viewWS = -WorldRayDirection();

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - rayIntersection.remainingDepth;
    const float3 positionWS = GetAbsolutePositionWS(fragInput.positionRWS);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // If we are in the case of a shadow ray or a volumetric ray, we do not have anything left to do
    if (rayIntersection.rayType == SHADOW_RAY || rayIntersection.rayType == VOLUMETRIC_RAY)
    {
        rayIntersection.outPosition = positionWS;
        rayIntersection.normal = fragInput.tangentToWorld[2];
        return;
    }

#ifdef HAS_LIGHTLOOP

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    if (rayIntersection.remainingDepth > 1)
    {
        // Generate the new sample (following values of the sequence)
        float4 inputSample = 0.0;
        inputSample.x = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth);
        inputSample.y = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 1);
        inputSample.z = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 2);
        inputSample.w = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 3);

        float isVolumetric = IsVolumetric(bsdfData) && IsAbove(bsdfData.geomNormalWS, viewWS);
        if (isVolumetric)
        {
            float3 result = 1.0;
            ScatteringResult scatteringResult = ScatteringWalk(bsdfData, rayIntersection, positionWS, viewWS, result);

            // Create a ray descriptor for the next ray
            RayDesc rayDescriptor;
            rayDescriptor.Origin = scatteringResult.outputPosition + scatteringResult.outputNormal * _RaytracingRayBias;
            rayDescriptor.Direction = scatteringResult.outputDirection;
            rayDescriptor.TMin = 0.0;
            rayDescriptor.TMax = FLT_INF;

            RayIntersection nextRayIntersection;
            nextRayIntersection.color = float3(1.0, 1.0, 1.0);
            nextRayIntersection.incidentDirection = scatteringResult.outputDirection;
            nextRayIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
            nextRayIntersection.pixelCoord = rayIntersection.pixelCoord;
            nextRayIntersection.rayType = MAIN_RAY;
            nextRayIntersection.rayCount = rayIntersection.rayCount;

            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE
                                                    , RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

            rayIntersection.color = nextRayIntersection.color * result;
        }
        else
        {
            // Generate the next direction for the path
            float3 nextDirection = SampleHemisphereCosine(inputSample.x, inputSample.y, bsdfData.normalWS);

            // Create a ray descriptor for the next ray
            RayDesc rayDescriptor;
            rayDescriptor.Origin = positionWS + bsdfData.normalWS * _RaytracingRayBias;
            rayDescriptor.Direction = nextDirection;
            rayDescriptor.TMin = 0.0;
            rayDescriptor.TMax = FLT_INF;

            RayIntersection nextRayIntersection;
            nextRayIntersection.color = float3(1.0, 1.0, 1.0);
            nextRayIntersection.incidentDirection = nextDirection;
            nextRayIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
            nextRayIntersection.pixelCoord = rayIntersection.pixelCoord;
            nextRayIntersection.rayType = MAIN_RAY;
            nextRayIntersection.rayCount = rayIntersection.rayCount;

            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE
                                                    , RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

            rayIntersection.color = nextRayIntersection.color * bsdfData.diffuseColor;
        }

    }
    else
    {
        rayIntersection.color = 0.0;
    }
#else // HAS_LIGHTLOOP
    rayIntersection.color = builtinData.emissiveColor * GetCurrentExposureMultiplier();
#endif
}

/*
// Handles fully transparent objects (not called if RAY_FLAG_FORCE_OPAQUE is set)
[shader("anyhit")]
void AnyHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Compute the distance of the ray
    rayIntersection.t = RayTCurrent();

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = 0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, -WorldRayDirection(), posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // If this fella should be culled, then we cull it
    if (!isVisible)
        IgnoreHit();

    // If the depth information is marked as invalid, we are shooting a transmission ray
    if (rayIntersection.remainingDepth > _RaytracingMaxRecursion)
        AcceptHitAndEndSearch();
}
*/
