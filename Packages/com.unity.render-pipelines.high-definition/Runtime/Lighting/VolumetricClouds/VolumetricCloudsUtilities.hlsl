#ifndef VOLUMETRIC_CLOUD_UTILITIES_H
#define VOLUMETRIC_CLOUD_UTILITIES_H

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricClouds/VolumetricCloudsDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudUtils.hlsl"

// The number of octaves for the multi-scattering
#define NUM_MULTI_SCATTERING_OCTAVES 2
#define PHASE_FUNCTION_STRUCTURE float2
// Global offset to the high frequency noise
#define CLOUD_DETAIL_MIP_OFFSET 0.0
// Global offset for reaching the LUT/AO
#define CLOUD_LUT_MIP_OFFSET 1.0
// Density below wich we consider the density is zero (optimization reasons)
#define CLOUD_DENSITY_TRESHOLD 0.001f
// Number of steps before we start the large steps
#define EMPTY_STEPS_BEFORE_LARGE_STEPS 8
// Forward eccentricity
#define FORWARD_ECCENTRICITY 0.7
// Forward eccentricity
#define BACKWARD_ECCENTRICITY 0.7
// Distance until which the erosion texture i used
#define MIN_EROSION_DISTANCE 3000.0
#define MAX_EROSION_DISTANCE 100000.0
// Value that is used to normalize the noise textures
#define NOISE_TEXTURE_NORMALIZATION_FACTOR 100000.0f
// Maximal size of a light step
#define LIGHT_STEP_MAXIMAL_SIZE 1000.0f

#define ConvertToPS(x) (x - _PlanetCenterPosition)

/// Common

// Function that takes a clip space positions and converts it to a view direction
float3 GetCloudViewDirWS(float2 positionCS)
{
    float4 viewDirWS = mul(float4(positionCS, 1.0f, 1.0f), _CloudsPixelCoordToViewDirWS[unity_StereoEyeIndex]);
    return -normalize(viewDirWS.xyz);
}

// Fonction that takes a world space position and converts it to a depth value
float ConvertCloudDepth(float3 position)
{
    float4 hClip = TransformWorldToHClip(position);
    return hClip.z / hClip.w;
}

TEXTURE2D_X(_CameraColorTexture);

// Tweak the transmittance to improve situation where the sun is behind the clouds
float EvaluateFinalTransmittance(float2 finalCoord, float transmittance)
{
    #ifdef PERCEPTUAL_TRANSMITTANCE
    // Due to the high intensity of the sun, we often need apply the transmittance in a tonemapped space
    // As we only produce one transmittance, we evaluate the approximation on the luminance of the color
    float luminance = Luminance(_CameraColorTexture[COORD_TEXTURE2D_X(finalCoord.xy)]);
    if (luminance > 0.0f)
    {
        // Apply the transmittance in tonemapped space
        float resultLuminance = FastTonemapPerChannel(luminance) * transmittance;
        resultLuminance = FastTonemapPerChannelInvert(resultLuminance);

        // By softening the transmittance attenuation curve for pixels adjacent to cloud boundaries when the luminance is super high,  
        // We can prevent sun flicker and improve perceptual blending. (https://www.desmos.com/calculator/vmly6erwdo)
        float finalTransmittance = max(resultLuminance / luminance, pow(transmittance, 6));

        // This approach only makes sense if the color is not black
        transmittance = lerp(transmittance, finalTransmittance, _ImprovedTransmittanceBlend);
    }
    #endif

    return saturate(transmittance);
}

/// Tracing

// Cloud description tables
Texture2D<float4> _CloudMapTexture;
SAMPLER(sampler_CloudMapTexture);

Texture2D<float3> _CloudLutTexture;

// Noise textures for adding details
Texture3D<float> _Worley128RGBA;
Texture3D<float> _ErosionNoise;

// Ambient probe. Contains a convolution with Cornette Shank phase function so it needs to sample a different buffer.
StructuredBuffer<float4> _VolumetricCloudsAmbientProbeBuffer;

