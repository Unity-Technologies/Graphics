#ifndef SAMPLE_WATER_SURFACE_H
#define SAMPLE_WATER_SURFACE_H

#if !defined(IGNORE_WATER_DEFORMATION)
#define SUPPORT_WATER_DEFORMATION
#endif

// Number of bands based on the multi compile
#if defined(WATER_TWO_BANDS)
    #define NUM_WATER_BANDS 2
#elif defined(WATER_THREE_BANDS)
    #define NUM_WATER_BANDS 3
#else
    #define NUM_WATER_BANDS 1
#endif

#define WATER_SYSTEM_CHOPPINESS 2.25
#define WATER_DEEP_FOAM_JACOBIAN_OVER_ESTIMATION 1.03
#define SURFACE_FOAM_BRIGHTNESS 1.0
#define SCATTERING_FOAM_BRIGHTNESS 2.0

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterCurrentUtilities.hlsl"

// Water simulation data
Texture2DArray<float4> _WaterDisplacementBuffer;
Texture2DArray<float4> _WaterAdditionalDataBuffer;

// Space transform overrides
float3 TransformObjectToWorld_Water(float3 positionOS)
{
    return mul(ApplyCameraTranslationToMatrix(_WaterSurfaceTransform), float4(positionOS, 1.0)).xyz;
}

float3 TransformObjectToWorldDir_Water(float3 dirOS)
{
    return mul((float3x3)ApplyCameraTranslationToMatrix(_WaterSurfaceTransform), dirOS);
}

float2 RotateUV(float2 uv)
{
    float2 axis1 = float2(_WaterSurfaceTransform[0].x, _WaterSurfaceTransform[2].x);
    float2 axis2 = float2(-axis1.y, axis1.x);
    return float2(dot(uv, axis1), dot(uv, axis2));
}

// Water Mask
TEXTURE2D(_WaterMask);
SAMPLER(sampler_WaterMask);

float3 EvaluateWaterMask(float3 positionOS)
{
    float2 maskUV = (positionOS.xz - _WaterMaskOffset) * _WaterMaskScale + 0.5f;
    float3 waterMask = SAMPLE_TEX2D(_WaterMask, sampler_WaterMask, maskUV).xyz;

    return _WaterMaskRemap.xxx + waterMask * _WaterMaskRemap.yyy;
}

// Deformation region
Texture2D<float> _WaterDeformationBuffer;
Texture2D<float2> _WaterDeformationSGBuffer;

float2 EvaluateDeformationUV(float3 transformedPositionAWS)
{
    return RotateUV(transformedPositionAWS.xz - _DeformationRegionOffset) * _DeformationRegionScale + 0.5f;
}

float EvaluateWaterDeformation(float3 positionAWS)
{
    float2 deformationUV = EvaluateDeformationUV(positionAWS);
    return SAMPLE_TEXTURE2D_LOD(_WaterDeformationBuffer, s_linear_clamp_sampler, deformationUV, 0);
}

// Band data
float4 GetBandPatchData(int bandIdx)
{
    switch (bandIdx)
    {
        case 0:
            return _Band0_ScaleOffset_AmplitudeMultiplier;
        case 1:
            return _Band1_ScaleOffset_AmplitudeMultiplier;
        default:
            return _Band2_ScaleOffset_AmplitudeMultiplier;
    }
}


float GetPatchAmplitudeMultiplier(int bandIdx)
{
    return GetBandPatchData(bandIdx).w;
}

// Attenuation is: lerp(data, 0, saturate((distance - fadeStart) / fadeDistance))
// = data * saturate(1 - distance / fadeDistance + fadeStart / fadeDistance)
// => FadeA = -1 / fadeDistance     FadeB = 1 + fadeStart / fadeDistance
float DistanceFade(float distanceToCamera, int bandIdx)
{
#ifndef IGNORE_WATER_FADE
    float2 patchFade;
    switch (bandIdx)
    {
        case 0:
            patchFade = _Band0_Fade;
            break;
        case 1:
            patchFade = _Band1_Fade;
            break;
        default:
            patchFade = _Band2_Fade;
            break;
    }

    float fade = saturate(distanceToCamera * patchFade.x + patchFade.y);

    // perform a remap from [0, 1] to [0, 1] on the fade value to make it smoother
    return Smoothstep01(fade * fade);
#else
    return 1.0f;
#endif
}

