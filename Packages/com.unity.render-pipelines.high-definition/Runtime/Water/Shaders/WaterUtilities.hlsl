#ifndef WATER_UTILITIES_H
#define WATER_UTILITIES_H

// These values are chosen so that an iFFT patch of 1000km^2 will
// yield a Phillips spectrum distribution in the [-1, 1] range
#define EARTH_GRAVITY 9.81
#define SQRT2 1.41421356237
#define ONE_OVER_SQRT2 0.70710678118
#define PHILLIPS_AMPLITUDE_SCALAR 0.2
#define WATER_IOR 1.3333
#define WATER_INV_IOR 1.0 / WATER_IOR
#define SURFACE_FOAM_BRIGHTNESS 1.0
#define SCATTERING_FOAM_BRIGHTNESS 1.75
#define AMBIENT_SCATTERING_INTENSITY 0.25
#define HEIGHT_SCATTERING_INTENSITY 0.25
#define DISPLACEMENT_SCATTERING_INTENSITY 0.25
#define UNDER_WATER_REFRACTION_DISTANCE 100.0
#define SUPPORT_WATER_DEFORMATION

// Number of bands based on the multi compile
#if defined(WATER_TWO_BANDS)
    #define NUM_WATER_BANDS 2
#elif defined(WATER_THREE_BANDS)
    #define NUM_WATER_BANDS 3
#else
    #define NUM_WATER_BANDS 1
#endif

// Includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterCurrentUtilities.hlsl"

// Water simulation data
Texture2DArray<float4> _WaterDisplacementBuffer;
Texture2DArray<float4> _WaterAdditionalDataBuffer;
Texture2D<float4> _WaterCausticsDataBuffer;
StructuredBuffer<float> _WaterCameraHeightBuffer;

// Water mask
TEXTURE2D(_WaterMask);
SAMPLER(sampler_WaterMask);

// Foam textures
Texture2D<float> _FoamTexture;
TEXTURE2D(_FoamMask);
SAMPLER(sampler_FoamMask);

// Water deformation data
Texture2D<float> _WaterDeformationBuffer;
Texture2D<float2> _WaterDeformationSGBuffer;

#if UNITY_ANY_INSTANCING_ENABLED
StructuredBuffer<float4> _WaterPatchData;
#endif

float2 OrientationToDirection(float orientation)
{
    return float2(cos(orientation), sin(orientation));
}

uint4 WaterHashFunctionUInt4(uint3 coord)
{
    uint4 x = coord.xyzz;
    x = ((x >> 16u) ^ x.yzxy) * 0x45d9f3bu;
    x = ((x >> 16u) ^ x.yzxz) * 0x45d9f3bu;
    x = ((x >> 16u) ^ x.yzxx) * 0x45d9f3bu;
    return x;
}

float4 WaterHashFunctionFloat4(uint3 p)
{
    return WaterHashFunctionUInt4(p) / (float)0xffffffffU;
}

//http://www.dspguide.com/ch2/6.htm
float GaussianDis(float u, float v)
{
    return sqrt(-2.0 * log(max(u, 1e-6f))) * cos(PI * v);
}

float Phillips(float2 k, float2 w, float V, float directionDampener, float patchSize)
{
    float kk = k.x * k.x + k.y * k.y;
    float result = 0.0;
    if (kk != 0.0)
    {
        float L = (V * V) / EARTH_GRAVITY;
        // To avoid _any_ directional bias when there is no wind we lerp towards 0.5f
        float wk = lerp(dot(normalize(k), w), 0.5, directionDampener);
        float phillips = (exp(-1.0f / (kk * L * L)) / (kk * kk)) * (wk * wk);
        result = phillips * (wk < 0.0f ? directionDampener : 1.0);
    }
    return PHILLIPS_AMPLITUDE_SCALAR * result / (patchSize * patchSize);
}

float2 ComplexExp(float arg)
{
    return float2(cos(arg), sin(arg));
}

