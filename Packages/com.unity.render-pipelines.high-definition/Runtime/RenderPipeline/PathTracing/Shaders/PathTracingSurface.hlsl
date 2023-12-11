#ifndef UNITY_PATH_TRACING_SURFACE_INCLUDED
#define UNITY_PATH_TRACING_SURFACE_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

struct SurfaceInfo
{
    FragInputs  fragInput;
    SurfaceData surfaceData;
    BuiltinData builtinData;
#ifndef SHADER_UNLIT
    BSDFData    bsdfData;
#endif
};

void PushSurfaceAOVData(SurfaceInfo surfaceInfo, inout PathPayload payload : SV_RayPayload)
{
    // We will write our AOV data in there
    AOVData aovData;
    float3 shadingPosition = surfaceInfo.fragInput.positionRWS;

#ifndef SHADER_UNLIT
    GetAOVData(surfaceInfo.bsdfData, aovData);
#else
    // Get the closest thing we have to a shading normal in the Unlit model
    float3 shadingNormal = surfaceInfo.fragInput.tangentToWorld[2];
    // Set AOV data for Unlit
    aovData.albedo = surfaceInfo.surfaceData.color;
    aovData.normal = shadingNormal;
#endif
    WriteAOVData(aovData, shadingPosition, payload);
}

void GetSurfaceInfo(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes, out SurfaceInfo surfaceInfo)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    BuildFragInputsFromIntersection(currentVertex, surfaceInfo.fragInput);

    // Make sure to add the additional travel distance to our cone
    payload.cone.width += payload.rayTHit * abs(payload.cone.spreadAngle);

#ifdef SHADER_UNLIT
    // This is quick and dirty way to avoid double contribution from light meshes
    if (payload.segmentID)
        payload.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = surfaceInfo.fragInput.positionRWS;
    posInput.positionSS = payload.pixelCoord;

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    surfaceInfo.fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    bool isVisible;
    GetSurfaceAndBuiltinData(surfaceInfo.fragInput, -WorldRayDirection(), posInput, surfaceInfo.surfaceData, surfaceInfo.builtinData, currentVertex, payload.cone, isVisible);

#ifndef SHADER_UNLIT

    // Compute the bsdf data
    surfaceInfo.bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceInfo.surfaceData);

#ifdef ENABLE_MATERIAL_AMBIENT_OCCLUSION
    // If this is the first segment, then there hasn't been any bounces yet
    if (payload.segmentID == 0)
    {
        // Disable AO for direct lighting
        surfaceInfo.bsdfData.ambientOcclusion = 1.0;
    }
#else
    surfaceInfo.bsdfData.ambientOcclusion = 1.0;
#endif

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    surfaceInfo.bsdfData.geomNormalWS = dot(surfaceInfo.bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

#endif // SHADER_UNLIT
}

