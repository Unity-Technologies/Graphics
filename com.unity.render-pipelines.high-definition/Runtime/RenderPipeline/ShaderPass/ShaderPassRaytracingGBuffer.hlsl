#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    const float3 incidentDir = WorldRayDirection();
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, incidentDir, fragInput);

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
    
    // First we pack the data into the standard bsdf data
    StandardBSDFData standardLitData;
    ZERO_INITIALIZE(StandardBSDFData, standardLitData);
    FitToStandardLit(surfaceData, builtinData, posInput.positionSS, standardLitData);

#ifdef MINIMAL_GBUFFER
    // Override all the parameters that we do not require for our minimal lit version
    standardLitData.specularOcclusion = 1.0;
    standardLitData.perceptualRoughness = 1.0;
    standardLitData.normalWS = fragInput.tangentToWorld[2];
    standardLitData.fresnel0 = 0.0;
    standardLitData.coatMask = 0.0;
    standardLitData.emissiveAndBaked = builtinData.emissiveColor;
#endif

    // Then export it to the gbuffer
    EncodeIntoStandardGBuffer(standardLitData, rayIntersectionGbuffer.gbuffer0, rayIntersectionGbuffer.gbuffer1, rayIntersectionGbuffer.gbuffer2, rayIntersectionGbuffer.gbuffer3);
    rayIntersectionGbuffer.t = standardLitData.isUnlit != 0 ? -1 : RayTCurrent();
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
#ifdef _SURFACE_TYPE_TRANSPARENT
    IgnoreHit();
#else

    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    const float3 incidentDir = WorldRayDirection();
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, incidentDir, fragInput);

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

    // If this fella should be culled, then we cull it
    if(!isVisible)
    {
        IgnoreHit();
    }
#endif
}
