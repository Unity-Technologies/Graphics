// These values are chosen so that an iFFT patch of 1000km^2 will
// yield a Phillips spectrum distribution in the [-1, 1] range
#define EARTH_GRAVITY 9.81
#define ONE_OVER_SQRT2 0.70710678118
#define PHILLIPS_PATCH_SCALAR 500.0
#define PHILLIPS_AMPLITUDE_SCALAR 10.0
#define WATER_IOR 1.3333
#define WATER_INV_IOR 1.0 / WATER_IOR

// Water simulation data
Texture2DArray<float4> _WaterDisplacementBuffer;
Texture2DArray<float4> _WaterAdditionalDataBuffer;

// Water mask
Texture2D<float2> _WaterMask;

// Foam textures
Texture2D<float> _FoamTexture;
Texture2D<float4> _FoamNormal;
Texture2D<float2> _FoamMask;

// This array converts an index to the local coordinate shift of the half resolution texture
static const float2 vertexPostion[4] = {float2(0, 0), float2(0, 1), float2(1, 1), float2(1, 0)};
static const uint triangleIndices[6] = {0, 1, 2, 0, 2, 3};

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

float4 GenerateRandomNumbers(uint3 currentThread)
{
    // Generate all the required noise samples
    float u0 = GetBNDSequenceSample(currentThread.xy, 0 + currentThread.z * 2, 0);
    float v0 = GetBNDSequenceSample(currentThread.xy, 0 + currentThread.z * 2, 1);
    float u1 = GetBNDSequenceSample(currentThread.xy, 1 + currentThread.z * 2, 0);
    float v1 = GetBNDSequenceSample(currentThread.xy, 1 + currentThread.z * 2, 1);
    return float4(u0, v0, u1, v1);
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
    float2 uv = positionWS.xz;
    uv /= _BandPatchSize.x;

    float R0 = _BandPatchUVScale.x;
    float O0 = 0 / R0;
    waterCoord.uvBand0 = ((uv + O0) * R0);

    float R1 = _BandPatchUVScale.y;
    float O1 = 0.5f / R1;
    waterCoord.uvBand1 = ((uv + O1) * R1);

    float R2 = _BandPatchUVScale.z;
    float O2 = 0.25 / R2;
    waterCoord.uvBand2 = ((uv + O2) * R2);

    float R3 = _BandPatchUVScale.w;
    float O3 = 0.125 / R3;
    waterCoord.uvBand3 = ((uv + O3) * R3);
}

#if !defined(WATER_SIMULATION)
// Fast random hash function
float2 SimpleHash2(float2 p)
{
    return frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), p)) * 43758.5453);
}

// Compute local triangle barycentric coordinates and vertex IDs
void TriangleGrid(float2 uv, out float w1, out float w2, out float w3, out int2 vertex1, out int2 vertex2, out int2 vertex3)
{
    // Scaling of the input
    uv *= 3.464; // 2 * sqrt(3)
    // Skew input space into simplex triangle grid
    const float2x2 gridToSkewedGrid = float2x2(1.0, -0.57735027, 0.0, 1.15470054);
    float2 skewedCoord = mul(gridToSkewedGrid, uv);
    // Compute local triangle vertex IDs and local barycentric coordinates
    int2 baseId = int2(floor(skewedCoord));
    float3 temp = float3(frac(skewedCoord), 0);
    temp.z = 1.0 - temp.x - temp.y;
    if (temp.z > 0.0)
    {
        w1 = temp.z;
        w2 = temp.y;
        w3 = temp.x;
        vertex1 = baseId;
        vertex2 = baseId + int2(0, 1);
        vertex3 = baseId + int2(1, 0);
    }
    else
    {
        w1 = -temp.z;
        w2 = 1.0 - temp.y;
        w3 = 1.0 - temp.x;
        vertex1 = baseId + int2(1, 1);
        vertex2 = baseId + int2(1, 0);
        vertex3 = baseId + int2(0, 1);
    }
}

