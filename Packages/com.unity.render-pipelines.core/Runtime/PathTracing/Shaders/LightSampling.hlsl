#ifndef _PATHTRACING_LIGHTSAMPLING_HLSL_
#define _PATHTRACING_LIGHTSAMPLING_HLSL_

#include "PathTracingCommon.hlsl"
#include "PathTracingMaterials.hlsl"
#include "PathTracingLightGrid.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/PseudoRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"

#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Environment/EnvironmentImportanceSampling.hlsl"

#define SOLID_ANGLE_SAMPLING
#define TRANSMISSION_IN_SHADOW_RAYS
#define USE_DISTANCE_ATTENUATION_LUT

TextureCube<float4>             g_EnvTex;
SamplerState                    sampler_g_EnvTex;
float                           g_EnvIntensityMultiplier;

int                             g_LightPickingMethod;
StructuredBuffer<PTLight>       g_LightList;
StructuredBuffer<float>         g_LightFalloff;
StructuredBuffer<float>         g_LightFalloffLUTRange;
uint                            g_LightFalloffLUTLength;
uint                            g_NumLights;
uint                            g_NumEmissiveMeshes;

// Light cookies
Texture2DArray<float4>          g_CookieAtlas;
TextureCubeArray<float4>        g_CubemapAtlas;
SamplerState                    sampler_g_CookieAtlas;
SamplerState                    sampler_g_CubemapAtlas;

// Light / reservoir grid
StructuredBuffer<ThinReservoir> g_LightGridCellsData;
StructuredBuffer<uint2>         g_LightGrid;

// Exposure texture - 1x1 RG16F (r: exposure mult, g: exposure EV100)
TEXTURE2D(_ExposureTexture);
int g_PreExpose;
float g_IndirectScale;
float g_ExposureScale;
int g_MaxIntensity;

uint _EnvironmentCdfConditionalResolution;
uint _EnvironmentCdfMarginalResolution;
StructuredBuffer<float> _EnvironmentCdfConditionalBuffer;
StructuredBuffer<float> _EnvironmentCdfMarginalBuffer;

struct LightShapeSample
{
    float3 lightVector;
    float3 L;
    float weight;
    float distanceToLight;
    float2 uv;
    int materialIndex;
};

struct LightSample
{
    uint lightType;
    float3 radiance;
    float3 direction;
    float risSourcePdf;
};

// Copied from com.unity.render-pipelines.high-definition\Runtime\ShaderLibrary\ShaderVariables.hlsl
// URP-only projects will not have this file, so for now copy-pasting here.
float GetCurrentExposureMultiplier()
{
    if (g_PreExpose)
    {
        #ifdef READ_EXPOSURE_FROM_TEXTURE
        // g_ExposureScale is a scale used to perform range compression to avoid saturation of the content of the probes. It is 1.0 if we are not rendering probes.
        return LOAD_TEXTURE2D(_ExposureTexture, int2(0, 0)).x * g_ExposureScale;
        #else
        return g_ExposureScale;
        #endif
    }
    else
    {
        return 1.0;
    }
}

float3 ClampRadiance(float3 value)
{
    float intensity = Luminance(value) * GetCurrentExposureMultiplier();
    return intensity > g_MaxIntensity ? value * g_MaxIntensity / intensity : value;
}

bool CastShadowRay(UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    float3 origin,
    float3 direction,
    float maxDistance,
    uint rayMask,
    out float3 attenuation)
{
    UnifiedRT::Ray shadowRay;
    shadowRay.origin = origin;
    shadowRay.direction = direction;
    shadowRay.tMin = 0.0;
    shadowRay.tMax = maxDistance;

    #ifdef TRANSMISSION_IN_SHADOW_RAYS
    uint rayFlags = 0;
    #else
    uint rayFlags = kRayFlagForceOpaque;
    #endif

    return !TraceShadowRay(dispatchInfo, accelStruct, rayMask, shadowRay, rayFlags, attenuation);
}

float3 CalculateJitteredLightVec(float shadowRadius, float2 rnd, float3 lightVec)
{
    float3x3 lightBasis = OrthoBasisFromVector(normalize(lightVec));
    float2 diskSample = MapSquareToDisk(rnd);
    float3 jitterOffset = float3(shadowRadius * diskSample, 0.0f);
    jitterOffset = mul(jitterOffset, lightBasis);
    return normalize(lightVec + jitterOffset);
}

bool CastJitteredShadowRay(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    float shadowRadius,
    float3 lightVector, // Vector from ray `origin` to light if light is not a directional light. Otherwise, it is a unit vector pointing toward the directional light.
    float3 origin,
    float3 direction,
    float distance,
    uint shadowRayMask,
    float2 uSample,
    out float3 attenuation)
{
    if (shadowRadius > 0.0f)
    {
        // jitter the light direction within the shadow radius
        direction = CalculateJitteredLightVec(shadowRadius, uSample, lightVector);
    }

    return CastShadowRay(dispatchInfo, accelStruct, origin, direction, distance, shadowRayMask, attenuation);
}

struct SphQuad
{
    float3 o, x, y, z;
    float z0, z0sq;
    float x0, y0, y0sq;
    float x1, y1, y1sq;
    float b0, b1, b0sq, k;
    float S;
};