float2 ComplexMult(float2 a, float2 b)
{
    return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

float FrequencyPhase(float2 complex)
{
    return atan(complex.y / complex.x);
}

float FrequencyAmpltiude(float2 complex)
{
    return sqrt(complex.x * complex.x + complex.y * complex.y);
}

float GetWaterCameraHeight()
{
    return _WaterCameraHeightBuffer[unity_StereoEyeIndex];
}

struct PatchSimData
{
    float2 uv;
    float blend;
    float4 swizzle;
};

void FillPatchSimData(float2 uv, int bandIdx, out PatchSimData data)
{
    data.uv = (uv - OrientationToDirection(_PatchOrientation[bandIdx]) * _PatchCurrentSpeed[bandIdx] * _SimulationTime) / _PatchSize[bandIdx];
    data.blend = 1.0;
    data.swizzle = float4(1, 0, 0, 1);
}

struct WaterSimCoord
{
    PatchSimData data[NUM_WATER_BANDS];
};

void ComputeWaterUVs(float2 uv, out WaterSimCoord simC)
{
    UNITY_UNROLL for (int bandIdx = 0; bandIdx < NUM_WATER_BANDS; ++bandIdx)
    {
        simC.data[bandIdx].uv = (uv - OrientationToDirection(_PatchOrientation[bandIdx]) * _PatchCurrentSpeed[bandIdx] * _SimulationTime) / _PatchSize[bandIdx];
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

float2 ComputeWaterUV(float3 positionWS, int bandIndex)
{
    return (positionWS.xz - OrientationToDirection(_PatchOrientation[bandIndex]) * _PatchCurrentSpeed[bandIndex] * _SimulationTime) / _PatchSize[bandIndex];
}

float3 ShuffleDisplacement(float3 displacement)
{
    return float3(-displacement.y, displacement.x, -displacement.z);
}

void EvaluateDisplacedPoints(float3 displacementC, float3 displacementR, float3 displacementU,
                                float normalization, float pixelSize,
                                out float3 p0, out float3 p1, out float3 p2)
{
    p0 = float3(displacementC.x, displacementC.y, displacementC.z) * normalization;
    p1 = float3(displacementR.x, displacementR.y, displacementR.z) * normalization + float3(pixelSize, 0, 0);
    p2 = float3(displacementU.x, displacementU.y, displacementU.z) * normalization + float3(0, 0, pixelSize);
}

float2 EvaluateSurfaceGradients(float3 p0, float3 p1, float3 p2)
{
    float3 v0 = normalize(p1 - p0);
    float3 v1 = normalize(p2 - p0);
    float3 geometryNormal = normalize(cross(v1, v0));
    return SurfaceGradientFromPerturbedNormal(float3(0, 1, 0), geometryNormal).xz;
}

float EvaluateJacobian(float3 p0, float3 p1, float3 p2, float pixelSize)
{
    // Compute the jacobian of this texel
    float Jxx = 1.f + (p1.x - p0.x) / pixelSize;
    float Jyy = 1.f + (p2.z - p0.z) / pixelSize;
    float Jyx = (p1.z - p0.z) / pixelSize;
    float Jxy = (p2.x - p0.x) / pixelSize;
    return (Jxx * Jyy - Jxy * Jyx);
}

float EvaluateFoam(float jacobian, float foamAmount)
{
    return saturate(-jacobian + foamAmount);
}

// Transforms normal from object to world space
float3 TransformCustomMeshNormal(float3 normalOS, bool doNormalize = true)
{
    // Normal need to be multiply by inverse transpose
    float3 normalWS = mul(normalOS, (float3x3)_WaterCustomMeshTransform_Inverse);
    if (doNormalize)
        return SafeNormalize(normalWS);
    return normalWS;
}

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
float3 WaterSimulationPositionInstanced(float3 objectPosition, uint instanceID)
{
    // Grab the patch data for the current instance/patch
    float4 patchData = _WaterPatchData[instanceID];

    // Scale the position by the size of the grid
    float3 simulationPos = objectPosition * float3(patchData.x, 1.0, patchData.y);

    // Offset the surface to where it should be
    simulationPos += float3(_PatchOffset.x + patchData.z, 0.0f, _PatchOffset.z + patchData.w);

    // Return the simulation position
    return simulationPos;
}
#else
float3 WaterSimulationPosition(float3 objectPosition)
{
    // Scale the position by the size of the grid
    float3 simulationPos = objectPosition * float3(_GridSize.x, 1.0, _GridSize.y);

    // Offset the surface to where it should be
    simulationPos += float3(_PatchOffset.x, _PatchOffset.y, _PatchOffset.z);

    // Return the simulation position
    return simulationPos;
}
#endif

float3 GetWaterVertexPosition(float3 positionRWS)
{
    // In case we are using custom geometries, we need to apply the tranform of each custom geometry to ensure that they are correctly connected
    return mul(_WaterCustomMeshTransform, float4(GetAbsolutePositionWS(positionRWS), 1.0)).xyz;
}

float3 GetWaterVertexNormal(float3 normalOS)
{
    return _WaterProceduralGeometry ? float3(0, 1, 0) : normalOS;
}

struct WaterDisplacementData
{
    float3 displacement;
    float lowFrequencyHeight;
};

float2 EvaluateWaterMaskUV(float2 maskUV)
{
    // Shift and scale
    return float2(maskUV.x - _WaterMaskOffset.x, maskUV.y + _WaterMaskOffset.y) * _WaterMaskScale + 0.5f;
}

float3 RemapWaterMaskValue(float3 waterMask)
{
    return _WaterMaskRemap.xxx + waterMask * _WaterMaskRemap.yyy;
}

float PackLowFrequencyHeight(float displacement)
{
    // Value goes from [-1 to 1]
    float normalizedDisplacement = displacement / _ScatteringWaveHeight;
    return saturate(normalizedDisplacement * 0.5 + 0.5);
}

float UnpackLowFrequencyHeight(float normalizedDisplacement)
{
    // Input value is from [0 to 1.0] out put is from [-1 to 1]
    return normalizedDisplacement * 2.0 - 1.0;
}

float2 AttenuateSignal2(float2 data, float distanceToCamera, int band)
{
    return lerp(data, data * _PatchFadeValue[band], saturate((distanceToCamera - _PatchFadeStart[band]) / _PatchFadeDistance[band]));
}

float3 AttenuateSignal3(float3 data, float distanceToCamera, int band)
{
    return lerp(data, data * _PatchFadeValue[band], saturate((distanceToCamera - _PatchFadeStart[band]) / _PatchFadeDistance[band]));
}

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
        // Grab the data for the current band
        PatchSimData currentData = waterCoord.data[bandIdx];

        // Read the raw simulation data
        float3 rawDisplacement = SampleDisplacement_VS(currentData.uv, (float)bandIdx);

        // Apply the global attenuations
        rawDisplacement *= _PatchAmplitudeMultiplier[bandIdx] * waterMask[bandIdx] * currentData.blend;

        // Apply the camera distance attenuation
        rawDisplacement = AttenuateSignal3(rawDisplacement, distanceToCamera, bandIdx);

        // Swizzle the displacement and add it
        totalDisplacement += float3(rawDisplacement.x, dot(rawDisplacement.yz, currentData.swizzle.xy), dot(rawDisplacement.yz, currentData.swizzle.zw));

        // Contribute to the low frequency height
        lowFrequencyHeight += bandIdx < 2 ? rawDisplacement.x : 0;
    }
}

void EvaluateWaterDisplacement(float3 positionOS, out WaterDisplacementData displacementData)
{
    // Evaluate the pre-displaced absolute position
    float3 positionAWS = mul(_WaterSurfaceTransform, float4(positionOS, 1.0)).xyz;

    // Evaluate the distance to the camera
    float distanceToCamera = length(GetCameraRelativePositionWS(positionAWS));

    // Attenuate using the water mask
    float2 maskUV = EvaluateWaterMaskUV(positionOS.xz);
    float3 waterMask = RemapWaterMaskValue(SAMPLE_TEXTURE2D_LOD(_WaterMask, sampler_WaterMask, maskUV, 0).xyz);

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
    EvaluateGroup0CurrentData_VS(positionOS.xz, currentData[0]);
    EvaluateGroup1CurrentData_VS(positionOS.xz, currentData[1]);

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
    totalDisplacement.yz *= _Choppiness;

    // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
    displacementData.lowFrequencyHeight = lowFrequencyHeight;
    displacementData.displacement = float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);

#if defined(SUPPORT_WATER_DEFORMATION)
    // Apply the deformation data
    float3 displacedPosition = positionAWS + displacementData.displacement;
    float2 deformationUV = (displacedPosition.xz - _WaterDeformationCenter) / _WaterDeformationExtent + 0.5f;
    float verticalDeformation = SAMPLE_TEXTURE2D_LOD(_WaterDeformationBuffer, s_linear_clamp_sampler, deformationUV, 0);
    displacementData.displacement += float3(0.0, verticalDeformation, 0.0);
    displacementData.lowFrequencyHeight += verticalDeformation;
#endif

    // Make sure the low frequency is packed
    displacementData.lowFrequencyHeight = PackLowFrequencyHeight(displacementData.lowFrequencyHeight);
}

