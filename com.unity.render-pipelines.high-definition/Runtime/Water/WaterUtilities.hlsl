#ifndef WATER_UTILITIES_H
#define WATER_UTILITIES_H
// These values are chosen so that an iFFT patch of 1000km^2 will
// yield a Phillips spectrum distribution in the [-1, 1] range
#define EARTH_GRAVITY 9.81
#define ONE_OVER_SQRT2 0.70710678118
#define PHILLIPS_AMPLITUDE_SCALAR 0.000001
#define WATER_IOR 1.3333
#define WATER_INV_IOR 1.0 / WATER_IOR
#define SURFACE_FOAM_BRIGHTNESS 0.7
#define SCATTERING_FOAM_BRIGHTNESS 1.5
#define UNDER_WATER_SCATTERING_ATTENUATION 0.25

// Water simulation data
Texture2DArray<float4> _WaterDisplacementBuffer;
Texture2DArray<float4> _WaterAdditionalDataBuffer;
Texture2D<float4> _WaterCausticsDataBuffer;
StructuredBuffer<float> _WaterCameraHeightBuffer;

// Water mask
Texture2D<float2> _WaterMask;

// Foam textures
Texture2D<float> _FoamTexture;
Texture2D<float> _FoamMask;

#if UNITY_ANY_INSTANCING_ENABLED
StructuredBuffer<float4> _WaterPatchData;
#endif

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

