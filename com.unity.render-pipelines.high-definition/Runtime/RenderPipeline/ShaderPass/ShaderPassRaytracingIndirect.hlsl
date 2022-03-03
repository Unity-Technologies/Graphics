#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/EvaluateRayTracingIndirect.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);


    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

#ifdef HAVE_VFX_MODIFICATION
    FragInputs fragInput;
    BuildFragInputsFromVFXIntersection(attributeData, fragInput);
#else
    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);
#endif
    // Compute the view vector
    float3 viewWS = -WorldRayDirection();
    float3 pointWSPos = fragInput.positionRWS;

    // Make sure to add the additional travel distance
    float travelDistance = length(fragInput.positionRWS - ObjectRayOrigin());
    rayIntersection.t = travelDistance;
    rayIntersection.cone.width += travelDistance * rayIntersection.cone.spreadAngle;

    PositionInputs posInput = GetPositionInput(rayIntersection.pixelCoord, _ScreenSize.zw, fragInput.positionRWS);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // No need for SurfaceData after this line
#ifdef HAS_LIGHTLOOP
    // We do not want to use the diffuse when we compute the indirect diffuse
    if (_RayTracingDiffuseLightingOnly)
    {
        builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
        builtinData.backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    }

    // Compute the prelight data
    PreLightData preLightData = GetPreLightData(viewWS, posInput, bsdfData);

    // Evaluate the lighting
    EvaluateIndirectRayTracingLighting(preLightData, posInput, bsdfData, builtinData, viewWS, pointWSPos, rayIntersection);
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
#ifdef _SURFACE_TYPE_TRANSPARENT
    IgnoreHit();
#else

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
    float3 viewWS = -WorldRayDirection();

    // Compute the distance of the ray
    float travelDistance = length(fragInput.positionRWS - ObjectRayOrigin());
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
    if(!isVisible)
    {
        IgnoreHit();
    }
#endif
}