// Band UV
struct PatchSimData
{
    float2 uv;
    float blend;
    float4 swizzle;
};

struct WaterSimCoord
{
    PatchSimData data[NUM_WATER_BANDS];
};

float2 TransformWaterUV(float2 uv, int bandIdx)
{
    float3 scaleOffset = GetBandPatchData(bandIdx).xyz;
    return uv * scaleOffset.x - scaleOffset.yz;
}

void ComputeWaterUVs(float2 uv, out WaterSimCoord simC)
{
    UNITY_UNROLL for (int bandIdx = 0; bandIdx < NUM_WATER_BANDS; ++bandIdx)
    {
        simC.data[bandIdx].uv = TransformWaterUV(uv, bandIdx);
        simC.data[bandIdx].blend = 1.0;
        simC.data[bandIdx].swizzle = float4(1, 0, 0, 1);
    }
}

void AggregateWaterSimCoords(in WaterSimCoord waterCoord[2],
    in CurrentData currentData[2], int firstPass,
    out WaterSimCoord simCoords)
{
    // Grab the sector data for both groups
    float4 dir[2];
    dir[0] = _WaterSectorData[int2(currentData[0].quadrant + SECTOR_DATA_OTHER_OFFSET, 0)];
    dir[1] = _WaterSectorData[int2(currentData[1].quadrant + SECTOR_DATA_OTHER_OFFSET, 0)];

    // Process the bands that we have
    UNITY_UNROLL for (int bandIdx = 0; bandIdx < NUM_WATER_BANDS; ++bandIdx)
    {
        int group = _PatchGroup[bandIdx];
        float4 packedDir = dir[group];
        simCoords.data[bandIdx].uv = waterCoord[group].data[bandIdx].uv;
        if (firstPass)
        {
            simCoords.data[bandIdx].blend = 1.0 - currentData[group].proportion;
            simCoords.data[bandIdx].swizzle = float4(packedDir.xy, -packedDir.y, packedDir.x);
        }
        else
        {
            simCoords.data[bandIdx].blend = currentData[group].proportion;
            simCoords.data[bandIdx].swizzle = float4(packedDir.zw, -packedDir.w, packedDir.z);
        }

    }
}

// Sample displacement
float3 SampleDisplacement_VS(float2 uv, float bandIdx)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, uv, bandIdx, 0).xyz;
}

void SampleSimulation_VS(WaterSimCoord waterCoord, float3 waterMask, float distanceToCamera,
                        out float3 totalDisplacement, out float lowFrequencyHeight)
{
    // Initialize the output
    totalDisplacement = 0.0;
    lowFrequencyHeight = 0.0;

    // Loop through the bands
    UNITY_UNROLL for (int bandIdx = 0; bandIdx < NUM_WATER_BANDS; ++bandIdx)
    {
        float distanceFade = DistanceFade(distanceToCamera, bandIdx);
        if (distanceFade == 0.0f) continue;

        // Grab the data for the current band
        PatchSimData currentData = waterCoord.data[bandIdx];

        // Read the raw simulation data
        float3 rawDisplacement = SampleDisplacement_VS(currentData.uv, (float)bandIdx);

        // Apply the global attenuations
        rawDisplacement *= GetPatchAmplitudeMultiplier(bandIdx) * waterMask[bandIdx] * currentData.blend;

        // Apply the camera distance attenuation
        rawDisplacement *= distanceFade;

        // Swizzle the displacement and add it
        totalDisplacement += float3(rawDisplacement.x, dot(rawDisplacement.yz, currentData.swizzle.xy), dot(rawDisplacement.yz, currentData.swizzle.zw));

        // Contribute to the low frequency height
        lowFrequencyHeight += bandIdx < 2 ? rawDisplacement.x : 0;
    }
}

// Evaluate Water displacement

struct WaterDisplacementData
{
    float3 displacement;
    float lowFrequencyHeight;
};

