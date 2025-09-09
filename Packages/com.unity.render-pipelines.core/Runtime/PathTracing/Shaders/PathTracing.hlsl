
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "TraceRayPathTracing.hlsl"

#include "PathTracingCommon.hlsl"

#include "PathTracingRandom.hlsl"

int     g_BounceCount;
uint    g_LightEvaluations;
int     g_CountNEERayAsPathSegment; // This flag must be enabled for MIS to work properly.
int     g_RenderedInstances;
int     g_PathtracerAsGiPreviewMode;
uint    g_PathTermination;
#include "LightSampling.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(g_SceneAccelStruct);


#define RENDER_ALL 0
#define RENDER_ONLY_STATIC 1
#define RENDER_ALL_IN_CAMERA_RAYS_THEN_ONLY_STATIC 2

#define RAY_TERMINATION     0
#define RAY_SCATTERING      1

int ScatterDiffusely(PTHitGeom hitGeom, float3 V, inout PathTracingSampler rngState, out UnifiedRT::Ray bounceRay, out float brdfPdf)
{
    float2 u = float2(rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_X), rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_Y));

    bounceRay = (UnifiedRT::Ray)0;

    float3 rayDirection;
    if (!SampleDiffuseBrdf(u, hitGeom.worldFaceNormal, hitGeom.worldNormal, V, rayDirection, brdfPdf))
        return RAY_TERMINATION;

    bounceRay.origin = hitGeom.NextRayOrigin();
    bounceRay.direction = rayDirection;
    bounceRay.tMin = 0;
    bounceRay.tMax = K_T_MAX;

    return RAY_SCATTERING;
}

uint RayMask(bool isFirstHitSurface)
{
    if (g_RenderedInstances == RENDER_ALL)
        return DIRECT_RAY_VIS_MASK | INDIRECT_RAY_VIS_MASK;
    else if (g_RenderedInstances == RENDER_ALL_IN_CAMERA_RAYS_THEN_ONLY_STATIC)
        return isFirstHitSurface ? DIRECT_RAY_VIS_MASK | INDIRECT_RAY_VIS_MASK : INDIRECT_RAY_VIS_MASK;
    else // RENDER_ONLY_STATIC
        return isFirstHitSurface ? DIRECT_RAY_VIS_MASK : INDIRECT_RAY_VIS_MASK;
}

uint ShadowRayMask()
{
    return SHADOW_RAY_VIS_MASK;
}

float ComputeEmissiveTriangleDensity(StructuredBuffer<UnifiedRT::InstanceData> instanceList, PTHitGeom hitGeom, int instanceIndex, float3 rayOrigin)
{
    float3 L = hitGeom.worldPosition - rayOrigin;
    float d = length(L);
    if (d > 0)
        L *= rcp(d);
    float cosTheta = dot(hitGeom.worldFaceNormal, L);
    // pdf to sample this as area light
    float weight = (d * d) / (hitGeom.triangleArea * cosTheta);

    // adjust pdf based on number of emissive triangles in the submesh that we hit
    int geometryIndex = instanceList[instanceIndex].geometryIndex;
    int numEmissiveTriangles = g_MeshList[geometryIndex].indexCount / uint(3);
    weight /= numEmissiveTriangles;

    return weight;
}

float ComputeMeshLightDensity(StructuredBuffer<UnifiedRT::InstanceData> instanceList, PTHitGeom hitGeom, int instanceIndex, float3 rayOrigin)
{
    float weight = ComputeEmissiveTriangleDensity(instanceList, hitGeom, instanceIndex, rayOrigin);

    // pdf to select the light source
    weight /= g_NumLights;

    return weight;
}

bool ShouldTreatAsBackface(UnifiedRT::Hit hitResult, MaterialProperties material)
{
    // Have we hit something that is considered a backface when double sided GI is taken into account?
    return !hitResult.isFrontFace && !material.doubleSidedGI;
}

bool ShouldTreatAsBackface(bool isFrontFace, bool doubleSidedGI)
{
    return !isFrontFace && !doubleSidedGI;
}

bool ShouldTransmitRay(inout PathTracingSampler rngState, MaterialProperties material)
{
    bool result = false;
    if (material.isTransmissive)
    {
        // With proper support for IOR, the probability of refraction should be based on the materials fresnel.
        // We don't have this information, so we base it on average transmission color, which matches the old baker.
        // Additionally, we should divide the contribution of the ray by the probability of choosing either reflection or refraction,
        // but we intentionally don't do this, since the old baker didn't do it either.
        float transmissionProbability = dot(material.transmission, 1.0f) / 3.0f;
        if (rngState.GetFloatSample(RAND_DIM_TRANSMISSION) < transmissionProbability)
        {
            result = true;
        }
        else
        {
            result = false;
        }
    }
    return result;
}

#define TRACE_MISS 0
#define TRACE_HIT 1
#define TRACE_TRANSMISSION 2

