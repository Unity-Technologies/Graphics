#ifndef WATER_UTILITIES_H
#define WATER_UTILITIES_H

#define WATER_IOR 1.3333
#define WATER_INV_IOR 1.0 / WATER_IOR
#define AMBIENT_SCATTERING_INTENSITY 0.25
#define HEIGHT_SCATTERING_INTENSITY 0.25
#define DISPLACEMENT_SCATTERING_INTENSITY 0.25
#define UNDER_WATER_REFRACTION_DISTANCE 100.0
#define MAX_MENISCUS_REFRACTION_MULTIPLIER 0.5
#define MENISCUS_THRESHOLD 0.05

// Includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/SampleWaterSurface.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"

float2 EvaluateSurfaceGradients(float3 p0, float3 p1, float3 p2)
{
    float3 v0 = normalize(p1 - p0);
    float3 v1 = normalize(p2 - p0);
    float3 geometryNormal = normalize(cross(v1, v0));
    return SurfaceGradientFromPerturbedNormal(float3(0, 1, 0), geometryNormal).xz;
}

#if !defined(WATER_SIMULATION)
float3 ComputeDebugNormal(float3 worldPos)
{
    float3 worldPosDdx = normalize(ddx(worldPos));
    float3 worldPosDdy = normalize(ddy(worldPos));
    return normalize(-cross(worldPosDdx, worldPosDdy));
}

float3 EvaluateWaterSurfaceGradient_VS(float3 positionAWS, int LOD, int bandIndex)
{
    // Compute the simulation coordinates
    float2 uvBand = TransformWaterUV(positionAWS.xz, bandIndex);

    // Compute the texture size param for the filtering
    int2 res = _BandResolution >> LOD;
    float4 texSize = 0.0;
    texSize.xy = res;
    texSize.zw = 1.0f / res;

    // First band
    float4 additionalData = SampleTexture2DArrayBicubicLOD(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), uvBand, bandIndex, texSize, LOD);
    float3 surfaceGradient = float3(additionalData.x, 0, additionalData.y);

    // Blend the various surface gradients
    return surfaceGradient;
}

float3 BlendWaterNormal(float3 normalTS, float3 normalWS)
{
    // TODO: figure out why i have to invert the x component here
    normalTS.x = -normalTS.x;
    float3x3 frame = GetLocalFrame(normalWS);
    return TransformTangentToWorld(normalTS, frame, true);
}
#endif

struct FoamData
{
    float smoothness;
    float foamValue;
};

void EvaluateFoamData(float surfaceFoam, float customFoam, float3 positionOS, out FoamData foamData)
{
    float foamLifeTime = saturate(1.0 - (surfaceFoam + customFoam));
    foamData.foamValue = FoamErosion(foamLifeTime, positionOS.xz);

    // Blend the smoothness of the water and the foam
    foamData.smoothness = lerp(_WaterSmoothness, _WaterFoamSmoothness, saturate(foamData.foamValue));
}

#define WATER_BACKGROUND_ABSORPTION_DISTANCE 1000.f

float EvaluateHeightBasedScattering(float lowFrequencyHeight, float distanceToCamera)
{
    // Lerp towards middle height of 0.5 as distance increases
    // height is already faded because it's computed form vertex displacement which is faded.
    // But add additional fading using twice the distance to reduce visual repetition
    lowFrequencyHeight = lerp(0.5, lowFrequencyHeight, DistanceFade(distanceToCamera * 2, 0));

    float heightScatteringValue = lerp(0.0, HEIGHT_SCATTERING_INTENSITY, lowFrequencyHeight);
    return lerp(0.0, heightScatteringValue, _HeightBasedScattering);
}

float EvaluateDisplacementScattering(float displacement)
{
    float displacementScatteringValue = lerp(0.0, DISPLACEMENT_SCATTERING_INTENSITY, displacement / (_ScatteringWaveHeight * WATER_SYSTEM_CHOPPINESS));
    return lerp(0.0, displacementScatteringValue, _DisplacementScattering);
}