void EvaluateWaterDisplacement(float3 positionOS, out WaterDisplacementData displacementData)
{
    // Evaluate the pre-displaced absolute position
    float3 positionRWS = TransformObjectToWorld_Water(positionOS);
    // Evaluate the distance to the camera
    float distanceToCamera = length(positionRWS);

    // Attenuate using the water mask
    float3 waterMask = EvaluateWaterMask(positionOS);

    float3 totalDisplacement = 0.0;
    float lowFrequencyHeight = 0.0;

#if !defined(WATER_LOCAL_CURRENT)
    // Compute the simulation coordinates
    WaterSimCoord waterCoord;
    ComputeWaterUVs(positionOS.xz, waterCoord);

    // Sample the simulation
    SampleSimulation_VS(waterCoord, waterMask, distanceToCamera, totalDisplacement, lowFrequencyHeight);
#else
    // Read the current data
    CurrentData currentData[2];
    EvaluateGroup0CurrentData(positionOS.xz, currentData[0]);
    EvaluateGroup1CurrentData(positionOS.xz, currentData[1]);

    // Compute the simulation coordinates
    float4 tapCoords[2];
    SwizzleSamplingCoordinates(positionOS.xz, currentData[0].quadrant, tapCoords[0]);
    SwizzleSamplingCoordinates(positionOS.xz, currentData[1].quadrant, tapCoords[1]);

    // Compute the 2 simulation coordinates
    WaterSimCoord waterCoord[2];
    WaterSimCoord finalCoords;

    // Sample the simulation (first time)
    ComputeWaterUVs(tapCoords[0].xy, waterCoord[0]);
    ComputeWaterUVs(tapCoords[1].xy, waterCoord[1]);
    AggregateWaterSimCoords(waterCoord, currentData, true, finalCoords);

    float3 totalDisplacement0 = 0.0;
    float lowFrequencyHeight0 = 0.0;
    SampleSimulation_VS(finalCoords, waterMask, distanceToCamera, totalDisplacement0, lowFrequencyHeight0);

    // Sample the simulation (second time)
    ComputeWaterUVs(tapCoords[0].zw, waterCoord[0]);
    ComputeWaterUVs(tapCoords[1].zw, waterCoord[1]);
    AggregateWaterSimCoords(waterCoord, currentData, false, finalCoords);

    float3 totalDisplacement1 = 0.0;
    float lowFrequencyHeight1 = 0.0;
    SampleSimulation_VS(finalCoords, waterMask, distanceToCamera, totalDisplacement1, lowFrequencyHeight1);

    // Combine both contributions
    totalDisplacement = totalDisplacement0 + totalDisplacement1;
    lowFrequencyHeight = lowFrequencyHeight0 + lowFrequencyHeight1;
#endif

    // We apply the choppiness to all bands
    totalDisplacement.yz *= WATER_SYSTEM_CHOPPINESS;

    // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
    displacementData.lowFrequencyHeight = lowFrequencyHeight;
    displacementData.displacement = float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);

#if defined(SUPPORT_WATER_DEFORMATION)
    // Apply the deformation data
    float verticalDeformation = EvaluateWaterDeformation(GetAbsolutePositionWS(positionRWS) + displacementData.displacement);
    displacementData.displacement += float3(0.0, verticalDeformation, 0.0);
    displacementData.lowFrequencyHeight += verticalDeformation;
#endif

#if defined(SHADER_STAGE_VERTEX) && !defined(WATER_DISPLACEMENT)
    ZERO_INITIALIZE(WaterDisplacementData, displacementData);
#endif

    // Remap lf height to [0, 1]
    displacementData.lowFrequencyHeight /= _ScatteringWaveHeight;
    displacementData.lowFrequencyHeight = saturate(displacementData.lowFrequencyHeight * 0.5 + 0.5);
}

// Foam
Texture2D<float2> _WaterFoamBuffer;
TEXTURE2D(_SimulationFoamMask);
SAMPLER(sampler_SimulationFoamMask);
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/FoamUtilities.hlsl"

// UV to sample the foam mask
float2 EvaluateFoamMaskUV(float2 foamUV)
{
    return foamUV * _SimulationFoamMaskScale - _SimulationFoamMaskOffset * _SimulationFoamMaskScale + 0.5f;
}

// UV to sample the foam simulation
float2 EvaluateFoamUV(float3 transformedPositionAWS)
{
    return RotateUV(transformedPositionAWS.xz - _FoamRegionOffset) * _FoamRegionScale + 0.5f;
}

float EvaluateFoam(float jacobian, float foamAmount)
{
    return saturate(-jacobian + foamAmount);
}