struct PackedWaterData
{
    float3 positionOS;
    float3 normalOS;
    float4 uv0;
    float4 uv1;
};

void PackWaterVertexData(float3 positionOS, float3 normalOS, float3 displacement, float lowFrequencyHeight, out PackedWaterData packedWaterData)
{
    packedWaterData.positionOS = positionOS + displacement;
    packedWaterData.normalOS = normalOS;
    packedWaterData.uv0 = float4(positionOS.x, positionOS.z, displacement.y, 0.0);
    packedWaterData.uv1 = float4(lowFrequencyHeight, length(float2(displacement.x, displacement.z)), 0.0, 0.0);
}

float2 EvaluateFoamMaskUV(float2 foamUV)
{
    // Shift and scale
    return float2(foamUV.x - _FoamMaskOffset.x, foamUV.y + _FoamMaskOffset.y) * _FoamMaskScale + 0.5f;
}

struct WaterAdditionalData
{
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float surfaceFoam;
    float deepFoam;
};

#if !defined(WATER_SIMULATION)
float3 ComputeDebugNormal(float3 worldPos)
{
    float3 worldPosDdx = normalize(ddx(worldPos));
    float3 worldPosDdy = normalize(ddy(worldPos));
    return normalize(-cross(worldPosDdx, worldPosDdy));
}

