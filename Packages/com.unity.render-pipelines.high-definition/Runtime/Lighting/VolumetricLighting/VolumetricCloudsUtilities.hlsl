#ifndef  VOLUMETRIC_CLOUD_UTILITIES_H
#define VOLUMETRIC_CLOUD_UTILITIES_H

// Maximal length of the cloud ray
#define MAX_CLOUD_RAY_LENGTH 200000

// Implementation inspired from https://www.shadertoy.com/view/3dVXDc
// Hash by David_Hoskins
#define UI0 1597334673U
#define UI1 3812015801U
#define UI2 uint2(UI0, UI1)
#define UI3 uint3(UI0, UI1, 2798796415U)
#define UIF (1.0 / float(0xffffffffU))

float3 hash33(float3 p)
{
    uint3 q = uint3(int3(p)) * UI3;
    q = (q.x ^ q.y ^ q.z) * UI3;
    return -1. + 2. * float3(q) * UIF;
}

// Density remapping function
float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

// Gradient noise by iq (modified to be tileable)
float GradientNoise(float3 x, float freq)
{
    // grid
    float3 p = floor(x);
    float3 w = frac(x);

    // quintic interpolant
    float3 u = w * w * w * (w * (w * 6. - 15.) + 10.);

    // gradients
    float3 ga = hash33(fmod(p + float3(0., 0., 0.), freq));
    float3 gb = hash33(fmod(p + float3(1., 0., 0.), freq));
    float3 gc = hash33(fmod(p + float3(0., 1., 0.), freq));
    float3 gd = hash33(fmod(p + float3(1., 1., 0.), freq));
    float3 ge = hash33(fmod(p + float3(0., 0., 1.), freq));
    float3 gf = hash33(fmod(p + float3(1., 0., 1.), freq));
    float3 gg = hash33(fmod(p + float3(0., 1., 1.), freq));
    float3 gh = hash33(fmod(p + float3(1., 1., 1.), freq));

    // projections
    float va = dot(ga, w - float3(0., 0., 0.));
    float vb = dot(gb, w - float3(1., 0., 0.));
    float vc = dot(gc, w - float3(0., 1., 0.));
    float vd = dot(gd, w - float3(1., 1., 0.));
    float ve = dot(ge, w - float3(0., 0., 1.));
    float vf = dot(gf, w - float3(1., 0., 1.));
    float vg = dot(gg, w - float3(0., 1., 1.));
    float vh = dot(gh, w - float3(1., 1., 1.));

    // interpolation
    return va +
        u.x * (vb - va) +
        u.y * (vc - va) +
        u.z * (ve - va) +
        u.x * u.y * (va - vb - vc + vd) +
        u.y * u.z * (va - vc - ve + vg) +
        u.z * u.x * (va - vb - ve + vf) +
        u.x * u.y * u.z * (-va + vb + vc - vd + ve - vf - vg + vh);
}

// There is a difference between the original implementation's mod and hlsl's fmod, so we mimic the glsl version for the algorithm
#define Modulo(x,y) (x-y*floor(x/y))
// Tileable 3D worley noise
float WorleyNoise(float3 uv, float freq)
{
    float3 id = floor(uv);
    float3 p = frac(uv);

    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for (float y = -1.; y <= 1.; ++y)
        {
            for (float z = -1.; z <= 1.; ++z)
            {
                float3 offset = float3(x, y, z);
                float3 idOffset = id + offset;
                float3 h = hash33(Modulo(idOffset.xyz, freq)) * .5 + .5;
                h += offset;
                float3 d = p - h;
                minDist = min(minDist, dot(d, d));
            }
        }
    }
    return minDist;
}

float EvaluatePerlinFractalBrownianMotion(float3 position, float initialFrequence, int numOctaves)
{
    const float G = exp2(-0.85);

    // Accumulation values
    float amplitude = 1.0;
    float frequence = initialFrequence;
    float result = 0.0;

    for (int i = 0; i < numOctaves; ++i)
    {
        result += amplitude * GradientNoise(position * frequence, frequence);
        frequence *= 2.0;
        amplitude *= G;
    }
    return result;
}

// Real-time only code
#ifdef REAL_TIME_VOLUMETRIC_CLOUDS

float HenyeyGreenstein(float cosAngle, float g)
{
    // There is a mistake in the GPU Gem7 Paper, the result should be divided by 1/(4.PI)
    float g2 = g * g;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / PositivePow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
}

float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

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

float ConvertCloudDepth(float3 position)
{
    float4 hClip = TransformWorldToHClip(position);
    return hClip.z / hClip.w;
}