float EvaluateFoamMask(float3 positionOS)
{
    float2 maskUV = (positionOS.xz - _SimulationFoamMaskOffset) * _SimulationFoamMaskScale + 0.5f;
    float foamMask = SAMPLE_TEX2D(_SimulationFoamMask, sampler_SimulationFoamMask, maskUV).x;

    return foamMask * _SimulationFoamIntensity;
}

// Evaluate additional data

struct WaterAdditionalData
{
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float surfaceFoam;
    float deepFoam;
};

#if !defined(WATER_SIMULATION)
float4 SampleAdditionalData(float2 uv, float bandIdx, float4 texSize)
{
#ifdef IGNORE_HQ_NORMAL_SAMPLE
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterAdditionalDataBuffer, s_linear_repeat_sampler, uv, bandIdx, 0);
#else
    //if (bandIdx < NUM_WATER_BANDS - 1)
    //    return SAMPLE_TEXTURE2D_ARRAY(_WaterAdditionalDataBuffer, s_linear_repeat_sampler, uv, bandIdx);
    //else
    //    return SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), uv, bandIdx, texSize);
    return SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), uv, bandIdx, texSize);
#endif
}

void SampleSimulation_PS(WaterSimCoord waterCoord,  float3 waterMask, float distanceToCamera, float4 texSize,
                        out float2 surfGrdt, out float2 lfSurfGrdt, out float jcbSurface, out float jcbDeep)
{
    // Initialize the outputs
    surfGrdt = 0.0;
    lfSurfGrdt = 0.0;
    jcbSurface = 0.0;
    jcbDeep = 0.0;

    // Loop through the bands
    UNITY_UNROLL for (int bandIdx = NUM_WATER_BANDS-1; bandIdx >= 0; --bandIdx)
    {
        // Grab the data for the current band
        PatchSimData currentData = waterCoord.data[bandIdx];

        // Read the raw additional data
        float4 additionalData = SampleAdditionalData(currentData.uv, bandIdx, texSize);
        additionalData.w = additionalData.z; // currently they are the same (see kernel EvaluateNormalsJacobian)

        // Compute the camera distance attenuation and water mask
        float fade = DistanceFade(distanceToCamera, bandIdx) * waterMask[bandIdx];

        // Add the jacobian contribution
        // This cannot be easily faded like displacement, but each band roughly has a maximum jacobian of 4
        // This is not accurate, empirically gives good enough results
        jcbSurface += currentData.blend * lerp(4.0f, additionalData.z, fade);
        jcbDeep    += currentData.blend * lerp(4.0f * WATER_DEEP_FOAM_JACOBIAN_OVER_ESTIMATION, additionalData.w, fade);

        if (fade == 0.0f) continue;
        additionalData.xy *= currentData.blend * fade;

        // Swizzle the displacement
        additionalData.xy = float2(dot(additionalData.xy, currentData.swizzle.xy), dot(additionalData.xy, currentData.swizzle.zw));

        // Evaluate the surface gradient
        surfGrdt += additionalData.xy;

        // Contribute to the low frequency height
        lfSurfGrdt += bandIdx < 2 ? additionalData.xy : 0;
    }
}