bool SampleRectangularLight(float2 rng, float3 P, PTLight light, inout LightShapeSample lightSample)
{
    float u = rng.x;
    float v = rng.y;

#ifndef SOLID_ANGLE_SAMPLING
    // Area sampling
    u -= 0.5f;
    v -= 0.5f;

    float3 position = light.position + u * light.width * light.right + v * light.height * light.up;
    lightSample.lightVector = position - P;
    lightSample.distanceToLight = length(lightSample.lightVector);
    lightSample.L = lightSample.lightVector * rcp(lightSample.distanceToLight);
    lightSample.weight = 0;
    lightSample.uv = 0;
    lightSample.materialIndex = -1;

    float cosTheta = -dot(lightSample.L, light.forward);
    if (cosTheta < 0.001)
        return false;

    float d = lightSample.distanceToLight;
    float lightArea = light.width * light.height;
    lightSample.weight = lightArea * cosTheta / (d * d);

    return true;

#else
    // Solid angle sampling
    SphQuad squad;
    {
        // compute local reference system ’R’
        squad.o = P;
        squad.x = light.right;
        squad.y = light.up;
        squad.z = cross(squad.x, squad.y);

        // Adjust the position of the light to be at the center of the rectangle
        light.position = light.position - 0.5 * light.width * light.right;
        light.position = light.position - 0.5 * light.height * light.up;

        // compute rectangle coords in local reference system
        float3 d = light.position - P;
        squad.z0 = dot(d, squad.z);

        // flip ’z’ to make it point against ’Q’
        if (squad.z0 > 0)
        {
            squad.z *= -1;
            squad.z0 *= -1;
        }

        float exl = light.width;
        float eyl = light.height;
        squad.z0sq = squad.z0 * squad.z0;
        squad.x0 = dot(d, squad.x);
        squad.y0 = dot(d, squad.y);
        squad.x1 = squad.x0 + exl;
        squad.y1 = squad.y0 + eyl;
        squad.y0sq = squad.y0 * squad.y0;
        squad.y1sq = squad.y1 * squad.y1;

        // create vectors to four vertices
        float3 v00 = float3 (squad.x0, squad.y0, squad.z0);
        float3 v01 = float3 (squad.x0, squad.y1, squad.z0);
        float3 v10 = float3 (squad.x1, squad.y0, squad.z0);
        float3 v11 = float3 (squad.x1, squad.y1, squad.z0);

        // compute normals to edges
        float3 n0 = normalize(cross(v00, v10));
        float3 n1 = normalize(cross(v10, v11));
        float3 n2 = normalize(cross(v11, v01));
        float3 n3 = normalize(cross(v01, v00));

        // compute internal angles (gamma_i)
        float g0 = acos(-dot(n0, n1));
        float g1 = acos(-dot(n1, n2));
        float g2 = acos(-dot(n2, n3));
        float g3 = acos(-dot(n3, n0));

        // compute predefined constants
        squad.b0 = n0.z;
        squad.b1 = n2.z;
        squad.b0sq = squad.b0 * squad.b0;
        squad.k = 2 * PI - g2 - g3;

        // compute solid angle from internal angles
        squad.S = g0 + g1 - squad.k;
    }

    // Generate sample
    lightSample = (LightShapeSample)0;

    if (squad.S < 0.00001 || isnan(squad.S))
        return false;

    // 1. compute ’cu’
    float au = u * squad.S + squad.k;
    float fu = (cos(au) * squad.b0 - squad.b1) / sin(au);
    float cu = 1 / sqrt(fu * fu + squad.b0sq);// *(fu > 0 ? +1 : -1);
    cu = (fu > 0.0f) ? cu : -cu;
    cu = clamp(cu, -1, 1); // avoid NaNs

    // 2. compute ’xu’
    float xu = -(cu * squad.z0) / sqrt(1 - cu * cu);
    xu = clamp(xu, squad.x0, squad.x1); // avoid Infs

    // 3. compute ’yv’
    float d = sqrt(xu * xu + squad.z0sq);
    float h0 = squad.y0 / sqrt(d * d + squad.y0sq);
    float h1 = squad.y1 / sqrt(d * d + squad.y1sq);
    float hv = h0 + v * (h1 - h0);
    float hv2 = hv * hv;
    float eps = 0.0001;
    float yv = (hv2 < 1.0 - eps) ? (hv * d) / sqrt(1.0 - hv2) : squad.y1;

    // 4. transform (xu,yv,z0) to world coords
    float3 position = (squad.o + xu * squad.x + yv * squad.y + squad.z0 * squad.z);

    lightSample.lightVector = position - P;
    lightSample.distanceToLight = length(lightSample.lightVector);
    lightSample.L = lightSample.lightVector * rcp(lightSample.distanceToLight);
    lightSample.weight = squad.S;

    // Cookie texture coordinates
    lightSample.uv = float2((xu - squad.x0) / (squad.x1 - squad.x0) - 0.5, (yv - squad.y0) / (squad.y1 - squad.y0) - 0.5);
    lightSample.uv.y = 1.0 - lightSample.uv.y;
    lightSample.materialIndex = -1;

    float cosTheta = -dot(lightSample.L, light.forward);
    if (cosTheta < eps)
        return false;

    return true;
#endif
}

bool SampleDiscLight(float2 rng, float3 P, PTLight light, inout LightShapeSample lightSample)
{
    float u = rng.x;
    float v = rng.y;

    float2 coord = SampleDiskUniform(u, v);
    const float radius = light.width;
    float3 posOnDisk = radius * (coord.x * light.right + coord.y * light.up);
    float3 position = light.position + posOnDisk;

    lightSample.lightVector = position - P;
    lightSample.distanceToLight = length(lightSample.lightVector);
    lightSample.L = lightSample.lightVector * rcp(lightSample.distanceToLight);
    lightSample.weight = 0;

    // Cookie texture coordinates
    float lightDiameter = 2 * light.width;
    float centerU = dot(posOnDisk, light.right) / (lightDiameter * Length2(light.right));
    float centerV = dot(posOnDisk, light.up) / (lightDiameter * Length2(light.up));
    lightSample.uv = float2(centerU - 0.5, centerV - 0.5);
    lightSample.uv.x = 1.0 - lightSample.uv.x;
    lightSample.materialIndex = -1;

    float cosTheta = -dot(lightSample.L, light.forward);
    if (cosTheta < 0.001)
        return false;

    float d = lightSample.distanceToLight;
    float lightArea = PI * light.width * light.width;
    lightSample.weight = lightArea * cosTheta / (d * d);
    return true;
}