// Sample by-example procedural noise at uv on decorrelated input
float DecorrelatedStochasticSample_R(float2 uv, Texture2D<float> Tinput)
{
    // Get triangle info
    float w1, w2, w3;
    int2 vertex1, vertex2, vertex3;
    TriangleGrid(uv, w1, w2, w3, vertex1, vertex2, vertex3);

    // Assign random offset to each triangle vertex
    float2 uv1 = uv + SimpleHash2(vertex1);
    float2 uv2 = uv + SimpleHash2(vertex2);
    float2 uv3 = uv + SimpleHash2(vertex3);

    // Precompute UV derivatives
    float2 duvdx = ddx(uv);
    float2 duvdy = ddy(uv);

    // Fetch Gaussian input
    float G1 = Tinput.SampleGrad(s_linear_repeat_sampler, uv1, duvdx, duvdy);
    float G2 = Tinput.SampleGrad(s_linear_repeat_sampler, uv2, duvdx, duvdy);
    float G3 = Tinput.SampleGrad(s_linear_repeat_sampler, uv3, duvdx, duvdy);

    // Variance-preserving blending
    float G = w1 * G1 + w2 * G2 + w3 * G3;
    G = G - 0.5;
    G = G * rsqrt(w1 * w1 + w2 * w2 + w3 * w3);
    G = G + 0.5;
    return G;
}

// Sample by-example procedural noise at uv on decorrelated input
float3 DecorrelatedStochasticSample(float2 uv, Texture2D<float4> Tinput)
{
    // Get triangle info
    float w1, w2, w3;
    int2 vertex1, vertex2, vertex3;
    TriangleGrid(uv, w1, w2, w3, vertex1, vertex2, vertex3);

    // Assign random offset to each triangle vertex
    float2 uv1 = uv + SimpleHash2(vertex1);
    float2 uv2 = uv + SimpleHash2(vertex2);
    float2 uv3 = uv + SimpleHash2(vertex3);

    // Precompute UV derivatives
    float2 duvdx = ddx(uv);
    float2 duvdy = ddy(uv);

    // Fetch Gaussian input
    float3 G1 = Tinput.SampleGrad(s_linear_repeat_sampler, uv1, duvdx, duvdy).rgb;
    float3 G2 = Tinput.SampleGrad(s_linear_repeat_sampler, uv2, duvdx, duvdy).rgb;
    float3 G3 = Tinput.SampleGrad(s_linear_repeat_sampler, uv3, duvdx, duvdy).rgb;

    // Variance-preserving blending
    float3 G = w1 * G1 + w2 * G2 + w3 * G3;
    G = G - 0.5;
    G = G * rsqrt(w1 * w1 + w2 * w2 + w3 * w3);
    G = G + 0.5;
    return G;
}

