#ifndef UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
#define UNITY_PATH_TRACING_INTEGRATOR_INCLUDED

// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingPayload.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSkySampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingAOV.hlsl"
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingVolume.hlsl"
#endif

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

float3 GetSkyValue(PathPayload payload, float3 direction)
{
    if (payload.segmentID == 0 && all(direction == WorldRayDirection()))
    {
        // If we can, access our high resolution screen-space background
        return GetSkyBackground(payload.pixelCoord).rgb;
    }

    // Otherwise query the lower resolution cubemap
    return GetSkyValue(direction);
}

#ifdef _ENABLE_SHADOW_MATTE

// Compute scalar visibility for shadow mattes, between 0 and 1
float ComputeVisibility(float3 position, float3 normal, float3 inputSample)
{
    // Select active types of lights
    bool withPoint = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_PUNCTUAL;
    bool withArea = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_AREA;
    bool withDistant = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_DIRECTIONAL;

    LightList lightList = CreateLightList(position, normal, DEFAULT_LIGHT_LAYERS, withPoint, withArea, withDistant);

    RayDesc ray;
    ray.Origin = position + normal * _RayTracingRayBias;
    ray.TMin = 0.0;

    // By default, full visibility
    float visibility = 1.0;

    // We will ignore value and pdf here, as we only want to catch occluders (no distance falloffs, cosines, etc.)
    float3 value;
    float pdf, shadowOpacity;

    if (SampleLights(lightList, inputSample, ray.Origin, normal, false, ray.Direction, value, pdf, ray.TMax, shadowOpacity))
    {
        // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
        PathPayload payload;
        payload.segmentID = SEGMENT_ID_TRANSMISSION;
        ray.TMax -= _RayTracingRayBias;
        payload.value = 1.0;

        // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                 RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, payload);

        visibility = Luminance(GetLightTransmission(payload.value, shadowOpacity));
    }

    return visibility;
}

#endif // _ENABLE_SHADOW_MATTE

// Function responsible for surface scattering
void ComputeSurfaceScattering(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes, float4 inputSample)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    // Make sure to add the additional travel distance to our cone
    payload.cone.width += payload.rayTHit * abs(payload.cone.spreadAngle);

#ifdef SHADER_UNLIT
    // This is quick and dirty way to avoid double contribution from light meshes
    if (payload.segmentID)
        payload.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = payload.pixelCoord;

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = payload.segmentID >= _RaytracingMinRecursion - 1;

    // Get our world space shading position
    float3 shadingPosition = fragInput.positionRWS;

    // We will write our AOV data in there
    AOVData aovData;