struct PathIterator
{
    UnifiedRT::Ray ray;
    UnifiedRT::Hit hitResult;
    PTHitGeom hitGeo;
    MaterialProperties material;
    float lastScatterProbabilityDensity;
    float3 radianceSample;
    float3 throughput;
};

void AddRadiance(inout float3 radianceAccumulator, float3 throughput, float3 radianceSample)
{
    radianceAccumulator += throughput * radianceSample;
}

void InitPathIterator(out PathIterator iter, UnifiedRT::Ray primaryRay)
{
    iter = (PathIterator)0;
    iter.lastScatterProbabilityDensity = 0;
    iter.radianceSample = 0;
    iter.throughput = 1;
    iter.ray = primaryRay;
}

uint TraceBounceRay(inout PathIterator iterator, int bounceIndex, uint rayMask, UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, inout PathTracingSampler rngState)
{
    bool isFirstRay = (bounceIndex == 0);
    uint traceResult = TRACE_MISS;

    // Trace the ray. For primary rays in LiveGI we want to respect the backfacing culling properties of the material. Bounce rays follow the culling behavior of the baker.
    int rayFlags = (bounceIndex == 0) ? UnifiedRT::kRayFlagCullBackFacingTriangles : UnifiedRT::kRayFlagNone;
    iterator.hitResult = TraceRayClosestHit(dispatchInfo, accelStruct, rayMask, iterator.ray, rayFlags);

    iterator.hitGeo = (PTHitGeom) 0;
    iterator.material = (MaterialProperties) 0;
    UnifiedRT::InstanceData instanceInfo = (UnifiedRT::InstanceData) 0;
    if (iterator.hitResult.IsValid())
    {
        instanceInfo = UnifiedRT::GetInstance(iterator.hitResult.instanceID);
        iterator.hitGeo = GetHitGeomInfo(instanceInfo, iterator.hitResult);
        iterator.hitGeo.FixNormals(iterator.ray.direction);

        // Evaluate material properties at hit location
        iterator.material = LoadMaterialProperties(instanceInfo, g_PathtracerAsGiPreviewMode && isFirstRay, iterator.hitGeo);
        traceResult = TRACE_HIT;
    }
    else
    {
        traceResult = TRACE_MISS;
    }

    if (traceResult == TRACE_HIT)
    {
        // If we hit a transmissive face, we should transmit or reflect.
        if (ShouldTransmitRay(rngState, iterator.material))
        {
            traceResult = TRACE_TRANSMISSION;
        }
        // We've hit a surface that should be treated as a backface, we should kill the ray instead of bouncing. This matches the old behavior.
        else if (ShouldTreatAsBackface(iterator.hitResult, iterator.material))
        {
            traceResult = TRACE_MISS;
        }
    }

    return traceResult;
}

// Add radiance due to a missed ray which should sample the environment.
void AddEnvironmentRadiance(inout PathIterator iterator, bool applyIndirectScale)
{
    float3 envRadiance;
    float envPdf;
    if (GetEnvironmentLightEmissionAndDensity(iterator.ray.direction, envRadiance, envPdf))
    {
        envPdf /= g_NumLights;

        if (applyIndirectScale)
            envRadiance *= g_IndirectScale;

        AddRadiance(iterator.radianceSample, iterator.throughput, envRadiance * EmissiveMISWeightForBrdfRay(envPdf, iterator.lastScatterProbabilityDensity));
    }
}

// Add radiance due to a hit emissive surface.
void AddEmissionRadiance(inout PathIterator iterator, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList, bool applyIndirectScale)
{
    float lightDensity = ComputeMeshLightDensity(instanceList, iterator.hitGeo, iterator.hitResult.instanceID, iterator.ray.origin);
    float3 emission = iterator.material.emissive;
    if (applyIndirectScale)
        emission *= g_IndirectScale;
    AddRadiance(iterator.radianceSample, iterator.throughput, emission * EmissiveMISWeightForBrdfRay(lightDensity, iterator.lastScatterProbabilityDensity));
}

// Add radiance due to directly sampled lights.
void AddRadianceFromDirectIllumination(
    inout PathIterator iterator,
    uint shadowRayMask,
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<UnifiedRT::InstanceData> instanceList,
    inout PathTracingSampler rngState,
    bool isDirect)
{
    const float3 radianceSample = EvalDirectIllumination(
        dispatchInfo, accelStruct, instanceList, isDirect, shadowRayMask, iterator.hitGeo, iterator.material, min(g_LightEvaluations, MAX_LIGHT_EVALUATIONS), rngState);
    AddRadiance(iterator.radianceSample, iterator.throughput, radianceSample);
}

// Add radiance due to directly sampled lights and randomly hit emissive surfaces.
void AddRadianceFromEmissionAndDirectIllumination(inout PathIterator iterator, int bounceIndex, uint shadowRayMask, UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList, inout PathTracingSampler rngState)
{
    bool isFirstRay = (bounceIndex == 0);

    // Emission
    if (!g_PathtracerAsGiPreviewMode || !isFirstRay)
    {
        AddEmissionRadiance(iterator, accelStruct, instanceList, !isFirstRay);
    }

    // Check if we should do NEE, respecting the max bounce count
    if (!g_CountNEERayAsPathSegment || bounceIndex < g_BounceCount)
    {
        AddRadianceFromDirectIllumination(iterator, shadowRayMask, dispatchInfo, accelStruct, instanceList, rngState, g_PathtracerAsGiPreviewMode && isFirstRay);
    }
}