#if defined(WATER_PROCEDURAL_GEOMETRY)
float3 GetVertexPositionFromVertexID(uint vertexID)
{
    // Compute the data about the quad of this vertex
    uint quadID = vertexID / 6;
    uint quadX = quadID / _GridRenderingResolution;
    uint quadZ = quadID & (_GridRenderingResolution - 1);

    // Evaluate the local position in the quad of this pixel
    int localVertexID = vertexID % 6;
    float2 localPos = vertexPostion[triangleIndices[localVertexID]];

    // We adjust the vertices if we detect an edge tesselation that is broken
    float xOffset = 0;
    float zOffset = 0;

    // X = 0
    if (quadX == 0 && (_TesselationMasks & 0x01) != 0)
    {
        int quadParity = (quadZ & 1);

        // Killed triangle
        if (quadParity == 0 && localVertexID == 1)
            zOffset = -1;

        // Extended triangles
        if (quadParity == 1 && (localVertexID == 0 || localVertexID == 3))
            zOffset = -1;
    }

    // Z =  0
    if (quadZ == 0 && (_TesselationMasks & 0x08) != 0)
    {
        int quadParity = (quadX & 1);
        // Killed triangle
        if (quadParity == 0 && localVertexID == 5)
            xOffset = -1;

        // Extended triangles
        if (quadParity == 1 && (localVertexID == 3 || localVertexID == 0))
            xOffset = -1;
    }

    // X = _GridRenderingResolution - 1
    if (quadX == (_GridRenderingResolution - 1) && (_TesselationMasks & 0x04) != 0)
    {
        int quadParity = (quadZ & 1);

        // Killed triangles
        if (quadParity == 0 && (localVertexID == 2 || localVertexID == 4))
            zOffset = -1;

        // Extended triangle
        if (quadParity == 1 && localVertexID == 5)
            zOffset = -1;
    }

    // Z = _GridRenderingResolution - 1
    if (quadZ == (_GridRenderingResolution - 1) && (_TesselationMasks & 0x02) != 0)
    {
        int quadParity = (quadX & 1);

        // Killed triangles
        if (quadParity == 0 && (localVertexID == 2 || localVertexID == 4))
            xOffset = -1;

        // Extended triangle
        if (quadParity == 1 && localVertexID == 1)
            xOffset = -1;
    }

    // Compute the position in the vertex (no specific case here)
    float3 worldPos = float3(localPos.x + quadX + xOffset, 0.0, localPos.y + quadZ + zOffset);

    // Normalize the coordinates
    worldPos.x = (worldPos.x - _GridRenderingResolution / 2) / _GridRenderingResolution;
    worldPos.z = (worldPos.z - _GridRenderingResolution / 2) / _GridRenderingResolution;
#else
float3 GetVertexPositionFromVertexID(uint vertexID, float3 positionOS)
{
    // Use the input position as the world space position
    float3 worldPos = positionOS;
#endif

    // Scale the position by the size of the grid
    worldPos.x *= _GridSize.x;
    worldPos.z *= _GridSize.y;

    worldPos = float3(worldPos.x * _WaterRotation.x - worldPos.z * _WaterRotation.y, worldPos.y, worldPos.x * _WaterRotation.y + worldPos.z * _WaterRotation.x);

    // Offset the tile and place it under the camera's relative position
    worldPos += float3(_PatchOffset.x, _PatchOffset.y, _PatchOffset.z);

    // Curve the water so that it fits the planetary shape
    float3 relativeWorldPos = worldPos - _WorldSpaceCameraPos;
    float L = sqrt(_EarthRadius * _EarthRadius + relativeWorldPos.x * relativeWorldPos.x + relativeWorldPos.z * relativeWorldPos.z) - _EarthRadius;
    float3 toEarthCenterVector = normalize(float3(0, -_EarthRadius , 0) - relativeWorldPos);
    worldPos += L * toEarthCenterVector;

    // Return the final world space position
    return worldPos;
}

struct WaterDisplacementData
{
    float3 displacement;
    float3 displacementNoChopiness;
    float lowFrequencyHeight;
    float sssMask;
};

float EvaluateSSSMask(float3 positionWS, float3 cameraPositionWS)
{
    float3 viewWS = normalize(cameraPositionWS - positionWS);
    float distanceToCamera = distance(cameraPositionWS, positionWS);
    float angleWithWaterPlane = pow(saturate(viewWS.y), .2);
    return (1.f - exp(-distanceToCamera * _SSSMaskCoefficient)) * angleWithWaterPlane;
}

void EvaluateWaterDisplacement(float3 positionAWS, out WaterDisplacementData displacementData)
{
    // Compute the simulation coordinates
    WaterSimulationCoordinates waterCoord;
    ComputeWaterUVs(positionAWS, waterCoord);

    // Compute the displacement normalization factor
    float4 patchSizes = _BandPatchSize / _BandPatchSize[0];
    float4 patchSizes2 = patchSizes * patchSizes;
    float4 displacementNormalization = _WaveAmplitude / patchSizes * PHILLIPS_AMPLITUDE_SCALAR;

    // Accumulate the displacement from the various layers
    float3 totalDisplacement = 0.0;
    float3 totalDisplacementNoChopiness = 0.0;
    float lowFrequencyHeight = 0.0;
    float normalizedDisplacement = 0.0;

    // Attenuate using the water mask
    float2 waterMask = SAMPLE_TEXTURE2D_LOD(_WaterMask, s_linear_clamp_sampler, float2(positionAWS.x - _WaterMaskOffset.x, positionAWS.z - _WaterMaskOffset.y) * _WaterMaskScale + 0.5f, 0);

    // First band
    float3 rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand0, 0, 0).xyz * displacementNormalization.x * waterMask.x;
    totalDisplacementNoChopiness += rawDisplacement;
    totalDisplacement += float3(rawDisplacement.x, rawDisplacement.yz);
    lowFrequencyHeight += rawDisplacement.x;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.x;

    // Second band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand1, 1, 0).xyz * displacementNormalization.y * waterMask.x;
    totalDisplacementNoChopiness += rawDisplacement;
    totalDisplacement += float3(rawDisplacement.x, rawDisplacement.yz);
    lowFrequencyHeight += rawDisplacement.x;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.y;

#if defined(HIGH_RESOLUTION_WATER)
    // Third band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand2, 2, 0).xyz * displacementNormalization.z * waterMask.y;
    totalDisplacementNoChopiness += rawDisplacement;
    totalDisplacement += float3(rawDisplacement.x, rawDisplacement.yz);
    lowFrequencyHeight += rawDisplacement.x * 0.5;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.z;

    // Fourth band
    rawDisplacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_WaterDisplacementBuffer, s_linear_repeat_sampler, waterCoord.uvBand3, 3, 0).xyz * displacementNormalization.w * waterMask.y;
    totalDisplacementNoChopiness += rawDisplacement;
    normalizedDisplacement = rawDisplacement.x / patchSizes2.w;