float Phillips(float2 k, float2 w, float V)
{
    float kk = k.x * k.x + k.y * k.y;
    float result = 0.0;
    if (kk != 0.0)
    {
        float L = (V * V) / EARTH_GRAVITY;
        // To avoid _any_ directional bias when there is no wind we lerp towards 0.5f
        float wk = lerp(dot(normalize(k), w), 0.5, _DirectionDampener);
        float phillips = (exp(-1.0f / (kk * L * L)) / (kk * kk)) * (wk * wk);
        result = phillips * (wk < 0.0f ? _DirectionDampener : 1.0);
    }
    return PHILLIPS_AMPLITUDE_SCALAR * result;
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

struct WaterSimulationCoordinates
{
    float2 uvBand0;
    float2 uvBand1;
    float2 uvBand2;
    float2 uvBand3;
};

void ComputeWaterUVs(float3 positionWS, out WaterSimulationCoordinates waterCoord)
{
    float2 uv = positionWS.xz + _WindDirection * _CurrentSpeed * _SimulationTime;
    waterCoord.uvBand0 = uv / _BandPatchSize.x;
    waterCoord.uvBand1 = uv / _BandPatchSize.y;
    waterCoord.uvBand2 = uv / _BandPatchSize.z;
    waterCoord.uvBand3 = uv / _BandPatchSize.w;
}

float2 ComputeWaterUV(float3 positionWS, int bandIndex)
{
    return positionWS.xz / _BandPatchSize[bandIndex];
}

float3 ShuffleDisplacement(float3 displacement)
{
    return float3(-displacement.y, displacement.x, -displacement.z);
}

float EvaluateDisplacementNormalization(uint bandIndex)
{
    // Compute the displacement normalization factor
    float patchSizeRatio = _BandPatchSize[bandIndex] / _BandPatchSize[0];
    return _WaveAmplitude[bandIndex] / patchSizeRatio;
}

float4 EvaluateDisplacementNormalization()
{
    // Compute the displacement normalization factor
    float4 patchSizeRatio = _BandPatchSize / _BandPatchSize[0];
    return _WaveAmplitude / patchSizeRatio;
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

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
float3 WaterSimulationPositionInstanced(float3 objectPosition, uint instanceID)
{
    // Grab the patch data for the current instance/patch
    float4 patchData = _WaterPatchData[instanceID];

    // Scale the position by the size of the grid
    float3 simulationPos = objectPosition * float3(patchData.x, 1.0, patchData.y);

    // Offset the surface to where it should be
    simulationPos += float3(_PatchOffset.x + patchData.z, _PatchOffset.y, _PatchOffset.z + patchData.w);

    // Return the simulation position
    return simulationPos;
}
#else
float3 WaterSimulationPosition(float3 objectPosition)
{
    // Scale the position by the size of the grid
    float3 simulationPos = objectPosition * float3(_GridSize.x, 1.0, _GridSize.y);

    // Apply the rotation and the offset
    simulationPos = float3(simulationPos.x * _WaterRotation.x - simulationPos.z * _WaterRotation.y, simulationPos.y, simulationPos.x * _WaterRotation.y + simulationPos.z * _WaterRotation.x);

    // Offset the surface to where it should be
    simulationPos += float3(_PatchOffset.x, _PatchOffset.y, _PatchOffset.z);

    // Return the simulation position
    return simulationPos;
}
#endif

float3 GetWaterVertexPosition(float3 positionRWS)
{
    // Get the absolute world position from the camera relative one
    return GetAbsolutePositionWS(positionRWS);
}

struct WaterDisplacementData
{
    float3 displacement;
    float lowFrequencyHeight;
    float sssMask;
};

float EvaluateSSSMask(float3 positionWS, float3 cameraPositionWS)
{
    float3 viewWS = normalize(cameraPositionWS - positionWS);
    float distanceToCamera = distance(cameraPositionWS, positionWS);
    float angleWithWaterPlane = PositivePow(saturate(viewWS.y), 0.2);
    return (1.f - exp(-distanceToCamera * _SSSMaskCoefficient)) * angleWithWaterPlane;
}

float2 EvaluateWaterMaskUV(float3 positionAWS)
{
    // Move back to object space
    float2 uv = positionAWS.xz;
    if (!_InfiniteSurface)
        uv -= float2(_PatchOffset.x, _PatchOffset.z);
    uv = float2(uv.x * _WaterRotation.x + uv.y * _WaterRotation.y, uv.x * _WaterRotation.y - uv.y * _WaterRotation.x);

    // Shift and scale
    return float2(uv.x - _WaterMaskOffset.x, uv.y - _WaterMaskOffset.y) * _WaterMaskScale + 0.5f;
}

void EvaluateWaterDisplacement(float3 positionAWS, float4 bandsMultiplier, out WaterDisplacementData displacementData)
{
    // Compute the simulation coordinates
    WaterSimulationCoordinates waterCoord;
    ComputeWaterUVs(positionAWS, waterCoord);

    // Compute the displacement normalization factor
    float4 patchSizes = _BandPatchSize / _BandPatchSize[0];
    float4 patchSizes2 = patchSizes * patchSizes;
    float4 displacementNormalization = EvaluateDisplacementNormalization();

    // Accumulate the displacement from the various layers
    float3 totalDisplacement = 0.0;
    float lowFrequencyHeight = 0.0;
    float normalizedDisplacement = 0.0;

    // Attenuate using the water mask
    float2 maskUV = EvaluateWaterMaskUV(positionAWS);
    float2 waterMask = SAMPLE_TEXTURE2D_LOD(_WaterMask, s_linear_clamp_sampler, maskUV, 0);

    // First band
    float3 rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand0, 0, 0).xyz * displacementNormalization.x * waterMask.x * bandsMultiplier.x;
    totalDisplacement += rawDisplacement;
    lowFrequencyHeight += rawDisplacement.x;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.x;

    // We only apply the choppiness to the first band, doesn't behave very good past those
    totalDisplacement.yz *= _Choppiness;

    // Second band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand1, 1, 0).xyz * displacementNormalization.y * waterMask.x * bandsMultiplier.y;
    totalDisplacement += rawDisplacement;
    lowFrequencyHeight += rawDisplacement.x;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.y;

#if defined(HIGH_RESOLUTION_WATER)
    // Third band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand2, 2, 0).xyz * displacementNormalization.z * waterMask.y * bandsMultiplier.z;
    totalDisplacement += rawDisplacement;
    lowFrequencyHeight += rawDisplacement.x * 0.5;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.z;

    // Fourth band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand3, 3, 0).xyz * displacementNormalization.w * waterMask.y * bandsMultiplier.w;
    totalDisplacement += rawDisplacement;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.w;
#endif

    // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
    displacementData.displacement = float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);
    displacementData.lowFrequencyHeight = (_MaxWaveHeight + lowFrequencyHeight) / _MaxWaveHeight - 0.5f;
    displacementData.sssMask = EvaluateSSSMask(positionAWS, _WorldSpaceCameraPos);
}

struct PackedWaterData
{
    float3 positionOS;
    float3 normalOS;
    float4 uv0;
    float4 uv1;
};

void PackWaterVertexData(float3 positionAWS, float3 displacement, float lowFrequencyHeight, float sssMask, out PackedWaterData packedWaterData)
{
    packedWaterData.positionOS = positionAWS + displacement;
    packedWaterData.normalOS = float3(0, 1, 0);
    packedWaterData.uv0 = float4(positionAWS.x, displacement.y, positionAWS.z, 0.0);
    packedWaterData.uv1 = float4(lowFrequencyHeight, 0.0, sssMask, length(float2(displacement.x, displacement.z)));
}

struct WaterAdditionalData
{
    float3 surfaceGradient;
    float3 lowFrequencySurfaceGradient;
    float surfaceFoam;
    float deepFoam;
};

