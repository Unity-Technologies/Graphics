#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

#define HAIR_TYPE_RIBBON 1 << 0
#define HAIR_TYPE_TUBE   1 << 1

#define HAIR_TYPE HAIR_TYPE_TUBE

float GetHFromTube(float3 L, float3 N, float3 T)
{
    // Angle of inclination from normal plane.
    float sinTheta = dot(L, T);

    // Project w to the normal plane.
    float3 LProj = L - sinTheta * T;

    // Find gamma in the normal plane.
    float cosGamma = dot(LProj, N);

    // Length along the fiber width.
    return SafeSqrt(1 - Sq(cosGamma));
}

// --------------------------------------------------------------------------------------

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, inout BSDFData bsdfData)
{
    // NOTE: Currently we don't support ray-aligned ribbons in the acceleration structure, so our only H-calculation routines
    // are either stochastic or derived from a tube intersection.
#if 1
    bsdfData.h = GetHFromTube(-WorldRayDirection(), bsdfData.normalWS, bsdfData.hairStrandDirectionWS);
#else
    bsdfData.h = -1 + 2 * GenerateHashedRandomFloat(uint3(pathIntersection.pixelCoord, _RaytracingSampleIndex));
#endif
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Kajiya not supported.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
        return false;

    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData.bsdfData);

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

    CBSDF cbsdf = EvaluateHairReference(wi, wo, mtlData.bsdfData);

    result.specValue = cbsdf.specR;

    // TODO: Importance Sample
    result.specPdf = INV_FOUR_PI;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    // We sample the sphere due to reflective and transmittive events.
    sampleDir = SampleSphereUniform(inputSample.x, inputSample.y);

    EvaluateMaterial(mtlData, sampleDir, result);

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

float3 ApplyAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow, float3 value)
{
    // TODO

    return value;
}
