#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Forward/EvaluateRayTracingForward.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitForward(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    IntersectionVertex currentVertex;
    #ifdef HAVE_VFX_MODIFICATION
        ZERO_INITIALIZE(IntersectionVertex, currentVertex);
        FragInputs fragInput;
        BuildFragInputsFromVFXIntersection(attributeData, fragInput);
    #else
        GetCurrentIntersectionVertex(attributeData, currentVertex);
        // Build the Frag inputs from the intersection vertice
        FragInputs fragInput;
        BuildFragInputsFromIntersection(currentVertex, fragInput);
    #endif

    // Compute the view vector
    float3 incidentDirection = WorldRayDirection();
    float3 viewWS = -incidentDirection;

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 pointWSPos = fragInput.positionRWS;

    // Make sure to add the additional travel distance
    float travelDistance = length(fragInput.positionRWS - WorldRayOrigin());
    rayIntersection.t = travelDistance;
    rayIntersection.cone.width += travelDistance * abs(rayIntersection.cone.spreadAngle);

    PositionInputs posInput = GetPositionInput(rayIntersection.pixelCoord, _ScreenSize.zw, fragInput.positionRWS);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);

    #if HAS_REFRACTION
    if (dot(incidentDirection, surfaceData.normalWS) > 0.0f)
    {
        surfaceData.normalWS = -surfaceData.normalWS;
        fragInput.tangentToWorld[2] = -fragInput.tangentToWorld[2];
    }
    #endif

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifdef HAS_LIGHTLOOP
    // Compute the prelight data
    PreLightData preLightData = GetPreLightData(viewWS, posInput, bsdfData);

    // Evaluate the forward lighting
    EvaluateForwardRayTracingLighting(preLightData, posInput, bsdfData, builtinData, surfaceData,
                                        viewWS, pointWSPos, fragInput.tangentToWorld[2], fragInput.isFrontFace,
                                        rayIntersection);
#else
    // Given that we will be multiplying the final color by the current exposure multiplier outside of this function, we need to make sure that
    // the unlit color is not impacted by that. Thus, we multiply it by the inverse of the current exposure multiplier.
    rayIntersection.color = bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor;
#endif

    // Apply fog attenuation
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), rayIntersection.t, rayIntersection.color, true);
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    // Compute the view vector
    float3 viewWS = -WorldRayDirection();

    // Compute the distance of the ray
    float travelDistance = length(GetAbsolutePositionWS(fragInput.positionRWS) - WorldRayOrigin());
    rayIntersection.t = travelDistance;

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = rayIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);

    // If this fella should be culled, then we cull it
    if (!isVisible)
    {
        IgnoreHit();
    }
}