#if !defined(WATER_SIMULATION)
void EvaluateWaterAdditionalData(float3 positionAWS, float4 bandsMultiplier, out WaterAdditionalData waterAdditionalData)
{
    // Compute the simulation coordinates
    WaterSimulationCoordinates waterCoord;
    ComputeWaterUVs(positionAWS, waterCoord);

    // Compute the texture size param for the filtering
    float4 texSize = 0.0;
    texSize.xy = _BandResolution;
    texSize.zw = 1.0f / _BandResolution;

    // Attenuate using the water mask
    float2 maskUV = EvaluateWaterMaskUV(positionAWS);
    float2 waterMask = SAMPLE_TEXTURE2D_LOD(_WaterMask, s_linear_clamp_sampler, maskUV, 0);

    // First band
    float4 additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand0, 0, texSize);
    float3 surfaceGradient = float3(additionalData.x, 0, additionalData.y) * waterMask.x * bandsMultiplier.x;
    float jacobianSurface = additionalData.z;
    float jacobianDeep = additionalData.w;

    // Second band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand1, 1, texSize);
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * waterMask.x * bandsMultiplier.y;
    jacobianSurface += additionalData.z;
    jacobianDeep += additionalData.w;

    // Up the second band, the low frequency and complete normals are the same
    float3 lowFrequencySurfaceGradient = surfaceGradient;

#if defined(HIGH_RESOLUTION_WATER)
    // Third band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand2, 2, texSize);
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * waterMask.y * bandsMultiplier.z;
    lowFrequencySurfaceGradient += float3(additionalData.x, 0, additionalData.y) * 0.5 * waterMask.y * bandsMultiplier.z;
    jacobianSurface += additionalData.z * 0.5;
    jacobianDeep += additionalData.w * 0.5;

    // Fourth band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand3, 3, texSize);
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * waterMask.y * bandsMultiplier.w;
#endif

    // Output the two surface gradients
    waterAdditionalData.surfaceGradient = surfaceGradient;
    waterAdditionalData.lowFrequencySurfaceGradient = lowFrequencySurfaceGradient;

    // Evaluate the foam from the jacobian
    waterAdditionalData.surfaceFoam = EvaluateFoam(jacobianSurface, _SimulationFoamAmount) * SURFACE_FOAM_BRIGHTNESS;
    waterAdditionalData.deepFoam = EvaluateFoam(jacobianDeep, _SimulationFoamAmount) * SCATTERING_FOAM_BRIGHTNESS;
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

float3 ComputeDebugNormal(float3 worldPos)
{
    float3 worldPosDdx = normalize(ddx(worldPos));
    float3 worldPosDdy = normalize(ddy(worldPos));
    return normalize(-cross(worldPosDdx, worldPosDdy));
}
#endif

float2 EvaluateFoamUV(float3 positionAWS)
{
    return (positionAWS.xz) * _FoamTilling;
}

float2 EvaluateFoamMaskUV(float3 positionAWS)
{
    // Move back to object space
    float2 uv = positionAWS.xz;
    if (!_InfiniteSurface)
        uv -= float2(_PatchOffset.x, _PatchOffset.z);
    uv = float2(uv.x * _WaterRotation.x + uv.y * _WaterRotation.y, uv.x * _WaterRotation.y - uv.y * _WaterRotation.x);

    // Shift and scale
    return float2(uv.x - _FoamMaskOffset.x, uv.y - _FoamMaskOffset.y) * _FoamMaskScale + 0.5f;
}

struct FoamData
{
    float smoothness;
    float foamValue;
    float3 surfaceGradient;
};

void EvaluateFoamData(float3 surfaceGradient, float3 lowFrequencySurfaceGradient,
    float surfaceFoam, float customFoam, float3 positionAWS, out FoamData foamData)
{
    // Attenuate using the foam mask
    float2 foamMaskUV = EvaluateFoamMaskUV(positionAWS);
    float foamMask = SAMPLE_TEXTURE2D(_FoamMask, s_linear_clamp_sampler, foamMaskUV).x;

    // Compute the surface foam
    float2 foamUV = EvaluateFoamUV(positionAWS);

    float foamTex = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, foamUV).x;

    // Final foam value
    foamData.foamValue = surfaceFoam * foamMask.x * _WindFoamAttenuation * foamTex;

    // Evaluate the foam mask
    float foamBlend = saturate(foamData.foamValue * 10.0f);

    // Combine it with the regular surface gradient
    foamData.surfaceGradient = lerp(surfaceGradient, lowFrequencySurfaceGradient, foamBlend);

    // Blend the smoothness of the water and the foam
    foamData.smoothness = lerp(_WaterSmoothness, _SimulationFoamSmoothness, foamBlend);
}

#define WATER_BACKGROUND_ABSORPTION_DISTANCE 1000.f

float EvaluateHeightBasedScattering(float lowFrequencyHeight)
{
    float heightBasedScattering = lerp(1.0, 0.01, saturate(lowFrequencyHeight));
    return lerp(1.0, heightBasedScattering, _HeightBasedScattering);
}