#ifdef CLOUDS_SIMPLE_PRESET
#define CLOUD_MAP_LUT_PRESET_SIZE 64
groupshared float gs_cloudLutDensity[CLOUD_MAP_LUT_PRESET_SIZE];
groupshared float gs_cloudLutErosion[CLOUD_MAP_LUT_PRESET_SIZE];
groupshared float gs_cloudLutAO[CLOUD_MAP_LUT_PRESET_SIZE];

void LoadCloudLutToLDS(uint groupThreadId)
{
    float3 densityErosionAO = LOAD_TEXTURE2D_LOD(_CloudLutTexture, int2(0, groupThreadId), 0);
    gs_cloudLutDensity[groupThreadId] = densityErosionAO.x;
    gs_cloudLutErosion[groupThreadId] = densityErosionAO.y;
    gs_cloudLutAO[groupThreadId] = densityErosionAO.z;
    GroupMemoryBarrierWithGroupSync();
}

float3 SampleCloudSliceLDS(float height)
{
    float tapCoord = clamp(height * CLOUD_MAP_LUT_PRESET_SIZE, 0, CLOUD_MAP_LUT_PRESET_SIZE - 1);
    float floorTap = floor(tapCoord);
    float ceilTap = ceil(tapCoord);
    float interp = tapCoord - floorTap;
    float3 floorData = float3(gs_cloudLutDensity[floorTap], gs_cloudLutErosion[floorTap], gs_cloudLutAO[floorTap]);
    float3 ceilData = float3(gs_cloudLutDensity[ceilTap], gs_cloudLutErosion[ceilTap], gs_cloudLutAO[ceilTap]);
    return lerp(floorData, ceilData, interp);
}
#endif

// Structure that holds all the lighting data required to light the cloud particles
struct EnvironmentLighting
{
    // Light direction (point to sun)
    float3 sunDirection;
    // Angle between the light and the ray direction
    float cosAngle;
    // Phase functions for the individual
    PHASE_FUNCTION_STRUCTURE phaseFunction;
};

// Structure that holds all the data required for the cloud ray marching
struct CloudRay
{
    // Origin of the ray in camera-relative space
    float3 originWS;
    // Direction of the ray in world space
    float3 direction;
    // Maximal ray length before hitting the far plane or an occluder
    float maxRayLength;
    // Integration Noise
    float integrationNoise;
    // Environement lighting
    EnvironmentLighting envLighting;
};

// Functions that evaluates all the lighting data that will be needed by the cloud ray
EnvironmentLighting EvaluateEnvironmentLighting(CloudRay ray, float3 entryEvaluationPointPS, float3 exitEvaluationPointPS)
{
    // Sun parameters
    EnvironmentLighting lighting;
    lighting.sunDirection = _SunDirection.xyz;

    // Evaluate cos of the theta angle between the view and light vectors
    lighting.cosAngle = dot(ray.direction, lighting.sunDirection);

    // Evaluate the phase function for each of the octaves
    float forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0), lighting.cosAngle);
    float backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0), lighting.cosAngle);
    lighting.phaseFunction[0] = forwardP + backwardsP;

    #if NUM_MULTI_SCATTERING_OCTAVES >= 2
    forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1), lighting.cosAngle);
    backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1), lighting.cosAngle);
    lighting.phaseFunction[1] = forwardP + backwardsP;
    #endif

    #if NUM_MULTI_SCATTERING_OCTAVES >= 3
    forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2), lighting.cosAngle);
    backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2), lighting.cosAngle);
    lighting.phaseFunction[2] = forwardP + backwardsP;
    #endif

    return lighting;
}

// Density remapping function
float DensityRemap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

// Horizon zero dawn technique to darken the clouds
float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

// Structure that describes the ray marching ranges that we should be iterating on
struct RayMarchRange
{
    // The start of the range
    float start;
    // The length of the range
    float end;
};

bool GetCloudVolumeIntersection(CloudRay ray, out RayMarchRange rayMarchRange)
{
    return IntersectCloudVolume(ConvertToPS(ray.originWS), ray.direction, _LowestCloudAltitude, _HighestCloudAltitude,
        rayMarchRange.start, rayMarchRange.end);
}

