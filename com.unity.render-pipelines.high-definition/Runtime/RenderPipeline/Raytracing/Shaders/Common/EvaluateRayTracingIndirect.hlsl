#if defined(HAS_LIGHTLOOP)
void EvaluateIndirectRayTracingLighting(PreLightData preLightData, PositionInputs posInput, BSDFData bsdfData, BuiltinData builtinData,
                                        float3 viewWS, float3 pointWSPos,
                                        inout RayIntersection rayIntersection)
{
    float3 reflected = float3(0.0, 0.0, 0.0);
    float reflectedWeight = 0.0;

    #ifdef MULTI_BOUNCE_INDIRECT
    // We only launch a ray if there is still some depth be used
    if (rayIntersection.remainingDepth < _RaytracingMaxRecursion)
    {
        // Generate the new sample (follwing values of the sequence)
        float2 theSample = float2(0.0, 0.0);
        theSample.x = GetBNDSequenceSample(rayIntersection.pixelCoord, rayIntersection.sampleIndex, rayIntersection.remainingDepth * 2);
        theSample.y = GetBNDSequenceSample(rayIntersection.pixelCoord, rayIntersection.sampleIndex, rayIntersection.remainingDepth * 2 + 1);

        float3 sampleDir;
        if (_RayTracingDiffuseLightingOnly)
        {
            sampleDir = SampleHemisphereCosine(theSample.x, theSample.y, bsdfData.normalWS);
        }
        else
        {
            sampleDir = SampleSpecularBRDF(bsdfData, theSample, viewWS);
        }

        // Create the ray descriptor for this pixel
        RayDesc rayDescriptor;
        rayDescriptor.Origin = pointWSPos + bsdfData.normalWS * _RayTracingRayBias;
        rayDescriptor.Direction = sampleDir;
        rayDescriptor.TMin = 0.0f;
        rayDescriptor.TMax = _RaytracingRayMaxLength;

        // Create and init the RayIntersection structure for this
        RayIntersection reflectedIntersection;
        reflectedIntersection.color = float3(0.0, 0.0, 0.0);
        reflectedIntersection.t = -1.0f;
        reflectedIntersection.remainingDepth = rayIntersection.remainingDepth + 1;
        reflectedIntersection.pixelCoord = rayIntersection.pixelCoord;
        reflectedIntersection.sampleIndex = rayIntersection.sampleIndex;

        // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
        reflectedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
        reflectedIntersection.cone.width = rayIntersection.cone.width;

        bool launchRay = true;
        if (!_RayTracingDiffuseLightingOnly)
            launchRay = dot(sampleDir, bsdfData.normalWS) > 0.0;

        // Evaluate the ray intersection
        if (launchRay)
            TraceRay(_RaytracingAccelerationStructure
                        , RAY_FLAG_CULL_BACK_FACING_TRIANGLES
                        , _RayTracingDiffuseLightingOnly ? RAYTRACINGRENDERERFLAG_GLOBAL_ILLUMINATION : RAYTRACINGRENDERERFLAG_REFLECTION
                        , 0, 1, 0, rayDescriptor, reflectedIntersection);

        // Contribute to the pixel
        if (_RayTracingDiffuseLightingOnly)
        {
            builtinData.bakeDiffuseLighting = reflectedIntersection.color;

            // This needs to be done here as the other sources of indirect lighting have it done elsewhere.
            #ifdef  MODIFY_BAKED_DIFFUSE_LIGHTING
                // Make sure the baked diffuse lighting is tinted with the diffuse color
                ModifyBakedDiffuseLighting(viewWS, posInput, preLightData, bsdfData, builtinData);
            #endif
        }
        else
        {
            // Override the reflected color
            reflected = reflectedIntersection.color;
            reflectedWeight = 1.0;
        }
    }
    #endif

    // Fill the ray context
    RayContext rayContext;
    rayContext.reflection = reflected;
    rayContext.reflectionWeight = reflectedWeight;
    rayContext.transmission = 0.0;
    rayContext.transmissionWeight = 0.0;
    #ifdef MULTI_BOUNCE_INDIRECT
    rayContext.useAPV = _RayTracingDiffuseLightingOnly ? rayIntersection.remainingDepth == _RaytracingMaxRecursion : 1;
    #else
    rayContext.useAPV = 1;
    #endif

    // Run the lightloop
    LightLoopOutput lightLoopOutput;
    LightLoop(viewWS, posInput, preLightData, bsdfData, builtinData, rayContext, lightLoopOutput);

    // Alias
    float3 diffuseLighting = lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    // Color display for the moment
    rayIntersection.color = diffuseLighting + specularLighting;
}
#endif
