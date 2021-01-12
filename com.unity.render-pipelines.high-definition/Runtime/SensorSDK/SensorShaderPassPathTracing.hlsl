// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RayTracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/SensorSDK/SensorPathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/SensorSDK/SensorLitPathTracing.hlsl"
#endif

#pragma enable_ray_tracing_shader_debug_symbols

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

#if !defined(SHADERGRAPH_SENSOR_DXR)
TEXTURE2D(_SensorCustomReflectance);
float 		Wavelength; 
#endif

int _SensorLightCount;

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    pathIntersection.color = float3(0.0f, 0.0f, 1.0f);
    
    // Always set the new t value
    pathIntersection.t = RayTCurrent();

    // If the max depth has been reached, bail out
    if (!pathIntersection.remainingDepth)
    {
        pathIntersection.color = float3(0.0f, 1.0f, 0.0f);
        
        pathIntersection.value = 0.0;
        return;
    }

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Such an invalid remainingDepth value means we are called from a subsurface computation
    if (pathIntersection.remainingDepth > _RaytracingMaxRecursion)
    {
        pathIntersection.color = float3(0.0f, 1.0f, 1.0f);
        
        pathIntersection.value = fragInput.tangentToWorld[2]; // Returns normal
        return;
    }

    pathIntersection.color = float3(1.0f, 0.0f, 0.0f);
    
    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    // Make sure to add the additional travel distance
    pathIntersection.cone.width += pathIntersection.t * abs(pathIntersection.cone.spreadAngle);

//#ifndef HAS_LIGHTLOOP
    // This is quick and dirty way to avoid double contribution from light meshes
//   if (currentDepth)
//        pathIntersection.cone.spreadAngle = -1.0;
//#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = pathIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    pathIntersection.alpha = builtinData.opacity;
    pathIntersection.alphatreshold = builtinData.alphaClipTreshold;
    
    pathIntersection.color = float3(1.0f, 0.0f, 1.0f);
    
    //if (!isVisible)
    //{
    //     // This should never happen, return magenta just in case
    //     pathIntersection.color = float3(1.0, 0.0, 0.5);
    //     pathIntersection.value = 0.0;
    //     return;
    //}

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    //We override the diffuce color when we are using standard lit shader.  Don't need to when using shader graph.
#if !defined(SHADERGRAPH_SENSOR_DXR)
    //bsdfData.diffuseColor = float3(_SensorCustomReflectance, _SensorCustomReflectance, _SensorCustomReflectance); //Override diffuse with material reflectance
    const float _minWaveLengthValue = 0.35f; // 350 nm
    const float _maxWaveLengthValue = 2.5f;  // 2500 nm

    float wlIdx           = clamp(Wavelength * 0.001f, _minWaveLengthValue, _maxWaveLengthValue);
    float wavelengthSpan  = _maxWaveLengthValue - _minWaveLengthValue + 1;
    float2 coordCurve     = float2(wlIdx / wavelengthSpan, 0);
 
    bsdfData.diffuseColor = SAMPLE_TEXTURE2D(_SensorCustomReflectance, s_linear_clamp_sampler, coordCurve);	

    pathIntersection.diffValue = bsdfData.diffuseColor;
    pathIntersection.customRefractance = bsdfData.diffuseColor;
#endif
    
    pathIntersection.color = float3(1.0f, 1.0f, 0.0f);
    
