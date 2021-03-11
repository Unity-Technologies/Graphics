#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"

// Lit Material Data:
//
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Coat GGX BRDF
// bsdfWeight2  Spec GGX BRDF
// bsdfWeight3  Spec GGX BTDF

void ProcessBSDFData(PathIntersection pathIntersection, BuiltinData builtinData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(pathIntersection.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(pathIntersection.maxRoughness, bsdfData.roughnessB);

    float NdotV = abs(dot(bsdfData.normalWS, WorldRayDirection()));

    // Modify fresnel0 value to take iridescence into account (code adapted from Lit.hlsl to produce identical results)
    if (bsdfData.iridescenceMask > 0.0)
    {
        float topIOR = lerp(1.0, CLEAR_COAT_IOR, bsdfData.coatMask);
        float viewAngle = sqrt(1.0 + (Sq(NdotV) - 1.0) / Sq(topIOR));

        bsdfData.fresnel0 = lerp(bsdfData.fresnel0, EvalIridescence(topIOR, viewAngle, bsdfData.iridescenceThickness, bsdfData.fresnel0), bsdfData.iridescenceMask);
    }

    // We store an energy compensation coefficient for GGX into the specular occlusion (code adapted from Lit.hlsl to produce identical results)
#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    float roughness = 0.5 * (bsdfData.roughnessT + bsdfData.roughnessB);
    float2 coordLUT = Remap01ToHalfTexelCoord(float2(sqrt(NdotV), roughness), FGDTEXTURE_RESOLUTION);
    float E = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_GGXDisneyDiffuse, s_linear_clamp_sampler, coordLUT, 0).y;
    bsdfData.specularOcclusion = (1.0 - E) / E;
#else
    bsdfData.specularOcclusion = 0.0;
#endif

#if defined(_SURFACE_TYPE_TRANSPARENT) && !HAS_REFRACTION
    // Turn alpha blending into proper refraction
    bsdfData.transmittanceMask = 1.0 - builtinData.opacity;
    bsdfData.ior = 1.0;
#endif
}

bool CreateMaterialData(PathIntersection pathIntersection, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(pathIntersection, builtinData, mtlData.bsdfData);

    mtlData.V = -WorldRayDirection();

    // Assume no coating by default
    float coatingTransmission = 1.0;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(mtlData.bsdfData.geomNormalWS, mtlData.V))
    {
        float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
        float Fcoat = F_Schlick(CLEAR_COAT_F0, NdotV) * mtlData.bsdfData.coatMask;
        float Fspec = Luminance(F_Schlick(mtlData.bsdfData.fresnel0, NdotV));

        // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
        bool consistentNormal = (NdotV > 0.001);
        mtlData.bsdfWeight[1] = consistentNormal ? Fcoat : 0.0;
        coatingTransmission = 1.0 - mtlData.bsdfWeight[1];
        mtlData.bsdfWeight[2] = consistentNormal ? coatingTransmission * lerp(Fspec, 0.5, 0.5 * (mtlData.bsdfData.roughnessT + mtlData.bsdfData.roughnessB)) * (1.0 + Fspec * mtlData.bsdfData.specularOcclusion) : 0.0;
        mtlData.bsdfWeight[3] = consistentNormal ? (coatingTransmission - mtlData.bsdfWeight[2]) * mtlData.bsdfData.transmittanceMask : 0.0;
        mtlData.bsdfWeight[0] = coatingTransmission * (1.0 - mtlData.bsdfData.transmittanceMask) * Luminance(mtlData.bsdfData.diffuseColor) * mtlData.bsdfData.ambientOcclusion;
    }
#ifdef _SURFACE_TYPE_TRANSPARENT
    else // Below
    {
        float NdotV = -dot(mtlData.bsdfData.normalWS, mtlData.V);
        float F = F_FresnelDielectric(1.0 / mtlData.bsdfData.ior, NdotV);

        // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
        bool consistentNormal = (NdotV > 0.001);
        mtlData.bsdfWeight[0] = 0.0;
        mtlData.bsdfWeight[1] = 0.0;
        mtlData.bsdfWeight[2] = consistentNormal ? F : 0.0;
        mtlData.bsdfWeight[3] = consistentNormal ? (1.0 - mtlData.bsdfWeight[1]) * mtlData.bsdfData.transmittanceMask : 0.0;
    }