// A Low-Distortion Map Between Triangle and Square, by Eric Heitz, 2019
// Preserves blue-noise quality of input samples better than sqrt parameterization.
float2 MapUnitSquareToUnitTriangle(float2 unitSquareCoords)
{
    if (unitSquareCoords.y > unitSquareCoords.x)
    {
        unitSquareCoords.x *= 0.5f;
        unitSquareCoords.y -= unitSquareCoords.x;
    }
    else
    {
        unitSquareCoords.y *= 0.5f;
        unitSquareCoords.x -= unitSquareCoords.y;
    }
    return unitSquareCoords;
}

bool SampleEmissiveMesh(StructuredBuffer<UnifiedRT::InstanceData> instanceList, float2 rand0, float2 rand1, float3 P, PTLight light, inout LightShapeSample lightSample)
{
    // random point in unit triangle
    float2 samplePoint = MapUnitSquareToUnitTriangle(rand0);

    int instanceIndex = light.height;
    int numPrimitives = light.attenuation.x;
    int primitiveIndex = (rand1.x * numPrimitives) % numPrimitives;

    // fetch the triangle vertices from the geometry pool

    int geometryIndex = instanceList[instanceIndex].geometryIndex;
    GeoPoolMeshChunk meshInfo = g_MeshList[geometryIndex];
    uint3 triangleVertexIndices = UnifiedRT::Internal::FetchTriangleIndices(meshInfo, primitiveIndex);

    GeoPoolVertex v1, v2, v3;
    v1 = UnifiedRT::Internal::FetchVertex(meshInfo, triangleVertexIndices.x);
    v2 = UnifiedRT::Internal::FetchVertex(meshInfo, triangleVertexIndices.y);
    v3 = UnifiedRT::Internal::FetchVertex(meshInfo, triangleVertexIndices.z);

    UnifiedRT::InstanceData instanceInfo = UnifiedRT::GetInstance(instanceIndex);
    v1.pos = mul(float4(v1.pos, 1), instanceInfo.localToWorld);
    v2.pos = mul(float4(v2.pos, 1), instanceInfo.localToWorld);
    v3.pos = mul(float4(v3.pos, 1), instanceInfo.localToWorld);

    float3 geometricNormal = cross(v2.pos - v1.pos, v3.pos - v1.pos) * instanceInfo.localToWorldDetSign;

    // compute the random position
    float3 position = samplePoint.x * v1.pos + samplePoint.y * v2.pos + (1 - samplePoint.x - samplePoint.y) * v3.pos;

    lightSample.lightVector = position - P;
    lightSample.distanceToLight = length(lightSample.lightVector);
    lightSample.L = lightSample.lightVector * rcp(lightSample.distanceToLight);
    lightSample.distanceToLight *= 0.99;    // avoid self intersection with mesh light geometry
    lightSample.weight = 0;

    // Interpolate the texture coordinates if needed
    int materialIndex = instanceInfo.userMaterialID;
    lightSample.materialIndex = materialIndex;
    PTMaterial matInfo = g_MaterialList[materialIndex];

    if (matInfo.emissionTextureIndex != -1)
    {
        if (matInfo.albedoAndEmissionUVChannel == 1)
            lightSample.uv = samplePoint.x * v1.uv1.xy + samplePoint.y * v2.uv1.xy + (1 - samplePoint.x - samplePoint.y) * v3.uv1.xy;
        else
            lightSample.uv = samplePoint.x * v1.uv0.xy + samplePoint.y * v2.uv0.xy + (1 - samplePoint.x - samplePoint.y) * v3.uv0.xy;
    }
    else
        lightSample.uv = 0;

    float cosTheta = -dot(lightSample.L, normalize(geometricNormal));
    const bool doubleSided = matInfo.flags & 2;
    if (cosTheta < 0.001 && !doubleSided)
        return false;

    float d = lightSample.distanceToLight;

    float lightArea = 0.5 * length(geometricNormal);

    lightSample.weight = lightArea * abs(cosTheta) / (d * d);
    lightSample.weight *= numPrimitives; //uniform light selection pdf

    return true;
}

float2 PunctualLightCookieUVs(float3 L, PTLight light)
{
    // Handles spot, box and directional lights.
    light.right *= 2.0f / light.width;
    light.up *= 2.0f / light.height;

    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    float3 positionLS = mul(-L, transpose(lightToWorld));

    float perspectiveZ = (light.type != BOX_LIGHT && light.type != DIRECTIONAL_LIGHT) ? positionLS.z : 1.0f;
    float2 positionCS = positionLS.xy / perspectiveZ;
    float2 positionNDC = positionCS * 0.5f + 0.5f;
    return positionNDC;
}

bool SamplePunctualLight(float3 P, PTLight light, out LightShapeSample lightSample)
{
    if (light.type == DIRECTIONAL_LIGHT)
    {
        lightSample.lightVector = -light.forward ;
        lightSample.distanceToLight = K_T_MAX;
    }
    else if (light.type != BOX_LIGHT) // SPOT_LIGHT or POINT_LIGHT
    {
        lightSample.lightVector = light.position - P;
        lightSample.distanceToLight = length(lightSample.lightVector);
    }
    else // BOX_LIGHT
    {
        float distanceToLight = dot(-light.forward, light.position - P);
        float3 v = light.position - P;

        float x = abs(dot(v, light.right));
        float y = abs(dot(v, light.up));
        float2 sideLimits = float2(light.width, light.height) * 0.5;

        if (distanceToLight < 0 || distanceToLight > light.range || x >= sideLimits.x || y >= sideLimits.y)
            return false;

        lightSample.lightVector = -light.forward * distanceToLight;
        lightSample.distanceToLight = distanceToLight;
    }

    lightSample.L = normalize(lightSample.lightVector);
    lightSample.weight = 1.0f;
    lightSample.materialIndex = -1;

    // Light cookie UVs (note: for pointlights we use the light vector to access the cookie)
    lightSample.uv = 0.0f;
    if (light.cookieIndex >= 0 && light.type != POINT_LIGHT)
    {
        lightSample.uv = PunctualLightCookieUVs(light.position - P, light);
    }

    return true;
}