// Given that the sky is virtually a skybox, we cannot use the motion vector buffer
float2 EvaluateCloudMotionVectors(float2 fullResCoord, float deviceDepth, float positionFlag)
{
    PositionInputs posInput = GetPositionInput(fullResCoord, _ScreenSize.zw, deviceDepth, _IsPlanarReflection ? _CameraInverseViewProjection_NO : UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    float4 worldPos = float4(posInput.positionWS, positionFlag);
    float4 prevPos = worldPos;

    float4 prevClipPos = mul(_IsPlanarReflection ? _CameraPrevViewProjection_NO : UNITY_MATRIX_PREV_VP, prevPos);
    float4 curClipPos = mul(_IsPlanarReflection ?  _CameraViewProjection_NO: UNITY_MATRIX_UNJITTERED_VP, worldPos);

    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;

    // Convert from Clip space (-1..1) to NDC 0..1 space
    float2 velocity = (positionCS - previousPositionCS) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
#endif
    return velocity;
}

// This function compute the checkerboard undersampling position
int ComputeCheckerBoardIndex(int2 traceCoord, int subPixelIndex)
{
    int localOffset = (traceCoord.x & 1 + traceCoord.y & 1) & 1;
    int checkerBoardLocation = (subPixelIndex + localOffset) & 0x3;
    return checkerBoardLocation;
}

// Our dispatch is a 8x8 tile. We can access up to 3x3 values at dispatch's half resolution
// around the center pixel which represents a total of 36 uniques values for the tile.
groupshared float gs_cacheR[36];
groupshared float gs_cacheG[36];
groupshared float gs_cacheB[36];
groupshared float gs_cacheA[36];
groupshared float gs_cacheDP[36];
groupshared float gs_cacheDC[36];
groupshared float gs_cachePS[36];

uint2 HalfResolutionIndexToOffset(uint index)
{
    return uint2(index & 0x1, index / 2);
}

uint OffsetToLDSAdress(uint2 groupThreadId, int2 offset)
{
    // Compute the tap coordinate in the 6x6 grid
    uint2 tapAddress = (uint2)((int2)(groupThreadId / 2 + 1) + offset);
    return clamp((uint)(tapAddress.x) % 6 + tapAddress.y * 6, 0, 35);
}

float GetCloudDepth_LDS(uint2 groupThreadId, int2 offset)
{
    return gs_cacheDC[OffsetToLDSAdress(groupThreadId, offset)];
}

float4 GetCloudLighting_LDS(uint2 groupThreadId, int2 offset)
{
    uint ldsTapAddress = OffsetToLDSAdress(groupThreadId, offset);
    return float4(gs_cacheR[ldsTapAddress], gs_cacheG[ldsTapAddress], gs_cacheB[ldsTapAddress], gs_cacheA[ldsTapAddress]);
}

struct CloudReprojectionData
{
    float4 cloudLighting;
    float pixelDepth;
    float cloudDepth;
};

CloudReprojectionData GetCloudReprojectionDataSample(uint index)
{
    CloudReprojectionData outVal;
    outVal.cloudLighting.r = gs_cacheR[index];
    outVal.cloudLighting.g = gs_cacheG[index];
    outVal.cloudLighting.b = gs_cacheB[index];
    outVal.cloudLighting.a = gs_cacheA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudReprojectionData GetCloudReprojectionDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudReprojectionDataSample(OffsetToLDSAdress(groupThreadId, offset));
}

// Function that fills the struct as we cannot use arrays
void FillCloudReprojectionNeighborhoodData_NOLDS(int2 traceCoord, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    neighborhoodData.lowValue0 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, -1));
    neighborhoodData.lowValue1 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, -1));
    neighborhoodData.lowValue2 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, -1));

    neighborhoodData.lowValue3 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, 0));
    neighborhoodData.lowValue4 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, 0));
    neighborhoodData.lowValue5 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, 0));

    neighborhoodData.lowValue6 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, 1));
    neighborhoodData.lowValue7 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, 1));
    neighborhoodData.lowValue8 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, 1));

    int2 traceTapCoord = traceCoord + int2(-1, -1);
    int checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    int2 representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.x = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    traceTapCoord = traceCoord + int2(0, -1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.y = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    traceTapCoord = traceCoord + int2(1, -1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.z = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    traceTapCoord = traceCoord + int2(-1, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.w = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    traceTapCoord = traceCoord + int2(0, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.x = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    traceTapCoord = traceCoord + int2(1, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.y = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    traceTapCoord = traceCoord + int2(-1, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.z = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    traceTapCoord = traceCoord + int2(0, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.w = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    traceTapCoord = traceCoord + int2(1, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthC = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;

    // In the reprojection case, all masks are valid
    neighborhoodData.lowMasksA = 1.0f;
    neighborhoodData.lowMasksB = 1.0f;
    neighborhoodData.lowMasksC = 1.0f;
}

// Function that fills the struct as we cannot use arrays
void FillCloudReprojectionNeighborhoodData(int2 groupThreadId, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudReprojectionData data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;

    // In the reprojection case, all masks are valid
    neighborhoodData.lowMasksA = 1.0f;
    neighborhoodData.lowMasksB = 1.0f;
    neighborhoodData.lowMasksC = 1.0f;
}

struct CloudUpscaleData
{
    float4 cloudLighting;
    float pixelDepth;
    float pixelStatus;
    float cloudDepth;
};

CloudUpscaleData GetCloudUpscaleDataSample(uint index)
{
    CloudUpscaleData outVal;
    outVal.cloudLighting.r = gs_cacheR[index];
    outVal.cloudLighting.g = gs_cacheG[index];
    outVal.cloudLighting.b = gs_cacheB[index];
    outVal.cloudLighting.a = gs_cacheA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.pixelStatus = gs_cachePS[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudUpscaleData GetCloudUpscaleDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudUpscaleDataSample(OffsetToLDSAdress(groupThreadId, offset));
}

// Function that fills the struct as we cannot use arrays
void FillCloudUpscaleNeighborhoodData(int2 groupThreadId, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudUpscaleData data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowMasksA.x = data.pixelStatus;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowMasksA.y = data.pixelStatus;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowMasksA.z = data.pixelStatus;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowMasksA.w = data.pixelStatus;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowMasksB.x = data.pixelStatus;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowMasksB.y = data.pixelStatus;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowMasksB.z = data.pixelStatus;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowMasksB.w = data.pixelStatus;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowMasksC = data.pixelStatus;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;
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
#endif // REAL_TIME_VOLUMETRIC_CLOUDS

#endif // VOLUMETRIC_CLOUD_UTILITIES_H