// Function responsible for surface scattering
void ComputeSurfaceScattering(inout PathPayload payload : SV_RayPayload, SurfaceInfo surfaceInfo, float4 inputSample)
{
    // Check if we want to add direct and emissive lighting for current depth (relative to min depth)
    bool minDepthAllowsEmissive = payload.segmentID >= _RaytracingMinRecursion - 1;
    bool minDepthAllowsDirect = payload.segmentID + 1 >= _RaytracingMinRecursion - 1;
    // Check if we want to send more rays after the segment that was just traced
    bool haveReachedMaxDepth = payload.segmentID + 1 > _RaytracingMaxRecursion - 1;

    // Get our world space shading position
    float3 shadingPosition = surfaceInfo.fragInput.positionRWS;

#ifndef SHADER_UNLIT

    // And reset the payload value, which will store our final radiance result for this path depth
    payload.value = minDepthAllowsEmissive ? surfaceInfo.builtinData.emissiveColor : 0.0;

    // If we have reached the maximum recursion depth, we want to stop the path
    if (haveReachedMaxDepth)
        return;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(payload, surfaceInfo.builtinData, surfaceInfo.bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        // Create the list of active lights
    #ifdef _SURFACE_TYPE_TRANSPARENT
        float3 lightNormal = 0.0;
    #else
        float3 lightNormal = GetLightNormal(mtlData);
    #endif
        LightList lightList = CreateLightList(shadingPosition, lightNormal, surfaceInfo.builtinData.renderingLayers);
        payload.lightListParams = float4(lightNormal, surfaceInfo.builtinData.renderingLayers);

        // Compute absorption along the just traced segment. PSA: absorption needs to be handled differently for thin vs other refraction models.
        #ifndef _REFRACTION_THIN
        float3 segmentAbsorption = GetMaterialAbsorption(mtlData, surfaceInfo.surfaceData, RayTCurrent(), IsBelow(mtlData, -1 * WorldRayDirection()));
        #else
        float3 segmentAbsorption = float3(1, 1, 1);
        #endif

        // multiply the absorption along the just traced segment into the throughput for the next segments
        payload.segmentThroughput *= segmentAbsorption;
        payload.throughput *= segmentAbsorption;
        // and into the emission
        payload.value *= segmentAbsorption;

        //  Sampling for new surface interaction
        float pdf, shadowOpacity;
        float3 value;
        MaterialResult mtlResult;

        // Next Event Estimation
        float3 sampleRayOrigin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RayTracingRayBias;
        float3 sampleRayDirection;
        float sampleDistance;

        if (minDepthAllowsDirect)
        {
            if (SampleLights(lightList, inputSample.xyz, sampleRayOrigin, lightNormal, false, sampleRayDirection, value, pdf, sampleDistance, shadowOpacity))
            {
                EvaluateMaterial(mtlData, sampleRayDirection, mtlResult);
                value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                if (GetCurrentExposureMultiplier() * Luminance(value) > 0.001)
                {
                    // When leaving a solid object, include absorption for Light Sampling
                    // When shading a thin object, don't
                    value *= segmentAbsorption;
                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    #ifdef _PATH_TRACED_DUAL_SCATTERING
                    SetPathTracingFlag(payload, PATHTRACING_FLAG_DUAL_SCATTERING_VIS);
                    #endif
                    // Store the information relating to the sample in the payload so the main loop can trace the shadow ray
                    PushLightSampleQuery(sampleRayOrigin, sampleRayDirection, sampleDistance - _RayTracingRayBias, value * misWeight, shadowOpacity, payload);
                }
            }
        }

        // Material sampling
        if (SampleMaterial(mtlData, inputSample.xyz, sampleRayDirection, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;
            payload.throughput *= value;
            payload.interactionThroughput *= value;
            payload.materialSamplePdf = pdf;

            // Compute absorption if we refract through a thin surface
            #ifndef _REFRACTION_THIN
            float3 interfaceAbsorption = float3(1, 1, 1);
            #else
            float3 interfaceAbsorption = GetMaterialAbsorption(mtlData, surfaceInfo.surfaceData, RayTCurrent(), IsBelow(mtlData, sampleRayDirection));
            #endif

            // Apply Russian roulette to our path (might be too aggressive)
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(payload.throughput);

            if (RussianRouletteTest(rrThreshold, rrValue, inputSample.w, rrFactor, !payload.segmentID))
            {
                // If the ray goes straight forward, set alpha accordingly
                if (dot(WorldRayDirection(), sampleRayDirection) > 0.999)
                    payload.alpha = 1.0 - rrFactor;

                bool isSampleBelow = IsBelow(mtlData, sampleRayDirection);

                sampleRayOrigin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RayTracingRayBias, isSampleBelow);

                // Adjust the path parameters to prepare for the next segment
                payload.interactionThroughput *= rrFactor * interfaceAbsorption;
                payload.throughput *= rrFactor * interfaceAbsorption;
                payload.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, payload.maxRoughness);
                payload.cone.spreadAngle = payload.cone.spreadAngle + roughnessToSpreadAngle(payload.maxRoughness);

                PushMaterialSampleQuery(sampleRayOrigin, sampleRayDirection, payload);
            }
        }
    }

