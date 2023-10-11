#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingPayload.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingBSDF.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingAOV.hlsl"

// Lit Material Data:
//
// bsdfWeight0  Diffuse BRDF
// bsdfWeight1  Coat GGX BRDF
// bsdfWeight2  Spec GGX BRDF
// bsdfWeight3  Spec GGX BTDF

float3 GetSpecularCompensation(MaterialData mtlData)
{
    return 1.0 + mtlData.bsdfData.specularOcclusion * mtlData.bsdfData.fresnel0;
}

void ProcessBSDFData(PathPayload payload, BuiltinData builtinData, MaterialData mtlData, inout BSDFData bsdfData)
{
    // Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(payload.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(payload.maxRoughness, bsdfData.roughnessB);

    float NdotV = abs(dot(GetSpecularNormal(mtlData), mtlData.V));

    // Modify fresnel0 value to take iridescence into account (code adapted from Lit.hlsl to produce identical results)
    if (bsdfData.iridescenceMask > 0.0)
    {
        float topIOR = lerp(1.0, CLEAR_COAT_IOR, bsdfData.coatMask);
        float viewAngle = sqrt(1.0 + (Sq(NdotV) - 1.0) / Sq(topIOR));

        bsdfData.fresnel0 = lerp(bsdfData.fresnel0, EvalIridescence(topIOR, viewAngle, bsdfData.iridescenceThickness, bsdfData.fresnel0), bsdfData.iridescenceMask);
    }

    // We store an energy compensation coefficient for GGX into the specular occlusion (code adapted from Lit.hlsl to produce identical results)
#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    bsdfData.specularOcclusion = BRDF::GetGGXMultipleScatteringEnergy(0.5 * (bsdfData.roughnessT + bsdfData.roughnessB), sqrt(NdotV));
#else
    bsdfData.specularOcclusion = 0.0;
#endif

#if defined(_SURFACE_TYPE_TRANSPARENT) && !HAS_REFRACTION
    // Turn alpha blending into proper refraction
    bsdfData.transmittanceMask = 1.0 - builtinData.opacity;
    bsdfData.ior = 1.0;
#endif
}

bool CreateMaterialData(PathPayload payload, BuiltinData builtinData, BSDFData bsdfData, inout float3 shadingPosition, inout float theSample, out MaterialData mtlData)
{
    // Alter values in the material's bsdfData struct, to better suit path tracing
    mtlData.V = -WorldRayDirection();
    mtlData.Nv = ComputeConsistentShadingNormal(mtlData.V, bsdfData.geomNormalWS, bsdfData.normalWS);
    mtlData.bsdfData = bsdfData;
    ProcessBSDFData(payload, builtinData, mtlData, mtlData.bsdfData);

    mtlData.bsdfWeight = 0.0;

    // Assume no coating by default
    float coatingTransmission = 1.0;

    // First determine if our incoming direction V is above (exterior) or below (interior) the surface
    if (IsAbove(mtlData))
    {
        float NdotV = dot(GetSpecularNormal(mtlData), mtlData.V);
        float Fcoat = F_Schlick(CLEAR_COAT_F0, NdotV);
        float Fspec = Luminance(F_Schlick(mtlData.bsdfData.fresnel0, NdotV));

        mtlData.bsdfWeight[1] = Fcoat * mtlData.bsdfData.coatMask;
        coatingTransmission = 1.0 - mtlData.bsdfWeight[1];
        mtlData.bsdfWeight[2] = coatingTransmission * lerp(Fspec, 0.5, 0.5 * (mtlData.bsdfData.roughnessT + mtlData.bsdfData.roughnessB)) * GetSpecularCompensation(mtlData).x; // assume spec is float as dieltric
        mtlData.bsdfWeight[3] = (coatingTransmission - mtlData.bsdfWeight[2]) * mtlData.bsdfData.transmittanceMask;
        mtlData.bsdfWeight[0] = coatingTransmission * (1.0 - mtlData.bsdfData.transmittanceMask) * Luminance(mtlData.bsdfData.diffuseColor) * max(mtlData.bsdfData.ambientOcclusion, 0.001);
    }
#ifdef _SURFACE_TYPE_TRANSPARENT
    else // Below
    {
        float NdotV = -dot(GetSpecularNormal(mtlData), mtlData.V);
        float F = F_FresnelDielectric(1.0 / mtlData.bsdfData.ior, NdotV);

        mtlData.bsdfWeight[2] = F;
        mtlData.bsdfWeight[3] = (1.0 - mtlData.bsdfWeight[2]) * mtlData.bsdfData.transmittanceMask;
    }
#endif

    // Normalize the weights
    float wSum = mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2] + mtlData.bsdfWeight[3];

    if (wSum < BSDF_WEIGHT_EPSILON)
        return false;

    mtlData.bsdfWeight /= wSum;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    float subsurfaceWeight = mtlData.bsdfWeight[0] * mtlData.bsdfData.subsurfaceMask * (1.0 - payload.maxRoughness);

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
        if (!SSS::RandomWalk(shadingPosition, GetDiffuseNormal(mtlData), mtlData.bsdfData.diffuseColor, meanFreePath, payload.pixelCoord, subsurfaceResult, isThin))
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

        result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask);

        return true;
    }
#endif

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float3 fresnelClearCoat = 0.0;

        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleDiffuse(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            result.diffPdf *= mtlData.bsdfWeight[0];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetSpecularNormal(mtlData), CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, value, pdf, fresnelClearCoat);
                fresnelClearCoat *= mtlData.bsdfData.coatMask;
                result.specValue += value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Clear coat BRDF
        {
            if (!BRDF::SampleGGX(mtlData, GetSpecularNormal(mtlData), CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, inputSample, sampleDir, result.specValue, result.specPdf, fresnelClearCoat))
                return false;

            fresnelClearCoat *= mtlData.bsdfData.coatMask;
            result.specValue *= mtlData.bsdfData.coatMask;
            result.specPdf *= mtlData.bsdfWeight[1];

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateDiffuse(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
                result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData);
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1] + mtlData.bsdfWeight[2]) // Specular BRDF
        {
            if (!BRDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specValue *= GetSpecularCompensation(mtlData);
            result.specPdf *= mtlData.bsdfWeight[2];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetSpecularNormal(mtlData), CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, value, pdf, fresnelClearCoat);
                fresnelClearCoat *= mtlData.bsdfData.coatMask;
                result.specValue = result.specValue * (1.0 - fresnelClearCoat) + value * mtlData.bsdfData.coatMask;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateDiffuse(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= mtlData.bsdfData.ambientOcclusion * (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }
#ifdef _SURFACE_TYPE_TRANSPARENT
        else // Specular BTDF
        {
            if (!BTDF::SampleAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.ior, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

    #ifdef _REFRACTION_THIN
            sampleDir = refract(sampleDir, GetSpecularNormal(mtlData), mtlData.bsdfData.ior);
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
        if (mtlData.bsdfData.transmittanceMask > 0.0)
        {
            // Just go through (although we should not end up here)
            sampleDir = -mtlData.V;
            result.specValue = DELTA_PDF;
            result.specPdf = DELTA_PDF;
        }
    #else
        if (inputSample.z < mtlData.bsdfWeight[2]) // Specular BRDF
        {
            if (!BRDF::SampleDelta(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.ior, sampleDir, result.specValue, result.specPdf))
                return false;

            result.specPdf *= mtlData.bsdfWeight[2];
        }
        else // Specular BTDF
        {
            if (!BTDF::SampleDelta(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.ior, sampleDir, result.specValue, result.specPdf))
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
        BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
        result.diffValue *= 1.0 - mtlData.bsdfData.transmittanceMask;
        result.diffValue *= mtlData.bsdfData.ambientOcclusion; // Take into account AO the same way as in SampleMaterial

        return;
    }
#endif

    if (IsAbove(mtlData))
    {
        float3 value;
        float  pdf;
        float3 fresnelClearCoat = 0.0;

        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, GetSpecularNormal(mtlData), CLEAR_COAT_ROUGHNESS, CLEAR_COAT_F0, sampleDir, result.specValue, result.specPdf, fresnelClearCoat);
            fresnelClearCoat *= mtlData.bsdfData.coatMask;
            result.specValue *= mtlData.bsdfData.coatMask;
            result.specPdf *= mtlData.bsdfWeight[1];
        }

        if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateDiffuse(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
            result.diffValue *= (1.0 - mtlData.bsdfData.transmittanceMask) * (1.0 - fresnelClearCoat);
            result.diffValue *= mtlData.bsdfData.ambientOcclusion; // Take into account AO the same way as in SampleMaterial
            result.diffPdf *= mtlData.bsdfWeight[0];
        }

        if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateAnisoGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB, mtlData.bsdfData.fresnel0, sampleDir, value, pdf);
            result.specValue += value * (1.0 - fresnelClearCoat) * GetSpecularCompensation(mtlData);
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        // We compensate for the fact that there is no spec when computing SSS
        result.specValue /= mtlData.subsurfaceWeightFactor;
#endif
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
    float adjustedPathRoughness = (mtlResult.specPdf * max(mtlData.bsdfData.roughnessT, mtlData.bsdfData.roughnessB) + mtlResult.diffPdf) / (mtlResult.diffPdf + mtlResult.specPdf);

#ifdef _SURFACE_TYPE_TRANSPARENT
    // When transmitting with an IOR close to 1.0, roughness is barely noticeable -> take that into account for path roughness adjustment
    if (IsBelow(mtlData) != isSampleBelow)
        adjustedPathRoughness = lerp(pathRoughness, adjustedPathRoughness, smoothstep(1.0, 1.3, mtlData.bsdfData.ior));
#endif

    return adjustedPathRoughness;
}

float3 GetMaterialAbsorption(MaterialData mtlData, SurfaceData surfaceData, float dist, bool isSampleBelow)
{
#if defined(_SURFACE_TYPE_TRANSPARENT) && HAS_REFRACTION
    // Apply absorption on rays below the interface, using Beer-Lambert's law
    if (isSampleBelow)
    {
    #ifdef _REFRACTION_THIN
        // On thin surfaces, we apply a fixed distance of absorption. 
        return exp(-mtlData.bsdfData.absorptionCoefficient * REFRACTION_THIN_DISTANCE);
    #else
        // We allow a reasonable max distance of 10 times the "atDistance" (so that objects do not end up appearing black)
        return exp(-mtlData.bsdfData.absorptionCoefficient * min(dist, max(surfaceData.atDistance, REAL_EPS) * 10.0));
    #endif
    }
#endif

    return 1.0;
}

void GetAOVData(BSDFData bsdfData, out AOVData aovData)
{
    aovData.albedo = bsdfData.diffuseColor;
    aovData.normal = bsdfData.normalWS;
}
