#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// Lit Material Data:
//
// bsdfCount    4
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Coat GGX BRDF
// bsdfWeight2  Spec GGX BRDF
// bsdfWeight3  Spec GGX BTDF

MaterialData CreateMaterialData(BSDFData bsdfData, float3 V)
{
    MaterialData mtlData;
    mtlData.bsdfCount = 4;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(bsdfData.geomNormalWS, V))
    {
        float NdotV = dot(bsdfData.normalWS, V);
        float Fcoat = HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT) ? F_Schlick(CLEAR_COAT_F0, NdotV) * bsdfData.coatMask : 0.0;
        float Fspec = Luminance(F_Schlick(bsdfData.fresnel0, NdotV));

        // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
        bool consistentNormal = (NdotV > 0.001);
        mtlData.bsdfWeight[1] = consistentNormal ? Fcoat : 0.0;
        mtlData.bsdfWeight[2] = consistentNormal ? (1.0 - mtlData.bsdfWeight[1]) * lerp(Fspec, 0.5, bsdfData.roughnessT) : 0.0;
        mtlData.bsdfWeight[3] = consistentNormal ? (1.0 - mtlData.bsdfWeight[1] - mtlData.bsdfWeight[2]) * bsdfData.transmittanceMask : 0.0;
        mtlData.bsdfWeight[0] = (1.0 - mtlData.bsdfWeight[1]) * (1.0 - bsdfData.transmittanceMask) * Luminance(bsdfData.diffuseColor);

    }
    else // Below
    {
        float NdotV = -dot(bsdfData.normalWS, V);
        float F = F_FresnelDielectric(1.0 / mtlData.bsdfData.ior, NdotV);

        // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
        bool consistentNormal = (NdotV > 0.001);
        mtlData.bsdfWeight[0] = 0.0;
        mtlData.bsdfWeight[1] = 0.0;
        mtlData.bsdfWeight[2] = consistentNormal ? F : 0.0;
        mtlData.bsdfWeight[3] = consistentNormal ? (1.0 - mtlData.bsdfWeight[1]) * bsdfData.transmittanceMask : 0.0;
    }

    // If we are basically black, no need to compute anything else for this material
    if (!IsBlack(mtlData))
    {
        float denom = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2] + mtlData.bsdfWeight[3];
        mtlData.bsdfWeight[0] /= denom;
        mtlData.bsdfWeight[1] /= denom;
        mtlData.bsdfWeight[2] /= denom;
        mtlData.bsdfWeight[3] /= denom;

        // Keep these around, rather than passing them to all methods
        mtlData.bsdfData = bsdfData;
        mtlData.V = V;
    }

    return mtlData;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value;
        float pdf;
        float fresnelSpec, fresnelClearCoat = 0.0;

        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleDiffuse(mtlData, inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            result.diffPdf *= mtlData.bsdfWeight[0];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, value, pdf, fresnelClearCoat);
                fresnelClearCoat *= mtlData.bsdfData.coatMask;
                result.specValue += value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, mtlData.bsdfData.roughnessT, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
                result.specValue += value * (1.0 - fresnelClearCoat);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Clear coat BRDF
        {
            if (!BRDF::SampleGGX(mtlData, CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, inputSample, sampleDir, result.specValue, result.specPdf, fresnelClearCoat))
                return false;

            fresnelClearCoat *= mtlData.bsdfData.coatMask;
            result.specValue *= mtlData.bsdfData.coatMask;
            result.specPdf *= mtlData.bsdfWeight[1];

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateDiffuse(mtlData, sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, mtlData.bsdfData.roughnessT, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
                result.specValue += value * (1.0 - fresnelClearCoat);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2]) // Specular BRDF
        {
            if (!BRDF::SampleGGX(mtlData, mtlData.bsdfData.roughnessT, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf, fresnelSpec))
                return false;

            result.specPdf *= mtlData.bsdfWeight[2];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, value, pdf, fresnelClearCoat);
                fresnelClearCoat *= mtlData.bsdfData.coatMask;
                result.specValue = result.specValue * (1.0 - fresnelClearCoat) + value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateDiffuse(mtlData, sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }
        else // Specular BTDF
        {
            if (!BTDF::SampleGGX(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

#ifdef _REFRACTION_THIN
            sampleDir = refract(sampleDir, mtlData.bsdfData.normalWS, mtlData.bsdfData.ior);
            if (!any(sampleDir))
                return false;
#endif

            result.specValue *= mtlData.bsdfData.transmittanceMask;
            result.specPdf *= mtlData.bsdfWeight[3];
        }
    }
    else // Below
    {
#ifdef _REFRACTION_THIN
        if (mtlData.bsdfData.transmittanceMask)
        {
            // Just go through (although we should not end up here)
            sampleDir = -mtlData.V;
            result.specValue = DELTA_PDF;
            result.specPdf = DELTA_PDF;
        }
#else
        if (inputSample.z < mtlData.bsdfWeight[2]) // Specular BRDF
        {
            if (!BRDF::SampleDelta(mtlData, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specPdf *= mtlData.bsdfWeight[2];
        }
        else // Specular BTDF
        {
            if (!BTDF::SampleDelta(mtlData, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specPdf *= mtlData.bsdfWeight[3];
        }
#endif
    }

    return true;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value;
        float pdf;
        float fresnelSpec, fresnelClearCoat = 0.0;

        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, result.specValue, result.specPdf, fresnelClearCoat);
            fresnelClearCoat *= mtlData.bsdfData.coatMask;
            result.specValue *= mtlData.bsdfData.coatMask;
            result.specPdf *= mtlData.bsdfWeight[1];
        }

        if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateDiffuse(mtlData, sampleDir, result.diffValue, result.diffPdf);
            result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
            result.diffPdf *= mtlData.bsdfWeight[0];
        }

        if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, mtlData.bsdfData.roughnessT, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
            result.specValue += value * (1.0 - fresnelClearCoat);
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }
    }
}