// Structure that holds all the data used to define the cloud density of a point in space
struct CloudCoverageData
{
    // From a top down view, in what proportions this pixel has clouds
    float coverage;
    // From a top down view, in what proportions this pixel has clouds
    float rainClouds;
    // Value that allows us to request the cloudtype using the density
    float cloudType;
    // Maximal cloud height
    float maxCloudHeight;
};

// Function that returns the normalized height inside the cloud layer
float EvaluateNormalizedCloudHeight(float3 positionPS)
{
    return RangeRemap(_LowestCloudAltitude, _HighestCloudAltitude, length(positionPS));
}

// Animation of the cloud map position
float3 AnimateCloudMapPosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _LargeWindSpeed;
}

// Animation of the cloud shape position
float3 AnimateShapeNoisePosition(float3 positionPS)
{
    // We reduce the top-view repetition of the pattern
    positionPS.y += (positionPS.x / 3.0f + positionPS.z / 7.0f);
    // We add the contribution of the wind displacements
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _MediumWindSpeed + float3(0.0, _VerticalShapeWindDisplacement, 0.0);
}

// Animation of the cloud erosion position
float3 AnimateErosionNoisePosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _SmallWindSpeed + float3(0.0, _VerticalErosionWindDisplacement, 0.0);
}

struct CloudProperties
{
    // Normalized float that tells the "amount" of clouds that is at a given location
    float density;
    // Ambient occlusion for the ambient probe
    float ambientOcclusion;
    // Normalized value that tells us the height within the cloud volume (vertically)
    float height;
    // Extinction over the interval
    float sigmaT;
};

// Function that evaluates the coverage data for a given point in planet space
void GetCloudCoverageData(float3 positionPS, out CloudCoverageData data)
{
    // Convert the position into dome space and center the texture is centered above (0, 0, 0)
    float2 normalizedPosition = AnimateCloudMapPosition(positionPS).xz / _NormalizationFactor * _CloudMapTiling.xy + _CloudMapTiling.zw + 0.5;
    #if defined(CLOUDS_SIMPLE_PRESET)
    float4 cloudMapData =  float4(0.9f, 0.0f, 0.25f, 1.0f);
    #else
    float4 cloudMapData =  SAMPLE_TEXTURE2D_LOD(_CloudMapTexture, sampler_CloudMapTexture, float2(normalizedPosition), 0);
    #endif
    data.coverage = cloudMapData.x;
    data.rainClouds = cloudMapData.y;
    data.cloudType = cloudMapData.z;
    data.maxCloudHeight = cloudMapData.w;
}

