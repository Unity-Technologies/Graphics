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

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 pointWSPos = GetAbsolutePositionWS(fragInput.positionRWS);

    // Make sure to add the additional travel distance
    float travelDistance = length(pointWSPos - rayIntersection.origin);
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
    float3 reflected = float3(0.0, 0.0, 0.0);
    float3 transmitted = float3(0.0, 0.0, 0.0);

    // The intersection will launch a refraction ray only if the object is transparent and is has the refraction flag
#ifdef _SURFACE_TYPE_TRANSPARENT
#if HAS_REFRACTION
    // Inverse the ior ratio if we are leaving the medium (we are hitting a back face)
    float invIOR = surfaceData.ior;
    if (fragInput.isFrontFace)
        invIOR = 1.0f / invIOR;
    
    // Let's compute the refracted direction
    float3 refractedDirection = refract(rayIntersection.incidentDirection, surfaceData.normalWS, invIOR);
    
    // If the refracted direction ends going in the same direction than the normal, we do not want to throw it
    // NOTE: The current state of the code does not support the case of the total internal reflection. So there is a problem in term
    // of energy conservation
    // We launch a ray if there is still some depth be used
    if (rayIntersection.remainingDepth > 0 && dot(refractedDirection, surfaceData.normalWS) < 0.0f)
    {
        
        // Build the transmitted ray structure
        RayDesc transmittedRay;
        transmittedRay.Origin = pointWSPos - surfaceData.normalWS * _RaytracingRayBias;
        transmittedRay.Direction = refractedDirection;
        transmittedRay.TMin = 0;
        transmittedRay.TMax = _RaytracingRayMaxLength;

        // Build the following intersection structure
        RayIntersection transmittedIntersection;
        transmittedIntersection.color = float3(0.0, 0.0, 0.0);
        transmittedIntersection.incidentDirection = transmittedRay.Direction;
        transmittedIntersection.origin = transmittedRay.Origin;
        transmittedIntersection.t = 0.0f;
        transmittedIntersection.remainingDepth = rayIntersection.remainingDepth - 1;

        // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
        transmittedIntersection.cone.spreadAngle = _RaytracingPixelSpreadAngle;
        transmittedIntersection.cone.width = rayIntersection.cone.width;
        
        // Evaluate the ray intersection
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, transmittedRay, transmittedIntersection);
            
        // Override the transmitted color
        transmitted = transmittedIntersection.color;
    }
#endif
#endif

    // We only launch a ray if there is still some depth be used
    if (rayIntersection.remainingDepth > 0)
    {
        // Compute the reflected direction
        float3 reflectedDir = reflect(rayIntersection.incidentDirection, surfaceData.normalWS);

        // Build the reflected ray
        RayDesc reflectedRay;
        reflectedRay.Origin = pointWSPos + surfaceData.normalWS * _RaytracingRayBias;
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

        // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
        reflectedIntersection.cone.spreadAngle = _RaytracingPixelSpreadAngle;
        reflectedIntersection.cone.width = rayIntersection.cone.width;
        
        // Evaluate the ray intersection
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, reflectedRay, reflectedIntersection);

        // Override the transmitted color
        reflected = reflectedIntersection.color;
    }

    // Run the lightloop
    float3 diffuseLighting;
    float3 specularLighting;
    LightLoop(viewWS, posInput, preLightData, bsdfData, builtinData, reflected, transmitted, diffuseLighting, specularLighting);

    // Compute the Color of the current intersection
    rayIntersection.color = (diffuseLighting + specularLighting) * GetCurrentExposureMultiplier();;
#else
    rayIntersection.color = bsdfData.color;
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