bool SampleEnvironmentLight(float2 rng, float3 P, PTLight light, out LightShapeSample lightSample)
{
    lightSample = (LightShapeSample)0;

#ifdef UNIFORM_ENVSAMPLING
    // Sample the environment with a random direction. Should only be used for reference / ground truth.
    lightSample.lightVector = SampleSphereUniform(rng.x, rng.y);
    lightSample.weight = 4 * PI;
#else
    float normalizationFactor = GetSkyPDFNormalizationFactor(_EnvironmentCdfMarginalBuffer);

    // A normalization factor of zero means that the environment cubemap was zero, and in this case
    // its PDF isn't well-defined. It is safe to bail in this case, because the environment wouldn't
    // contribute anything anyway.
    if (normalizationFactor == 0.0f)
        return false;

    const float2 u = SampleSky(
        rng,
        _EnvironmentCdfMarginalResolution,
        _EnvironmentCdfMarginalBuffer,
        _EnvironmentCdfConditionalResolution,
        _EnvironmentCdfConditionalBuffer);
    lightSample.lightVector = MapUVToSkyDirection(u);

    float3 envValue = g_EnvTex.SampleLevel(sampler_g_EnvTex, lightSample.lightVector, 0).xyz;
    if (all(envValue == 0.0))
        return false;

    float skyPdf = GetSkyPDFFromValue(envValue, normalizationFactor);
    lightSample.weight = rcp(skyPdf);
#endif

    lightSample.L = normalize(lightSample.lightVector);
    lightSample.distanceToLight = K_T_MAX;
    lightSample.uv = 0;
    lightSample.materialIndex = -1;

    return true;
}

bool SampleLightShape(StructuredBuffer<UnifiedRT::InstanceData> instanceList, float2 rand0, float2 rand1, float3 P, PTLight light, out LightShapeSample lightSample)
{
    lightSample = (LightShapeSample)0;
    bool result = false;
    if (light.type == RECTANGULAR_LIGHT)
    {
        result = SampleRectangularLight(rand0, P, light, lightSample);
    }
    else if (light.type == DISC_LIGHT)
    {
        result = SampleDiscLight(rand0, P, light, lightSample);
    }
    else if (light.type == EMISSIVE_MESH)
    {
        result = SampleEmissiveMesh(instanceList, rand0, rand1, P, light, lightSample);
    }
    else if (light.type == ENVIRONMENT_LIGHT)
    {
        result = SampleEnvironmentLight(rand0, P, light, lightSample);
    }
    else
    {
        result = SamplePunctualLight(P, light, lightSample);
    }
    return result && lightSample.distanceToLight != 0.0f;
}

// Find the smallest `i` between 0 and `cdfLength-1` such that `cdf[i] < rand < cdf[i+1]`.
// `cdf` must be normalized and `rand` must be between 0 and 1.
uint BinarySearchCdf(in StructuredBuffer<float> cdf, uint cdfLength, float rand)
{
    const uint maxSteps = log2((float)cdfLength) + 1;

    uint leftIdx = 0;
    uint rightIdx = cdfLength - 1;
    for (uint i = 0; i < maxSteps; ++i)
    {
        const uint candidateIdx  = (leftIdx + rightIdx) / 2;
        const float higherVal = cdf[candidateIdx];
        if (higherVal < rand)
        {
            leftIdx = candidateIdx + 1;
            continue;
        }

        const float lowerVal = candidateIdx == 0 ? 0.0f : cdf[candidateIdx - 1];
        if (rand < lowerVal)
        {
            rightIdx = candidateIdx - 1;
            continue;
        }

        return candidateIdx;
    }

    return UINT_MAX; // Unexpected.
}

void PickLightUniformly(float randomSample, out uint lightIndex, out float probabilityMass)
{
    lightIndex = g_NumLights * randomSample;
    // Guard in case the RNG returns 1 or higher
    lightIndex = lightIndex % g_NumLights;
    probabilityMass = 1.0f / g_NumLights;
}

void PickLightFromGrid(float randomSample, float3 pos, out uint lightIndex, out float probabilityMass)
{
    uint cellIndex = GetCellIndexFromPosition(pos);
    uint firstReservoir = g_LightGrid[cellIndex].x;
    uint numReservoirs = g_LightGrid[cellIndex].y;
    uint reservoirIndex = min(numReservoirs * randomSample, numReservoirs - 1); // Guard in case the RNG returns 1 or higher

    ThinReservoir reservoir = g_LightGridCellsData[firstReservoir + reservoirIndex];

    lightIndex = reservoir.lightIndex;
    probabilityMass = reservoir.weight / numReservoirs; // TODO: move the division at build time
}

uint FindEmissiveMeshLightIndex(int instanceID)
{
    for (uint i = 0; i < g_NumEmissiveMeshes; i++)
        if (GetMeshLightInstanceID(g_LightList[i]) == instanceID)
            return i;
    return UINT_MAX; // Unexpected.
}

float SampleFalloff(int falloffIndex, float distance, float range)
{
    const float LUTRange = g_LightFalloffLUTRange[falloffIndex];
    if (distance > LUTRange)
        return 0.0f; // The distance is outside the range of the falloff LUT.
    const float normalizedSamplePosition = distance / LUTRange;
    const int sampleCount = g_LightFalloffLUTLength;
    const int LUTOffset = falloffIndex * sampleCount;
    const float index = normalizedSamplePosition * float(sampleCount);

    // compute the index pair
    const int loIndex = min(int(index), int(sampleCount - 1));
    const int hiIndex = min(int(index) + 1, int(sampleCount - 1));
    const float hiFraction = (index - float(loIndex));

    const float sampleLo = g_LightFalloff[LUTOffset+loIndex];
    const float sampleHi = g_LightFalloff[LUTOffset+hiIndex];

    // do the lerp
    return (1.0f - hiFraction) * sampleLo + hiFraction * sampleHi;
}