float GetWaveTipThickness(float normalizedDistanceToMaxWaveHeightPlane, float3 worldView, float3 refractedRay)
{
    float H = saturate(normalizedDistanceToMaxWaveHeightPlane);
    return 1.f - saturate(1 + refractedRay.y - 0.2) * (H * H) / 0.4;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

float EdgeBlendingFactor(float2 screenPosition, float distanceToWaterSurface)
{
    // Convert the screen position to NDC
    float2 screenPosNDC = screenPosition * 2 - 1;

    // We want the value to be 0 at the center and go to 1 at the edges
    float distanceToEdge = 1.0 - min((1.0 - abs(screenPosNDC.x)), (1.0 - abs(screenPosNDC.y)));

    // What we want here is:
    // - +inf -> 0.5 value is 0
    // - 0.5-> 0.25 value is going from  0 to 1
    // - 0.25 -> 0 value is 1
    float distAttenuation = 1.0 - saturate((distanceToWaterSurface - 0.75) / 0.25);

    // Based on if the water surface is close, we want to make the blending region even bigger
    return lerp(saturate((distanceToEdge - 0.8) / (0.2)), saturate(distanceToEdge + 0.25), distAttenuation);
}

void ComputeWaterRefractionParams(float3 waterPosRWS, float2 positionNDC, float3 V,
    float3 waterNormal, float3 lowFrequencyNormals, bool aboveWater,
    bool disableUnderWaterIOR, float3 upVector, float maxRefractionDistance,
    float3 extinctionCoeff,
    out float3 refractedWaterPosRWS, out float2 distortedWaterNDC, out float3 absorptionTint)
{
    absorptionTint = 0.0f;

    // Compute the position of the surface behind the water surface
    float  directWaterDepth = SampleCameraDepth(positionNDC);
    float3 directWaterPosRWS = ComputeWorldSpacePosition(positionNDC, directWaterDepth, UNITY_MATRIX_I_VP);

    // Compute the distance between the water surface and the object behind
    float underWaterDistance = directWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(directWaterPosRWS - waterPosRWS);

    // We approach the refraction differently if we are under or above water for various reasons

    float3 distortedWaterWS = 0.0f;
    if (aboveWater || disableUnderWaterIOR)
    {
        float3 refractedView = lerp(waterNormal, upVector, EdgeBlendingFactor(positionNDC, length(waterPosRWS))) * (1 - upVector);

        // If camera is below the water surface, we mulitply the maxRefractionDistance with (half) the distance between the water surface pixel and the camera water depth.
        float refractionDistance;
        if (!aboveWater)
            refractionDistance = maxRefractionDistance * abs(waterPosRWS.y) * 0.5f;
        else
            refractionDistance = min(underWaterDistance, maxRefractionDistance);

        distortedWaterWS = waterPosRWS + refractedView * refractionDistance;
    }

    // If underwater
    if (!aboveWater)
    {
        float3 refractedView = refract(-V, waterNormal, WATER_IOR);
        if (any(refractedView != 0.0f)) // not TIR
            absorptionTint = 1.0f;

        if (disableUnderWaterIOR)
        {
            // At the limit between refraction and internal reflection, we simulate a higher refraction to avoid having a harsh threshold between both ray directions.
            float NdotV = dot(waterNormal, V);
            float k = 1.f - WATER_IOR * WATER_IOR * (1.f - NdotV * NdotV);
            float refractionValueMultiplier = 1;
            if (k >= -MENISCUS_THRESHOLD && k <= MENISCUS_THRESHOLD)
            {
                float lerpFactor = saturate((k + MENISCUS_THRESHOLD) / (2 * MENISCUS_THRESHOLD));
                refractionValueMultiplier *= lerp(MAX_MENISCUS_REFRACTION_MULTIPLIER, 0, lerpFactor);
                distortedWaterWS += refractedView * refractionValueMultiplier;

                absorptionTint = saturate(2 * lerpFactor - 1);
            }
        }
        else
        {
            distortedWaterWS = waterPosRWS + refractedView * UNDER_WATER_REFRACTION_DISTANCE;
        }
    }

    // Project the point on screen
    distortedWaterNDC = saturate(ComputeNormalizedDeviceCoordinates(distortedWaterWS, UNITY_MATRIX_VP));
    distortedWaterNDC = min(distortedWaterNDC, 1.0f - _ScreenSize.zw);

    // Compute the position of the surface behind the water surface
    float refractedWaterDepth = SampleCameraDepth(distortedWaterNDC);
    refractedWaterPosRWS = ComputeWorldSpacePosition(distortedWaterNDC, refractedWaterDepth, UNITY_MATRIX_I_VP);
    float refractedWaterDistance = refractedWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(refractedWaterPosRWS - waterPosRWS);

    // If the point that we are reading is closer than the water surface
    if (dot(refractedWaterPosRWS - waterPosRWS, V) > 0.0)
    {
        // We read the direct depth (no refraction)
        refractedWaterDistance = underWaterDistance;
        refractedWaterPosRWS = directWaterPosRWS;
        distortedWaterNDC = positionNDC;
    }

    // Evaluate the absorption tint
    if (aboveWater)
        absorptionTint = exp(-refractedWaterDistance * extinctionCoeff);
}

float EvaluateTipThickness(float3 viewWS, float3 lowFrequencyNormals, float lowFrequencyHeight)
{
    // Compute the tip thickness
    float tipHeight = saturate(lowFrequencyHeight * 2.0 - 1.0); // ignore negative displacement
    float3 lowFrequencyRefractedRay = refract(-viewWS, lowFrequencyNormals, WATER_INV_IOR);
    return GetWaveTipThickness(max(0.01, tipHeight), viewWS, lowFrequencyRefractedRay);
}

float3 EvaluateRefractionColor(float3 absorptionTint, float3 caustics)
{
    // Evaluate the refraction color (we need to account for the initial absoption (light to underwater))
    return absorptionTint * caustics * absorptionTint;
}

float3 EvaluateScatteringColor(float3 positionOS, float lowFrequencyHeight, float horizontalDisplacement, float3 absorptionTint, float deepFoam)
{
    // Evaluate the pre-displaced absolute position
    float3 positionRWS = TransformObjectToWorld(positionOS);
    float distanceToCamera = length(positionRWS);

    // Evaluate the scattering terms (where the refraction doesn't happen)
    float heightBasedScattering = EvaluateHeightBasedScattering(lowFrequencyHeight, distanceToCamera);
    float displacementScattering = EvaluateDisplacementScattering(horizontalDisplacement);
    float ambientScattering = AMBIENT_SCATTERING_INTENSITY * _AmbientScattering;

    // Sum the scattering terms
    float scatteringTerms = saturate(ambientScattering + heightBasedScattering + displacementScattering);
    return _WaterAlbedo.xyz * scatteringTerms * (1.f - absorptionTint) * (1.0 + deepFoam);
}
#endif // WATER_UTILITIES_H