#ifndef SHADER_UNLIT

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // Take care of AOV data right away
    GetAOVData(bsdfData, aovData);
    WriteAOVData(aovData, shadingPosition, payload);

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    bsdfData.geomNormalWS = dot(bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

    // And reset the payload value, which will store our final radiance result for this path depth
    payload.value = computeDirect ? builtinData.emissiveColor : 0.0;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(payload, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        // Create the list of active lights
    #ifdef _SURFACE_TYPE_TRANSPARENT
        float3 lightNormal = 0.0;
    #else
        float3 lightNormal = GetLightNormal(mtlData);
    #endif
        LightList lightList = CreateLightList(shadingPosition, lightNormal, builtinData.renderingLayers);

        float pdf, shadowOpacity;
        float3 value;
        MaterialResult mtlResult;

        RayDesc ray;
        ray.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RayTracingRayBias;
        ray.TMin = 0.0;

        PathPayload shadowPayload;

        // Light sampling
        if (computeDirect)
        {
            if (SampleLights(lightList, inputSample.xyz, ray.Origin, lightNormal, false, ray.Direction, value, pdf, ray.TMax, shadowOpacity))
            {
                EvaluateMaterial(mtlData, ray.Direction, mtlResult);

                value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                if (Luminance(value) > 0.001)
                {
                    // Shoot a transmission ray
                    shadowPayload.segmentID = SEGMENT_ID_TRANSMISSION;
                    shadowPayload.value = 1.0;
                    ray.TMax -= _RayTracingRayBias;

                    // FIXME: For the time being, there is no front/back face culling for shadows
                    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                             RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, shadowPayload);

                    // Add direct light sampling contribution
                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    payload.value += value * GetLightTransmission(shadowPayload.value, shadowOpacity) * misWeight;
                }
            }
        }

        // Material sampling
        if (SampleMaterial(mtlData, inputSample.xyz, ray.Direction, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

            payload.throughput *= value;

            // Apply Russian roulette to our path (might be too aggressive)
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(payload.throughput);

            if (RussianRouletteTest(rrThreshold, rrValue, inputSample.w, rrFactor, !payload.segmentID))
            {
                // If the ray goes straight forward, set alpha accordingly
                if (dot(WorldRayDirection(), ray.Direction) > 0.999)
                    payload.alpha = 1.0 - rrFactor;

                bool isSampleBelow = IsBelow(mtlData, ray.Direction);

                ray.Origin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RayTracingRayBias, isSampleBelow);
                ray.TMax = FLT_INF;

                // Prepare our shadow payload with all required information
                shadowPayload.segmentID = SEGMENT_ID_NEAREST_HIT;
                shadowPayload.rayTHit = FLT_INF;

                // Shoot a ray returning nearest tHit, both to shadow direct lighting and optimize the continuation ray in the same direction
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER | RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, ray, shadowPayload);
                bool hit = shadowPayload.rayTHit < FLT_INF;

                // Compute material absorption (typically, tinted refraction), and throw in the Russian roulette compensation
                float3 absorption = rrFactor * GetMaterialAbsorption(mtlData, surfaceData, shadowPayload.rayTHit, isSampleBelow);

                if (computeDirect)
                {
                    // Use the hit distance to know which lights are visible
                    ray.TMax = shadowPayload.rayTHit + _RayTracingRayBias;
                    float3 lightValue;
                    float lightPdf;
                    EvaluateLights(lightList, ray, lightValue, lightPdf);

                    // Add direct material sampling contribution
                    value *= absorption;
                    float misWeight = PowerHeuristic(pdf, lightPdf);
                    payload.value += value * lightValue * misWeight;

                    // Add sky contribution separately, if not doing sky sampling
                    if (!IsSkySamplingEnabled() && !hit)
                    {
                        float3 skyValue = GetSkyValue(ray.Direction);
                        ApplyFogAttenuation(ray.Origin, ray.Direction, skyValue);
                        payload.value += value * skyValue;
                    }
                }

                // If we have a hit, we want to prepare our payload for a continuation ray
                if (hit)
                {
                    // Apply aborption to the throughput
                    payload.throughput *= absorption;

                    // Adjust the path max roughness (used for roughness clamping, to reduce fireflies)
                    payload.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, payload.maxRoughness);

                    // To perform texture filtering, we maintain a footprint of the pixel
                    payload.cone.spreadAngle = payload.cone.spreadAngle + roughnessToSpreadAngle(payload.maxRoughness);

                    // Update the actual continuation ray parameters
                    SetContinuationRay(ray.Origin, ray.Direction, shadowPayload.rayTHit, payload);
                }
            }
        }
    }

#else // SHADER_UNLIT

    payload.value = computeDirect ? surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;

    #ifdef _ENABLE_SHADOW_MATTE
    if (computeDirect)
    {
        float visibility = ComputeVisibility(fragInput.positionRWS, surfaceData.normalWS, inputSample.xyz);

        // Shadow color's alpha has a slightly different meaning depending on whether the surface is transparent or opaque
        #ifdef _SURFACE_TYPE_TRANSPARENT
        float3 shadowColor = surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier();
        builtinData.opacity = lerp(surfaceData.shadowTint.a, builtinData.opacity, visibility);
        #else
        float3 shadowColor = lerp(payload.value, surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier(), surfaceData.shadowTint.a);
        #endif

        payload.value = lerp(shadowColor, payload.value, visibility);
    }
    #endif // _ENABLE_SHADOW_MATTE

    // Get the closest thing we have to a shading normal in the Unlit model
    float3 shadingNormal = fragInput.tangentToWorld[2];

    // Grab AOV data for Unlit
    aovData.albedo = surfaceData.color;
    aovData.normal = shadingNormal;
    WriteAOVData(aovData, shadingPosition, payload);

    #ifdef _SURFACE_TYPE_TRANSPARENT
    if (builtinData.opacity < 1.0)
    {
        // Simulate opacity blending by simply continuing along the current ray
        PathPayload shadowPayload;
        shadowPayload.segmentID = SEGMENT_ID_NEAREST_HIT;
        shadowPayload.rayTHit = FLT_INF;

        float bias = dot(WorldRayDirection(), shadingNormal) > 0.0 ? _RayTracingRayBias : -_RayTracingRayBias;

        RayDesc ray;
        ray.Origin = shadingPosition + bias * shadingNormal;
        ray.Direction = WorldRayDirection();
        ray.TMin = 0.0;
        ray.TMax = FLT_INF;

        // Shoot a ray returning nearest tHit, to decide if we fetch the sky value or fire a continuation ray in the same direction
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER | RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, ray, shadowPayload);
        bool hit = shadowPayload.rayTHit < FLT_INF;

        if (computeDirect)
        {
            payload.value *= builtinData.opacity;
            if (!hit)
            {
                float3 skyValue = GetSkyValue(payload, ray.Direction);
                ApplyFogAttenuation(ray.Origin, ray.Direction, skyValue);
                payload.value += (1.0 - builtinData.opacity) * skyValue;
            }
        }

        if (hit)
        {
            // Update our payload to fire a continuation ray
            payload.throughput *= 1.0 - builtinData.opacity;
            SetContinuationRay(ray.Origin, ray.Direction, shadowPayload.rayTHit, payload);
        }

        // Set alpha to the opacity value
        payload.alpha = builtinData.opacity;
    }
    #endif

#endif // SHADER_UNLIT
}

// Generic function that handles one scattering event (a vertex along the full path), can be either:
// - Surface scattering
// - Volume scattering
[shader("closesthit")]
void ClosestHit(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t and initial alpha value
    payload.rayTHit = RayTCurrent();
    payload.alpha = 1.0;

    bool computeDirect = payload.segmentID >= _RaytracingMinRecursion - 1;
    bool sampleVolume = false;

    float4 inputSample = 0.0;
    float volSurfPdf = 1.0;

#ifdef HAS_LIGHTLOOP

    float3 lightPosition;
    bool sampleLocalLights;

    // Generate a 4D unit-square sample for this depth, from our QMC sequence
    inputSample = GetSample4D(payload.pixelCoord, _RaytracingSampleIndex, 4 * payload.segmentID);

    // For the time being, we test for volumetric scattering only on camera rays
    if (!payload.segmentID && computeDirect)
        sampleVolume = SampleVolumeScatteringPosition(payload.pixelCoord, inputSample.w, payload.rayTHit, volSurfPdf, sampleLocalLights, lightPosition);

    if (sampleVolume)
        ComputeVolumeScattering(payload, inputSample.xyz, sampleLocalLights, lightPosition);
    else
        ComputeSurfaceScattering(payload, attributeData, inputSample);

    computeDirect &= !sampleVolume;

#else // HAS_LIGHTLOOP

    ComputeSurfaceScattering(payload, attributeData, inputSample);

#endif // HAS_LIGHTLOOP

    // Apply volumetric attenuation (beware of passing the right distance to the shading point)
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), sampleVolume ? payload.rayTHit : RayTCurrent(),
                        payload.value, payload.alpha, payload.throughput, computeDirect);

    // Apply the volume/surface PDF
    payload.value /= volSurfPdf;
    payload.alpha /= volSurfPdf;
    payload.throughput /= volSurfPdf;
}

[shader("anyhit")]
void AnyHit(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
#ifdef _ALPHATEST_ON

    // First grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = payload.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    // Check alpha clipping
    if (!isVisible)
    {
        IgnoreHit();
        return;
    }

#endif // _ALPHATEST_ON

    if (payload.segmentID == SEGMENT_ID_NEAREST_HIT )
    {
        // We just need the nearest hit distance here
        payload.rayTHit = min(payload.rayTHit, RayTCurrent());
    }
    else if (payload.segmentID == SEGMENT_ID_RANDOM_WALK)
    {
        if (RayTCurrent() < payload.rayTHit)
        {

#ifndef _ALPHATEST_ON

            // First grab the intersection vertex
            IntersectionVertex currentVertex;
            GetCurrentIntersectionVertex(attributeData, currentVertex);

            // Build the Frag inputs from the intersection vertex
            FragInputs fragInput;
            BuildFragInputsFromIntersection(currentVertex, fragInput);

#endif // _ALPHATEST_ON

            payload.value = fragInput.tangentToWorld[2];
            payload.rayTHit = RayTCurrent();
        }
    }
    else if (payload.segmentID == SEGMENT_ID_TRANSMISSION)
    {

#ifdef _SURFACE_TYPE_TRANSPARENT

    #ifndef _ALPHATEST_ON

        // First grab the intersection vertex
        IntersectionVertex currentVertex;
        GetCurrentIntersectionVertex(attributeData, currentVertex);

        // Build the Frag inputs from the intersection vertex
        FragInputs fragInput;
        BuildFragInputsFromIntersection(currentVertex, fragInput);

        PositionInputs posInput;
        posInput.positionWS = fragInput.positionRWS;
        posInput.positionSS = payload.pixelCoord;

        // Build the surfacedata and builtindata
        SurfaceData surfaceData;
        BuiltinData builtinData;
        bool isVisible;
        GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    #endif // _ALPHATEST_ON

    #if HAS_REFRACTION
        payload.value *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        payload.value *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(payload.value) > 0)
        {
            IgnoreHit();
        }

#else // _SURFACE_TYPE_TRANSPARENT

        payload.value = 0.0; // Opaque surface

#endif // _SURFACE_TYPE_TRANSPARENT

    }
}

#endif // UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
