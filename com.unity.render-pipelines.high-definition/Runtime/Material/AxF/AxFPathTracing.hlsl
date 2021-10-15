#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// AxF Material Data:
//
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Clearoat BRDF
// bsdfWeight2  Specular BRDF(s)

float3 GetCoatNormal(MaterialData mtlData)
{
    return mtlData.bsdfData.clearcoatNormalWS;
}

#ifdef _AXF_BRDF_TYPE_CAR_PAINT
float GetSpecularCoeffSum(MaterialData mtlData)
{
    return mtlData.bsdfData.height_mm;
}
#endif

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, MaterialData mtlData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughness.x = max(pathIntersection.maxRoughness, bsdfData.roughness.x);
    bsdfData.roughness.y = max(pathIntersection.maxRoughness, bsdfData.roughness.y);
#ifdef _AXF_BRDF_TYPE_CAR_PAINT
    bsdfData.roughness.z = max(pathIntersection.maxRoughness, bsdfData.roughness.z);
#endif

    // One of the killer features of AxF, optional specular Fresnel...
    if (!HasFresnelTerm())
        bsdfData.fresnel0 = 1.0;

    // Make sure we can get valid coat normal reflection directions
    if (HasClearcoat())
        bsdfData.clearcoatNormalWS = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.clearcoatNormalWS);

#ifdef _AXF_BRDF_TYPE_CAR_PAINT
    // We hijack height_mm, as it is not used here otherwise, to store the specular coefficients sum
    bsdfData.height_mm = 0.0;
    UNITY_UNROLL
    for (uint i = 0; i < CARPAINT2_LOBE_COUNT; i++)
        bsdfData.height_mm += _CarPaint2_CTCoeffs[i];
#endif
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.V = -WorldRayDirection();
    mtlData.Nv = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.normalWS);
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(mtlData))
    {
        float NcoatdotV = dot(GetCoatNormal(mtlData), mtlData.V);
        float NspecdotV = dot(GetSpecularNormal(mtlData), mtlData.V);
        float Fcoat = F_Schlick(IorToFresnel0(bsdfData.clearcoatIOR), NcoatdotV);
        float Fspec = Luminance(F_Schlick(mtlData.bsdfData.fresnel0, NspecdotV));

#if defined(_AXF_BRDF_TYPE_SVBRDF)
        float specularCoeff = Luminance(mtlData.bsdfData.specularColor);
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
        float specularCoeff = GetSpecularCoeffSum(mtlData);
#endif

        mtlData.bsdfWeight[1] = HasClearcoat() ? Fcoat * Luminance(mtlData.bsdfData.clearcoatColor) : 0.0;
        float clearcoatTransmission = HasClearcoat() ? 1.0 - Fcoat : 1.0;
        mtlData.bsdfWeight[2] = clearcoatTransmission * lerp(Fspec, 0.5, GetScalarRoughness(mtlData.bsdfData.roughness)) * specularCoeff;
        mtlData.bsdfWeight[0] = clearcoatTransmission * Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;
    }

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

    return true;
}

#if defined(_AXF_BRDF_TYPE_SVBRDF)
#   include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPathTracingSVBRDF.hlsl"
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
#   include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPathTracingCarPaint.hlsl"
#endif

float3 GetLightNormal(MaterialData mtlData)
{
    // If both diffuse and specular normals are quasi-indentical, return one of them, otherwise return a null vector
    return dot(GetDiffuseNormal(mtlData), GetSpecularNormal(mtlData)) > 0.99 ? GetDiffuseNormal(mtlData) : float3(0.0, 0.0, 0.0);
}

float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // Adjust the max roughness, based on the estimated diff/spec ratio
    float maxSpecRoughness = Max3(mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.roughness.z);
    float adjustedPathRoughness = (mtlResult.specPdf * maxSpecRoughness + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);

    return adjustedPathRoughness;
}

float3 ApplyAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow, float3 value)
{
    return value;
}
