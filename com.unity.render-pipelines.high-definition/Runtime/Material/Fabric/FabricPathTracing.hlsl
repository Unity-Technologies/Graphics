#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// Fabric Material Data:
//
// Cotton/Wool mode:
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Sheen BRDF
// bsdfWeight2  Diffuse BTDF
//
// Silk mode:
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Spec GGX BRDF
// bsdfWeight2  Diffuse BTDF

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(pathIntersection.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(pathIntersection.maxRoughness, bsdfData.roughnessB);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        // This is hacky, but applied to match the raster implementation (Fabric.hlsl)
        bsdfData.diffuseColor *= FabricLambertNoPI(bsdfData.roughnessT);
    }
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;
    mtlData.V = -WorldRayDirection();
    mtlData.Nv = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.normalWS);

    if (!IsAbove(mtlData))
        return false;

    mtlData.bsdfWeight[0] = Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;

    // If N.V < 0 (can happen with normal mapping, or smooth normals on coarsely tesselated objects) we want to avoid spec sampling
    float NdotV = dot(GetSpecularNormal(mtlData), mtlData.V);
    if (NdotV > 0.001)
    {
        // For the cotton/wool material, diffuse and sheen BRDFs share the same cosine-weighted sampling, so we only give the upper hemisphere
        // a gentle nudge with a small added weight (hence the 0.1 factor), while making sure it is not null if diffuse color is black
        mtlData.bsdfWeight[1] = HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL) ?
            0.1 * Luminance(mtlData.bsdfData.fresnel0) : Luminance(F_Schlick(mtlData.bsdfData.fresnel0, NdotV));
    }

    bool hasTransmission = HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION);
    if (hasTransmission)
        mtlData.bsdfWeight[2] = mtlData.bsdfWeight[0] * Luminance(mtlData.bsdfData.transmittance);

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

    if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
    {
        float subsurfaceWeight = mtlData.bsdfWeight[0] * mtlData.bsdfData.subsurfaceMask * (1.0 - pathIntersection.maxRoughness);

        mtlData.isSubsurface = theSample < subsurfaceWeight;
        if (mtlData.isSubsurface)
        {
            // We do a full, ray-traced subsurface scattering computation here:
            // Let's try and change shading position and normal, and replace the diffuse color by the subsurface throughput
            mtlData.subsurfaceWeightFactor = subsurfaceWeight;

            SSS::Result subsurfaceResult;
            float3 meanFreePath = 0.001 / (_ShapeParamsAndMaxScatterDists[mtlData.bsdfData.diffusionProfileIndex].rgb * _WorldScalesAndFilterRadiiAndThicknessRemaps[mtlData.bsdfData.diffusionProfileIndex].x);

            if (!SSS::RandomWalk(shadingPosition, GetDiffuseNormal(mtlData), mtlData.bsdfData.diffuseColor, meanFreePath, pathIntersection.pixelCoord, subsurfaceResult, hasTransmission))
                return false;

            shadingPosition = subsurfaceResult.exitPosition;
            mtlData.bsdfData.normalWS = subsurfaceResult.exitNormal;
            mtlData.bsdfData.geomNormalWS = subsurfaceResult.exitNormal;
            mtlData.bsdfData.diffuseColor = subsurfaceResult.throughput;
        }
        else
        {
            // Otherwise, we just compute BSDFs as usual
            mtlData.subsurfaceWeightFactor = 1.0 - subsurfaceWeight;

            mtlData.bsdfWeight[0] = max(mtlData.bsdfWeight[0] - subsurfaceWeight, BSDF_WEIGHT_EPSILON);
            mtlData.bsdfWeight /= mtlData.subsurfaceWeightFactor;

            theSample -= subsurfaceWeight;
        }

        // Rescale the sample we used for the SSS selection test
        theSample /= mtlData.subsurfaceWeightFactor;
    }

    return true;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
    {
        if (mtlData.isSubsurface)
        {
            if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            result.diffValue *= mtlData.bsdfData.ambientOcclusion;

            return true;
        }
    }

    if (!IsAbove(mtlData))
        return false;

    if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // BRDFs
    {
        if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
        {
            float3 value;
            float pdf;

            if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, value, pdf))
                return false;

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                result.diffValue = value * mtlData.bsdfData.ambientOcclusion;
                result.diffPdf = pdf * mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateSheen(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, sampleDir, result.specValue, result.specPdf);
                result.specPdf *= mtlData.bsdfWeight[1];
            }
        }
        else // MATERIALFEATUREFLAGS_FABRIC_SILK
        {
            if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
            {
                if (!BRDF::SampleBurley(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                    return false;

                result.diffValue *= mtlData.bsdfData.ambientOcclusion;
                result.diffPdf *= mtlData.bsdfWeight[0];

                if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
                {
                    BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, sampleDir, result.specValue, result.specPdf);
                    result.specPdf *= mtlData.bsdfWeight[1];
                }
            }
            else // Spec GGX BRDF
            {
                if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf))
                    return false;

                result.specPdf *= mtlData.bsdfWeight[1];

                if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
                {
                    BRDF::EvaluateBurley(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                    result.diffValue *= mtlData.bsdfData.ambientOcclusion;
                    result.diffPdf *= mtlData.bsdfWeight[0];
                }
            }
        }

        if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
        {
            // We compensate for the fact that there is no spec when computing SSS
            result.specValue /= mtlData.subsurfaceWeightFactor;
        }
    }
    else // Diffuse BTDF
    {
        if (!BTDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
            return false;

        result.diffValue *= mtlData.bsdfData.transmittance * mtlData.bsdfData.ambientOcclusion;
        result.diffPdf *= mtlData.bsdfWeight[2];

        if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
        {
            // We compensate for the fact that there is no transmission when computing SSS
            result.diffValue /= mtlData.subsurfaceWeightFactor;
        }
    }

    return result.diffPdf + result.specPdf > 0.0;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
    {
        if (mtlData.isSubsurface)
        {
            BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
            return;
        }
    }

    if (IsAbove(mtlData))
    {
        if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
        {
            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateSheen(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, sampleDir, result.specValue, result.specPdf);
                result.specPdf *= mtlData.bsdfWeight[1];
            }
        }
        else // MATERIALFEATUREFLAGS_FABRIC_SILK
        {
            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateBurley(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, sampleDir, result.specValue, result.specPdf);
                result.specPdf *= mtlData.bsdfWeight[1];
            }
        }

        if (IsBelow(GetDiffuseNormal(mtlData), sampleDir) && mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            BTDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
            result.diffValue *= mtlData.bsdfData.transmittance;
            result.diffPdf *= mtlData.bsdfWeight[2];

            if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
            {
                // We compensate for the fact that there is no transmission when computing SSS
                result.diffValue /= mtlData.subsurfaceWeightFactor;
            }
        }

        if (HasFlag(mtlData.bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
        {
            // We compensate for the fact that there is no spec when computing SSS
            result.specValue /= mtlData.subsurfaceWeightFactor;
        }
    }
}

float3 GetLightNormal(MaterialData mtlData)
{
    // If both diffuse and specular normals are quasi-indentical, return one of them, otherwise return a null vector
    return dot(GetDiffuseNormal(mtlData), GetSpecularNormal(mtlData)) > 0.99 ? GetDiffuseNormal(mtlData) : float3(0.0, 0.0, 0.0);
}

float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // Adjust the max roughness, based on the estimated diff/spec ratio
    return (mtlResult.specPdf * max(mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB) + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);
}

float3 ApplyAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow, float3 value)
{
    // No absorption here
    return value;
}
