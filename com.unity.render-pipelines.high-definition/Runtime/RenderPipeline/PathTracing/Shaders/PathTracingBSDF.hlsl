#define DELTA_PDF 1000000.0
#define MAX_GGX_ROUGHNESS 0.999

namespace BRDF
{

bool SampleGGX(MaterialData mtlData,
               float roughness,
               float3 fresnel0,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf,
           out float3 fresnel)
{
    roughness = min(roughness, MAX_GGX_ROUGHNESS);

    float NdotL, NdotH, VdotH;
    float3x3 localToWorld = GetLocalFrame(mtlData.bsdfData.normalWS);
    SampleGGXDir(inputSample.xy, mtlData.V, localToWorld, roughness, outgoingDir, NdotL, NdotH, VdotH);

    if (NdotL < 0.001 || !IsAbove(mtlData, outgoingDir))
        return false;

    float D = D_GGX(NdotH, roughness);
    pdf = D * NdotH / (4.0 * VdotH);

    if (pdf < 0.001)
        return false;

    float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
    float Vg = V_SmithJointGGX(NdotL, NdotV, roughness);
    fresnel = F_Schlick(fresnel0, VdotH);

    value = fresnel * D * Vg * NdotL;

    return true;
}

void EvaluateGGX(MaterialData mtlData,
                 float roughness,
                 float3 fresnel0,
                 float3 outgoingDir,
             out float3 value,
             out float pdf,
             out float3 fresnel)
{
    float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
    if (NdotV < 0.001)
    {
        value = 0.0;
        pdf = 0.0;
        return;
    }
    float NdotL = dot(mtlData.bsdfData.normalWS, outgoingDir);

    roughness = min(roughness, MAX_GGX_ROUGHNESS);

    float3 H = normalize(mtlData.V + outgoingDir);
    float NdotH = dot(mtlData.bsdfData.normalWS, H);
    float VdotH = dot(mtlData.V, H);
    float D = D_GGX(NdotH, roughness);
    pdf = D * NdotH / (4.0 * VdotH);

    float Vg = V_SmithJointGGX(NdotL, NdotV, roughness);
    fresnel = F_Schlick(fresnel0, VdotH);

    value = fresnel * D * Vg * NdotL;
}

bool SampleDelta(MaterialData mtlData,
             out float3 outgoingDir,
             out float3 value,
             out float pdf)
{
    if (IsAbove(mtlData))
    {
        outgoingDir = reflect(-mtlData.V, mtlData.bsdfData.normalWS);
        float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
        value = F_Schlick(mtlData.bsdfData.fresnel0, NdotV);
    }
    else // Below
    {
        outgoingDir = -reflect(mtlData.V, mtlData.bsdfData.normalWS);
        float NdotV = -dot(mtlData.bsdfData.normalWS, mtlData.V);
        value = F_FresnelDielectric(1.0 / mtlData.bsdfData.ior, NdotV);
    }

    value *= DELTA_PDF;
    pdf = DELTA_PDF;

    return any(outgoingDir);
}

bool SampleLambert(MaterialData mtlData,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, mtlData.bsdfData.normalWS);

    if (!IsAbove(mtlData, outgoingDir))
        return false;

    pdf = dot(mtlData.bsdfData.normalWS, outgoingDir) * INV_PI;

    if (pdf < 0.001)
        return false;

    value = mtlData.bsdfData.diffuseColor * pdf;

    return true;
}

void EvaluateLambert(MaterialData mtlData,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
    pdf = dot(mtlData.bsdfData.normalWS, outgoingDir) * INV_PI;
    value = mtlData.bsdfData.diffuseColor * pdf;
}

bool SampleBurley(MaterialData mtlData,
                  float3 inputSample,
              out float3 outgoingDir,
              out float3 value,
              out float pdf)
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, mtlData.bsdfData.normalWS);

    if (!IsAbove(mtlData, outgoingDir))
        return false;

    float NdotL = dot(mtlData.bsdfData.normalWS, outgoingDir);
    pdf = NdotL * INV_PI;

    if (pdf < 0.001)
        return false;

    float NdotV = saturate(dot(mtlData.bsdfData.normalWS, mtlData.V));
    float LdotV = saturate(dot(outgoingDir, mtlData.V));
    value = mtlData.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtlData.bsdfData.perceptualRoughness) * pdf;

    return true;
}

