#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHitForward(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	// The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertice
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, rayIntersection.incidentDirection, fragInput);

    // Compute the view vector
    float3 viewWS = -rayIntersection.incidentDirection;

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 pointWSPos = GetAbsolutePositionWS(fragInput.positionRWS);

    // Make sure to add the additional travel distance
    float travelDistance = length(pointWSPos - rayIntersection.origin);
    rayIntersection.t = travelDistance;
    rayIntersection.cone.width += travelDistance * abs(rayIntersection.cone.spreadAngle);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = rayIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);
    
    // Compute the bsdf data
    BSDFData bsdfData =  ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifdef HAS_LIGHTLOOP
    // Compute the prelight data
    PreLightData preLightData = GetPreLightData(viewWS, posInput, bsdfData);
    float3 reflected = float3(0.0, 0.0, 0.0);
    float reflectedWeight = 0.0;
    float3 transmitted = float3(0.0, 0.0, 0.0);
    float refractedWeight = 0.0;
    uint additionalRayCount = 0;

    // The intersection will launch a refraction ray only if the object is transparent and is has the refraction flag
#ifdef _SURFACE_TYPE_TRANSPARENT
#if HAS_REFRACTION
    // Inverse the ior ratio if we are leaving the medium (we are hitting a back face)
    float invIOR = surfaceData.ior;
    if (fragInput.isFrontFace)
        invIOR = 1.0f / invIOR;

    // Let's compute the refracted direction
    float3 refractedDir = refract(rayIntersection.incidentDirection, surfaceData.normalWS, invIOR);

    // If the refracted direction ends going in the same direction than the normal, we do not want to throw it
    // NOTE: The current state of the code does not support the case of the total internal reflection. So there is a problem in term
    // of energy conservation
    // We launch a ray if there is still some depth be used
    if (rayIntersection.remainingDepth > 0 && dot(refractedDir, surfaceData.normalWS) < 0.0f)
    {
        // Make sure we apply ray bias on the right side of the surface
        const float biasSign = sign(dot(fragInput.tangentToWorld[2], refractedDir));

        // Build the transmitted ray structure
        RayDesc transmittedRay;
        transmittedRay.Origin = pointWSPos + biasSign * fragInput.tangentToWorld[2] * _RaytracingRayBias;
        transmittedRay.Direction = refractedDir;
        transmittedRay.TMin = 0;
        transmittedRay.TMax = _RaytracingRayMaxLength;

        // Build the following intersection structure
        RayIntersection transmittedIntersection;
        transmittedIntersection.color = float3(0.0, 0.0, 0.0);
        transmittedIntersection.incidentDirection = transmittedRay.Direction;
        transmittedIntersection.origin = transmittedRay.Origin;
        transmittedIntersection.t = 0.0f;
        transmittedIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
        transmittedIntersection.rayCount = 1;
        transmittedIntersection.pixelCoord = rayIntersection.pixelCoord;

        // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
        transmittedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
        transmittedIntersection.cone.width = rayIntersection.cone.width;

        // Evaluate the ray intersection
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_RECURSIVE_RENDERING, 0, 1, 0, transmittedRay, transmittedIntersection);

        // Override the transmitted color
        transmitted = transmittedIntersection.color;
        refractedWeight = 1.0;
        additionalRayCount += transmittedIntersection.rayCount;

        // Given that we are sharing code with rasterization, we need to override properly the refraction parameters
        OverrideRefractionData(surfaceData,
                                transmittedIntersection.t,
                                pointWSPos + transmittedIntersection.t * refractedDir,
                                bsdfData,
                                preLightData);
    }
#endif
#endif

    // We only launch a ray if there is still some depth be used
    if (rayIntersection.remainingDepth > 0)
    {
        // Compute the reflected direction
        float3 reflectedDir = reflect(rayIntersection.incidentDirection, surfaceData.normalWS);

        // Make sure we apply ray bias on the right side of the surface
        const float biasSign = sign(dot(fragInput.tangentToWorld[2], reflectedDir));

        // Build the reflected ray
        RayDesc reflectedRay;
        reflectedRay.Origin = pointWSPos + biasSign * fragInput.tangentToWorld[2] * _RaytracingRayBias;
        reflectedRay.Direction = reflectedDir;
        reflectedRay.TMin = 0;
        reflectedRay.TMax = _RaytracingRayMaxLength;

        // Create and init the RayIntersection structure for this
        RayIntersection reflectedIntersection;
        reflectedIntersection.color = float3(0.0, 0.0, 0.0);
        reflectedIntersection.incidentDirection = reflectedRay.Direction;
        reflectedIntersection.origin = reflectedRay.Origin;
        reflectedIntersection.t = 0.0f;
        reflectedIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
        reflectedIntersection.rayCount = 1;
        reflectedIntersection.pixelCoord = rayIntersection.pixelCoord;

        // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
        reflectedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
        reflectedIntersection.cone.width = rayIntersection.cone.width;

        // Evaluate the ray intersection
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_RECURSIVE_RENDERING, 0, 1, 0, reflectedRay, reflectedIntersection);

        // Override the transmitted color
        reflected = reflectedIntersection.color;
        reflectedWeight = 1.0;
        additionalRayCount += reflectedIntersection.rayCount;
    }

    // Run the lightloop
    float3 diffuseLighting;
    float3 specularLighting;
    LightLoop(viewWS, posInput, preLightData, bsdfData, builtinData, reflectedWeight, refractedWeight, reflected, transmitted, diffuseLighting, specularLighting);

    // Color display for the moment
    rayIntersection.color = diffuseLighting + specularLighting;
    rayIntersection.rayCount += additionalRayCount;
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

    // If this fella should be culled, then we cull it
    if (!isVisible)
    {
        IgnoreHit();
    }
}
