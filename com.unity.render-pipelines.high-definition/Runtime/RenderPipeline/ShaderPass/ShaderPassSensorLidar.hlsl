// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/SensorIntersection.hlsl"
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#endif

int _SensorLightCount;

#ifdef SENSORSDK_OVERRIDE_REFLECTANCE

TEXTURE2D(_SensorCustomReflectance);
float Wavelength;

float3 OverrideReflectance()
{
    const float minWaveLengthValue = 0.35; // 350 nm
    const float maxWaveLengthValue = 2.5; // 2500 nm

    float wlIdx = clamp(Wavelength * 0.001, minWaveLengthValue, maxWaveLengthValue);
    float wavelengthSpan = maxWaveLengthValue - minWaveLengthValue + 1.0;
    float2 coordCurve = float2(wlIdx / wavelengthSpan, 0.0);

    return SAMPLE_TEXTURE2D(_SensorCustomReflectance, s_linear_clamp_sampler, coordCurve);
}

#endif // SENSORSDK_OVERRIDE_REFLECTANCE

bool SampleBeam(LightData lightData,
                float3 lightPosition,
                float3 lightDirection,
                float3 position,
                float3 normal,
                out float3 outgoingDir,
                out float3 value)
{
    const float MM_TO_M = 1e-3;
    const float M_TO_MM = 1e3;

    outgoingDir = position - lightPosition;
    float dist = length(outgoingDir);
    outgoingDir /= dist;

    float apertureRadius = lightData.size.x;
    float w0 = lightData.size.y;
    float zr = lightData.size.z;
    float distToWaist = lightData.size.w;

    // get the hit point in the coordinate frame of the laser as a depth(z) and
    // radial measure(r)
    float ctheta = dot(lightDirection, outgoingDir);
    float zFromAperture = ctheta * dist;
    float rSq = Sq(dist) - Sq(zFromAperture);
    float3 radialDirection = dist * outgoingDir - zFromAperture*lightDirection;

    float zFromWaist = abs(zFromAperture * M_TO_MM - distToWaist);
    if (dot(normal, -outgoingDir) < 0.001)
        return false;

    // Total beam power, note: different from the output here which is irradiance.
    float P = lightData.color.x;

    const float zRatio = Sq(zFromWaist / zr);
    const float wz = w0 * sqrt(1 + zRatio) * MM_TO_M;
    const float Eoz = 2 * P;
    const float wzSq = wz*wz;

    float gaussianFactor = exp(-2 * rSq / wzSq) / (PI * wzSq); // 1/m^2
    value.x = gaussianFactor * Eoz; // W/m^2
    value.y = wz; // beamRadius
    value.z = zFromAperture; // beamDepth

    // sampling a point in the "virtual" aperture
    // Find the actual point in the beam aperture that corresponds to this point
    float rRatio = apertureRadius / wz;
    float3 pAperture = lightPosition + rRatio * radialDirection; // location of the point in the aperture

    outgoingDir = pAperture - position; // corrected outgoing vector using the assumption below
    dist = length(outgoingDir);
    outgoingDir /= dist;

    return value.x > 0.0;
}

[shader("closesthit")]
void ClosestHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    pathIntersection.t = RayTCurrent();

    // Then grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    // Such an invalid remainingDepth value means we are called from a subsurface computation
    if (pathIntersection.remainingDepth > _RaytracingMaxRecursion)
    {
        pathIntersection.value = fragInput.tangentToWorld[2]; // Returns normal
        return;
    }

    // Fetch, then clear the beam data aliased in our payload
    const float3 beamOrigin = GetBeamOrigin(pathIntersection);
    const float3 beamDirection = GetBeamDirection(pathIntersection);
    clearBeamData(pathIntersection);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = pathIntersection.pixelCoord;

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    #ifdef SENSORSDK_OVERRIDE_REFLECTANCE
    // Override the diffuce color when using builtin lit shader (but not with shader graph)
    bsdfData.diffuseColor = OverrideReflectance();
    #endif

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    bsdfData.geomNormalWS = dot(bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

    // Compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 shadingPosition = fragInput.positionRWS;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;

    const int nBounce = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    float3 inputSample = GetSample(pathIntersection.pixelCoord, _RaytracingSampleIndex, nBounce*3);


    float lightValue = 0;
    float directLighting = 0;
    float indirectLighting = 0;
    if (CreateMaterialData(pathIntersection, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        RayDesc rayDescriptor;
        rayDescriptor.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RaytracingRayBias;
        rayDescriptor.TMin = 0.0;

        float3 value, direction;
        MaterialResult mtlResult;
        if(nBounce < 1)
        {
            for (uint i = 0; i < _SensorLightCount; i++)
            {
                if (SampleBeam(_LightDatasRT[i], beamOrigin, beamDirection, shadingPosition, bsdfData.normalWS, direction, value))
                {
                    EvaluateMaterial(mtlData, direction, mtlResult);

                    // value is in radian (w/sr) not in lumen (cd/sr), only on the red channel
                    lightValue += value.x;
                    float pdf = mtlResult.diffPdf.x + mtlResult.specPdf.x;
                    directLighting += value.x * (mtlResult.diffValue.x + mtlResult.specValue.x) / pdf;
                    //pathIntersection.value.x += mtlResult.diffValue.x * value.x;
                }
            }
        }
        else
        {
            EvaluateMaterial(mtlData, -WorldRayDirection(), mtlResult);
            lightValue = pathIntersection.value;

            float pdf = mtlResult.diffPdf.x + mtlResult.specPdf.x;
            directLighting = lightValue.x * (mtlResult.diffValue.x * mtlResult.specValue.x) / pdf;
        }
/*
#ifdef _SURFACE_TYPE_TRANSPARENT
        if(SampleMaterial(mtlData, inputSample.xyz, rayDescriptor.Direction, mtlResult))
        {
            PathIntersection nextPathIntersection;
            float value = (mtlResult.diffValue.x + mtlResult.specValue.x) / (mtlResult.diffPdf + mtlResult.specPdf);

            bool isSampleBelow = IsBelow(mtlData, rayDescriptor.Direction);

            float3 offset = _RaytracingRayBias * mtlData.bsdfData.geomNormalWS;
            rayDescriptor.Origin = shadingPosition + isSampleBelow ? -offset : offset;
            rayDescriptor.TMax = FLT_INF;

            // Copy path constants across
          //  nextPathIntersection.pixelCoord = pathIntersection.pixelCoord;
          //  nextPathIntersection.cone.width = pathIntersection.cone.width;

            // Complete PathIntersection structure for this sample
            nextPathIntersection.value.x = lightValue.x * value.x;
            nextPathIntersection.remainingDepth = pathIntersection.remainingDepth - 1;
            nextPathIntersection.t = rayDescriptor.TMax;

            // Adjust the path max roughness (used for roughness clamping, to reduce fireflies)
            nextPathIntersection.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, pathIntersection.maxRoughness);

            // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
            //nextPathIntersection.cone.spreadAngle = pathIntersection.cone.spreadAngle + roughnessToSpreadAngle(nextPathIntersection.maxRoughness);

            // Shoot ray for indirect lighting
            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 2, rayDescriptor, nextPathIntersection);

            if(directLighting.x < nextPathIntersection.value.x)
            {
                pathIntersection.t += nextPathIntersection.t;
            }

            pathIntersection.value = directLighting + nextPathIntersection.value;
        }
#endif
*/
        ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), pathIntersection.t, pathIntersection.value);

        // Copy the last beam radius and depth to the payload
        pathIntersection.value.yz = value.yz;
    }
}
