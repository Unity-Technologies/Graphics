#ifndef UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED
#define UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED

// AxF SVBRDF Material Data:
//
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Clearoat BRDF
// bsdfWeight2  Specular BRDF

float3 GetCoatNormal(MaterialData mtlData)
{
    return mtlData.bsdfData.clearcoatNormalWS;
}

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, MaterialData mtlData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughness.x = max(pathIntersection.maxRoughness, bsdfData.roughness.x);
    bsdfData.roughness.y = max(pathIntersection.maxRoughness, bsdfData.roughness.y);

    // One of the killer features of AxF, optional specular Fresnel...
    if (!HasFresnelTerm())
        bsdfData.fresnel0 = 1.0;

    // Make sure we can get valid coat normal reflection directions
    if (HasClearcoat())
        bsdfData.clearcoatNormalWS = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.clearcoatNormalWS);
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

        mtlData.bsdfWeight[1] = HasClearcoat() ? Fcoat * Luminance(mtlData.bsdfData.clearcoatColor) : 0.0;
        float clearcoatTransmission = (1.0 - Fcoat); // clearcoatColor only tints the clear coat reflection in this material model...
        mtlData.bsdfWeight[2] = clearcoatTransmission * lerp(Fspec, 0.5, 0.5 * (mtlData.bsdfData.roughness.x + mtlData.bsdfData.roughness.y)) * Luminance(mtlData.bsdfData.specularColor);
        mtlData.bsdfWeight[0] = clearcoatTransmission * Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;
    }

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

    return true;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float  f0ClearCoat = IorToFresnel0(mtlData.bsdfData.clearcoatIOR);
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            result.diffPdf *= mtlData.bsdfWeight[0];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = 1.0 - fresnelClearCoat;
                result.specValue += value * mtlData.bsdfData.clearcoatColor;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * mtlData.bsdfData.specularColor;
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Clear coat BRDF
        {
            if (!BRDF::SampleGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, inputSample, sampleDir, result.specValue, result.specPdf, fresnelClearCoat))
                return false;

            coatingTransmission = 1.0 - fresnelClearCoat;
            result.specValue *= mtlData.bsdfData.clearcoatColor;
            result.specPdf *= mtlData.bsdfWeight[1];

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * mtlData.bsdfData.specularColor;
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else // Specular BRDF
        {
            if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;
            result.specValue *= mtlData.bsdfData.specularColor;
            result.specPdf *= mtlData.bsdfWeight[2];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = 1.0 - fresnelClearCoat;
                result.specValue = result.specValue * coatingTransmission + value * mtlData.bsdfData.clearcoatColor;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }
    }

    return result.diffPdf + result.specPdf > 0.0;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, IorToFresnel0(mtlData.bsdfData.clearcoatIOR), sampleDir, result.specValue, result.specPdf, fresnelClearCoat);
            coatingTransmission = 1.0 - fresnelClearCoat;
            result.specValue *= mtlData.bsdfData.clearcoatColor;
            result.specPdf *= mtlData.bsdfWeight[1];
        }

        if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
            result.diffValue *= coatingTransmission; // AO purposedly ignored here
            result.diffPdf *= mtlData.bsdfWeight[0];
        }

        if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
            result.specValue += value * coatingTransmission * mtlData.bsdfData.specularColor;
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }
    }
}

#endif // UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED
