#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	// The first thing that we should do is grab the intersection vertice
    IntersectionVertice currentvertex;
    GetCurrentIntersectionVertice(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersection.incidentDirection;

    // Make sure to add the additional travel distance
    float travelDistance = length(GetAbsolutePositionWS(fragInput.positionRWS) - rayIntersection.origin);
    rayIntersection.t = travelDistance;
    rayIntersection.cone.width += travelDistance * rayIntersection.cone.spreadAngle;
    
    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = uint2(0, 0);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceDataFromIntersection(fragInput, viewWS, posInput, currentvertex, rayIntersection.cone, surfaceData, builtinData);

    // Compute the bsdf data
    BSDFData bsdfData =  ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifdef HAS_LIGHTLOOP
    // Compute the prelight data
    PreLightData preLightData = GetPreLightData(viewWS, posInput, bsdfData);

    // Run the lightloop
    float3 diffuseLighting;
    float3 specularLighting;
    LightLoop(viewWS, posInput, preLightData, bsdfData, builtinData, float3(0.0, 0.0, 0.0),  float3(0.0, 0.0, 0.0), diffuseLighting, specularLighting);

    // Color display for the moment
    rayIntersection.color = diffuseLighting + specularLighting;
#else
    // Given that we will be multiplying the final color by the current exposure multiplier outside of this function, we need to make sure that
    // the unlit color is not impacted by that. Thus, we multiply it by the inverse of the current exposure multiplier.
    rayIntersection.color = bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor;
#endif
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertice currentvertex;
    GetCurrentIntersectionVertice(attributeData, currentvertex);

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

    // If this fella should be culled, then we cull it
    if(!isVisible)
    {
        IgnoreHit();
    }
}