float EvaluateDisplacementScattering(float displacement)
{
    float displacementScattering = lerp(1.0, 0.01, displacement / max(max(_WaveAmplitude.x, _WaveAmplitude.y), 1.0));
    return lerp(1.0, displacementScattering, _DisplacementScattering);
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

float3 EvaluateCausticsUV(float3 causticPosAWS)
{
    return (causticPosAWS) * _CausticsTiling + float3(_CausticsOffset.x, 0.0, _CausticsOffset.y);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

float EvaluateSimulationCaustics(float3 refractedWaterPosRWS, float3 waterPositionRWS, float2 distortedWaterNDC)
{
    // Convert the position to absolute world space
    float3 causticPosAWS = GetAbsolutePositionWS(refractedWaterPosRWS);

    // Evaluate the caustics weight
    float causticDepth = abs(waterPositionRWS.y - refractedWaterPosRWS.y);
    float causticWeight = saturate(causticDepth / _CausticsPlaneBlendDistance);

    // Evaluate the normal of the surface (using partial derivatives of the absolute world pos is not possible as it is not stable enough)
    NormalData normalData;
    float4 normalBuffer = LOAD_TEXTURE2D_X_LOD(_NormalBufferTexture, distortedWaterNDC * _ScreenSize.xy, 0);
    DecodeFromNormalBuffer(normalBuffer, normalData);
    float3 triplanarW = ComputeTriplanarWeights(normalData.normalWS);

    // Will hold the results of the caustics evaluation
    float3 causticsValues = 0.0;

    // TODO: Is this worth a multicompile?
    if (_WaterCausticsType == 0)
    {
        // Evaluate the triplanar coodinates
        float3 sampleCoord = causticPosAWS / (_CausticsRegionSize * 0.5) + 0.5;
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(sampleCoord, uv0, uv1, uv2);

        // Evaluate the sharpness of the caustics based on the depth
        float sharpness = (1.0 - causticWeight) * 2;

        // sample the caustics texture
        causticsValues.x = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv0, sharpness).x;
        causticsValues.y = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv1, sharpness).x;
        causticsValues.z = SAMPLE_TEXTURE2D_LOD(_WaterCausticsDataBuffer, s_linear_repeat_sampler, uv2, sharpness).x;
    }
    else
    {
        // Evaluate the triplanar coodinates
        float3 causticsUV = EvaluateCausticsUV(causticPosAWS);
        float2 uv0, uv1, uv2;
        GetTriplanarCoordinate(causticsUV, uv0, uv1, uv2);

        // Evaluate the caustic s
        causticsValues.x = VoronoiNoise(uv0);
        causticsValues.y = VoronoiNoise(uv1);
        causticsValues.z = VoronoiNoise(uv2);
    }

    // Evaluate the triplanar weights and blend the samples togheter
    return 1.0 + lerp(0, causticsValues.x * triplanarW.y + causticsValues.y * triplanarW.z + causticsValues.z * triplanarW.x, causticWeight) * _CausticsIntensity;
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
    if (aboveWater)
        refractedView = lerp(refractionNormal, float3(0, 1, 0), EdgeBlendingFactor(screenUV, length(waterPosRWS))) * float3(1, 0, 1);
    else
        refractedView = refract(-viewWS, refractionNormal, WATER_IOR);

    // Evaluate the refracted point
    float3 distortedWaterWS = waterPosRWS + refractedView * min(underWaterDistance, maxRefractionDistance);
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
    float3 lowFreqeuncyRefractedRay = refract(-viewWS, lowFrequencyNormals, WATER_INV_IOR);
    return GetWaveTipThickness(max(0.01, lowFrequencyHeight), viewWS, lowFreqeuncyRefractedRay);
}

float3 EvaluateRefractionColor(float3 absorptionTint, float3 caustics)
{
    // Evaluate the refraction color (we need to account for the initial absoption (light to underwater))
    return absorptionTint * caustics * absorptionTint;
}

float3 EvaluateScatteringColor(float sssMask, float lowFrequencyHeight, float horizontalDisplacement, float3 absorptionTint, float deepFoam)
{
    // Evlaute the scattering color (where the refraction doesn't happen)
    float heightBasedScattering = EvaluateHeightBasedScattering(lowFrequencyHeight);
    float displacementScattering = EvaluateDisplacementScattering(horizontalDisplacement);
    float3 scatteringCoefficients = (displacementScattering + heightBasedScattering) * (1.f - _ScatteringColorTips.rgb);
    float3 scatteringTint = _ScatteringColorTips.xyz * exp(-scatteringCoefficients) ;
    float lambertCompensation = lerp(_ScatteringLambertLighting.z, _ScatteringLambertLighting.w, sssMask);
    return scatteringTint * (1.f - absorptionTint) * lambertCompensation * (1.0 + deepFoam);
}
#endif // WATER_UTILITIES_H
