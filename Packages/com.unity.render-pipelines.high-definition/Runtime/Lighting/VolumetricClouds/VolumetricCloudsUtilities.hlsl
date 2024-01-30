#ifndef VOLUMETRIC_CLOUD_UTILITIES_H
#define VOLUMETRIC_CLOUD_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VolumetricCloudsDef.cs.hlsl"

// The number of octaves for the multi-scattering
#define NUM_MULTI_SCATTERING_OCTAVES 2
#define PHASE_FUNCTION_STRUCTURE float2
// Global offset to the high frequency noise
#define CLOUD_DETAIL_MIP_OFFSET 0.0
// Global offset for reaching the LUT/AO
#define CLOUD_LUT_MIP_OFFSET 1.0
// Density blow wich we consider the density is zero (optimization reasons)
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
// Maximal distance until which the "skybox"
#define MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE 200000.0f
// Maximal size of a light step
#define LIGHT_STEP_MAXIMAL_SIZE 1000.0f

// Just define a flag when the other is not defined as it is easier for the logic
#if !defined(LOCAL_VOLUMETRIC_CLOUDS)
    #define DISTANT_VOLUMETRIC_CLOUDS
#endif

// Cloud description tables
Texture2D<float4> _CloudMapTexture;
Texture2D<float3> _CloudLutTexture;

// Noise textures for adding details
Texture3D<float> _Worley128RGBA;
Texture3D<float> _ErosionNoise;

// Ambient probe. Contains a convolution with Cornette Shank phase function so it needs to sample a different buffer.
StructuredBuffer<float4> _VolumetricCloudsAmbientProbeBuffer;

// Function that interects a ray with a sphere (optimized for very large sphere), returns up to two positives distances.
int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b*b) - 4.0*a*c;
    result = 0.0;
    int numSolutions = 0;
    if (d >= 0.0)
    {
        // Compute the values required for the solution eval
        float sqrtD = sqrt(d);
        float q = -0.5*(b + FastSign(b) * sqrtD);
        result = float2(c/q, q/a);
        // Remove the solutions we do not want
        numSolutions = 2;
        if (result.x < 0.0)
        {
            numSolutions--;
            result.x = result.y;
        }
        if (result.y < 0.0)
            numSolutions--;
    }
    // Return the number of solutions
    return numSolutions;
}

// Function that interects a ray with a sphere (optimized for very large sphere), and says if there is at least one intersection
bool RaySphereIntersection(float3 startWS, float3 dir, float radius)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;
    bool flag = false;
    if (d >= 0.0)
    {
        // Compute the values required for the solution eval
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        float2 result = float2(c/q, q/a);
        flag = result.x > 0.0 || result.y > 0.0;
    }
    return flag;
}

// Function that intersects a ray with a plane and returns a flag and the intersection point
bool IntersectPlane(float3 ray_originWS, float3 ray_dir, float3 pos, float3 normal, out float t)
{
    float3 ray_originPS = ray_originWS + float3(0, _EarthRadius, 0);
    float denom = dot(normal, ray_dir);
    bool flag = false;
    t = -1.0f;
    if (abs(denom) > 1e-6)
    {
        float3 d = pos - ray_originPS;
        t = dot(d, normal) / denom;
        flag = (t >= 0);
    }
    return flag;
}

// Structure that holds all the lighting data required to light the cloud particles
struct EnvironmentLighting
{
    // Light direction (point to sun)
    float3 sunDirection;

    // Light intensity/color of the sun, this already takes into account the atmospheric scattering
    float3 sunColor0;
    float3 sunColor1;

    // Ambient term from the ambient probe
    float3 ambientTermTop;
    float3 ambientTermBottom;

    // Angle between the light and the ray direction
    float cosAngle;

    // Phase functions for the individual
    PHASE_FUNCTION_STRUCTURE phaseFunction;
};

// This functions evaluates the sun color attenuation at a given point (if the physicaly based sky is active)
void EvaluateSunColorAttenuation(float3 evaluationPointWS, float3 sunDirection, inout float3 sunColor)
{
#ifdef PHYSICALLY_BASED_SUN
    if(_PhysicallyBasedSun == 1)
    // TODO: move this into a shared function
    {
        float3 X = evaluationPointWS;
        float3 C = _PlanetCenterPosition.xyz;

        float r        = distance(X, C);
        float cosHoriz = ComputeCosineOfHorizonAngle(r);
        float cosTheta = dot(X - C, sunDirection) * rcp(r); // Normalize

        if (cosTheta >= cosHoriz) // Above horizon
        {
            float3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
            // Cannot do this once for both the sky and the fog because the sky may be desaturated. :-(
            float3 transm  = TransmittanceFromOpticalDepth(oDepth);
            float3 opacity = 1 - transm;
            sunColor *= 1 - (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
        }
        else
        {
            // return 0; // Kill the light. This generates a warning, so can't early out. :-(
           sunColor = 0;
        }
    }
#endif
}

// Structure that holds all the data required for the cloud ray marching
struct CloudRay
{
    // Depth value of the pixel
    float depthValue;
    // Origin of the ray in world space
    float3 originWS;
    // Direction of the ray in world space
    float3 direction;
    // Maximal ray length before hitting the far plane or an occluder
    float maxRayLength;
    // Flag to track if we are inside the cloud layers
    float insideClouds;
    // Distance to earth center
    float toEarthCenter;
    // Integration Noise
    float integrationNoise;
    // Environement lighting
    EnvironmentLighting envLighting;
};

// Phase term function
float HenyeyGreenstein(float cosAngle, float g)
{
    // There is a mistake in the GPU Gem7 Paper, the result should be divided by 1/(4.PI)
    float g2 = g * g;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / PositivePow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
}

// Functions that evaluates all the lighting data that will be needed by the cloud ray
EnvironmentLighting EvaluateEnvironmentLighting(CloudRay ray, float3 entryEvaluationPointWS, float3 exitEvaluationPointWS)
{
    // Sun parameters
    EnvironmentLighting lighting;
    lighting.sunDirection = _SunDirection.xyz;
    lighting.sunColor0 = _SunLightColor.xyz * GetCurrentExposureMultiplier();
    lighting.sunColor1 = lighting.sunColor0;
    lighting.ambientTermTop = SampleSH9(_VolumetricCloudsAmbientProbeBuffer, float3(0, 1, 0)) * GetCurrentExposureMultiplier();
    lighting.ambientTermBottom = max(SampleSH9(_VolumetricCloudsAmbientProbeBuffer, float3(0, -1, 0)), 0) * GetCurrentExposureMultiplier();

    // evaluate the attenuation at both points (entrance and exit of the cloud layer)
    EvaluateSunColorAttenuation(entryEvaluationPointWS, lighting.sunDirection, lighting.sunColor0);
    EvaluateSunColorAttenuation(exitEvaluationPointWS, lighting.sunDirection, lighting.sunColor1);

    // Evaluate cos of the theta angle between the view and light vectors
    lighting.cosAngle = dot(ray.direction, lighting.sunDirection);

    // Evaluate the phase function for each of the octaves
    float forwardP = HenyeyGreenstein(lighting.cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0));
    float backwardsP = HenyeyGreenstein(lighting.cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0));
    lighting.phaseFunction[0] = forwardP + backwardsP;

    #if NUM_MULTI_SCATTERING_OCTAVES >= 2
    forwardP = HenyeyGreenstein(lighting.cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1));
    backwardsP = HenyeyGreenstein(lighting.cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1));
    lighting.phaseFunction[1] = forwardP + backwardsP;
    #endif

    #if NUM_MULTI_SCATTERING_OCTAVES >= 3
    forwardP = HenyeyGreenstein(lighting.cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2));
    backwardsP = HenyeyGreenstein(lighting.cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2));
    lighting.phaseFunction[2] = forwardP + backwardsP;
    #endif

    return lighting;
}

// Function that evaluates the sun color along the ray
float3 EvaluateSunColor(EnvironmentLighting envLighting, float relativeRayDistance)
{
    return lerp(envLighting.sunColor0, envLighting.sunColor1, relativeRayDistance);
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

// Function that takes a clip space positions and converts it to a view direction
float3 GetCloudViewDirWS(float2 positionCS)
{
    float4 viewDirWS = mul(float4(positionCS, 1.0f, 1.0f), _CloudsPixelCoordToViewDirWS);
    return -normalize(viewDirWS.xyz);
}

// Fonction that takes a world space position and converts it to a depth value
float ConvertCloudDepth(float3 position)
{
    float4 hClip = TransformWorldToHClip(position);
    return hClip.z / hClip.w;
}

// Function that converts an oblique depth to a non oblique one (for planar reflection probes)
float ConvertObliqueDepthToNonOblique(int2 currentCoord, float obliqueDepth)
{
    // Compute the world position of the tapped pixel
    // Note: the view matrix here is not really used, but a valid matrix needs to be passed to this function.
    PositionInputs centralPosInput = GetPositionInput(currentCoord, _FinalScreenSize.zw, obliqueDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

    // For some reason, with oblique matrices, when the point is on the background the reconstructed position ends up behind the camera and at the wrong position
    float3 rayDirection = normalize(-centralPosInput.positionWS);
    rayDirection = obliqueDepth == 0.0 ? -rayDirection : rayDirection;

    // Adjust the position
    centralPosInput.positionWS = obliqueDepth == 0.0 ? rayDirection * _ProjectionParams.z : centralPosInput.positionWS;

    // Re-do the projection, but this time without the oblique part and export it
    float4 hClip = mul(_CameraViewProjection_NO, float4(centralPosInput.positionWS, 1.0));

    // Divide by the homogenous coordinate
    return saturate(hClip.z / hClip.w);
}

// Structure that describes the ray marching ranges that we should be iterating on
struct RayMarchRange
{
    // The start of the range
    float start;
    // The length of the range
    float distance;
};

bool GetCloudVolumeIntersection(float3 originWS, float3 dir, float insideClouds, float toEarthCenter, out RayMarchRange rayMarchRange)
#ifdef LOCAL_VOLUMETRIC_CLOUDS
{
    ZERO_INITIALIZE(RayMarchRange, rayMarchRange);

    // intersect with all three spheres
    float2 intersectionInter, intersectionOuter;
    int numInterInner = RaySphereIntersection(originWS, dir, _LowestCloudAltitude + _EarthRadius, intersectionInter);
    int numInterOuter = RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersectionOuter);
    bool intersectEarth = RaySphereIntersection(originWS, dir, insideClouds < -1.5 ? toEarthCenter : _EarthRadius);

    // Did we achieve any intersection ?
    bool intersect = numInterInner > 0 || numInterOuter > 0;

    // If we are inside the lower cloud bound
    if (insideClouds < -0.5)
    {
        // The ray starts at the first intersection with the lower bound and goes up to the first intersection with the outer bound
        rayMarchRange.start = intersectionInter.x;
        rayMarchRange.distance = intersectionOuter.x - intersectionInter.x;
    }
    else if (insideClouds == 0.0)
    {
        // If we are inside, the ray always starts at 0
        rayMarchRange.start = 0;

        // if we intersect the earth, this means the ray has only one range
        if (intersectEarth)
            rayMarchRange.distance = intersectionInter.x;
        // if we do not untersect the earth and the lower bound. This means the ray exits to outer space
        else if(numInterInner == 0)
            rayMarchRange.distance = intersectionOuter.x;
        // If we do not intersect the earth, but we do intersect the lower bound, we have two ranges.
        else
            rayMarchRange.distance = intersectionInter.x;
    }
    // We are in outer space
    else
    {
        // We always start from our intersection with the outer bound
        rayMarchRange.start = intersectionOuter.x;

        // If we intersect the earth, ony one range
        if(intersectEarth)
            rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
        else
        {
            // If we do not intersection the lower bound, the ray exits from the upper bound
            if(numInterInner == 0)
                rayMarchRange.distance = intersectionOuter.y - intersectionOuter.x;
            else
                rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
        }
    }
    // Mke sure we cannot go beyond what the number of samples
    rayMarchRange.distance = clamp(0, rayMarchRange.distance, _MaxRayMarchingDistance);

    // Return if we have an intersection
    return intersect;
}
#else
{
    ZERO_INITIALIZE(RayMarchRange, rayMarchRange);

    // intersect with all three spheres
    float2 intersectionInter, intersectionOuter;
    int numInterInner = RaySphereIntersection(originWS, dir, _LowestCloudAltitude + _EarthRadius, intersectionInter);
    int numInterOuter = RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersectionOuter);

    // The ray starts at the first intersection with the lower bound and goes up to the first intersection with the outer bound
    rayMarchRange.start = intersectionInter.x;
    rayMarchRange.distance = intersectionOuter.x - intersectionInter.x;

    // Return if we have an intersection
    return true;
}
#endif

// Structure that holds all the data used to define the cloud density of a point in space
struct CloudCoverageData
{
    // From a top down view, in what proportions this pixel has clouds
    float2 coverage;
    // From a top down view, in what proportions this pixel has clouds
    float rainClouds;
    // Value that allows us to request the cloudtype using the density
    float cloudType;
    // Maximal cloud height
    float maxCloudHeight;
};

// Function that returns if a given point in planet space position in inside or outside the cloud volume
bool PointInsideCloudVolume(float3 positionPS)
{
    float toEarthCenter2 = dot(positionPS, positionPS);
    return toEarthCenter2 < _CloudRangeSquared.y && toEarthCenter2 > _CloudRangeSquared.x;
}

// Function that returns the normalized height inside the cloud layer
float EvaluateNormalizedCloudHeight(float3 positionPS)
{
    return (length(positionPS) - (_LowestCloudAltitude + _EarthRadius)) / ((_HighestCloudAltitude + _EarthRadius) - (_LowestCloudAltitude + _EarthRadius));
}

// Animation of the cloud map position
float3 AnimateCloudMapPosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _LargeWindSpeed;
}

// Animation of the cloud shape position
float3 AnimateBaseNoisePosition(float3 positionPS)
{
    // We reduce the top-view repetition of the pattern
    positionPS.y += (positionPS.x / 3.0f + positionPS.z / 7.0f);
    // We add the contribution of the wind displacements
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _MediumWindSpeed + float3(0.0, _VerticalShapeWindDisplacement, 0.0);
}

// Animation of the cloud erosion position
float3 AnimateFineNoisePosition(float3 positionPS)
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
    // Transmittance of the cloud
    float sigmaT;
};

// Function that evaluates the coverage data for a given point in planet space
void GetCloudCoverageData(float3 positionPS, out CloudCoverageData data)
{
    // Convert the position into dome space and center the texture is centered above (0, 0, 0)
    float2 normalizedPosition = AnimateCloudMapPosition(positionPS).xz / _NormalizationFactor * _CloudMapTiling.xy + _CloudMapTiling.zw - 0.5;
    // Read the data from the texture
    float4 cloudMapData =  SAMPLE_TEXTURE2D_LOD(_CloudMapTexture, s_linear_repeat_sampler, float2(normalizedPosition), 0);
    data.coverage = float2(cloudMapData.x, cloudMapData.x * cloudMapData.x);
    data.rainClouds = cloudMapData.y;
    data.cloudType = cloudMapData.z;
    data.maxCloudHeight = cloudMapData.w;
}

// Function that evaluates the cloud properties at a given absolute world space position
void EvaluateCloudProperties(float3 positionWS, float noiseMipOffset, float erosionMipOffset, bool cheapVersion, bool lightSampling,
                            out CloudProperties properties)
{
    // Convert to planet space
    float3 positionPS = positionWS + float3(0, _EarthRadius, 0);

    // Initliaze all the values to 0 in case
    ZERO_INITIALIZE(CloudProperties, properties);

    // By default the ambient occlusion is 1.0
    properties.ambientOcclusion = 1.0;

    // If the next sampling point is not inside the coud volume the density
    if (!PointInsideCloudVolume(positionPS) || positionPS.y < 0.0f)
        return;

    // Compute the normalized position for the three channels
    float3 normalizedPos = positionPS / _NormalizationFactor;

    // Evaluate the normalized height of the position within the cloud volume
    properties.height = EvaluateNormalizedCloudHeight(positionPS);

    // Evaluate the generic sampling coordinates
    float3 baseNoiseSamplingCoordinates = float3(AnimateBaseNoisePosition(positionPS).xzy / NOISE_TEXTURE_NORMALIZATION_FACTOR) * _ShapeScale - float3(_ShapeNoiseOffset.x, _ShapeNoiseOffset.y, _VerticalShapeNoiseOffset);

    // Evaluate the coordinates at which the noise will be sampled and apply wind displacement
    baseNoiseSamplingCoordinates += properties.height * float3(_WindDirection.x, _WindDirection.y, 0.0f) * _AltitudeDistortion;

    // Read the low frequency Perlin-Worley and Worley noises
    float lowFrequencyNoise = SAMPLE_TEXTURE3D_LOD(_Worley128RGBA, s_trilinear_repeat_sampler, baseNoiseSamplingCoordinates.xyz, noiseMipOffset);

    // Initiliaze the erosion and shape factors (that will be overriden)
    float shapeFactor = lerp(0.1, 1.0, _ShapeFactor);
    float erosionFactor = _ErosionFactor;

    // Evaluate the cloud coverage data for this position
    CloudCoverageData cloudCoverageData;
    GetCloudCoverageData(positionPS, cloudCoverageData);

    // If this region of space has no cloud coverage, exit right away
    if (cloudCoverageData.coverage.x <= CLOUD_DENSITY_TRESHOLD || cloudCoverageData.maxCloudHeight < properties.height)
        return;

    // Read from the LUT
    float3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudLutTexture, s_linear_clamp_sampler, float2(cloudCoverageData.cloudType, properties.height), CLOUD_LUT_MIP_OFFSET);

    // Adjust the shape and erosion factor based on the LUT and the coverage
    shapeFactor = shapeFactor * densityErosionAO.y;
    erosionFactor = erosionFactor * densityErosionAO.y;

    // Combine with the low frequency noise, we want less shaping for large clouds
    lowFrequencyNoise = lerp(1.0, lowFrequencyNoise, shapeFactor);
    float base_cloud = 1.0 - densityErosionAO.x * cloudCoverageData.coverage.x * (1.0 - shapeFactor);
    base_cloud = saturate(DensityRemap(lowFrequencyNoise, base_cloud, 1.0, 0.0, 1.0)) * cloudCoverageData.coverage.y;

    // Weight the ambient occlusion's contribution
    properties.ambientOcclusion = densityErosionAO.z;

    // Change the sigma based on the rain cloud data
    properties.sigmaT = lerp(0.04, 0.12, cloudCoverageData.rainClouds);

    // The ambient occlusion value that is baked is less relevant if there is shaping or erosion, small hack to compensate that
    float ambientOcclusionBlend = saturate(1.0 - max(erosionFactor, shapeFactor) * 0.5);
    properties.ambientOcclusion = lerp(1.0, properties.ambientOcclusion, ambientOcclusionBlend);

    // Apply the erosion for nifer details
    if (!cheapVersion)
    {
        float3 fineNoiseSamplingCoordinates = AnimateFineNoisePosition(positionPS) / NOISE_TEXTURE_NORMALIZATION_FACTOR * _ErosionScale;
        float highFrequencyNoise = 1.0 - SAMPLE_TEXTURE3D_LOD(_ErosionNoise, s_linear_repeat_sampler, fineNoiseSamplingCoordinates, CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).x;
        // Compute the weight of the low frequency noise
        highFrequencyNoise = lerp(0.0, highFrequencyNoise, erosionFactor * 0.75f * cloudCoverageData.coverage.x * _ErosionFactorCompensation);
        base_cloud = DensityRemap(base_cloud, highFrequencyNoise, 1.0, 0.0, 1.0);
        properties.ambientOcclusion = saturate(properties.ambientOcclusion - sqrt(highFrequencyNoise * _ErosionOcclusion));
    }

    // Given that we are not sampling the erosion texture, we compensate by substracting an erosion value
    if (lightSampling)
        base_cloud -= erosionFactor * 0.1;

    // Make sure we do not send any negative values
    base_cloud = max(0, base_cloud);

    // Attenuate everything by the density multiplier
    properties.density = base_cloud * _DensityMultiplier;
}

// Structure that holds the result of our volumetric ray
struct VolumetricRayResult
{
    // Amount of lighting that comes from the clouds
    float3 inScattering;
    // Transmittance through the clouds
    float transmittance;
    // Mean distance of the clouds
    float meanDistance;
    // Flag that defines if the ray is valid or not
    bool invalidRay;
};

// Function that intersects a ray in absolute world space, the ray is guaranteed to start inside the volume
bool GetCloudVolumeIntersection_Light(float3 originWS, float3 dir, out float totalDistance)
{
    // Given that this is a light ray, it will always start from inside the volume and is guaranteed to exit
    float2 intersection, intersectionEarth;
    RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersection);
    bool intersectEarth = RaySphereIntersection(originWS, dir, _EarthRadius);
    totalDistance = intersection.x;
    // If the ray intersects the earth, then the sun is occlued by the earth
    return !intersectEarth;
}