#endif

    // Apply the choppiness modification
    totalDisplacement.yz *= _Choppiness;

    // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
    displacementData.displacement = float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);
    displacementData.displacementNoChopiness = float3(-totalDisplacementNoChopiness.y, totalDisplacementNoChopiness.x - positionAWS.y, -totalDisplacementNoChopiness.z);
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

void PackWaterVertexData(float3 positionAWS, float3 displacement, float3 displacementNoChopiness, float lowFrequencyHeight, float customFoam, float sssMask, out PackedWaterData packedWaterData)
{
    packedWaterData.positionOS = positionAWS + displacement;
    packedWaterData.normalOS = float3(0, 1, 0);
    packedWaterData.uv0 = float4(positionAWS + displacementNoChopiness, 0.0);
    packedWaterData.uv1 = float4(lowFrequencyHeight, customFoam, sssMask, length(float2(displacementNoChopiness.x, displacementNoChopiness.z)));
}

struct WaterAdditionalData
{
    float3 surfaceGradient;
    float3 lowFrequencySurfaceGradient;
    float2 simulationFoam;
};

void EvaluateWaterAdditionalData(float3 positionAWS, out WaterAdditionalData waterAdditionalData)
{
    // Compute the simulation coordinates
    WaterSimulationCoordinates waterCoord;
    ComputeWaterUVs(positionAWS, waterCoord);

    // Compute the texture size param for the filtering
    float4 texSize = 0.0;
    texSize.xy = _BandResolution;
    texSize.zw = 1.0f / _BandResolution;

    // Attenuate using the water mask
    float2 waterMask = SAMPLE_TEXTURE2D_LOD(_WaterMask, s_linear_clamp_sampler, float2(positionAWS.x - _WaterMaskOffset.x, positionAWS.z - _WaterMaskOffset.y) * _WaterMaskScale + 0.5f, 0);

    // First band
    float4 additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand0, 0, texSize) * waterMask.x;
    float3 surfaceGradient = float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.x;
    float3 lowFrequencySurfaceGradient = surfaceGradient;
    float lowSurfaceFoam = additionalData.z * saturate(_WaveAmplitude.x);
    float deepFoam = additionalData.w * saturate(_WaveAmplitude.x);

    // Second band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand1, 1, texSize) * waterMask.x;
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.y;
    lowFrequencySurfaceGradient += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.y;
    lowSurfaceFoam += additionalData.z;
    deepFoam += additionalData.w * saturate(_WaveAmplitude.y);

#if defined(HIGH_RESOLUTION_WATER)
    // Third band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand2, 2, texSize)* waterMask.y;
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.z;

    // Fourth band
    additionalData = SampleTexture2DArrayBicubic(TEXTURE2D_ARRAY_ARGS(_WaterAdditionalDataBuffer, s_linear_repeat_sampler), waterCoord.uvBand3, 3, texSize) * waterMask.y;
    surfaceGradient += float3(additionalData.x, 0, additionalData.y) * _WaveAmplitude.w;
#endif

    // Blend the various surface gradients
    waterAdditionalData.surfaceGradient = surfaceGradient;
    waterAdditionalData.lowFrequencySurfaceGradient = lowFrequencySurfaceGradient;
    waterAdditionalData.simulationFoam = float2(deepFoam, lowSurfaceFoam);
}

