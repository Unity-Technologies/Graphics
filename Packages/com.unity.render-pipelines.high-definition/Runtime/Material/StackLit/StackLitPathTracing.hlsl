#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// StackLit Material Data:
//
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Coat GGX BRDF
// bsdfWeight2  SpecA GGX BRDF
// bsdfWeight3  SpecB GGX BRDF

float3 GetCoatNormal(MaterialData mtlData)
{
    return mtlData.bsdfData.coatNormalWS;
}

float3 GetSpecularCompensationA(MaterialData mtlData)
{
    return 1.0 + mtlData.bsdfData.specularOcclusionCustomInput * mtlData.bsdfData.fresnel0;
}

float3 GetSpecularCompensationB(MaterialData mtlData)
{
    return 1.0 + mtlData.bsdfData.soFixupStrengthFactor * mtlData.bsdfData.fresnel0;
}

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, MaterialData mtlData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughnessAT = max(pathIntersection.maxRoughness, bsdfData.roughnessAT);
    bsdfData.roughnessAB = max(pathIntersection.maxRoughness, bsdfData.roughnessAB);
    bsdfData.roughnessBT = max(pathIntersection.maxRoughness, bsdfData.roughnessBT);
    bsdfData.roughnessBB = max(pathIntersection.maxRoughness, bsdfData.roughnessBB);

    float NdotV = abs(dot(GetSpecularNormal(mtlData), mtlData.V));

    // Modify fresnel0 value to take iridescence into account
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE) && bsdfData.iridescenceMask > 0.0)
        bsdfData.fresnel0 = lerp(bsdfData.fresnel0, EvalIridescence(1.0, NdotV, bsdfData.iridescenceThickness, bsdfData.fresnel0), bsdfData.iridescenceMask);

    // We store energy compensation coefficients for GGX into the specular occlusion and strength factor
#ifdef STACK_LIT_USE_GGX_ENERGY_COMPENSATION
    float sqrtNdotV = sqrt(NdotV);

    if (bsdfData.lobeMix < 1.0)
        bsdfData.specularOcclusionCustomInput = BRDF::GetGGXMultipleScatteringEnergy(0.5 * (bsdfData.roughnessAT + bsdfData.roughnessAB), sqrtNdotV);
    if (bsdfData.lobeMix > 0.0)
        bsdfData.soFixupStrengthFactor = BRDF::GetGGXMultipleScatteringEnergy(0.5 * (bsdfData.roughnessBT + bsdfData.roughnessBB), sqrtNdotV);
#else
    bsdfData.specularOcclusionCustomInput = 0.0;
    bsdfData.soFixupStrengthFactor = 0.0;
