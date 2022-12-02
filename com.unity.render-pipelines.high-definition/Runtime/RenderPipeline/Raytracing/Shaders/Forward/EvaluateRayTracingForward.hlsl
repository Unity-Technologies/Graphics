#if defined(HAS_LIGHTLOOP)
void EvaluateForwardRayTracingLighting(PreLightData preLightData, PositionInputs posInput, BSDFData bsdfData, BuiltinData builtinData, SurfaceData surfaceData,
                                        float3 viewWS, float3 pointWSPos, float3 geometryNormal, bool isFrontFace,
                                        inout RayIntersection rayIntersection)
{
    // Variables used to handle the additional bounces
    float3 reflected = float3(0.0, 0.0, 0.0);
    float reflectedWeight = 0.0;
    float3 transmitted = float3(0.0, 0.0, 0.0);
    float refractedWeight = 0.0;
    uint additionalRayCount = 0;

    // The intersection will launch a refraction ray only if the object is transparent and is has the refraction flag
#ifdef _SURFACE_TYPE_TRANSPARENT
    // If the mesh has a refraction mode, then we do proper refraction
    #if HAS_REFRACTION
        // We only allow for inside-medium paths if the surface is flagged as non-thin refractive and is double sided
        #if defined(_REFRACTION_THIN) || !defined(_DOUBLESIDED_ON)
        float invIOR = 1.0;
        #else
        float invIOR = bsdfData.ior;
        #endif

        #if !defined(_REFRACTION_THIN)
        // Inverse the ior ratio if we are leaving the medium (we are hitting a back face)
        if (isFrontFace)
            invIOR = 1.0f / invIOR;
        #endif

        // Let's compute the refracted direction
        float3 refractedDir = refract(-viewWS, bsdfData.normalWS, invIOR);

        // If the refracted direction ends going in the same direction than the normal, we do not want to throw it
        // NOTE: The current state of the code does not support the case of the total internal reflection. So there is a problem in term
        // of energy conservation
        // We launch a ray if there is still some depth be used
        if (rayIntersection.remainingDepth > 0 && dot(refractedDir, bsdfData.normalWS) < 0.0f)
        {
            // Make sure we apply ray bias on the right side of the surface
            const float biasSign = sign(dot(geometryNormal, refractedDir));

            // Build the transmitted ray structure
            RayDesc transmittedRay;
            transmittedRay.Origin = pointWSPos + biasSign * geometryNormal * _RayTracingRayBias;
            transmittedRay.Direction = refractedDir;
            transmittedRay.TMin = 0;
            transmittedRay.TMax = _RaytracingRayMaxLength;

            // Build the following intersection structure
            RayIntersection transmittedIntersection;
            transmittedIntersection.color = float3(0.0, 0.0, 0.0);
            transmittedIntersection.t = _RaytracingRayMaxLength;
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
    #else
        if (rayIntersection.remainingDepth > 0)
        {
            // Make sure we apply ray bias on the right side of the surface
            const float biasSign = sign(dot(geometryNormal, -viewWS));

            // Build the transmitted ray structure
            RayDesc transmittedRay;
            transmittedRay.Origin = pointWSPos + biasSign * geometryNormal * _RayTracingRayBias;
            transmittedRay.Direction = -viewWS;
            transmittedRay.TMin = 0;
            transmittedRay.TMax = _RaytracingRayMaxLength;

            // Build the following intersection structure
            RayIntersection transmittedIntersection;
            transmittedIntersection.color = float3(0.0, 0.0, 0.0);
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
            refractedWeight = 0.0;
            additionalRayCount += transmittedIntersection.rayCount;
        }
    #endif
#endif

    // We only launch a ray if there is still some depth be used and if the reflection smoothnes threshold was not reached.
    if (rayIntersection.remainingDepth > 0 && RecursiveRenderingReflectionPerceptualSmoothness(bsdfData) >= _RaytracingReflectionMinSmoothness)
    {
        // Compute the reflected direction
        float3 reflectedDir = reflect(-viewWS, bsdfData.normalWS);

        // Make sure we apply ray bias on the right side of the surface
        const float biasSign = sign(dot(geometryNormal, reflectedDir));

        // Build the reflected ray
        RayDesc reflectedRay;
        reflectedRay.Origin = pointWSPos + biasSign * geometryNormal * _RayTracingRayBias;
        reflectedRay.Direction = reflectedDir;
        reflectedRay.TMin = 0;
        reflectedRay.TMax = _RaytracingRayMaxLength;

        // Create and init the RayIntersection structure for this
        RayIntersection reflectedIntersection;
        reflectedIntersection.color = float3(0.0, 0.0, 0.0);
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

    // Fill the ray context
    RayContext rayContext;
    rayContext.reflection = reflected;
    rayContext.reflectionWeight = reflectedWeight;
    rayContext.transmission = transmitted;
    rayContext.transmissionWeight = refractedWeight;
    rayContext.useAPV = 1;

    // Run the lightloop
    LightLoopOutput lightLoopOutput;
    LightLoop(viewWS, posInput, preLightData, bsdfData, builtinData, rayContext, lightLoopOutput);

    // Alias
    float3 diffuseLighting = lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    // Color display for the moment
    rayIntersection.color = diffuseLighting + specularLighting;
    rayIntersection.rayCount += additionalRayCount;

    #ifdef _SURFACE_TYPE_TRANSPARENT
    // If the mesh is transparent, not refractive we need to alpha blend
        #if !HAS_REFRACTION
        rayIntersection.color = lerp(transmitted, rayIntersection.color, builtinData.opacity);
        #endif
    #endif
}
#endif