void EvaluateBurley(MaterialData mtlData,
                    float3 outgoingDir,
                out float3 value,
                out float pdf)
{
    float NdotL = dot(mtlData.bsdfData.normalWS, outgoingDir);
    float NdotV = saturate(dot(mtlData.bsdfData.normalWS, mtlData.V));
    float LdotV = saturate(dot(outgoingDir, mtlData.V));

    pdf = NdotL * INV_PI;
    value = mtlData.bsdfData.diffuseColor * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, mtlData.bsdfData.perceptualRoughness) * pdf;
}

bool SampleDiffuse(MaterialData mtlData,
                   float3 inputSample,
               out float3 outgoingDir,
               out float3 value,
               out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    return SampleLambert(mtlData, inputSample, outgoingDir, value, pdf);
#else
    return SampleBurley(mtlData, inputSample, outgoingDir, value, pdf);
#endif
}

void EvaluateDiffuse(MaterialData mtlData,
                     float3 outgoingDir,
                 out float3 value,
                 out float pdf)
{
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    EvaluateLambert(mtlData, outgoingDir, value, pdf);
#else
    EvaluateBurley(mtlData, outgoingDir, value, pdf);
#endif
}

} // namespace BRDF

namespace BTDF
{

bool SampleDelta(MaterialData mtlData,
             out float3 outgoingDir,
             out float3 value,
             out float pdf)
{
    if (IsAbove(mtlData))
    {
        outgoingDir = refract(-mtlData.V, mtlData.bsdfData.normalWS, 1.0 / mtlData.bsdfData.ior);
        float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
        value = 1.0 - F_Schlick(mtlData.bsdfData.fresnel0, NdotV);
    }
    else // Below
    {
        outgoingDir = -refract(mtlData.V, mtlData.bsdfData.normalWS, mtlData.bsdfData.ior);
        float NdotV = -dot(mtlData.bsdfData.normalWS, mtlData.V);
        value = 1.0 - F_FresnelDielectric(1.0 / mtlData.bsdfData.ior, NdotV);
    }

    value *= DELTA_PDF;
    pdf = DELTA_PDF;

    return any(outgoingDir);
}

bool SampleGGX(MaterialData mtlData,
               float3 inputSample,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float roughness = min(mtlData.bsdfData.roughnessT, MAX_GGX_ROUGHNESS);

    float NdotL, NdotH, VdotH;
    float3x3 localToWorld = GetLocalFrame(mtlData.bsdfData.normalWS);
    SampleGGXDir(inputSample.xy, mtlData.V, localToWorld, roughness, outgoingDir, NdotL, NdotH, VdotH);

    // FIXME: won't be necessary after new version of SampleGGXDir()
    float3 H = normalize(mtlData.V + outgoingDir);
    outgoingDir = refract(-mtlData.V, H, 1.0 / mtlData.bsdfData.ior);
    NdotL = dot(mtlData.bsdfData.normalWS, outgoingDir);

    if (NdotL > -0.001 || !IsBelow(mtlData, outgoingDir))
        return false;

    float NdotV = dot(mtlData.bsdfData.normalWS, mtlData.V);
    float LdotH = dot(outgoingDir, H);

    float3 F = F_Schlick(mtlData.bsdfData.fresnel0, VdotH);
    float  D = D_GGX(NdotH, roughness);
    float Vg = V_SmithJointGGX(-NdotL, NdotV, roughness);

    // Compute the Jacobian
    float jacobian = max(abs(VdotH + mtlData.bsdfData.ior * LdotH), 0.001);
    jacobian = Sq(mtlData.bsdfData.ior) * abs(LdotH) / Sq(jacobian);

    pdf = D * NdotH * jacobian;
    value = abs(4.0 * (1.0 - F) * D * Vg * NdotL * VdotH * jacobian);

    return (pdf > 0.001);
}

} // namespace BTDF