// TODO: Add support for angular falloff LUT, that is used in progressive lightmapper. https://jira.unity3d.com/browse/LIGHT-1770
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, lightData.forward).
float PunctualLightAngleAttenuation(float4 distances, float rangeAttenuationScale, float rangeAttenuationBias, float lightAngleScale, float lightAngleOffset)
{
    float distRcp = distances.z;
    float distProj = distances.w;
    float cosFwd = distProj * distRcp;
    float attenuation = AngleAttenuation(cosFwd, lightAngleScale, lightAngleOffset);
    return Sq(attenuation);
}

float GetPunctualAttenuation(PTLight light, LightShapeSample lightSample)
{
    if (light.type == BOX_LIGHT)
        return 1.0;

    if (light.type == PYRAMID_LIGHT)
    {
        // compute lit position in light basis
        float x = abs(dot(lightSample.lightVector, light.right));
        float y = abs(dot(lightSample.lightVector, light.up));
        float z = abs(dot(lightSample.lightVector, light.forward));

        float2 sideLimits = float2(light.width, light.height) * 0.5 * z;
        if (x >= sideLimits.x || y >= sideLimits.y)
            return 0.0f;
    }

    // Punctual attenuation
    float attenuation = 1.0f;
    const float d = lightSample.distanceToLight;
    float4 distances = float4(d, Sq(d), rcp(d), -d * dot(lightSample.L, light.forward));

#ifdef USE_DISTANCE_ATTENUATION_LUT
    if (light.falloffIndex >= 0)
        attenuation *= SampleFalloff(light.falloffIndex, d, light.range);

    attenuation *= PunctualLightAngleAttenuation(distances, light.attenuation.x, light.attenuation.y, light.attenuation.z, light.attenuation.w);
#else
    attenuation *= PunctualLightAttenuation(distances, light.attenuation.x, light.attenuation.y, light.attenuation.z, light.attenuation.w);
#endif

    return attenuation;
}

float3 PointLightCookie(PTLight light, LightShapeSample lightSample)
{
    if (light.cookieIndex >= 0)
    {
        float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
        float3 L = mul(-lightSample.L, transpose(lightToWorld));
        return g_CubemapAtlas.SampleLevel(sampler_g_CubemapAtlas, float4(L, light.cookieIndex), 0).xyz;
    }
    else
        return 1.0f;
}

float3 LightCookie(PTLight light, LightShapeSample lightSample)
{
    if (light.cookieIndex >= 0)
    {
        return g_CookieAtlas.SampleLevel(sampler_g_CookieAtlas, float3(lightSample.uv, light.cookieIndex), 0).xyz;
    }
    else
        return 1.0f;
}

float3 AreaCookieAttenuation(PTLight light, LightShapeSample lightSample)
{
    if (light.cookieIndex >= 0)
    {
        return g_CookieAtlas.SampleLevel(sampler_g_CookieAtlas, float3(lightSample.uv, light.cookieIndex), 0).xyz;
    }
    else
        return 1.0f;
}

float3 PunctualCookieAttenuation(PTLight light, LightShapeSample lightSample)
{
    if (light.type == SPOT_LIGHT || light.type == BOX_LIGHT || light.type == PYRAMID_LIGHT)
        return LightCookie(light, lightSample);
    else if (light.type == POINT_LIGHT)
        return PointLightCookie(light, lightSample);
    else
        return 1.0;
}

float3 GetPunctualEmission(PTLight light, LightShapeSample lightSample)
{
    float3 emission = light.intensity * GetPunctualAttenuation(light, lightSample);
    float3 cookieAttenuation = PunctualCookieAttenuation(light, lightSample);
    return emission * cookieAttenuation;
}

float3 GetDirectionalEmission(PTLight light, LightShapeSample lightSample)
{
    return light.intensity * LightCookie(light, lightSample);
}

float3 GetRectangularLightEmission(PTLight light, LightShapeSample lightSample)
{
    if (light.range < lightSample.distanceToLight)
        return 0.f;
    float3 emission = light.intensity;

    float3 cookieAttenuation = AreaCookieAttenuation(light, lightSample);
    return emission * cookieAttenuation;
}

float3 GetDiscLightEmission(PTLight light, LightShapeSample lightSample)
{
    if (light.range < lightSample.distanceToLight)
        return float3(0.f, 0.f, 0.f);
    else
    {
        float3 emission = light.intensity;
        float3 cookieAttenuation = AreaCookieAttenuation(light, lightSample);
        return emission * cookieAttenuation;
    }
}

float3 GetEmissiveMeshEmission(LightShapeSample lightSample)
{
    PTMaterial matInfo = g_MaterialList[lightSample.materialIndex];

    float3 emission = 0;
    if (matInfo.emissionTextureIndex != -1)
    {
        emission = SampleAtlas(g_EmissionTextures, sampler_g_EmissionTextures, matInfo.emissionTextureIndex, lightSample.uv, matInfo.emissionScale, matInfo.emissionOffset, false).rgb;
    }
    else
    {
        emission = matInfo.emissionColor;
    }

    // To match the behavior of the old baker, emission should be attenuated by transmission.
    // Think about a mesh with a cutout shader - the portions that are cut out should obviously not emit light.
    if (matInfo.transmissionTextureIndex != -1)
    {
        float3 transmission = SampleAtlas(g_TransmissionTextures, sampler_g_TransmissionTextures, matInfo.transmissionTextureIndex, lightSample.uv, matInfo.transmissionScale, matInfo.transmissionOffset, false).rgb;
        emission *= 1.0-transmission;
    }

    return emission;
}

float3 GetEnvironmentLightEmission(float3 direction)
{
    // Sample the environment and apply intensity multiplier
    return g_EnvIntensityMultiplier * g_EnvTex.SampleLevel(sampler_g_EnvTex, direction, 0).xyz;
}

bool GetEnvironmentLightEmissionAndDensity(float3 direction, out float3 emission, out float density)
{
    emission = GetEnvironmentLightEmission(direction);

    // In the special case when the environment is zero/black, its importance sampling PDF is undefined.
    // This check ensures we don't evaluate the PDF in this case.
    if (any(emission != 0.0f))
    {
        #ifdef UNIFORM_ENVSAMPLING
        density = rcp(4 * PI);
        #else
        density = GetSkyPDFFromValue(emission, _EnvironmentCdfMarginalBuffer);
        #endif
        return true;
    }
    else
    {
        density = 0;
        return false;
    }
}