float3 ComputeDebugNormal(float3 worldPos)
{
    float3 worldPosDdx = normalize(ddx(worldPos));
    float3 worldPosDdy = normalize(ddy(worldPos));
    return normalize(-cross(worldPosDdx, worldPosDdy));
}

float2 EvaluateFoamUV(float3 positionAWS)
{
    return (positionAWS.xz + _FoamOffsets.xy) * _FoamTilling;
}

struct FoamData
{
    float smoothness;
    float3 foamValue;
    float3 surfaceGradient;
};

void EvaluateFoamData(float3 surfaceGradient, float3 lowFrequencySurfaceGradient,
    float2 simulationFoam, float lowFrequencyHeight, float customFoam,
    float3 normalWS, float3 positionAWS, out FoamData foamData)
{
    // Attenuate using the foam mask
    float2 foamMask = SAMPLE_TEXTURE2D(_FoamMask, s_linear_clamp_sampler, float2(positionAWS.x - _FoamMaskOffset.x, positionAWS.z - _FoamMaskOffset.y) * _FoamMaskScale + 0.5f).xy;

    // Compute the surface foam
    float2 foamUV = EvaluateFoamUV(positionAWS);
    float foamTex = DecorrelatedStochasticSample_R(foamUV, _FoamTexture);
    float3 foamNormal = DecorrelatedStochasticSample(foamUV, _FoamNormal);

    // We want less details in the top of the waves and more when we go down
    foamTex = PositivePow(foamTex, lerp(2.0, 0.75, saturate(lowFrequencyHeight)));

    // Compute the deep foam color
    float3 deepFoam = _DeepFoamAmount * simulationFoam.x * lerp(0.5, 0.8, saturate(lowFrequencyHeight)) * foamMask.x * _DeepFoamColor;

    // Compute the top foam color
    float topFoam = saturate((simulationFoam.y + customFoam) * foamMask.y * _SurfaceFoamIntensity * _WindFoamAttenuation * foamTex);

    // Transition between water and foam
    float foamTransition = saturate(topFoam * 4.0f);

    // Fix the normal, remove this
    float3 surfaceFoamNormals = foamNormal.xyz;
    surfaceFoamNormals -= float3(0.5, 0.5, 0);
    surfaceFoamNormals = normalize(surfaceFoamNormals.xzy);

    // Compute the surface gradient of the foam
    float3 foamSurfaceGradient = SurfaceGradientFromPerturbedNormal(normalWS, surfaceFoamNormals) + lowFrequencySurfaceGradient;

    // Combine it with the regular surface gradient
    foamData.surfaceGradient = lerp(surfaceGradient, foamSurfaceGradient, foamTransition);

    // Blend the smoothness of the water and the foam
    foamData.smoothness = lerp(_WaterSmoothness, _FoamSmoothness, foamTransition);

    // Final foam value
    foamData.foamValue = saturate(deepFoam + topFoam);
}

#define WATER_BACKGROUND_ABSORPTION_DISTANCE 1000.f

struct ScatteringData
{
    float3 scatteringColor;
    float3 refractionColor;
    float tipThickness;
};

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
    return 1.f - saturate(1 + refractedRay.y - 0.2) * pow(H, 2) / 0.4;
}

float2 EvaluateCausticsUV(float3 causticPosAWS)
{
    return (causticPosAWS.xz) * _CausticsTiling + _CausticsOffset.xy;
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
    return sin(TWO_PI * position + _TimeParameters.x) * 0.45 + 0.5;
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

    // Fake fast gamma
    float minDist = abs(minDistToCell);
    return minDist * minDist;
}

float EvaluateCaustics(float3 bedPositionAWS)
{
    float2 causticsUV = EvaluateCausticsUV(bedPositionAWS);
    float normalizedDepth = saturate((_PatchOffset.y - bedPositionAWS.y - _CausticsPlaneOffset));
    return  VoronoiNoise(causticsUV) * normalizedDepth * _CausticsIntensity;
}