// Function that evaluates the luminance at a given cloud position (only the contribution of the sun)
float3 EvaluateSunLuminance(float3 positionWS, float3 sunDirection, float3 sunColor, float powderEffect, PHASE_FUNCTION_STRUCTURE phaseFunction)
{
    // Compute the Ray to the limits of the cloud volume in the direction of the light
    float totalLightDistance = 0.0;
    float3 luminance = float3(0.0, 0.0, 0.0);

    // If we early out, this means we've hit the earth itself
    if (GetCloudVolumeIntersection_Light(positionWS, sunDirection, totalLightDistance))
    {
        // Because of the very limited numebr of light steps and the potential humongous distance to cover, we decide to potnetially cover less and make it more useful
        totalLightDistance = clamp(totalLightDistance, 0, _NumLightSteps * LIGHT_STEP_MAXIMAL_SIZE);

        // Apply a small bias to compensate for the imprecision in the ray-sphere intersection at world scale.
        totalLightDistance += 5.0f;

        // Compute the size of the current step
        float intervalSize = totalLightDistance / (float)_NumLightSteps;

        // Sums the ex
        float extinctionSum = 0;

        // Collect total density along light ray.
        float lastDist = 0;
        for (int j = 0; j < _NumLightSteps; j++)
        {
            // Here we intentionally do not take the right step size for the first step
            // as it helps with darkening the clouds a bit more than they should at low light samples
            float dist = intervalSize * (0.25 + j);

            // Evaluate the current sample point
            float3 currentSamplePointWS = positionWS + sunDirection * dist;
            // Get the cloud properties at the sample point
            CloudProperties lightRayCloudProperties;
            EvaluateCloudProperties(currentSamplePointWS, 3.0f * j / _NumLightSteps, 0.0, true, true, lightRayCloudProperties);

            // Normally we would evaluate the transmittance at each step and multiply them
            // but given the fact that exp exp (extinctionA) * exp(extinctionB) = exp(extinctionA + extinctionB)
            // We can sum the extinctions and do the extinction only once
            extinctionSum += max(lightRayCloudProperties.density * lightRayCloudProperties.sigmaT, 1e-6);

            // Move on to the next step
            lastDist = dist;
        }

        // Compute the luminance for each octave
        float3 sunColorXPowderEffect = sunColor * powderEffect;
        float3 extinction = intervalSize * _ScatteringTint.xyz * extinctionSum;
        for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
        {
            float msFactor = PositivePow(_MultiScattering, o);
            float3 tranmittance = exp(-extinction * msFactor);
            luminance += tranmittance * sunColorXPowderEffect * phaseFunction[o] * msFactor;
        }
    }

    // return the combined luminance
    return luminance;
}