float3 GetEmission(PTLight light, LightShapeSample lightSample)
{
    float3 emission = 0;
    if (light.type == DIRECTIONAL_LIGHT)
    {
        emission = GetDirectionalEmission(light, lightSample);
    }
    else if (light.type == POINT_LIGHT || light.type == SPOT_LIGHT || light.type == BOX_LIGHT || light.type == PYRAMID_LIGHT)
    {
        emission = GetPunctualEmission(light, lightSample);
    }
    else if (light.type == RECTANGULAR_LIGHT)
    {
        emission = GetRectangularLightEmission(light, lightSample);
    }
    else if (light.type == DISC_LIGHT)
    {
        emission = GetDiscLightEmission(light, lightSample);
    }
    else if (light.type == EMISSIVE_MESH)
    {
        emission = GetEmissiveMeshEmission(lightSample);
    }
    else if (light.type == ENVIRONMENT_LIGHT)
    {
        emission = GetEnvironmentLightEmission(lightSample.L);
    }
    return emission;
}

// Emissive surfaces are also sampled explicitely as lights. The following functions compute MIS weight to combine the two sampling strategies.
float EmissiveMISWeightForLightRay(int lightType, float3 lightDirection, float lightPdf, float3 worldNormal)
{
    if (lightType == EMISSIVE_MESH || lightType == ENVIRONMENT_LIGHT)
    {
        float cosTheta = dot(worldNormal, lightDirection);
        float brdfPdf = cosTheta / PI;
#if (EMISSIVE_SAMPLING == LIGHT_SAMPLING)
        float misWeight = 1;
#elif (EMISSIVE_SAMPLING == BRDF_SAMPLING)
        float misWeight = 0;
#else
        float misWeight = PowerHeuristic(lightPdf, brdfPdf);
#endif
        return misWeight;
    }
    return 1.0;
}

float EmissiveMISWeightForBrdfRay(float lightPdf, float brdfPdf){

#if (EMISSIVE_SAMPLING == LIGHT_SAMPLING)
    float misWeight = 0;
#elif (EMISSIVE_SAMPLING == BRDF_SAMPLING)
    float misWeight = 1;
#else
    // brdfPdf > 0 condition is here to be able to disable MIS for LiveGI's primary rays.
    // When the primary camera ray directly hits an emissive surface, we should not do MIS at all.
    // We did not have a bounce yet and we therefore set iterator.lastScatterProbabilityDensityto (brdfPdf) to 0.
    float misWeight = brdfPdf > 0 ? PowerHeuristic(brdfPdf, lightPdf) : 1.0;
#endif
    return misWeight;
}

PTLight FetchLight(int lightIndex)
{
    return g_LightList[lightIndex];
}

uint PackLightInfo(PTLight light)
{
    uint bitmask = 0;
    bitmask |= light.castsShadows;
    bitmask |= (light.type << 1);
    return bitmask;
}

void UnpackLightInfo(uint info, out int castsShadows, out int lightType)
{
    castsShadows = info & 1;
    lightType = info >> 1;
}

struct ReservoirLightSample
{
    uint    lightInfo; // packs the "cast shadows" flag and the light type
    float3  lightDirection;
    float   distanceToLight;
    float3  radiance;
    float   shadowRadius;
    float   pdf; // for MIS
};

struct Reservoir
{
    float   totalWeights;
    ReservoirLightSample bestSample;

    void Update(ReservoirLightSample candidateSample, float candidateRisWeight, float rand01)
    {
        totalWeights += candidateRisWeight;

        if (rand01 * totalWeights < candidateRisWeight)
            bestSample = candidateSample;
    }
};

uint GetNumLights(float3 origin)
{
    if (g_LightPickingMethod == LIGHT_PICKING_METHOD_RESERVOIR_GRID  || g_LightPickingMethod == LIGHT_PICKING_METHOD_LIGHT_GRID)
    {
        uint cellIndex = GetCellIndexFromPosition(origin);
        uint numReservoirs = g_LightGrid[cellIndex].y;
        return numReservoirs;
    }

    return g_NumLights;
}

PTLight GetLightInCell(float3 origin, uint lightIndex)
{
    if (g_LightPickingMethod == LIGHT_PICKING_METHOD_RESERVOIR_GRID  || g_LightPickingMethod == LIGHT_PICKING_METHOD_LIGHT_GRID)
    {
        uint cellIndex = GetCellIndexFromPosition(origin);
        uint firstReservoir = g_LightGrid[cellIndex].x;
        ThinReservoir reservoir = g_LightGridCellsData[firstReservoir + lightIndex];
        lightIndex = reservoir.lightIndex;
    }

    return FetchLight(lightIndex);
}

bool SampleLight(PTLight light, float3 shadowRayOrigin, float3 normal, bool isDirect, uint enabledLightsLayerMask, StructuredBuffer<UnifiedRT::InstanceData> instanceList, float2 rand0, float2 rand1, out ReservoirLightSample lightSample)
{
    lightSample = (ReservoirLightSample)0;

    // skip light if it contributes only to indirect illumination
    if (isDirect && !light.contributesToDirectLighting)
        return false;

    // skip lights based on light layer
    if ((light.layerMask & enabledLightsLayerMask) == 0)
        return false;

    LightShapeSample lightShapeSample;

    if (!SampleLightShape(instanceList, rand0, rand1, shadowRayOrigin, light, lightShapeSample))
        return false;

    float3 emission = GetEmission(light, lightShapeSample);
    if (!isDirect)
        emission *= light.indirectScale * g_IndirectScale;

    if (dot(normal, lightShapeSample.L) < 0)
        return false;

    if (Luminance(emission * lightShapeSample.weight) == 0)
        return false;

    lightSample.lightInfo = PackLightInfo(light);
    lightSample.lightDirection = lightShapeSample.L;
    lightSample.distanceToLight = lightShapeSample.distanceToLight;
    lightSample.pdf = rcp(lightShapeSample.weight);
    lightSample.radiance = emission;
    lightSample.shadowRadius = light.shadowRadius;

    return true;
}