void EvaluateWaterAdditionalData(float3 positionOS, float3 transformedPosition, float3 meshNormalOS, out WaterAdditionalData waterAdditionalData)
{
    ZERO_INITIALIZE(WaterAdditionalData, waterAdditionalData);
    if (_GridSize.x < 0)
        return;

    // Evaluate the pre-displaced absolute position
    float3 positionRWS = TransformObjectToWorld_Water(positionOS);
    // Evaluate the distance to the camera
    float distanceToCamera = length(positionRWS);
    // Get the world space transformed postion
    float3 transformedAWS = GetAbsolutePositionWS(transformedPosition);

    // Compute the texture size param for the filtering
    float4 texSize = 0.0;
    texSize.xy = _BandResolution;
    texSize.zw = 1.0f / _BandResolution;

    // Attenuate using the water mask
    float3 waterMask = EvaluateWaterMask(positionOS);

    // Initialize the surface gradients
    float2 surfaceGradient = 0.0;
    float2 lFSurfaceGradient = 0.0;
    float jacobianSurface = 0.0;
    float jacobianDeep = 0.0;

#ifdef WATER_DISPLACEMENT
    #if !defined(WATER_LOCAL_CURRENT)
    // Sample the simulation
    WaterSimCoord waterCoord;
    ComputeWaterUVs(positionOS.xz, waterCoord);
    SampleSimulation_PS(waterCoord,  waterMask, distanceToCamera, texSize,
                        surfaceGradient, lFSurfaceGradient, jacobianSurface, jacobianDeep);
    #else
    // Read the current data
    CurrentData currentData[2];
    EvaluateGroup0CurrentData(positionOS.xz, currentData[0]);
    EvaluateGroup1CurrentData(positionOS.xz, currentData[1]);

    // Compute the simulation coordinates
    float4 tapCoords[2];
    SwizzleSamplingCoordinates(positionOS.xz, currentData[0].quadrant, tapCoords[0]);
    SwizzleSamplingCoordinates(positionOS.xz, currentData[1].quadrant, tapCoords[1]);

    // Compute the 2 simulation coordinates
    WaterSimCoord waterCoord[2];
    WaterSimCoord finalCoords;

    // Sample the simulation (first time)
    ComputeWaterUVs(tapCoords[0].xy, waterCoord[0]);
    ComputeWaterUVs(tapCoords[1].xy, waterCoord[1]);
    AggregateWaterSimCoords(waterCoord, currentData, true, finalCoords);

    float2 surfaceGradient0 = 0.0;
    float2 lowFrequencySurfaceGradient0 = 0.0;
    float jacobianSurface0 = 0.0;
    float jacobianDeep0 = 0.0;
    SampleSimulation_PS(finalCoords,  waterMask, distanceToCamera, texSize,
                        surfaceGradient0, lowFrequencySurfaceGradient0, jacobianSurface0, jacobianDeep0);

    // Sample the simulation (second time)
    ComputeWaterUVs(tapCoords[0].zw, waterCoord[0]);
    ComputeWaterUVs(tapCoords[1].zw, waterCoord[1]);
    AggregateWaterSimCoords(waterCoord, currentData, false, finalCoords);

    float2 surfaceGradient1 = 0.0;
    float2 lowFrequencySurfaceGradient1 = 0.0;
    float jacobianSurface1 = 0.0;
    float jacobianDeep1 = 0.0;
    SampleSimulation_PS(finalCoords, waterMask, distanceToCamera, texSize,
                        surfaceGradient1, lowFrequencySurfaceGradient1, jacobianSurface1, jacobianDeep1);

    // Combine both contributions
    surfaceGradient = surfaceGradient0 + surfaceGradient1;
    lFSurfaceGradient = lowFrequencySurfaceGradient0 + lowFrequencySurfaceGradient1;
    jacobianSurface = jacobianSurface0 + jacobianSurface1;
    jacobianDeep = jacobianDeep0 + jacobianDeep1;
    #endif

    #if defined(SUPPORT_WATER_DEFORMATION) || defined(WATER_POST_INCLUDE_DEFORMATION)
    // Apply the deformation data
    float2 deformationUV = EvaluateDeformationUV(transformedAWS);
    float2 deformationSG = SAMPLE_TEXTURE2D_LOD(_WaterDeformationSGBuffer, s_linear_clamp_sampler, deformationUV, 0);
    lFSurfaceGradient += deformationSG;
    surfaceGradient += deformationSG;
    #endif
#endif

    // Evaluate the normals
    float3 lowFrequencyNormalOS = SurfaceGradientResolveNormal(meshNormalOS, float3(lFSurfaceGradient.x, 0, lFSurfaceGradient.y));
    waterAdditionalData.lowFrequencyNormalWS = TransformObjectToWorldDir_Water(lowFrequencyNormalOS);
    float3 normalOS = SurfaceGradientResolveNormal(meshNormalOS, float3(surfaceGradient.x, 0, surfaceGradient.y));
    waterAdditionalData.normalWS = TransformObjectToWorldDir_Water(normalOS);

#ifdef WATER_DISPLACEMENT
    // Attenuate using the foam mask
    float foamMask = EvaluateFoamMask(positionOS);

    // Evaluate the foam from the jacobian
    waterAdditionalData.surfaceFoam = SURFACE_FOAM_BRIGHTNESS * foamMask * EvaluateFoam(jacobianSurface, _SimulationFoamAmount);
    waterAdditionalData.deepFoam = SCATTERING_FOAM_BRIGHTNESS * foamMask * EvaluateFoam(jacobianDeep, _SimulationFoamAmount * WATER_DEEP_FOAM_JACOBIAN_OVER_ESTIMATION);
#else
    waterAdditionalData.surfaceFoam = 0;
    waterAdditionalData.deepFoam = 0;
#endif

#if !defined(IGNORE_FOAM_REGION)
    // Evaluate the foam region coordinates
    float2 foamUV = EvaluateFoamUV(transformedAWS);
    if (_WaterFoamRegionResolution > 0 && all(foamUV == saturate(foamUV)))
    {
        float2 foamRegion = SAMPLE_TEXTURE2D(_WaterFoamBuffer, s_linear_clamp_sampler, foamUV).xy;
        waterAdditionalData.surfaceFoam += foamRegion.x;
        waterAdditionalData.deepFoam += foamRegion.y;
    }
#endif

    // Final foam value
    waterAdditionalData.deepFoam = FoamErosion(1.0 - waterAdditionalData.deepFoam, positionOS.xz, false, 4);
}
#endif

// Iteratively search water height

struct TapData
{
    float3 currentDisplacement;
    float3 displacedPoint;
    float2 offset;
    float distance;
    float height;
};

TapData EvaluateDisplacementData(float3 currentLocation, float3 referencePosition)
{
    TapData data;

    // Evaluate the displacement at the current point
    WaterDisplacementData displacementData;
    EvaluateWaterDisplacement(currentLocation, displacementData);
    data.currentDisplacement = displacementData.displacement;

    // Evaluate the complete position
    data.displacedPoint = currentLocation + data.currentDisplacement;

    // Evaluate the distance to the reference point
    data.offset = data.displacedPoint.xz - referencePosition.xz;

    // Length of the offset vector
    data.distance = length(data.offset);

    // Simulation height of the position of the offset vector
    data.height = displacementData.displacement.y;

    return data;
}

float FindVerticalDisplacement(float3 positionWS, int iterationCount, float distanceThreshold, out int stepCount, out float currentError
#if !defined(WATER_SIMULATION)
    , out float3 normal
    , out float2 current
#endif
)
{
    // The point we will be looking for needs to be converted into the local space of the water simulation
    float3 targetPosition = mul(_WaterCustomTransform_Inverse, float4(positionWS, 1.0)).xyz;

    // Initialize the search data
    bool found = false;
    TapData tapData = EvaluateDisplacementData(targetPosition, targetPosition);
    float3 currentLocation = targetPosition;
    float2 stepSize = tapData.offset;
    float currentHeight = tapData.height;

    currentError = tapData.distance;
    stepCount = 0;

    while (stepCount < iterationCount)
    {
        bool progress = false;
        // Is the point close enough to target position?
        if (currentError < distanceThreshold)
        {
            found = true;
            break;
        }

        // Keep track of the step size that will be use for the 4 samples
        float2 localSearchStepSize = stepSize;

        float3 candidateLocation = currentLocation - float3(localSearchStepSize.x, 0, localSearchStepSize.y);
        TapData tapData = EvaluateDisplacementData(candidateLocation, targetPosition);
        if (tapData.distance < currentError)
        {
            currentLocation = candidateLocation;
            stepSize = tapData.offset;
            currentError = tapData.distance;
            currentHeight = tapData.height;
            progress = true;
        }

        // If we didn't make any progress in this step, this means our steps are probably too big make them smaller
        if (!progress)
            stepSize *= 0.25;

        // If none of the 4 steps managed to get closer, we need a smaller step
        stepCount++;
    }

#ifdef WATER_POST_INCLUDE_DEFORMATION
    currentHeight += EvaluateWaterDeformation(positionWS);
#endif

#if !defined(WATER_SIMULATION)
    WaterAdditionalData waterAdditionalData;
    EvaluateWaterAdditionalData(currentLocation, GetCameraRelativePositionWS(positionWS), float3(0, 1, 0), waterAdditionalData);
    normal = waterAdditionalData.normalWS;

    current = OrientationToDirection(_PatchOrientation.x);

    #ifdef WATER_LOCAL_CURRENT
    CurrentData currentData;
    EvaluateGroup0CurrentData(currentLocation.xz, currentData);
    float2 currentDir = OrientationToDirection(currentData.angle);
    current = float2(dot(current, float2(currentDir.x, -currentDir.y)), dot(current, currentDir.yx));
    #endif
#endif

    return targetPosition.y - currentHeight;
}

#endif