// Function that evaluates the cloud properties at a given absolute world space position
void EvaluateCloudProperties(float3 positionPS, float noiseMipOffset, float erosionMipOffset, bool cheapVersion, bool lightSampling,
                            out CloudProperties properties)
{
    // Initliaze all the values to 0 in case
    ZERO_INITIALIZE(CloudProperties, properties);

#ifndef CLOUDS_SIMPLE_PRESET
    // When using a cloud map, we cannot support the full planet due to UV issues
    if (positionPS.y < 0.0f)
        return;
#endif

    // By default the ambient occlusion is 1.0
    properties.ambientOcclusion = 1.0;

    // Evaluate the normalized height of the position within the cloud volume
    properties.height = EvaluateNormalizedCloudHeight(positionPS);

    // When rendering in camera space, we still want horizontal scrolling
    positionPS.xz += _WorldSpaceCameraPos.xz * _CameraSpace;

    // Evaluate the generic sampling coordinates
    float3 baseNoiseSamplingCoordinates = float3(AnimateShapeNoisePosition(positionPS).xzy / NOISE_TEXTURE_NORMALIZATION_FACTOR) * _ShapeScale - float3(_ShapeNoiseOffset.x, _ShapeNoiseOffset.y, _VerticalShapeNoiseOffset);

    // Evaluate the coordinates at which the noise will be sampled and apply wind displacement
    baseNoiseSamplingCoordinates += properties.height * float3(_WindDirection.x, _WindDirection.y, 0.0f) * _AltitudeDistortion;

    // Read the low frequency Perlin-Worley and Worley noises
    float lowFrequencyNoise = SAMPLE_TEXTURE3D_LOD(_Worley128RGBA, s_trilinear_repeat_sampler, baseNoiseSamplingCoordinates.xyz, noiseMipOffset);

    // Evaluate the cloud coverage data for this position
    CloudCoverageData cloudCoverageData;
    GetCloudCoverageData(positionPS, cloudCoverageData);

    // If this region of space has no cloud coverage, exit right away
    if (cloudCoverageData.coverage.x <= CLOUD_DENSITY_TRESHOLD || cloudCoverageData.maxCloudHeight < properties.height)
        return;

    // Read from the LUT
    #if defined(CLOUDS_SIMPLE_PRESET)
    float3 densityErosionAO = SampleCloudSliceLDS(properties.height);
    #else
    float3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudLutTexture, s_linear_clamp_sampler, float2(cloudCoverageData.cloudType, properties.height), CLOUD_LUT_MIP_OFFSET);
    #endif

    // Adjust the shape and erosion factor based on the LUT and the coverage
    float shapeFactor = lerp(0.1, 1.0, _ShapeFactor) * densityErosionAO.y;
    float erosionFactor = _ErosionFactor * densityErosionAO.y;
    #if defined(CLOUDS_MICRO_EROSION)
    float microDetailFactor = _MicroErosionFactor * densityErosionAO.y;
    #endif

    // Combine with the low frequency noise, we want less shaping for large clouds
    lowFrequencyNoise = lerp(1.0, lowFrequencyNoise, shapeFactor);
    float base_cloud = 1.0 - densityErosionAO.x * cloudCoverageData.coverage.x * (1.0 - shapeFactor);
    base_cloud = saturate(DensityRemap(lowFrequencyNoise, base_cloud, 1.0, 0.0, 1.0)) * cloudCoverageData.coverage.x * cloudCoverageData.coverage.x;

    // Weight the ambient occlusion's contribution
    properties.ambientOcclusion = densityErosionAO.z;

    // Change the sigma based on the rain cloud data
    properties.sigmaT = lerp(0.04, 0.12, cloudCoverageData.rainClouds);

    // The ambient occlusion value that is baked is less relevant if there is shaping or erosion, small hack to compensate that
    float ambientOcclusionBlend = saturate(1.0 - max(erosionFactor, shapeFactor) * 0.5);
    properties.ambientOcclusion = lerp(1.0, properties.ambientOcclusion, ambientOcclusionBlend);

    // Apply the erosion for nicer details
    if (!cheapVersion)
    {
        float3 erosionCoords = AnimateErosionNoisePosition(positionPS) / NOISE_TEXTURE_NORMALIZATION_FACTOR * _ErosionScale;
        float erosionNoise = 1.0 - SAMPLE_TEXTURE3D_LOD(_ErosionNoise, s_linear_repeat_sampler, erosionCoords, CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).x;
        erosionNoise = lerp(0.0, erosionNoise, erosionFactor * 0.75f * cloudCoverageData.coverage.x * _ErosionFactorCompensation);
        properties.ambientOcclusion = saturate(properties.ambientOcclusion - sqrt(erosionNoise * _ErosionOcclusion));
        base_cloud = DensityRemap(base_cloud, erosionNoise, 1.0, 0.0, 1.0);

        #if defined(CLOUDS_MICRO_EROSION)
        float3 fineCoords = AnimateErosionNoisePosition(positionPS) / (NOISE_TEXTURE_NORMALIZATION_FACTOR) * _MicroErosionScale;
        float fineNoise = 1.0 - SAMPLE_TEXTURE3D_LOD(_ErosionNoise, s_linear_repeat_sampler, fineCoords, CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).x;
        fineNoise = lerp(0.0, fineNoise, microDetailFactor * 0.5f * cloudCoverageData.coverage.x * _ErosionFactorCompensation);
        base_cloud = DensityRemap(base_cloud, fineNoise, 1.0, 0.0, 1.0);
        #endif
    }

    // Given that we are not sampling the erosion texture, we compensate by substracting an erosion value
    if (lightSampling)
    {
        base_cloud -= erosionFactor * 0.1;
        #if defined(CLOUDS_MICRO_EROSION)
        base_cloud -= microDetailFactor * 0.15;
        #endif
    }

    // Make sure we do not send any negative values
    base_cloud = max(0, base_cloud);

    // Attenuate everything by the density multiplier
    properties.density = base_cloud * _DensityMultiplier;
}