struct SampleLightsOptions
{
    bool isDirect;
    bool receiveShadows;
    uint shadowRayMask;
    uint lightsRenderingLayerMask;
    uint numLightCandidates;
};

PTLight PickLight(float pickingSample, float3 receiverOrigin, out float lightPickingPmf)
{
    uint lightIndex;
    if (g_LightPickingMethod == LIGHT_PICKING_METHOD_RESERVOIR_GRID || g_LightPickingMethod == LIGHT_PICKING_METHOD_LIGHT_GRID)
        PickLightFromGrid(pickingSample, receiverOrigin, lightIndex, lightPickingPmf);
    else
        PickLightUniformly(pickingSample, lightIndex, lightPickingPmf);

    return FetchLight(lightIndex);
}

bool SampleLightsRadianceRIS(
    UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    float3 receiverOrigin, float3 receiverNormal, SampleLightsOptions options, inout PathTracingSampler rngState, out LightSample resLightSample)
{
    resLightSample = (LightSample)0;

    // Find how many lights we will evaluate
    uint numLightCandidates = min(GetNumLights(receiverOrigin), options.numLightCandidates);

    // We want to seed the 2 RNGs below with the pixel coordinate and sample index. To avoid calculating a 3D hash,
    // we just offset the sequence using the sample index. The constant added to the pixel coord is to decorrelate
    // them from the main RNG used elsewhere in the path tracer, and from each other.

    // RNG for generating samples.
    QRNG_TYPE sampleGenerationRng;
    sampleGenerationRng.Init(rngState.pixelCoord+3141, rngState.pathIndex * numLightCandidates);

    // RNG for selecting the RIS winner. Use white instead of blue noise for this one.
    QrngPcg4D winnerSelectionRng;
    winnerSelectionRng.Init(rngState.pixelCoord+2718, rngState.pathIndex * numLightCandidates);

    Reservoir reservoir = (Reservoir) 0;
    for (uint i = 0; i < numLightCandidates; ++i)
    {
        float pickingSample = sampleGenerationRng.GetSample(RAND_DIM_LIGHT_SELECTION).x;
        float winnerSample = winnerSelectionRng.GetSample();

        float lightPickingPmf;
        PTLight light = PickLight(pickingSample, receiverOrigin, lightPickingPmf);

        ReservoirLightSample lightSample;
        float2 sampleRand0 = sampleGenerationRng.GetSample(RAND_DIM_LIGHT_SELECTION + 1);
        float2 sampleRand1 = sampleGenerationRng.GetSample(RAND_DIM_LIGHT_SELECTION + 2);
        if (SampleLight(light, receiverOrigin, receiverNormal, options.isDirect, options.lightsRenderingLayerMask, instanceList, sampleRand0, sampleRand1, lightSample))
        {
            lightSample.pdf *= lightPickingPmf;

            float targetFunc = Luminance(lightSample.radiance);
            float risWeight = targetFunc / lightSample.pdf; // w = targetFunc / sourcePdf
            reservoir.Update(lightSample, risWeight, winnerSample);
        }

        sampleGenerationRng.NextSample();
        winnerSelectionRng.NextSample();
    }

    float sampleTargetFuncEval = Luminance(reservoir.bestSample.radiance);
    if (!(reservoir.totalWeights > 0.0f && sampleTargetFuncEval > 0.0f))
        return false;

    ReservoirLightSample bestSample = reservoir.bestSample;

    int castShadows;
    int lightType;
    UnpackLightInfo(bestSample.lightInfo, castShadows, lightType);

    // cast a shadow ray
    float3 attenuation = 1.0f;
    float2 shadowSample = rngState.GetSample2D(RAND_DIM_JITTERED_SHADOW);
    bool isShadowed = castShadows && options.receiveShadows && !CastJitteredShadowRay(dispatchInfo, accelStruct, bestSample.shadowRadius, lightType == DIRECTIONAL_LIGHT ? bestSample.lightDirection : bestSample.lightDirection*bestSample.distanceToLight, receiverOrigin, bestSample.lightDirection, bestSample.distanceToLight, options.shadowRayMask, shadowSample, attenuation);
    if (isShadowed)
        return false;

    float unbiasedContributionWeight = (reservoir.totalWeights / float(numLightCandidates)) / sampleTargetFuncEval;
    float3 radiance = bestSample.radiance * unbiasedContributionWeight * attenuation;

    resLightSample.radiance = radiance;
    resLightSample.direction = bestSample.lightDirection;
    resLightSample.risSourcePdf = bestSample.pdf;
    resLightSample.lightType = lightType;

    return true;
}

// Uniform sampling of the light list, used for reference / debuging
bool SampleLightsRadianceMC(
    UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    float3 receiverOrigin, float3 receiverNormal, SampleLightsOptions options, inout PathTracingSampler rngState, out LightSample resLightSample)
{
    resLightSample = (LightSample)0;

    float pickingSample = rngState.GetSample1D(RAND_DIM_LIGHT_SELECTION);
    float lightPickingPmf;
    PTLight light = PickLight(pickingSample, receiverOrigin, lightPickingPmf);

    ReservoirLightSample lightSample;
    float2 sampleRand0 = rngState.GetSample2D(RAND_DIM_LIGHT_SELECTION + 1);
    float2 sampleRand1 = rngState.GetSample2D(RAND_DIM_LIGHT_SELECTION + 2);
    if (!SampleLight(light, receiverOrigin, receiverNormal, options.isDirect, options.lightsRenderingLayerMask, instanceList, sampleRand0, sampleRand1, lightSample))
        return false;

    lightSample.pdf *= lightPickingPmf;

    float3 attenuation = 1.0f;
    float2 shadowSample = rngState.GetSample2D(RAND_DIM_JITTERED_SHADOW);
    bool isShadowed = light.castsShadows && options.receiveShadows && !CastJitteredShadowRay(dispatchInfo, accelStruct, light.shadowRadius, light.type == DIRECTIONAL_LIGHT ? lightSample.lightDirection : lightSample.lightDirection*lightSample.distanceToLight, receiverOrigin, lightSample.lightDirection, lightSample.distanceToLight, options.shadowRayMask, shadowSample, attenuation);
    if (isShadowed)
        return false;

    resLightSample.radiance = attenuation * lightSample.radiance / lightSample.pdf;
    resLightSample.direction = lightSample.lightDirection;
    resLightSample.risSourcePdf = lightSample.pdf;
    resLightSample.lightType = light.type;

    return true;
}

