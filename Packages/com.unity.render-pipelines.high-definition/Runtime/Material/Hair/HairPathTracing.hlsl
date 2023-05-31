// Dual scattering was added to the path tracer to supplement the development of our rasterized approximation of it.
// Currently we compile all of it away like this in the shipped HDRP as we do not actually recommend it for achieving
// multiple scattering for hair in the path tracer. For that, the user should instead set a high bounce count on the volume
// setting and ensure that their hair strands are a typical width (~0.12 millimeters).
// #define _PATH_TRACED_DUAL_SCATTERING

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingPayload.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingAOV.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceFillingCurves.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

// https://www.pbrt.org/hair.pdf
float2 DemuxFloat(float x)
{
    uint64_t v = x * (((uint64_t)1) << 32);

    uint2 bits = uint2(Compact1By1((uint)v), Compact1By1((uint)(v >> 1)));

    return float2(bits.x / float(1 << 16),
                  bits.y / float(1 << 16));
}

// --------------------------------------------------------------------------------------

void ProcessBSDFData(PathPayload payload, BuiltinData builtinData, inout BSDFData bsdfData)
{
    // NOTE: Currently we don't support ray-aligned ribbons in the acceleration structure, so our only H-calculation routines
    // are either stochastic or derived from a tube intersection.
#if 0
    bsdfData.h = GetHFromTube(-WorldRayDirection(), bsdfData.normalWS, bsdfData.hairStrandDirectionWS);
#else
    bsdfData.h = -1 + 2 * InterleavedGradientNoise(payload.pixelCoord, _RaytracingSampleIndex);
#endif

    // Do not use the spline visibility term when path tracing.
    bsdfData.visibility = -1;
}

bool CreateMaterialData(PathPayload payload, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Kajiya not supported.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
        return false;

    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(payload, builtinData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;
    mtlData.V = -WorldRayDirection();

#ifdef _PATH_TRACED_DUAL_SCATTERING
    mtlData.positionWS = shadingPosition;
#endif

    return true;
}

#ifdef _PATH_TRACED_DUAL_SCATTERING

float3 ComputeDualScatteringRayShooting(MaterialData mtlData, float3 sampleDir, inout float3 Fs)
{
    // Initialize the strand count payload
    PathPayload strandCountPayload;
    strandCountPayload.segmentID = SEGMENT_ID_DUAL_SCATTERING;
    strandCountPayload.alpha     = 0.0; // Encode the intersected strand count in alpha.

    // Initialize the shadow ray
    RayDesc ray;
    ray.Origin    = mtlData.positionWS + mtlData.bsdfData.geomNormalWS * _RayTracingRayBias;
    ray.Direction = sampleDir;
    ray.TMin      = 0.0;
    ray.TMax      = 10.0; // TODO: Dist to light

    // Shoot a ray that counts the number of intersected strands
    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
             RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, ray, strandCountPayload);

    // Retrieve angles via spherical coordinates in the hair shading space.
    HairAngle angles;
    ZERO_INITIALIZE(HairAngle, angles);

    // Transform to the local frame for spherical coordinates,
    // Note that the strand direction is assumed to lie pointing down the +X axis.
    const float3x3 frame = GetLocalFrame(mtlData.bsdfData.normalWS, mtlData.bsdfData.hairStrandDirectionWS);
    const float3 wi = mul(sampleDir, transpose(frame));
    const float3 wo = mul(mtlData.V, transpose(frame));
    GetHairAngleLocal(wo, wi, angles);

    // Pass along the strand count to the general dual scattering computation
    return ComputeDualScattering(mtlData.bsdfData, angles, strandCountPayload.alpha, Fs);
}

#endif

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    // Transform to the local frame for spherical coordinates,
    // Note that the strand direction is assumed to lie pointing down the X axis, as this is expected by the BSDF.
    float3x3 frame = GetLocalFrame(mtlData.bsdfData.normalWS, mtlData.bsdfData.hairStrandDirectionWS);
    float3 wo = mul(sampleDir, transpose(frame));
    float3 wi = mul(mtlData.V, transpose(frame));

    CBSDF cbsdf = EvaluateHairReference(wo, wi, mtlData.bsdfData);

    result.specValue = cbsdf.specR * abs(wi.z);

#ifdef _PATH_TRACED_DUAL_SCATTERING
    // Dual-scattering approximation rather than brute-forcing with a high path depth.
    result.specValue = ComputeDualScatteringRayShooting(mtlData, sampleDir, result.specValue);
#endif
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

#ifdef _PATH_TRACED_DUAL_SCATTERING
    // Do not evaluate more bounces if we are computing dual scattering for hair.
    return false;
#endif

#if 0
    // We sample the sphere due to reflective and transmittive events.
    sampleDir = SampleSphereUniform(inputSample.x, inputSample.y);

    EvaluateMaterial(mtlData, sampleDir, result);

    result.specPdf = INV_FOUR_PI;
#else
    // Transform to the local frame for spherical coordinates,
    // Note that the strand direction is assumed to lie pointing down the X axis, as this is expected by the BSDF.
    float3x3 frame = GetLocalFrame(mtlData.bsdfData.normalWS, mtlData.bsdfData.hairStrandDirectionWS);
    float3 wo = mul(mtlData.V, transpose(frame));

    // Need four random samples, derive two extra ones from the given third.
    float4 u = float4(
        inputSample.xy,
        DemuxFloat(inputSample.z)
    );

    CBSDF cbsdf = SampleHairReference(wo, sampleDir, result.specPdf, u, mtlData.bsdfData);
    result.specValue = cbsdf.specR * abs(sampleDir.z);

    // Transform back into world space.
    sampleDir = mul(sampleDir, frame);
#endif

    return true;
}

float3 GetLightNormal(MaterialData mtlData)
{
    // If both diffuse and specular normals are quasi-indentical, return one of them, otherwise return a null vector
    return dot(GetDiffuseNormal(mtlData), GetSpecularNormal(mtlData)) > 0.99 ? GetDiffuseNormal(mtlData) : float3(0.0, 0.0, 0.0);
}


float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // TODO
    return pathRoughness;
}

float3 GetMaterialAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow)
{
    // TODO
    return 1.0;
}

void GetAOVData(BSDFData bsdfData, out AOVData aovData)
{
    aovData.albedo = bsdfData.diffuseColor;
    aovData.normal = bsdfData.normalWS;
}