void EvaluateScatteringData(float3 waterPosRWS, float3 waterNormal, float3 lowFrequencyNormals,
    float2 screenPosition, float3 viewWS,
    float sssMask, float lowFrequencyHeight, float lowFrequencyDisplacement, float foamIntensity,
    out ScatteringData scatteringData)
{
    // Compute the position of the surface behind the water surface
    float  directWaterDepth = SampleCameraDepth(screenPosition);
    float3 directWaterPosRWS = ComputeWorldSpacePosition(screenPosition, directWaterDepth, UNITY_MATRIX_I_VP);

    // Compute the distance between the water surface and the object behind
    float underWaterDistance = directWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(directWaterPosRWS - waterPosRWS);

    // Blend both normals to decide what normal will be used for the refraction
    float3 refractionNormal = normalize(lerp(waterNormal, lowFrequencyNormals, saturate(underWaterDistance / _MaxRefractionDistance)));

    // Compute the distorded water position and NDC
    float3 distortionNormal = refractionNormal * float3(1, 0, 1); // I guess this is a refract?
    float3 distortedWaterWS = waterPosRWS + distortionNormal * min(underWaterDistance, _MaxRefractionDistance);
    float2 distortedWaterNDC = ComputeNormalizedDeviceCoordinates(distortedWaterWS, UNITY_MATRIX_VP);

    // Compute the position of the surface behind the water surface
    float refractedWaterDepth = SampleCameraDepth(distortedWaterNDC);
    float3 refractedWaterPosRWS = ComputeWorldSpacePosition(distortedWaterNDC, refractedWaterDepth, UNITY_MATRIX_I_VP);
    float refractedWaterDistance = refractedWaterDepth == UNITY_RAW_FAR_CLIP_VALUE ? WATER_BACKGROUND_ABSORPTION_DISTANCE : length(refractedWaterPosRWS - waterPosRWS);

    // Evaluate the distorded under water color
    float3 refractedView = refract(-viewWS, refractionNormal, 1.0 / WATER_IOR);

    // If the point that we are reading is closer than the
    if (dot(refractedWaterPosRWS - waterPosRWS, viewWS) > 0.0)
    {
        // We read the direct depth (no refraction)
        refractedWaterDistance = underWaterDistance;
        // We kill the refraction and take the straight ray.
        distortedWaterNDC = screenPosition;
        // The refracted view is now straight
        refractedView = -viewWS;
    }

    // Evaluate the absorption tint
    float3 absorptionCoefficients = refractedWaterDistance * _OutScatteringCoefficient * (1.f - _TransparencyColor);
    float3 absorptionTint = exp(-absorptionCoefficients);

    // Evlaute the scattering color (where the refraction doesn't happen)
    float heightBasedScattering = EvaluateHeightBasedScattering(lowFrequencyHeight);
    float displacementScattering = EvaluateDisplacementScattering(lowFrequencyDisplacement);
    float3 scatteringCoefficients = (displacementScattering * heightBasedScattering) * (1.f - _ScatteringColorTips.rgb);
    float3 scatteringTint = _ScatteringColorTips * exp(-scatteringCoefficients);
    float lambertCompensation = lerp(_ScatteringLambertLighting.z, _ScatteringLambertLighting.w, sssMask);
    scatteringData.scatteringColor =  scatteringTint * (1.f - absorptionTint) * lambertCompensation * _ScatteringIntensity * (1.0 - foamIntensity);

    // Compute how deep the ray travels (in the [0, 1] space)
    float normalizedTravelLength = saturate(underWaterDistance / _MaxAbsorptionDistance);

    // Evaluate the blur that is applied to the underwater signal
    float blurLod = saturate(refractedWaterDistance / _MaxAbsorptionDistance);

    // Evaluate the refracted camera color
    float3 cameraColor = LoadCameraColor(distortedWaterNDC * _ScreenSize.xy, 0);

    float3 causticPosAWS = GetAbsolutePositionWS(waterPosRWS + refractedView * underWaterDistance);
    float causticsIntensity = EvaluateCaustics(causticPosAWS);

    // Evaluate the refraction color (we need to account for the initial absoption (light to underwater))
    scatteringData.refractionColor = cameraColor * (absorptionTint + causticsIntensity) * absorptionTint * GetInverseCurrentExposureMultiplier() * (1.0 - foamIntensity);

    // Compute the tip thickness
    float3 lowFreqeuncyRefractedRay = refract(-viewWS, lowFrequencyNormals, WATER_INV_IOR);
    scatteringData.tipThickness = GetWaveTipThickness(max(0.01, lowFrequencyHeight), viewWS, lowFreqeuncyRefractedRay);
}
#endif