#endif

    // We restore the original coatIor by reverting the premultiplication, when possible (see StackLit.hlsl, l471)
    if (bsdfData.coatMask > 0.001)
        bsdfData.coatIor = (bsdfData.coatIor + bsdfData.coatMask - 1.0) / bsdfData.coatMask;

    // Override exctinction, that we won't need, with the transmission value for the incoming segment
    float sinThetaI = sqrt(1.0 - Sq(NdotV));
    float cosThetaO = sqrt(1.0 - Sq(sinThetaI / bsdfData.coatIor));
    bsdfData.coatExtinction = exp(-bsdfData.coatThickness * bsdfData.coatExtinction / cosThetaO);

    // Make sure we can get valid coat normal reflection directions
    bsdfData.coatNormalWS = IsCoatNormalMapEnabled(bsdfData) ?
        ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.coatNormalWS) : GetSpecularNormal(mtlData);
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.V = -WorldRayDirection();
    mtlData.Nv = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.normalWS);
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;

    // Assume no coating by default
    float3 coatingTransmission = 1.0;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(mtlData))
    {
        float NcoatdotV = dot(GetCoatNormal(mtlData), mtlData.V);
        float NspecdotV = dot(GetSpecularNormal(mtlData), mtlData.V);
        float Fcoat = F_Schlick(IorToFresnel0(bsdfData.coatIor), NcoatdotV);
        float Fspec = Luminance(F_Schlick(mtlData.bsdfData.fresnel0, NspecdotV));

        mtlData.bsdfWeight[1] = Fcoat * mtlData.bsdfData.coatMask;
        coatingTransmission = (1.0 - mtlData.bsdfWeight[1]) * mtlData.bsdfData.coatExtinction;
        float coatingTransmissionWeight = Luminance(coatingTransmission);
        mtlData.bsdfWeight[2] = coatingTransmissionWeight * (1.0 - mtlData.bsdfData.lobeMix) * lerp(Fspec, 0.5, 0.5 * (mtlData.bsdfData.roughnessAT + mtlData.bsdfData.roughnessAB)) * GetSpecularCompensationA(mtlData);
        mtlData.bsdfWeight[3] = coatingTransmissionWeight * mtlData.bsdfData.lobeMix * lerp(Fspec, 0.5, 0.5 * (mtlData.bsdfData.roughnessBT + mtlData.bsdfData.roughnessBB)) * GetSpecularCompensationB(mtlData);
        mtlData.bsdfWeight[0] = coatingTransmissionWeight * Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;
    }

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2] + mtlData.bsdfWeight[3];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    float subsurfaceWeight = mtlData.bsdfWeight[0] * mtlData.bsdfData.subsurfaceMask * (1.0 - pathIntersection.maxRoughness);

    mtlData.isSubsurface = theSample < subsurfaceWeight;
    if (mtlData.isSubsurface)
    {
        // We do a full, ray-traced subsurface scattering computation here:
        // Let's try and change shading position and normal, and replace the diffuse color by the subsurface throughput
        mtlData.subsurfaceWeightFactor = subsurfaceWeight;

        SSS::Result subsurfaceResult;
        float3 meanFreePath = 0.001 / (_ShapeParamsAndMaxScatterDists[mtlData.bsdfData.diffusionProfileIndex].rgb * _WorldScalesAndFilterRadiiAndThicknessRemaps[mtlData.bsdfData.diffusionProfileIndex].x);

#ifdef _MATERIAL_FEATURE_TRANSMISSION
        bool isThin = true;
#else
        bool isThin = false;
#endif
        if (!SSS::RandomWalk(shadingPosition, GetDiffuseNormal(mtlData), mtlData.bsdfData.diffuseColor, meanFreePath, pathIntersection.pixelCoord, subsurfaceResult, isThin))
            return false;

        shadingPosition = subsurfaceResult.exitPosition;
        mtlData.bsdfData.normalWS = subsurfaceResult.exitNormal;
        mtlData.bsdfData.geomNormalWS = subsurfaceResult.exitNormal;
        mtlData.bsdfData.diffuseColor = subsurfaceResult.throughput * coatingTransmission;
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
#endif

    return true;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    if (mtlData.isSubsurface)
    {
        if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
            return false;

        result.diffValue *= mtlData.bsdfData.ambientOcclusion;

        return true;
    }
#endif

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float  f0ClearCoat = IorToFresnel0(mtlData.bsdfData.coatIor);
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            result.diffPdf *= mtlData.bsdfWeight[0];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), mtlData.bsdfData.coatRoughness, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = (1.0 - fresnelClearCoat * mtlData.bsdfData.coatMask) * mtlData.bsdfData.coatExtinction;
                result.specValue += value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * (1.0 - mtlData.bsdfData.lobeMix) * GetSpecularCompensationA(mtlData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }

            if (mtlData.bsdfWeight[3] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * mtlData.bsdfData.lobeMix * GetSpecularCompensationB(mtlData);
                result.specPdf += mtlData.bsdfWeight[3] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Clear coat BRDF
        {
            if (!BRDF::SampleGGX(mtlData, GetCoatNormal(mtlData), mtlData.bsdfData.coatRoughness, f0ClearCoat, inputSample, sampleDir, result.specValue, result.specPdf, fresnelClearCoat))
                return false;

            coatingTransmission = (1.0 - fresnelClearCoat * mtlData.bsdfData.coatMask) * mtlData.bsdfData.coatExtinction;
            result.specValue *= mtlData.bsdfData.coatMask;
            result.specPdf *= mtlData.bsdfWeight[1];

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * (1.0 - mtlData.bsdfData.lobeMix) * GetSpecularCompensationA(mtlData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }

            if (mtlData.bsdfWeight[3] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * coatingTransmission * mtlData.bsdfData.lobeMix * GetSpecularCompensationB(mtlData);
                result.specPdf += mtlData.bsdfWeight[3] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2]) // Specular A BRDF
        {
            if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specValue *= (1.0 - mtlData.bsdfData.lobeMix) * GetSpecularCompensationA(mtlData);
            result.specPdf *= mtlData.bsdfWeight[2];

            if (mtlData.bsdfWeight[3] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * mtlData.bsdfData.lobeMix * GetSpecularCompensationB(mtlData);
                result.specPdf += mtlData.bsdfWeight[3] * pdf;
            }

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), mtlData.bsdfData.coatRoughness, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = (1.0 - fresnelClearCoat * mtlData.bsdfData.coatMask) * mtlData.bsdfData.coatExtinction;
                result.specValue = result.specValue * coatingTransmission + value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }
        else // Specular B BRDF
        {
            if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specValue *= mtlData.bsdfData.lobeMix * GetSpecularCompensationB(mtlData);
            result.specPdf *= mtlData.bsdfWeight[3];

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * (1.0 - mtlData.bsdfData.lobeMix) * GetSpecularCompensationA(mtlData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), mtlData.bsdfData.coatRoughness, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = (1.0 - fresnelClearCoat * mtlData.bsdfData.coatMask) * mtlData.bsdfData.coatExtinction;
                result.specValue = result.specValue * coatingTransmission + value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        // We compensate for the fact that there is no spec when computing SSS
        result.specValue /= mtlData.subsurfaceWeightFactor;
#endif
    }

    return result.diffPdf + result.specPdf > 0.0;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    if (mtlData.isSubsurface)
    {
        BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
        return;
    }