// Trace the next path segment and accumulate radiance due to directly sampled lights, randomly hit emissive surfaces, and missed rays which sample the environment.
uint TraceBounceRayAndAddRadiance(inout PathIterator iterator, int bounceIndex, uint rayMask, uint shadowRayMask, UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, StructuredBuffer<UnifiedRT::InstanceData> instanceList, inout PathTracingSampler rngState)
{
    uint traceResult = TraceBounceRay(iterator, bounceIndex, rayMask, dispatchInfo, accelStruct, rngState);

    if (traceResult == TRACE_HIT)
    {
        AddRadianceFromEmissionAndDirectIllumination(iterator, bounceIndex, shadowRayMask, dispatchInfo, accelStruct, instanceList, rngState);
    }
    else if (traceResult == TRACE_MISS)
    {
        AddEnvironmentRadiance(iterator, bounceIndex != 0);
    }

    float bullet = rngState.GetFloatSample(RAND_DIM_RUSSIAN_ROULETTE);

    if (bounceIndex >= RUSSIAN_ROULETTE_MIN_BOUNCES)
    {
        float p = max(iterator.throughput.x, max(iterator.throughput.y, iterator.throughput.z));
        if (bullet > p)
            traceResult = TRACE_MISS;
        else
            iterator.throughput /= p;
    }

    return traceResult;
}


bool Scatter(inout PathIterator iterator, inout PathTracingSampler rngState)
{
    float brdfPdf;
    int event = ScatterDiffusely(iterator.hitGeo, -iterator.ray.direction, rngState, iterator.ray, brdfPdf);
    if (event == RAY_TERMINATION)
        return false;

    // Here we assumes two things:
    // 1) We use cosine distribution for bounce.
    // 2) We never multiply the cosine density, cos(θ)/π.
    // This cancels out the cosine term of the rendering equation and the division by π
    // in the diffuse BRDF. Thus we only need to multiply by albedo below.
    iterator.throughput *= iterator.material.baseColor;

    iterator.lastScatterProbabilityDensity = brdfPdf;
    return true;
}

float3 LoadMaterialTransmission(UnifiedRT::InstanceData instanceInfo, float2 uv0)
{
    int materialIndex = instanceInfo.userMaterialID;
    PTMaterial matInfo = g_MaterialList[materialIndex];

    bool pointSampleTransmission = (matInfo.flags & 4) != 0;

    return saturate(SampleAtlas(g_TransmissionTextures, sampler_g_TransmissionTextures, matInfo.transmissionTextureIndex, uv0, matInfo.transmissionScale, matInfo.transmissionOffset, pointSampleTransmission).rgb);
}

uint AnyHitExecute(UnifiedRT::HitContext hitContext, inout PathTracingPayload payload)
{
    UnifiedRT::Hit hit;
    hit.instanceID = hitContext.InstanceID();
    hit.primitiveIndex = hitContext.PrimitiveIndex();
    hit.uvBarycentrics = hitContext.UvBarycentrics();
    hit.hitDistance = hitContext.RayTCurrent();
    hit.isFrontFace = hitContext.IsFrontFace();

    UnifiedRT::InstanceData instanceInfo = UnifiedRT::GetInstance(hitContext.InstanceID());
    UnifiedRT::HitGeomAttributes attributes = UnifiedRT::FetchHitGeomAttributes(hit);
    float2 uv0 = attributes.uv0.xy;

    float3 transmission = LoadMaterialTransmission(instanceInfo, uv0);

    // With proper support for IOR, the probability of refraction should be based on the materials fresnel.
    // We don't have this information, so we base it on average transmission color, which matches the old baker.
    // Additionally, we should divide the contribution of the ray by the probability of choosing either reflection or refraction,
    // but we intentionally don't do this, since the old baker didn't do it either.

    if (all(saturate(transmission) < 0.000001f))
    {
        return UnifiedRT::kAcceptHit;
    }
    else
    {
        payload.SetTransmission(payload.GetTransmission() * saturate(transmission));
        return UnifiedRT::kIgnoreHit;
    }
}

void ClosestHitExecute(UnifiedRT::HitContext hitContext, inout PathTracingPayload payload)
{
    if (payload.IsShadowRay())
    {
        payload.MarkHit();

    }
    else
    {
        UnifiedRT::Hit hit = (UnifiedRT::Hit)0;
        hit.instanceID = hitContext.InstanceID();
        hit.primitiveIndex = hitContext.PrimitiveIndex();
        hit.uvBarycentrics = hitContext.UvBarycentrics();
        hit.isFrontFace = hitContext.IsFrontFace();
        hit.hitDistance = hitContext.RayTCurrent();
        payload.SetHit(hit);
    }
}