// Structure that holds the result of our volumetric ray
struct VolumetricRayResult
{
    // Amount of lighting that reach the clouds
    // We keep track of sun light and ambient light separately for optimization
    // They are combine at the end of tracing
    float3 scattering;
    float ambient;
    // Transmittance through the clouds
    float transmittance;
    // Mean distance of the clouds
    float meanDistance;
    // Flag that defines if the ray is valid or not
    bool invalidRay;
};

// Function that evaluates the transmittance to the sun at a given cloud position
float3 EvaluateSunTransmittance(float3 positionPS, float3 sunDirection, PHASE_FUNCTION_STRUCTURE phaseFunction)
{
    // Compute the Ray to the limits of the cloud volume in the direction of the light
    float totalLightDistance = 0.0;
    float3 transmittance = 0.0f;

    // If we early out, this means we've hit the earth itself
    if (ExitCloudVolume(positionPS, sunDirection, _HighestCloudAltitude, totalLightDistance))
    {
        // Because of the very limited numebr of light steps and the potential humongous distance to cover, we decide to potnetially cover less and make it more useful
        totalLightDistance = clamp(totalLightDistance, 0, _NumLightSteps * LIGHT_STEP_MAXIMAL_SIZE);

        // Apply a small bias to compensate for the imprecision in the ray-sphere intersection at world scale.
        totalLightDistance += 5.0f;

        // Compute the size of the current step
        float intervalSize = totalLightDistance / (float)_NumLightSteps;
        float opticalDepth = 0;

        // Collect total density along light ray.
        for (int j = 0; j < _NumLightSteps; j++)
        {
            // Here we intentionally do not take the right step size for the first step
            // as it helps with darkening the clouds a bit more than they should at low light samples
            float dist = intervalSize * (0.25 + j);

            // Evaluate the current sample point
            float3 currentSamplePointPS = positionPS + sunDirection * dist;
            // Get the cloud properties at the sample point
            CloudProperties lightRayCloudProperties;
            EvaluateCloudProperties(currentSamplePointPS, 3.0f * j / _NumLightSteps, 0.0, true, true, lightRayCloudProperties);

            opticalDepth += lightRayCloudProperties.density * lightRayCloudProperties.sigmaT;
        }

        // Compute the luminance for each octave
        // https://magnuswrenninge.com/wp-content/uploads/2010/03/Wrenninge-OzTheGreatAndVolumetric.pdf
        float3 extinction = intervalSize * opticalDepth * _ScatteringTint.xyz;
        for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
        {
            float msFactor = PositivePow(_MultiScattering, o);
            transmittance += exp(-extinction * msFactor) * (phaseFunction[o] * msFactor);
        }
    }

    return transmittance;
}