#endif

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), mtlData.bsdfData.coatRoughness, IorToFresnel0(mtlData.bsdfData.coatIor), sampleDir, result.specValue, result.specPdf, fresnelClearCoat);
            coatingTransmission = (1.0 - fresnelClearCoat * mtlData.bsdfData.coatMask) * mtlData.bsdfData.coatExtinction;
            result.specValue *= mtlData.bsdfData.coatMask;
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
            BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
            result.specValue += value * coatingTransmission * (1.0 - mtlData.bsdfData.lobeMix) * GetSpecularCompensationA(mtlData);
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }

        if (mtlData.bsdfWeight[3] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
            result.specValue += value * coatingTransmission * mtlData.bsdfData.lobeMix * GetSpecularCompensationB(mtlData);
            result.specPdf += mtlData.bsdfWeight[3] * pdf;
        }

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        // We compensate for the fact that there is no spec when computing SSS
        result.specValue /= mtlData.subsurfaceWeightFactor;
#endif
    }
}

float3 GetLightNormal(MaterialData mtlData)
{
    // If diffuse, specular and coating normals are quasi-indentical, return one of them, otherwise return a null vector
    const float dotThreshold = 0.99;
    return dot(GetDiffuseNormal(mtlData), GetSpecularNormal(mtlData)) > dotThreshold && dot(GetDiffuseNormal(mtlData), GetCoatNormal(mtlData)) > dotThreshold ?
        GetDiffuseNormal(mtlData) : float3(0.0, 0.0, 0.0);
}

float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // Adjust the max roughness, based on the estimated diff/spec ratio
    float maxSpecRoughness = lerp(max(mtlData.bsdfData.roughnessAT, mtlData.bsdfData.roughnessAB), max(mtlData.bsdfData.roughnessBT, mtlData.bsdfData.roughnessBB), mtlData.bsdfData.lobeMix);
    float adjustedPathRoughness = (mtlResult.specPdf * maxSpecRoughness + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);

    return adjustedPathRoughness;
}

float3 ApplyAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow, float3 value)
{
    // No absorption here
    return value;
}