//#ifdef HAS_LIGHTLOOP

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 shadingPosition = GetAbsolutePositionWS(fragInput.positionRWS);

    // Generate the new sample (following values of the sequence)
    float3 inputSample = 0.0;
    inputSample.x = GetSample(pathIntersection.pixelCoord, _RaytracingSampleIndex, 4 * currentDepth);
    inputSample.y = GetSample(pathIntersection.pixelCoord, _RaytracingSampleIndex, 4 * currentDepth + 1);
    inputSample.z = GetSample(pathIntersection.pixelCoord, _RaytracingSampleIndex, 4 * currentDepth + 2);

    // Get current path throughput
    float3 pathThroughput = pathIntersection.value;

    // And reset the ray intersection color, which will store our final result
    pathIntersection.value = computeDirect ? builtinData.emissiveColor : 0.0;

    pathIntersection.color = float3(1.0f, 1.0f, 1.0f);

    pathIntersection.materialFeatures = bsdfData.materialFeatures;
    pathIntersection.diffuseColor = bsdfData.diffuseColor;
    pathIntersection.fresnel0 = bsdfData.fresnel0;
    pathIntersection.ambientOcclusion = bsdfData.ambientOcclusion;
    pathIntersection.specularOcclusion = bsdfData.specularOcclusion;
    pathIntersection.normalWS = bsdfData.normalWS;
    pathIntersection.perceptualRoughness = bsdfData.perceptualRoughness;
    pathIntersection.coatMask = bsdfData.coatMask;
    pathIntersection.diffusionProfileIndex = bsdfData.diffusionProfileIndex;
    pathIntersection.subsurfaceMask = bsdfData.subsurfaceMask;
    pathIntersection.thickness = bsdfData.thickness;
    pathIntersection.useThickObjectMode = bsdfData.useThickObjectMode;
    pathIntersection.transmittance = bsdfData.transmittance;
    pathIntersection.tangentWS = bsdfData.tangentWS;
    pathIntersection.bitangentWS = bsdfData.bitangentWS;
    pathIntersection.roughnessT = bsdfData.roughnessT;
    pathIntersection.roughnessB = bsdfData.roughnessB;
    pathIntersection.anisotropy = bsdfData.anisotropy;
    pathIntersection.iridescenceThickness = bsdfData.iridescenceThickness;
    pathIntersection.iridescenceMask = bsdfData.iridescenceMask;
    pathIntersection.coatRoughness = bsdfData.coatRoughness;
    pathIntersection.geomNormalWS = bsdfData.geomNormalWS;
    pathIntersection.ior = bsdfData.ior;
    pathIntersection.absorptionCoefficient = bsdfData.absorptionCoefficient;
    pathIntersection.transmittanceMask = bsdfData.transmittanceMask;
    
    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(pathIntersection, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        pathIntersection.bsdfWeight0 = mtlData.bsdfWeight[0];
        pathIntersection.bsdfWeight1 = mtlData.bsdfWeight[1];
        pathIntersection.bsdfWeight2 = mtlData.bsdfWeight[2];
        pathIntersection.bsdfWeight3 = mtlData.bsdfWeight[3];
        
        pathIntersection.color = float3(0.0f, 0.0f, 2.0f);
        
        // Create the list of active lights
        //LightList lightList = CreateLightList(shadingPosition, mtlData.bsdfData.geomNormalWS, builtinData.renderingLayers);

        // Bunch of variables common to material and light sampling
        float pdf;
        float3 value;
        MaterialResult mtlResult;

        RayDesc rayDescriptor;
        rayDescriptor.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RaytracingRayBias;
        rayDescriptor.TMin = 0.0;

        PathIntersection nextPathIntersection;

        pathIntersection.lightPosition = rayDescriptor.Origin;
        pathIntersection.lightDirection = bsdfData.normalWS;
        pathIntersection.lightCount = _SensorLightCount;
        
        // Light sampling
        //if (computeDirect)
        //{
            pathIntersection.value = 0.0;
            for (uint i = 0; i < _SensorLightCount; i++)
            {
                if (SampleBeam(_LightDatasRT[i], rayDescriptor.Origin, bsdfData.normalWS, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax))
                {
                    pathIntersection.lightOutgoing = rayDescriptor.Direction;
                    pathIntersection.lightIntensity = _LightDatasRT[i].color.x;
                    pathIntersection.lightAngleScale = _LightDatasRT[i].angleScale;
                    pathIntersection.lightAngleOffset = _LightDatasRT[i].angleOffset;
                    pathIntersection.lightValue = value.x;
                    pathIntersection.lightPDF = pdf;
                    
                    EvaluateMaterial(mtlData, rayDescriptor.Direction, mtlResult);

                    pathIntersection.diffValue = mtlResult.diffValue;
                    pathIntersection.diffPdf = mtlResult.diffPdf;
                    pathIntersection.specValue = mtlResult.specValue;
                    pathIntersection.specPdf = mtlResult.specPdf;

                    //pathIntersection.color = float3(_SensorCustomReflectance, _SensorCustomReflectance, _SensorCustomReflectance);
                    pathIntersection.color = float3(0.0f, 2.0f, 0.0f);

                    // value is in radian (w/sr) not in lumen (cd/sr) and only the r channel is used
                    value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                
                    /*
                    if (value.r > 0.001)
                    {
                        // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
                        //nextPathIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
                        //rayDescriptor.TMax -= _RaytracingRayBias;
                        //nextPathIntersection.value = 1.0;

                        //TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER, RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, rayDescriptor, nextPathIntersection);

                        //if (nextpathIntersection.t >= rayDescriptor.TMax)
                        {
                            float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                            pathIntersection.value += value * nextPathIntersection.value * misWeight;
                        }
                    }
                    */
                    pathIntersection.value += value;
                }
            }
        /*
        // Material sampling
        if (SampleMaterial(mtlData, inputSample, rayDescriptor.Direction, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

            pathThroughput *= value;

            // Apply Russian roulette to our path
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(pathThroughput);
            float rrSample = GetSample(pathIntersection.pixelCoord, _RaytracingSampleIndex, 4 * currentDepth + 3);

            if (RussianRouletteTest(rrThreshold, rrValue, rrSample, rrFactor, !currentDepth))
            {
                bool isSampleBelow = IsBelow(mtlData, rayDescriptor.Direction);

                rayDescriptor.Origin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RaytracingRayBias, isSampleBelow);
                rayDescriptor.TMax = FLT_INF;

                // Copy path constants across
                nextPathIntersection.pixelCoord = pathIntersection.pixelCoord;
                nextPathIntersection.cone.width = pathIntersection.cone.width;

                // Complete PathIntersection structure for this sample
                nextPathIntersection.value = pathThroughput * rrFactor;
                nextPathIntersection.remainingDepth = pathIntersection.remainingDepth - 1;
                nextPathIntersection.t = rayDescriptor.TMax;

                // Adjust the max roughness, based on the estimated diff/spec ratio
                nextPathIntersection.maxRoughness = (mtlResult.specPdf * max(mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB) + mtlResult.diffPdf) / pdf;

                // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
                nextPathIntersection.cone.spreadAngle = pathIntersection.cone.spreadAngle + roughnessToSpreadAngle(nextPathIntersection.maxRoughness);

#ifdef _SURFACE_TYPE_TRANSPARENT
                // When transmitting with an IOR close to 1.0, roughness is barely noticeable -> take that into account for roughness clamping
                if (IsBelow(mtlData) != isSampleBelow)
                    nextPathIntersection.maxRoughness = lerp(pathIntersection.maxRoughness, nextPathIntersection.maxRoughness, smoothstep(1.0, 1.3, mtlData.bsdfData.ior));
#endif

                // Shoot ray for indirect lighting
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 2, rayDescriptor, nextPathIntersection);

                if (computeDirect)
                {
                    // Use same ray for direct lighting (use indirect result for occlusion)
                    rayDescriptor.TMax = nextPathIntersection.t + _RaytracingRayBias;
                    float3 lightValue;
                    float lightPdf;
                    EvaluateLights(lightList, rayDescriptor, lightValue, lightPdf);

                    float misWeight = PowerHeuristic(pdf, lightPdf);
                    nextPathIntersection.value += lightValue * misWeight;
                }

#if defined(_SURFACE_TYPE_TRANSPARENT) && HAS_REFRACTION
                // Apply absorption on rays below the interface, using Beer-Lambert's law
                if (isSampleBelow)
                {
    #ifdef _REFRACTION_THIN
                    nextPathIntersection.value *= exp(-mtlData.bsdfData.absorptionCoefficient * REFRACTION_THIN_DISTANCE);
    #else
                    // FIXME: maxDist might need some more tweaking
                    float maxDist = surfaceData.atDistance * 10.0;
                    nextPathIntersection.value *= exp(-mtlData.bsdfData.absorptionCoefficient * min(nextPathIntersection.t, maxDist));
    #endif
                }
#endif

                pathIntersection.value += value * rrFactor * nextPathIntersection.value;
            }
        }
        */
    }

