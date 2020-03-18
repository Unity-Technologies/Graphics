#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// Lit Material Data:
//
// bsdfCount    3
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  GGX BRDF
// bsdfWeight2  GGX BTDF

MaterialData CreateMaterialData(BSDFData bsdfData, float3 V)
{
    MaterialData mtlData;
    mtlData.bsdfCount = 3;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(bsdfData.geomNormalWS, V))
    {
        float  NdotV = dot(bsdfData.normalWS, V);
        float3 F = F_Schlick(bsdfData.fresnel0, NdotV);

        // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
        bool consistentNormal = (NdotV > 0.001);
        mtlData.bsdfWeight[0] = Luminance(bsdfData.diffuseColor) * (1.0 - bsdfData.transmittanceMask);
        mtlData.bsdfWeight[1] = consistentNormal ? lerp(Luminance(F), 0.5, bsdfData.roughnessT) : 0.0;
        mtlData.bsdfWeight[2] = consistentNormal ? (1.0 - mtlData.bsdfWeight[1]) * bsdfData.transmittanceMask : 0.0;
    }
    else // Below
    {
        mtlData.bsdfWeight[0] = 0.0;
        mtlData.bsdfWeight[1] = 0.0;
        mtlData.bsdfWeight[2] = 1.0;
    }

    // If we are basically black, no need to compute anything else for this material
    if (!IsBlack(mtlData))
    {
        float denom = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2];
        mtlData.bsdfWeight[0] /= denom;
        mtlData.bsdfWeight[1] /= denom;
        mtlData.bsdfWeight[2] /= denom;

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
        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleDiffuse(mtlData, inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, sampleDir, result.specValue, result.specPdf);
                result.specPdf *= mtlData.bsdfWeight[1];
            }

            result.diffPdf *= mtlData.bsdfWeight[0];
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Specular BRDF
        {
            if (!BRDF::SampleGGX(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateDiffuse(mtlData, sampleDir, result.diffValue, result.diffPdf);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            result.specPdf *= mtlData.bsdfWeight[1];
        }
        else // Specular BTDF
        {
            if (!BTDF::SampleGGX(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specPdf *= mtlData.bsdfWeight[2];
        }
    }
    else // Below
    {
        if (!BTDF::SampleDelta(mtlData, sampleDir, result.specValue, result.specPdf))
            return false;
    }

    return true;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateDiffuse(mtlData, sampleDir, result.diffValue, result.diffPdf);
            result.diffPdf *= mtlData.bsdfWeight[0];
        }
        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, sampleDir, result.specValue, result.specPdf);
            result.specPdf *= mtlData.bsdfWeight[1];
        }
    }
}