// Evaluates the inscattering from this position
void EvaluateCloud(CloudProperties cloudProperties, EnvironmentLighting envLighting,
                float3 currentPositionWS, float stepSize, float relativeRayDistance,
                inout VolumetricRayResult volumetricRay)
{
    // Apply the extinction
    const float extinction = cloudProperties.density * cloudProperties.sigmaT;
    const float transmittance = exp(-extinction * stepSize);

    // Compute the powder effect
    float powder_effect = PowderEffect(cloudProperties.density, envLighting.cosAngle, _PowderEffectIntensity);

    // Evaluate the sun color at the position
    float3 sunColor = EvaluateSunColor(envLighting, relativeRayDistance);

    // Evaluate the sun's luminance
    float3 totalLuminance = EvaluateSunLuminance(currentPositionWS, envLighting.sunDirection, sunColor, powder_effect, envLighting.phaseFunction);

    // Add the environement lighting contribution
    totalLuminance += lerp(envLighting.ambientTermBottom, envLighting.ambientTermTop, cloudProperties.height) * cloudProperties.ambientOcclusion;

    // Note: This is an alterated version of the  "Energy-conserving analytical integration"
    // For some reason the divison by the clamped extinction just makes it all wrong.
    const float3 integScatt = (totalLuminance - totalLuminance * transmittance);
    volumetricRay.inScattering += integScatt * volumetricRay.transmittance;
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
    volumetricRay.inScattering = 0.0;
    volumetricRay.transmittance = 1.0;
    volumetricRay.meanDistance = _MaxCloudDistance;
    volumetricRay.invalidRay = true;

    // Determine if ray intersects bounding volume, if the ray does not intersect the cloud volume AABB, skip right away
    RayMarchRange rayMarchRange;
    if (GetCloudVolumeIntersection(cloudRay.originWS, cloudRay.direction, cloudRay.insideClouds, cloudRay.toEarthCenter, rayMarchRange))
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
            float totalDistance = min(rayMarchRange.distance, cloudRay.maxRayLength - rayMarchRange.start);

            // Compute the environment lighting that is going to be used for the cloud evaluation
            float3 rayMarchStartPos = cloudRay.originWS + rayMarchRange.start * cloudRay.direction;
            float3 rayMarchEndPos = rayMarchStartPos + totalDistance * cloudRay.direction;
            cloudRay.envLighting = EvaluateEnvironmentLighting(cloudRay, rayMarchStartPos, rayMarchEndPos);

            // Evaluate our integration step
            float stepS = totalDistance / (float)_NumPrimarySteps;

            // Tracking the number of steps that have been made
            int currentIndex = 0;

            // Normalization value of the depth
            float meanDistanceDivider = 0.0f;

            // Current position for the evaluation
            float3 currentPositionWS = cloudRay.originWS + rayMarchRange.start * cloudRay.direction;

            // Current Distance that has been marched
            float currentDistance = 0;

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

                // Should we be evaluating the clouds or just doing the large ray marching
                if (activeSampling)
                {
                    // If the density is null, we can skip as there will be no contribution
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionWS, 0.0f, erosionMipOffset, false, false, cloudProperties);

                    // Apply the fade in function to the density
                    cloudProperties.density *= densityAttenuationValue;

                    if (cloudProperties.density > CLOUD_DENSITY_TRESHOLD)
                    {
                        // Contribute to the average depth (must be done first in case we end up inside a cloud at the next step)
                        float transmitanceXdensity = volumetricRay.transmittance * cloudProperties.density;
                        volumetricRay.meanDistance += (rayMarchRange.start + currentDistance) * transmitanceXdensity;
                        meanDistanceDivider += transmitanceXdensity;

                        // Evaluate the cloud at the position
                        EvaluateCloud(cloudProperties, cloudRay.envLighting, currentPositionWS, stepS, currentDistance / totalDistance, volumetricRay);

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
                    float relativeStepSize = lerp(cloudRay.integrationNoise, 1.0, saturate(currentIndex));
                    currentPositionWS += cloudRay.direction * stepS * relativeStepSize;
                    currentDistance += stepS * relativeStepSize;
                }
                else
                {
                    // Sample the cheap version of the clouds
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionWS, 1.0f, 0.0, true, false, cloudProperties);

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
                        activeSampling = true;
                        sequentialEmptySamples = 0;
                    }
                }

                currentIndex++;
            }

            // Normalized the depth we computed
            if (volumetricRay.meanDistance == 0.0)
                volumetricRay.invalidRay = true;
            else
            {
                volumetricRay.meanDistance /= meanDistanceDivider;
                volumetricRay.invalidRay = false;
            }
        }
    }

    // return the final ray result
    return volumetricRay;
}

// This function compute the checkerboard undersampling position
int ComputeCheckerBoardIndex(int2 traceCoord, int subPixelIndex)
{
    int localOffset = (traceCoord.x & 1 + traceCoord.y & 1) & 1;
    int checkerBoardLocation = (subPixelIndex + localOffset) & 0x3;
    return checkerBoardLocation;
}

float EvaluateFinalTransmittance(float3 color, float transmittance)
{
    // Due to the high intensity of the sun, we often need apply the transmittance in a tonemapped space
    // As we only produce one transmittance, we evaluate the approximation on the luminance of the color
    float luminance = Luminance(color);

    // Apply the tone mapping and then the transmittance
    float resultLuminance = luminance / (1.0 + luminance) * transmittance;

    // reverse the tone mapping
    resultLuminance = resultLuminance / (1.0 - resultLuminance);

    // This approach only makes sense if the color is not black
    return luminance > 0.0 ? lerp(transmittance, resultLuminance / luminance, _ImprovedTransmittanceBlend) : transmittance;
}

#endif // VOLUMETRIC_CLOUD_UTILITIES_H
