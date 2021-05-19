#ifndef UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED
#define UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED

// By defining this, we replace specular GGX values with the original ones used in the raster version
#define AXF_PATH_TRACING_SVBRDF_USE_RASTER_SPECULAR

#ifdef AXF_PATH_TRACING_SVBRDF_USE_RASTER_SPECULAR
void OverrideSpecularValue(MaterialData mtlData, float3 sampleDir, inout float3 specularValue)
{
    float3 H = normalize(mtlData.V + sampleDir);
    float LdotH = dot(sampleDir, H);
    float NdotL = dot(GetSpecularNormal(mtlData), sampleDir);
    float NdotV = dot(GetSpecularNormal(mtlData), mtlData.V);

    // We set a threshold of 10x the value computed by our path tracing GGX BRDF
    float maxLum = 10.0 * Luminance(specularValue);

    switch (AXF_SVBRDF_BRDFTYPE_SPECULARTYPE)
    {
        case 0:
            specularValue = ComputeWard(H, LdotH, NdotL, NdotV, (PreLightData)0, mtlData.bsdfData) * NdotL / mtlData.bsdfData.specularColor;
            break;
        case 1:
            specularValue = ComputeBlinnPhong(H, LdotH, NdotL, NdotV, (PreLightData)0, mtlData.bsdfData) * NdotL / mtlData.bsdfData.specularColor;
            break;
        case 2:
            specularValue = ComputeCookTorrance(H, LdotH, NdotL, NdotV, (PreLightData)0, mtlData.bsdfData) * NdotL / mtlData.bsdfData.specularColor;
            break;
        default:
            return;
    }

    // Make sure the new value is not above our luminance threshold, for robustness sake, as the Compute*(...) above are numerically sensitive...
    float lum = Luminance(specularValue);
    if (lum > maxLum)
        specularValue *= maxLum / lum;
}
#endif

bool SampleSpecular(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out float3 value, out float pdf)
{
    if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, inputSample, sampleDir, value, pdf))
        return false;

#ifdef AXF_PATH_TRACING_SVBRDF_USE_RASTER_SPECULAR
    OverrideSpecularValue(mtlData, sampleDir, value);
#endif

    return true;
}

void EvaluateSpecular(MaterialData mtlData, float3 sampleDir, out float3 value, out float pdf)
{
    BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness.x, mtlData.bsdfData.roughness.y, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);

#ifdef AXF_PATH_TRACING_SVBRDF_USE_RASTER_SPECULAR
    OverrideSpecularValue(mtlData, sampleDir, value);
#endif
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
                EvaluateSpecular(mtlData, sampleDir, value, pdf);
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
                EvaluateSpecular(mtlData, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * mtlData.bsdfData.specularColor;
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else // Specular BRDF
        {
            if (!SampleSpecular(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
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
            EvaluateSpecular(mtlData, sampleDir, value, pdf);
            result.specValue += value * coatingTransmission * mtlData.bsdfData.specularColor;
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }
    }
}

#endif // UNITY_AXF_PATH_TRACING_SVBRDF_INCLUDED