#else // SHADER_UNLIT

    // Set the flag for Unlit
    SetPathTracingFlag(payload, PATHTRACING_FLAG_UNLIT_MODEL);

    // Get the closest thing we have to a shading normal in the Unlit model
    float3 shadingNormal = surfaceInfo.fragInput.tangentToWorld[2];

    // Compute the base value for Unlit
    float3 finalShadowValue = minDepthAllowsEmissive ? surfaceInfo.surfaceData.color * GetInverseCurrentExposureMultiplier() + surfaceInfo.builtinData.emissiveColor : 0.0;

    // If we have reached the maximum recursion depth, we want to stop the path
    if (haveReachedMaxDepth)
    {
        payload.value = finalShadowValue;
        return;
    }

    // If shadow mattes are enabled, compute the shadow color
    #ifdef _ENABLE_SHADOW_MATTE
    float3 shadowColor = float3(0, 0, 0);

    if (minDepthAllowsDirect)
    {
        // Shadow color's alpha has a slightly different meaning depending on whether the surface is transparent or opaque
    #ifdef _SURFACE_TYPE_TRANSPARENT
        SetPathTracingFlag(payload, PATHTRACING_FLAG_INTERPOLATE_OPACITY);
        payload.lightSampleShadowOpacityAndShadowTint.y = surfaceInfo.surfaceData.shadowTint.a;
        shadowColor = surfaceInfo.surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier();
    #else // _SURFACE_TYPE_TRANSPARENT
        shadowColor = lerp(finalShadowValue, surfaceInfo.surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier(), surfaceInfo.surfaceData.shadowTint.a);
    #endif // _SURFACE_TYPE_TRANSPARENT
    }
    #endif // _ENABLE_SHADOW_MATTE

    // In case the surface is transparent, set the ray direction for continued path tracing
    #ifdef _SURFACE_TYPE_TRANSPARENT
    // Simulate opacity blending by simply continuing along the current ray -- we don't actually sample anything, but we put the parameters into the material sample to continue path tracing
    float bias = dot(WorldRayDirection(), shadingNormal) > 0.0 ? _RayTracingRayBias : -_RayTracingRayBias;
    // Don't touch MIS sample pdf, as the ray will be continued; the original value remains relevant.
    PushMaterialSampleQuery(shadingPosition + bias * shadingNormal, WorldRayDirection(), payload);

    // We can only make sure the ray is needed once we have the correct opacity value, which depends on the visibility calculation. This needs to happen in the main loop.
    payload.alpha = surfaceInfo.builtinData.opacity;
    #endif // _SURFACE_TYPE_TRANSPARENT

    // Finally, if shadow mattes are enabled, set the shadow ray to be traced in the main loop
    #ifdef _ENABLE_SHADOW_MATTE
    if(minDepthAllowsDirect)
    {
        // Select active types of lights
        bool withPoint = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_PUNCTUAL;
        bool withArea = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_AREA;
        bool withDistant = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_DIRECTIONAL;

        float3 sampleRayOrigin = surfaceInfo.fragInput.positionRWS + surfaceInfo.surfaceData.normalWS * _RayTracingRayBias;
        float3 sampleRayDirection;
        float sampleDistance;

        // We will ignore value and pdf here, as we only want to catch occluders (no distance falloffs, cosines, etc.)
        float3 ignoredValue;
        float pdf, shadowOpacity;

        LightList lightList = CreateLightList(surfaceInfo.fragInput.positionRWS, surfaceInfo.surfaceData.normalWS, RENDERING_LAYERS_MASK, withPoint, withArea, withDistant);
        if (SampleLights(lightList, inputSample.xyz, sampleRayOrigin, surfaceInfo.surfaceData.normalWS, false, sampleRayDirection, ignoredValue, pdf, sampleDistance, shadowOpacity))
        {
            // Store the information relating to the sample in the payload so the main loop can trace the shadow ray
            PushLightSampleQueryUnlit(sampleRayOrigin, sampleRayDirection, sampleDistance - _RayTracingRayBias, finalShadowValue, shadowOpacity, shadowColor, payload);
        }
    }
    else
    {
        // otherwise simply add the computed value to the final result
        payload.value = finalShadowValue;
    }
    #else // otherwise always add the computed value to the final result
    payload.value = finalShadowValue;
    #endif // _ENABLE_SHADOW_MATTE
#endif // SHADER_UNLIT

}

#endif // UNITY_PATH_TRACING_SURFACE_INCLUDED
