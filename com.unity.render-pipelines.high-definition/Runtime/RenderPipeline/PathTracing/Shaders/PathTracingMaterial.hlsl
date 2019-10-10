// FIXME: these sample/evaluate functions will be refactored when introducing transmission (BRDF/BTDF)

bool IsSampleValid(float3 geoNormal, float3 sampleDir)
{
    return dot(geoNormal, sampleDir) > 0.0;
}

bool SampleGGX(float2 inputSample,
               float3x3 localToWorld,
               float3 geoNormal,
               float3 incomingDir,
               BSDFData bsdfData,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float NdotL, NdotH, VdotH;
    SampleGGXDir(inputSample, incomingDir, localToWorld, bsdfData.roughnessT, outgoingDir, NdotL, NdotH, VdotH);

    if (NdotL < 0.001 || !IsSampleValid(geoNormal, outgoingDir))
        return false;

    float D = D_GGX(NdotH, bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    if (pdf < 0.001)
        return false;

    float NdotV = dot(localToWorld[2], incomingDir);
    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);

    value = F * D * V * NdotL;

    return true;
}

void EvaluateGGX(float3x3 localToWorld,
                 float3 incomingDir,
                 float3 outgoingDir,
                 BSDFData bsdfData,
             out float3 value,
             out float pdf)
{
    float NdotV = dot(localToWorld[2], incomingDir);
    if (NdotV < 0.001)
    {
        value = 0.0;
        pdf = 0.0;
        return;
    }
    float NdotL = dot(localToWorld[2], outgoingDir);

    float3 H = normalize(incomingDir + outgoingDir);
    float NdotH = dot(localToWorld[2], H);
    float VdotH = dot(incomingDir, H);
    float D = D_GGX(NdotH, bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);

    value = F * D * V * NdotL;
}

bool SampleLambert(float2 inputSample,
                   float3 normal,
                   float3 geoNormal,
                   BSDFData bsdfData,
               out float3 outgoingDir,
               out float3 value,
               out float pdf )
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, normal);

    if (!IsSampleValid(geoNormal, outgoingDir))
        return false;

    pdf = dot(normal, outgoingDir) * INV_PI;

    if (pdf < 0.001)
        return false;

    value = bsdfData.diffuseColor * pdf;

    return true;
}

void EvaluateLambert(float3 normal,
                     float3 outgoingDir,
                     BSDFData bsdfData,
                 out float3 value,
                 out float pdf)
{
    pdf = dot(normal, outgoingDir) * INV_PI;
    value = bsdfData.diffuseColor * pdf;
}

bool SampleBurley(float2 inputSample,
                  float3 normal,
                  float3 geoNormal,
                  float3 incomingDir,
                  BSDFData bsdfData,
              out float3 outgoingDir,
              out float3 value,
              out float pdf )
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, normal);

    if (!IsSampleValid(geoNormal, outgoingDir))
        return false;

    float NdotL = dot(normal, outgoingDir);
    pdf = NdotL * INV_PI;

    if (pdf < 0.001)
        return false;

    float NdotV = saturate(dot(normal, incomingDir));
    float LdotV = saturate(dot(outgoingDir, incomingDir));
    value = bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness) * pdf;

    return true;
}

void EvaluateBurley(float3 normal,
                    float3 incomingDir,
                    float3 outgoingDir,
                    BSDFData bsdfData,
                out float3 value,
                out float pdf)
{
    float NdotL = dot(normal, outgoingDir);
    float NdotV = saturate(dot(normal, incomingDir));
    float LdotV = saturate(dot(outgoingDir, incomingDir));

    pdf = NdotL * INV_PI;
    value = bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness) * pdf;
}

bool SampleDiffuse(float2 inputSample,
                   float3 normal,
                   float3 geoNormal,
                   float3 incomingDir,
                   BSDFData bsdfData,
               out float3 outgoingDir,
               out float3 value,
               out float pdf )
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    return SampleLambert(inputSample, normal, geoNormal, bsdfData, outgoingDir, value, pdf);
#else
    return SampleBurley(inputSample, normal, geoNormal, incomingDir, bsdfData, outgoingDir, value, pdf);
#endif
}

void EvaluateDiffuse(float3 normal,
                     float3 incomingDir,
                     float3 outgoingDir,
                     BSDFData bsdfData,
                 out float3 value,
                 out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    EvaluateLambert(normal, outgoingDir, bsdfData, value, pdf);
#else
    EvaluateBurley(normal, incomingDir, outgoingDir, bsdfData, value, pdf);
#endif
}

struct MaterialResult
{
    float3 diffValue;
    float  diffPdf;
    float3 specValue;
    float  specPdf;
};

// FIXME: WIP, only partial support of Lit
struct Material
{
    BSDFData    bsdfData;
    float3      G; // geometric normal
    float3      V; // view vector
    float3x3    localToWorld;
    float       diffProb;
    float       specProb;
};

bool IsBlack(Material mtl)
{
    return mtl.diffProb + mtl.specProb < 0.001;
}

Material CreateMaterial(BSDFData bsdfData, float3 G, float3 V)
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
        mtl.G = G;
        mtl.V = V;

        // Compute a local frame from the normal
        // FIXME: compute tangent frame for anisotropy support
        mtl.localToWorld = GetLocalFrame(bsdfData.normalWS);
    }

    return mtl;
}

bool SampleMaterial(Material mtl, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    if (inputSample.z < mtl.specProb)
    {
        if (!SampleGGX(inputSample, mtl.localToWorld, mtl.G, mtl.V, mtl.bsdfData, sampleDir, result.specValue, result.specPdf))
            return false;

        EvaluateDiffuse(mtl.bsdfData.normalWS, mtl.V, sampleDir, mtl.bsdfData, result.diffValue, result.diffPdf);
    }
    else
    {
        if (!SampleDiffuse(inputSample, mtl.bsdfData.normalWS, mtl.G, mtl.V, mtl.bsdfData, sampleDir, result.diffValue, result.diffPdf))
            return false;

        EvaluateGGX(mtl.localToWorld, mtl.V, sampleDir, mtl.bsdfData, result.specValue, result.specPdf);
    }

    result.diffPdf *= mtl.diffProb;
    result.specPdf *= mtl.specProb;

    return true;
}

void EvaluateMaterial(Material mtl, float3 sampleDir, out MaterialResult result)
{
    EvaluateDiffuse(mtl.bsdfData.normalWS, mtl.V, sampleDir, mtl.bsdfData, result.diffValue, result.diffPdf);
    EvaluateGGX(mtl.localToWorld, mtl.V, sampleDir, mtl.bsdfData, result.specValue, result.specPdf);
    result.diffPdf *= mtl.diffProb;
    result.specPdf *= mtl.specProb;
}
