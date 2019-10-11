// FIXME: these sample/evaluate functions will be refactored when introducing transmission (BRDF/BTDF)

struct Material
{
    BSDFData bsdfData;
    float3   V;
    float    diffProb;
    float    specProb;
};

bool IsSampleValid(float3 geoNormal, float3 sampleDir)
{
    return dot(geoNormal, sampleDir) > 0.0;
}

bool SampleGGX(Material mtl,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float NdotL, NdotH, VdotH;
    float3x3 localToWorld = float3x3(mtl.bsdfData.normalWS, mtl.bsdfData.tangentWS, mtl.bsdfData.bitangentWS);
    SampleGGXDir(inputSample, mtl.V, localToWorld, mtl.bsdfData.roughnessT, outgoingDir, NdotL, NdotH, VdotH);

    if (NdotL < 0.001 || !IsSampleValid(mtl.bsdfData.geomNormalWS, outgoingDir))
        return false;

    float D = D_GGX(NdotH, mtl.bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    if (pdf < 0.001)
        return false;

    float NdotV = dot(mtl.bsdfData.normalWS, mtl.V);
    float3 F = F_Schlick(mtl.bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, mtl.bsdfData.roughnessT);

    value = F * D * V * NdotL;

    return true;
}

void EvaluateGGX(Material mtl,
                 float3 outgoingDir,
             out float3 value,
             out float pdf)
{
    float NdotV = dot(mtl.bsdfData.normalWS, mtl.V);
    if (NdotV < 0.001)
    {
        value = 0.0;
        pdf = 0.0;
        return;
    }
    float NdotL = dot(mtl.bsdfData.normalWS, outgoingDir);

    float3 H = normalize(mtl.V + outgoingDir);
    float NdotH = dot(mtl.bsdfData.normalWS, H);
    float VdotH = dot(mtl.V, H);
    float D = D_GGX(NdotH, mtl.bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    float3 F = F_Schlick(mtl.bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, mtl.bsdfData.roughnessT);

    value = F * D * V * NdotL;
}

bool SampleLambert(Material mtl,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, mtl.bsdfData.normalWS);

    if (!IsSampleValid(mtl.bsdfData.geomNormalWS, outgoingDir))
        return false;

    pdf = dot(mtl.bsdfData.normalWS, outgoingDir) * INV_PI;

    if (pdf < 0.001)
        return false;

    value = mtl.bsdfData.diffuseColor * pdf;

    return true;
}

void EvaluateLambert(Material mtl,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
    pdf = dot(mtl.bsdfData.normalWS, outgoingDir) * INV_PI;
    value = mtl.bsdfData.diffuseColor * pdf;
}

bool SampleBurley(Material mtl,
                  float3 inputSample,
              out float3 outgoingDir,
              out float3 value,
              out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, mtl.bsdfData.normalWS);

    if (!IsSampleValid(mtl.bsdfData.geomNormalWS, outgoingDir))
        return false;

    float NdotL = dot(mtl.bsdfData.normalWS, outgoingDir);
    pdf = NdotL * INV_PI;

    if (pdf < 0.001)
        return false;

    float NdotV = saturate(dot(mtl.bsdfData.normalWS, mtl.V));
    float LdotV = saturate(dot(outgoingDir, mtl.V));
    value = mtl.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtl.bsdfData.perceptualRoughness) * pdf;

    return true;
}

void EvaluateBurley(Material mtl,
                    float3 outgoingDir,
                out float3 value,
                out float pdf)
{
    float NdotL = dot(mtl.bsdfData.normalWS, outgoingDir);
    float NdotV = saturate(dot(mtl.bsdfData.normalWS, mtl.V));
    float LdotV = saturate(dot(outgoingDir, mtl.V));

    pdf = NdotL * INV_PI;
    value = mtl.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtl.bsdfData.perceptualRoughness) * pdf;
}

bool SampleDiffuse(Material mtl,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    return SampleLambert(mtl, inputSample, outgoingDir, value, pdf);
#else
    return SampleBurley(mtl, inputSample, outgoingDir, value, pdf);
#endif
}

void EvaluateDiffuse(Material mtl,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    EvaluateLambert(mtl, outgoingDir, value, pdf);
#else
    EvaluateBurley(mtl, outgoingDir, value, pdf);
#endif
}

struct MaterialResult
{
    float3 diffValue;
    float  diffPdf;
    float3 specValue;
    float  specPdf;
};

bool IsBlack(Material mtl)
{
    return mtl.diffProb + mtl.specProb < 0.001;
}

Material CreateMaterial(BSDFData bsdfData, float3 V)
{
    Material mtl;

    float  NdotV = dot(bsdfData.normalWS, V);
    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);

    // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
    mtl.specProb = NdotV > 0.001 ? Luminance(F) : 0.0;
    mtl.diffProb = Luminance(bsdfData.diffuseColor);

    // If we are basically black, no need to compute anything else for this material
    if (!IsBlack(mtl))
    {
        mtl.specProb /= mtl.diffProb + mtl.specProb;
        mtl.diffProb = 1.0 - mtl.specProb;

        // Keep these around, rather than passing them to all methods
        mtl.bsdfData = bsdfData;
        mtl.V = V;
    }

    return mtl;
}

bool SampleMaterial(Material mtl, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    if (inputSample.z < mtl.specProb)
    {
        if (!SampleGGX(mtl, inputSample, sampleDir, result.specValue, result.specPdf))
            return false;

        EvaluateDiffuse(mtl, sampleDir, result.diffValue, result.diffPdf);
    }
    else
    {
        if (!SampleDiffuse(mtl, inputSample, sampleDir, result.diffValue, result.diffPdf))
            return false;

        EvaluateGGX(mtl, sampleDir, result.specValue, result.specPdf);
    }

    result.diffPdf *= mtl.diffProb;
    result.specPdf *= mtl.specProb;

    return true;
}

void EvaluateMaterial(Material mtl, float3 sampleDir, out MaterialResult result)
{
    EvaluateDiffuse(mtl, sampleDir, result.diffValue, result.diffPdf);
    EvaluateGGX(mtl, sampleDir, result.specValue, result.specPdf);
    result.diffPdf *= mtl.diffProb;
    result.specPdf *= mtl.specProb;
}