float4 SampleAdditionalData(float2 uv, float bandIdx, float4 texSize)
{
    return SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), uv, bandIdx, texSize);
    // return SAMPLE_TEXTURE2D_ARRAY(_WaterAdditionalDataBuffer, s_linear_repeat_sampler, uv, bandIdx);
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
    UNITY_UNROLL for (int bandIdx = 0; bandIdx < NUM_WATER_BANDS; ++bandIdx)
    {
        // Grab the data for the current band
        PatchSimData currentData = waterCoord.data[bandIdx];

        // Read the raw additional data
        float4 additionalData = SampleAdditionalData(currentData.uv, bandIdx, texSize);

        // Apply the global attenuations
        additionalData *= waterMask[bandIdx] * currentData.blend;

        // Add the jacobian contribution
        jcbSurface += additionalData.z;
        jcbDeep += additionalData.w;

        // Attenuate base on the camera distance
        additionalData.xy = AttenuateSignal2(additionalData.xy, distanceToCamera, bandIdx);
        additionalData.xy = float2(dot(additionalData.xy, currentData.swizzle.xy), dot(additionalData.xy, currentData.swizzle.zw));

        // Evaluate the surface gradient
        surfGrdt += additionalData.xy;

        // Contribute to the low frequency height
        lfSurfGrdt += bandIdx < 2 ? additionalData.xy : 0;
    }
}