//#else // HAS_LIGHTLOOP
//    pathIntersection.value = (!currentDepth || computeDirect) ? bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;
//#endif

//    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), pathIntersection.t, pathIntersection.value, computeDirect);

//    if (currentDepth)
//    {
//        // Bias the result (making it too dark), but reduces fireflies a lot
//        float intensity = Luminance(pathIntersection.value) * GetCurrentExposureMultiplier();
//        if (intensity > _RaytracingIntensityClamp)
//           pathIntersection.value *= _RaytracingIntensityClamp / intensity;
//    }
}

[shader("anyhit")]
void AnyHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = pathIntersection.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    // Check alpha clipping
    if (!isVisible)
    {
        pathIntersection.color = float3(0.2f, 0.2f, 0.2f);
        pathIntersection.value = 0.98;
        IgnoreHit();
    }
    else if (pathIntersection.remainingDepth > _RaytracingMaxRecursion)
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
    #if HAS_REFRACTION
        pathIntersection.value *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        pathIntersection.value *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(pathIntersection.value) < 0.001)
            AcceptHitAndEndSearch();
        else
            IgnoreHit();
#else
        // Opaque surface
        pathIntersection.color = float3(0.3f, 0.3f, 0.3f);
        pathIntersection.value = 0.90;
        
        AcceptHitAndEndSearch();
#endif
    }
}
