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

    uint2 bits = uint2(Compact1By1(v), Compact1By1(v >> 1));

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

    return true;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    // Transform to the local frame for spherical coordinates,
    // Note that the strand direction is assumed to lie pointing down the X axis, as this is expected by the BSDF.
    float3x3 frame = GetLocalFrame(mtlData.bsdfData.normalWS, mtlData.bsdfData.hairStrandDirectionWS);
    float3 wi = mul(sampleDir, transpose(frame));
    float3 wo = mul(mtlData.V, transpose(frame));

    CBSDF cbsdf = EvaluateHairReference(wo, wi, mtlData.bsdfData);

    result.specValue = cbsdf.specR * abs(wi.z);
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

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