void EvaluateWaterAdditionalData(float3 positionOS, float3 transformedPosition, float3 meshNormalOS, out WaterAdditionalData waterAdditionalData)
{
    // Evaluate the pre-displaced absolute position
    float3 positionAWS = mul(_WaterSurfaceTransform, float4(positionOS, 1.0)).xyz;

    // Compute the texture size param for the filtering
    float4 texSize = 0.0;
    texSize.xy = _BandResolution;
    texSize.zw = 1.0f / _BandResolution;

    // Evaluate the distance to the camera
    float distanceToCamera = length(GetCameraRelativePositionWS(positionAWS));

    // Attenuate using the water mask
    float2 maskUV = EvaluateWaterMaskUV(positionOS.xz);
    float3 waterMask = RemapWaterMaskValue(SAMPLE_TEXTURE2D(_WaterMask, sampler_WaterMask, maskUV).xyz);

    // Initialize the surface gradients
    float2 surfaceGradient = 0.0;
    float2 lFSurfaceGradient = 0.0;
    float jacobianSurface = 0.0;
    float jacobianDeep = 0.0;

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

#if defined(SUPPORT_WATER_DEFORMATION)
    // Apply the deformation data
    float3 transformedAWS = GetAbsolutePositionWS(transformedPosition);
    float2 deformationUV = (transformedAWS.xz - _WaterDeformationCenter) / _WaterDeformationExtent;
    float2 deformationSG = SAMPLE_TEXTURE2D_LOD(_WaterDeformationSGBuffer, s_linear_clamp_sampler, deformationUV + 0.5f, 0);
    lFSurfaceGradient += deformationSG;
    surfaceGradient += deformationSG;
#endif

    // Evaluate the normals
    float3 lowFrequencyNormalOS = SurfaceGradientResolveNormal(meshNormalOS, float3(lFSurfaceGradient.x, 0, lFSurfaceGradient.y));
    waterAdditionalData.lowFrequencyNormalWS = mul(_WaterSurfaceTransform, float4(lowFrequencyNormalOS, 0.0)).xyz;
    float3 normalOS = SurfaceGradientResolveNormal(meshNormalOS, float3(surfaceGradient.x, 0, surfaceGradient.y));
    waterAdditionalData.normalWS = mul(_WaterSurfaceTransform, float4(normalOS, 0.0)).xyz;

    // Attenuate using the foam mask
    float2 foamMaskUV = EvaluateFoamMaskUV(positionOS.xz);
    float foamMask = SAMPLE_TEXTURE2D(_FoamMask, sampler_FoamMask, foamMaskUV).x;

    // Evaluate the foam from the jacobian
    waterAdditionalData.surfaceFoam = EvaluateFoam(jacobianSurface, _SimulationFoamAmount) * SURFACE_FOAM_BRIGHTNESS * _FoamIntensity * foamMask;
    waterAdditionalData.deepFoam = EvaluateFoam(jacobianDeep, _SimulationFoamAmount) * SCATTERING_FOAM_BRIGHTNESS * _FoamIntensity * foamMask;
}

float3 EvaluateWaterSurfaceGradient_VS(float3 positionAWS, int LOD, int bandIndex)
{
    // Compute the simulation coordinates
    float2 uvBand = ComputeWaterUV(positionAWS, bandIndex);

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

float2 EvaluateFoamUV(float3 positionAWS)
{
    return (positionAWS.xz - OrientationToDirection(_PatchOrientation[0]) * _PatchCurrentSpeed[0] * _SimulationTime) * _FoamTilling;
}

struct FoamData
{
    float smoothness;
    float foamValue;
};

void EvaluateFoamData(float surfaceFoam, float customFoam, float3 positionAWS, out FoamData foamData)
{
    // Compute the surface foam
    float2 foamUV = EvaluateFoamUV(positionAWS);
    float foamTex = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, foamUV).x;

    // Final foam value
    foamData.foamValue = (surfaceFoam * _WindFoamAttenuation + customFoam) * foamTex;

    // Blend the smoothness of the water and the foam
    foamData.smoothness = lerp(_WaterSmoothness, _SimulationFoamSmoothness, saturate(foamData.foamValue * 10.0f));
}

#define WATER_BACKGROUND_ABSORPTION_DISTANCE 1000.f

float EvaluateHeightBasedScattering(float lowFrequencyHeight)
{
    float heightScatteringValue = lerp(0.0, HEIGHT_SCATTERING_INTENSITY, lowFrequencyHeight);
    return lerp(0.0, heightScatteringValue, _HeightBasedScattering);
}

float EvaluateDisplacementScattering(float displacement)
{
    float displacementScatteringValue = lerp(0.0, DISPLACEMENT_SCATTERING_INTENSITY, displacement / (_ScatteringWaveHeight * _Choppiness));
    return lerp(0.0, displacementScatteringValue, _DisplacementScattering);
}

float GetWaveTipThickness(float normalizedDistanceToMaxWaveHeightPlane, float3 worldView, float3 refractedRay)
{
    float H = saturate(normalizedDistanceToMaxWaveHeightPlane);
    return 1.f - saturate(1 + refractedRay.y - 0.2) * (H * H) / 0.4;
}