// Evaluates the inscattering from this position
void EvaluateCloud(CloudProperties cloudProperties, EnvironmentLighting envLighting,
                float3 currentPositionPS, float stepSize, float relativeRayDistance,
                inout VolumetricRayResult volumetricRay)
{
    // Apply the extinction
    const float extinction = cloudProperties.density * cloudProperties.sigmaT;
    const float transmittance = exp(-extinction * stepSize);

    // Compute the powder effect
    float powderEffect = PowderEffect(cloudProperties.density, envLighting.cosAngle, _PowderEffectIntensity);

    // Evaluate the sun visibility
    float3 sunTransmittance = EvaluateSunTransmittance(currentPositionPS, envLighting.sunDirection, envLighting.phaseFunction);

    // Compute luminance separately to factor out color multiplication at the end of the loop
    // Use 1 as placeholder to compute the 'transfer function'
    float3 sunLuminance = 1.0f * sunTransmittance * powderEffect;
    float ambientLuminance = 1.0f * cloudProperties.ambientOcclusion;

    // "Energy-conserving analytical integration"
    // See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
    // No division by clamped extinction because albedo == 1 => sigma_s == sigma_e so it simplifies
    // Note: this is not true anymore when _ScatteringTint is modified, but it still looks correct
    volumetricRay.scattering += sunLuminance     * (volumetricRay.transmittance - volumetricRay.transmittance * transmittance);
    volumetricRay.ambient    += ambientLuminance * (volumetricRay.transmittance - volumetricRay.transmittance * transmittance);
    volumetricRay.transmittance *= transmittance;
}

// Global attenuation of the density based on the camera distance
float DensityFadeValue(float distanceToCamera)
{
    return saturate((distanceToCamera - _FadeInStart) / (_FadeInStart + _FadeInDistance));
}

// Evaluate the erosion mip offset based on the camera distance
float ErosionMipOffset(float distanceToCamera)
{
    return lerp(0.0, 4.0, saturate((distanceToCamera - MIN_EROSION_DISTANCE) / (MAX_EROSION_DISTANCE - MIN_EROSION_DISTANCE)));
}