// Sample incoming radiance from a specific light.
bool SampleLightRadiance(
    UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    float3 receiverOrigin, float3 receiverNormal, SampleLightsOptions options, PTLight light, inout PathTracingSampler rngState, out LightSample resLightSample)
{
    resLightSample = (LightSample)0;

    uint numLights = GetNumLights(receiverOrigin);
    float lightPickingPmf = rcp(numLights);

    ReservoirLightSample lightSample;
    float2 sampleRand0 = rngState.GetSample2D(RAND_DIM_LIGHT_SELECTION + 1);
    float2 sampleRand1 = rngState.GetSample2D(RAND_DIM_LIGHT_SELECTION + 2);
    if (!SampleLight(light, receiverOrigin, receiverNormal, options.isDirect, options.lightsRenderingLayerMask, instanceList, sampleRand0, sampleRand1, lightSample))
        return false;

    lightSample.pdf *= lightPickingPmf;

    float3 attenuation = 1.0f;
    float2 shadowSample = rngState.GetSample2D(RAND_DIM_JITTERED_SHADOW);
    bool isShadowed = light.castsShadows && options.receiveShadows && !CastJitteredShadowRay(dispatchInfo, accelStruct, light.shadowRadius, light.type == DIRECTIONAL_LIGHT ? lightSample.lightDirection : lightSample.lightDirection*lightSample.distanceToLight, receiverOrigin, lightSample.lightDirection, lightSample.distanceToLight, options.shadowRayMask, shadowSample, attenuation);
    if (isShadowed)
        return false;

    resLightSample.radiance = attenuation * lightSample.radiance / lightSample.pdf;
    resLightSample.direction = lightSample.lightDirection;
    resLightSample.risSourcePdf = lightSample.pdf;
    resLightSample.lightType = light.type;

    return true;
}

// Sample incoming radiance from a random light, picked using either RIS or raw monte carlo.
bool SampleLightsRadiance(
    UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT:: InstanceData> instanceList,
    float3 receiverOrigin, float3 receiverNormal, SampleLightsOptions options, inout PathTracingSampler rngState, out LightSample resLightSample)
{
#ifdef RESAMPLED_IMPORTANCE_SAMPLING
    return SampleLightsRadianceRIS(dispatchInfo, accelStruct, instanceList, receiverOrigin, receiverNormal, options, rngState, resLightSample);
#else
    return SampleLightsRadianceMC(dispatchInfo, accelStruct, instanceList, receiverOrigin, receiverNormal, options, rngState, resLightSample);
#endif
}

float3 EvalDirectIllumination(
    UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    bool isDirect, uint shadowRayMask, PTHitGeom hitGeom, MaterialProperties material, uint maxLightEvaluations, inout PathTracingSampler rngState)
{
    float3 irradiance = 0.0f;
    float3 shadowRayOrigin = hitGeom.NextRayOrigin();

    SampleLightsOptions options;
    options.isDirect = isDirect;
    options.receiveShadows = true;
    options.shadowRayMask = shadowRayMask;
    options.lightsRenderingLayerMask = hitGeom.renderingLayerMask;
    options.numLightCandidates = maxLightEvaluations;

    LightSample lightSample;
    if (SampleLightsRadiance(dispatchInfo, accelStruct, instanceList, shadowRayOrigin, hitGeom.worldNormal, options, rngState, lightSample))
    {
        float3 eval = EvalDiffuseBrdf(material.baseColor) * ClampedCosine(hitGeom.worldNormal, lightSample.direction) * lightSample.radiance;
        eval *= EmissiveMISWeightForLightRay(lightSample.lightType, lightSample.direction, lightSample.risSourcePdf, hitGeom.worldNormal);
        irradiance = ClampRadiance(eval);
    }

    return irradiance;
}

// Evaluate direct shadows from a single light given the index of the light. Used for baking.
bool IsLightVisibleFromPoint(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    uint shadowRayMask,
    float3 worldPosition,
    inout PathTracingSampler rngState,
    uint dimsOffset,
    PTLight light,
    bool receiveShadows,
    out float3 attenuation)
{
    attenuation = 1.0f;

    LightShapeSample lightSample;
    float2 sampleRand0 = rngState.GetSample2D(dimsOffset);
    float2 sampleRand1 = rngState.GetSample2D(dimsOffset+1);
    if (!SampleLightShape(instanceList, sampleRand0, sampleRand1, worldPosition, light, lightSample))
        return false;

    if (light.type != DIRECTIONAL_LIGHT && lightSample.distanceToLight >= light.range)
        return false;
    if ((light.type == SPOT_LIGHT || light.type == BOX_LIGHT || light.type == PYRAMID_LIGHT) && GetPunctualAttenuation(light, lightSample) < 0.000001f)
        return false;

    if (light.castsShadows && receiveShadows)
    {
        float2 shadowSample = rngState.GetSample2D(dimsOffset+2);
        return CastJitteredShadowRay(dispatchInfo, accelStruct, light.shadowRadius, lightSample.lightVector, worldPosition, lightSample.L, lightSample.distanceToLight, shadowRayMask, shadowSample, attenuation);
    }
    return true;
}

#endif // _PATHTRACING_LIGHTSAMPLING_HLSL_