float2 Molulo2D(float2 divident, float2 divisor)
{
    float2 positiveDivident = divident % divisor + divisor;
    return positiveDivident % divisor;
}

float2 MorphingNoise(float2 position)
{
    float n = sin(dot(position, float2(41, 289)));
    position = frac(float2(262144, 32768)*n);
    return sin(TWO_PI * position + _SimulationTime) * 0.45 + 0.5;
}

float VoronoiNoise(float2 coordinate)
{
    // The voronoi rotation is fixed
    const float voronoiRotation = 20.0f;

    float2 baseCell = floor(coordinate);

    //first pass to find the closest cell
    float minDistToCell = 10;
    float2 toClosestCell;
    float2 closestCell;
    float smoothDistance = 0;
    for(int x1 = -1; x1 <= 1; x1 ++)
    {
        for(int y1 = -1; y1 <= 1; y1++)
        {
            float2 cell = baseCell + float2(x1, y1);
            float2 tiledCell = Molulo2D(cell, voronoiRotation);
            float2 cellPosition = cell + MorphingNoise(tiledCell);
            float2 toCell = cellPosition - coordinate;
            float distToCell = length(toCell);

            if(distToCell < minDistToCell)
            {
                minDistToCell = distToCell;
                closestCell = cell;
                toClosestCell = toCell;
            }
        }
    }

    return minDistToCell * minDistToCell;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

float EvaluateSimulationCaustics(float3 refractedWaterPosRWS, float refractedWaterDepth, float2 distortedWaterNDC)
{
    // Will hold the results of the caustics evaluation
    float3 causticsValues = 0.0;
    float3 triplanarW = 0.0;
    float causticWeight = 0.0;

    // TODO: Is this worth a multicompile?
    if (_WaterCausticsEnabled)
    {
        // Evaluate the caustics weight
        causticWeight = saturate(refractedWaterDepth / _CausticsPlaneBlendDistance);

        // Evaluate the normal of the surface (using partial derivatives of the absolute world pos is not possible as it is not stable enough)
        NormalData normalData;
        float4 normalBuffer = LOAD_TEXTURE2D_X_LOD(_NormalBufferTexture, distortedWaterNDC * _ScreenSize.xy, 0);
        DecodeFromNormalBuffer(normalBuffer, normalData);

        // Evaluate the triplanar weights
        triplanarW = ComputeTriplanarWeights(mul(_WaterSurfaceTransform_Inverse, float4(normalData.normalWS, 0.0)).xyz);

        // Convert the position to absolute world space and move the position to the water local space
        float3 causticPosOS = mul(_WaterSurfaceTransform_Inverse, float4(GetAbsolutePositionWS(refractedWaterPosRWS), 1.0)).xyz;

        // Evaluate the triplanar coodinates
        float3 sampleCoord = causticPosOS / (_CausticsRegionSize * 0.5) + 0.5;
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(sampleCoord, uv0, uv1, uv2);

        // Evaluate the sharpness of the caustics based on the depth
        float sharpness = (1.0 - causticWeight) * 2;

        // sample the caustics texture
#if defined(SHADER_STAGE_COMPUTE)
        causticsValues.x = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
#else
        causticsValues.x = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_BIAS(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
#endif
    }

    // Evaluate the triplanar weights and blend the samples togheter
    return 1.0 + lerp(0, causticsValues.x * triplanarW.y
                        + causticsValues.y * triplanarW.z
                        + causticsValues.z * triplanarW.x, causticWeight) * _CausticsIntensity;
}

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

void ComputeWaterRefractionParams(float3 waterPosRWS, float3 waterNormal, float3 lowFrequencyNormals,
    float2 screenUV, float3 viewWS, bool aboveWater,
    float maxRefractionDistance, float3 transparencyColor, float outScatteringCoeff,
    out float3 refractedWaterPosRWS, out float2 distortedWaterNDC, out float refractedWaterDistance, out float3 absorptionTint)
{
    // Compute the position of the surface behind the water surface
    float  directWaterDepth = SampleCameraDepth(screenUV);
    float3 directWaterPosRWS = ComputeWorldSpacePosition(screenUV, directWaterDepth, UNITY_MATRIX_I_VP);

    // Compute the distance between the water surface and the object behind
    float underWaterDistance = directWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(directWaterPosRWS - waterPosRWS);

    // Blend both normals to decide what normal will be used for the refraction
    float3 refractionNormal = normalize(lerp(waterNormal, lowFrequencyNormals, saturate(underWaterDistance / max(maxRefractionDistance, 0.00001f))));

    // We approach the refraction differently if we are under or above water for various reasons
    float3 refractedView;
    float3 distortedWaterWS;
    if (aboveWater)
    {
        refractedView = lerp(refractionNormal, float3(0, 1, 0), EdgeBlendingFactor(screenUV, length(waterPosRWS))) * float3(1, 0, 1);
        distortedWaterWS = waterPosRWS + refractedView * min(underWaterDistance, maxRefractionDistance);
    }
    else
    {
        refractedView = refract(-viewWS, waterNormal, WATER_IOR);
        distortedWaterWS = waterPosRWS + refractedView * UNDER_WATER_REFRACTION_DISTANCE;
    }

    // Project the point on screen
    distortedWaterNDC = ComputeNormalizedDeviceCoordinates(distortedWaterWS, UNITY_MATRIX_VP);

    // Compute the position of the surface behind the water surface
    float refractedWaterDepth = SampleCameraDepth(distortedWaterNDC);
    refractedWaterPosRWS = ComputeWorldSpacePosition(distortedWaterNDC, refractedWaterDepth, UNITY_MATRIX_I_VP);
    refractedWaterDistance = refractedWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(refractedWaterPosRWS - waterPosRWS);

    // If the point that we are reading is closer than the water surface
    if (dot(refractedWaterPosRWS - waterPosRWS, viewWS) > 0.0)
    {
        // We read the direct depth (no refraction)
        refractedWaterDistance = underWaterDistance;
        refractedWaterPosRWS = directWaterPosRWS;
        distortedWaterNDC = screenUV;
    }

    // Evaluate the absorption tint
    absorptionTint = exp(-refractedWaterDistance * outScatteringCoeff * (1.f - transparencyColor));

    // If we are underwater and we detect a total internal refraction, we need to adjust the parameters
    if (!aboveWater)
    {
        // Evaluate the absorption tint
        bool invalidSamplePoint = dot(-viewWS, refractedView) <= 0.0
                                || distortedWaterNDC.x < 0.0 || distortedWaterNDC.y < 0.0
                                || distortedWaterNDC.x > 1.0 || distortedWaterNDC.y > 1.0;
        absorptionTint = invalidSamplePoint ? 0.0 : 1;
    }
}

float EvaluateTipThickness(float3 viewWS, float3 lowFrequencyNormals, float lowFrequencyHeight)
{
    // Compute the tip thickness
    float tipHeight = saturate(UnpackLowFrequencyHeight(lowFrequencyHeight));
    float3 lowFrequencyRefractedRay = refract(-viewWS, lowFrequencyNormals, WATER_INV_IOR);
    return GetWaveTipThickness(max(0.01, tipHeight), viewWS, lowFrequencyRefractedRay);
}

float3 EvaluateRefractionColor(float3 absorptionTint, float3 caustics)
{
    // Evaluate the refraction color (we need to account for the initial absoption (light to underwater))
    return absorptionTint * caustics * absorptionTint;
}

float3 EvaluateScatteringColor(float lowFrequencyHeight, float horizontalDisplacement, float3 absorptionTint, float deepFoam)
{
    // Evaluate the scattering terms (where the refraction doesn't happen)
    float heightBasedScattering = EvaluateHeightBasedScattering(lowFrequencyHeight);
    float displacementScattering = EvaluateDisplacementScattering(horizontalDisplacement);
    float ambientScattering = AMBIENT_SCATTERING_INTENSITY * _AmbientScattering;

    // Stum the scattering terms
    float scatteringTerms = saturate(ambientScattering + heightBasedScattering + displacementScattering);
    float3 scatteringTint = _ScatteringColorTips.xyz * scatteringTerms;
    return scatteringTint * (1.f - absorptionTint) * (1.0 + deepFoam);
}
#endif // WATER_UTILITIES_H