VolumetricRayResult TraceVolumetricRay(CloudRay cloudRay)
{
    // Initiliaze the volumetric ray
    VolumetricRayResult volumetricRay;
    volumetricRay.scattering = 0.0;
    volumetricRay.ambient = 0.0;
    volumetricRay.transmittance = 1.0;
    volumetricRay.meanDistance = FLT_MAX;
    volumetricRay.invalidRay = true;

    // Determine if ray intersects bounding volume, if the ray does not intersect the cloud volume AABB, skip right away
    RayMarchRange rayMarchRange;
    if (GetCloudVolumeIntersection(cloudRay, rayMarchRange))
    {
        if (cloudRay.maxRayLength >= rayMarchRange.start)
        {
            // Initialize the depth for accumulation
            volumetricRay.meanDistance = 0.0;

            // Total distance that the ray must travel including empty spaces
            // Clamp the travel distance to whatever is closer
            // - Sky Occluder
            // - Volume end
            // - Far plane
            float totalDistance = min(rayMarchRange.end, cloudRay.maxRayLength) - rayMarchRange.start;

            // Evaluate our integration step
            float stepS = min(totalDistance / (float)_NumPrimarySteps, _MaxStepSize);
            totalDistance = stepS * _NumPrimarySteps;

            // Compute the environment lighting that is going to be used for the cloud evaluation
            float3 rayMarchStartPS = ConvertToPS(cloudRay.originWS) + rayMarchRange.start * cloudRay.direction;
            float3 rayMarchEndPS = rayMarchStartPS + totalDistance * cloudRay.direction;
            cloudRay.envLighting = EvaluateEnvironmentLighting(cloudRay, rayMarchStartPS, rayMarchEndPS);

            // Tracking the number of steps that have been made
            int currentIndex = 0;

            // Normalization value of the depth
            float meanDistanceDivider = 0.0f;

            // Current position for the evaluation, apply blue noise to start position
            float currentDistance = cloudRay.integrationNoise * stepS;
            float3 currentPositionWS = cloudRay.originWS + (rayMarchRange.start + currentDistance) * cloudRay.direction;

            // Initialize the values for the optimized ray marching
            bool activeSampling = true;
            int sequentialEmptySamples = 0;

            // Do the ray march for every step that we can.
            while (currentIndex < _NumPrimarySteps && currentDistance < totalDistance)
            {
                // Compute the camera-distance based attenuation
                float densityAttenuationValue = DensityFadeValue(rayMarchRange.start + currentDistance);
                // Compute the mip offset for the erosion texture
                float erosionMipOffset = ErosionMipOffset(rayMarchRange.start + currentDistance);

                // Accumulate in WS and convert at each iteration to avoid precision issues
                float3 currentPositionPS = ConvertToPS(currentPositionWS);

                // Should we be evaluating the clouds or just doing the large ray marching
                if (activeSampling)
                {
                    // If the density is null, we can skip as there will be no contribution
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionPS, 0.0f, erosionMipOffset, false, false, cloudProperties);

                    // Apply the fade in function to the density
                    cloudProperties.density *= densityAttenuationValue;

                    if (cloudProperties.density > CLOUD_DENSITY_TRESHOLD)
                    {
                        // Contribute to the average depth (must be done first in case we end up inside a cloud at the next step)
                        // page 43: https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf
                        volumetricRay.meanDistance += (rayMarchRange.start + currentDistance) * volumetricRay.transmittance;
                        meanDistanceDivider += volumetricRay.transmittance;

                        // Evaluate the cloud at the position
                        EvaluateCloud(cloudProperties, cloudRay.envLighting, currentPositionPS, stepS, currentDistance / totalDistance, volumetricRay);

                        // if most of the energy is absorbed, just leave.
                        if (volumetricRay.transmittance < 0.003)
                        {
                            volumetricRay.transmittance = 0.0;
                            break;
                        }

                        // Reset the empty sample counter
                        sequentialEmptySamples = 0;
                    }
                    else
                        sequentialEmptySamples++;

                    // If it has been more than EMPTY_STEPS_BEFORE_LARGE_STEPS, disable active sampling and start large steps
                    if (sequentialEmptySamples == EMPTY_STEPS_BEFORE_LARGE_STEPS)
                        activeSampling = false;

                    // Do the next step
                    currentPositionWS += cloudRay.direction * stepS;
                    currentDistance += stepS;
                }
                else
                {
                    // Sample the cheap version of the clouds
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionPS, 1.0f, 0.0, true, false, cloudProperties);

                    // Apply the fade in function to the density
                    cloudProperties.density *= densityAttenuationValue;

                    // If the density is lower than our tolerance,
                    if (cloudProperties.density < CLOUD_DENSITY_TRESHOLD)
                    {
                        currentPositionWS += cloudRay.direction * stepS * 2.0f;
                        currentDistance += stepS * 2.0f;
                    }
                    else
                    {
                        // Somewhere between this step and the previous clouds started
                        // We reset all the counters and enable active sampling
                        currentPositionWS -= cloudRay.direction * stepS;
                        currentDistance -= stepS;
                        currentIndex -= 1;
                        activeSampling = true;
                        sequentialEmptySamples = 0;
                    }
                }

                currentIndex++;
            }

            // Normalized the depth we computed
            if (volumetricRay.meanDistance != 0.0)
            {
                volumetricRay.invalidRay = false;
                volumetricRay.meanDistance /= meanDistanceDivider;
                volumetricRay.meanDistance = min(volumetricRay.meanDistance, cloudRay.maxRayLength);

                float3 currentPositionPS = ConvertToPS(cloudRay.originWS) + volumetricRay.meanDistance * cloudRay.direction;
                float relativeHeight = EvaluateNormalizedCloudHeight(currentPositionPS);

                float3 sunColor = _SunLightColor.xyz;
                #ifdef PHYSICALLY_BASED_SUN
                sunColor *= EvaluateSunColorAttenuation(currentPositionPS, cloudRay.envLighting.sunDirection, true);
                #endif

                float3 ambientTermTop = SampleSH9(_VolumetricCloudsAmbientProbeBuffer, float3(0, 1, 0));
                float3 ambientTermBottom = SampleSH9(_VolumetricCloudsAmbientProbeBuffer, float3(0, -1, 0));
                float3 ambient = max(0, lerp(ambientTermBottom, ambientTermTop, relativeHeight));

                volumetricRay.scattering = sunColor * volumetricRay.scattering;
                volumetricRay.scattering += ambient * volumetricRay.ambient;
                volumetricRay.scattering *= GetCurrentExposureMultiplier();
            }
        }
    }

    // return the final ray result
    return volumetricRay;
}

#endif // VOLUMETRIC_CLOUD_UTILITIES_H