#endif

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

        if (!SSS::RandomWalk(shadingPosition, mtlData.bsdfData.normalWS, mtlData.bsdfData.diffuseColor, meanFreePath, pathIntersection.pixelCoord, subsurfaceResult))
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

// Little helper to get the specular compensation term
float3 GetSpecularCompensation(BSDFData bsdfData)
{
    return 1.0 + bsdfData.specularOcclusion * bsdfData.fresnel0;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    if (mtlData.isSubsurface)
    {
        if (!BRDF::SampleLambert(mtlData, inputSample, sampleDir, result.diffValue, result.diffPdf))
            return false;

        result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask);

        return true;
    }
#endif

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float  fresnelSpec, fresnelClearCoat = 0.0;

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

            result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
                result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData.bsdfData);
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
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
                result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData.bsdfData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2]) // Specular BRDF
        {
            if (!BRDF::SampleAnisoGGX(mtlData, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf, fresnelSpec))
                return false;

            result.specValue *= GetSpecularCompensation(mtlData.bsdfData);
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
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }
#ifdef _SURFACE_TYPE_TRANSPARENT
        else // Specular BTDF
        {
            if (!BTDF::SampleAnisoGGX(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

    #ifdef _REFRACTION_THIN
            sampleDir = refract(sampleDir, mtlData.bsdfData.normalWS, mtlData.bsdfData.ior);
            if (!any(sampleDir))
                return false;
    #endif

            result.specValue *= mtlData.bsdfData.transmittanceMask;
            result.specPdf *= mtlData.bsdfWeight[3];
        }
#endif

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        // We compensate for the fact that there is no spec when computing SSS
        result.specValue /= mtlData.subsurfaceWeightFactor;
#endif
    }
    else // Below
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
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
#else
        return false;
#endif
    }

    return true;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    if (mtlData.isSubsurface)
    {
        BRDF::EvaluateLambert(mtlData, sampleDir, result.diffValue, result.diffPdf);
        result.diffValue *= 1.0 - mtlData.bsdfData.transmittanceMask; // AO purposedly ignored here

        return;
    }
#endif

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
            result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat); // AO purposedly ignored here
            result.diffPdf *= mtlData.bsdfWeight[0];
        }

        if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateAnisoGGX(mtlData, mtlData.bsdfData.fresnel0, sampleDir, value, pdf, fresnelSpec);
            result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData.bsdfData);
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        // We compensate for the fact that there is no spec when computing SSS
        result.specValue /= mtlData.subsurfaceWeightFactor;
#endif
    }
}

float AdjustPathRoughness(MaterialData mtlData, MaterialResult mtlResult, bool isSampleBelow, float pathRoughness)
{
    // Adjust the max roughness, based on the estimated diff/spec ratio
    float adjustedPathRoughness = (mtlResult.specPdf * max(mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB) + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);

#ifdef _SURFACE_TYPE_TRANSPARENT
    // When transmitting with an IOR close to 1.0, roughness is barely noticeable -> take that into account for path roughness adjustment
    if (IsBelow(mtlData) != isSampleBelow)
        adjustedPathRoughness = lerp(pathRoughness, adjustedPathRoughness, smoothstep(1.0, 1.3, mtlData.bsdfData.ior));
#endif

    return adjustedPathRoughness;
}

float3 ApplyAbsorption(MaterialData mtlData, float dist, bool isSampleBelow, float3 value)
{
#if defined(_SURFACE_TYPE_TRANSPARENT) && HAS_REFRACTION
    // Apply absorption on rays below the interface, using Beer-Lambert's law
    if (isSampleBelow)
    {
    #ifdef _REFRACTION_THIN
        value *= exp(-mtlData.bsdfData.absorptionCoefficient * REFRACTION_THIN_DISTANCE);
    #else
        value *= exp(-mtlData.bsdfData.absorptionCoefficient * dist);
    #endif
    }
#endif

    return value;
}
