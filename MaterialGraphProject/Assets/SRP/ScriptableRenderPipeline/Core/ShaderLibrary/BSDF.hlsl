#ifndef UNITY_BSDF_INCLUDED
#define UNITY_BSDF_INCLUDED

// Note: All NDF and diffuse term have a version with and without divide by PI.
// Version with divide by PI are use for direct lighting.
// Version without divide by PI are use for image based lighting where often the PI cancel during importance sampling

//-----------------------------------------------------------------------------
// Fresnel term
//-----------------------------------------------------------------------------

float F_Schlick(float f0, float f90, float u)
{
    float x  = 1.0 - u;
    float x2 = x * x;
    float x5 = x * x2 * x2;
    return (f90 - f0) * x5 + f0;                // sub mul mul mul sub mad
}

float F_Schlick(float f0, float u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad
}

float3 F_Schlick(float3 f0, float f90, float u)
{
    float x  = 1.0 - u;
    float x2 = x * x;
    float x5 = x * x2 * x2;
    return f0 * (1.0 - x5) + (f90 * x5);        // sub mul mul mul sub mul mad*3
}

float3 F_Schlick(float3 f0, float u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad*3
}

// Does not handle TIR.
float F_Transm_Schlick(float f0, float f90, float u)
{
    float x  = 1.0 - u;
    float x2 = x * x;
    float x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad
}

// Does not handle TIR.
float F_Transm_Schlick(float f0, float u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad
}

// Does not handle TIR.
float3 F_Transm_Schlick(float3 f0, float f90, float u)
{
    float x  = 1.0 - u;
    float x2 = x * x;
    float x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad*3
}

// Does not handle TIR.
float3 F_Transm_Schlick(float3 f0, float u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad*3
}

//-----------------------------------------------------------------------------
// Specular BRDF
//-----------------------------------------------------------------------------

// With analytical light (not image based light) we clamp the minimun roughness in the NDF to avoid numerical instability.
#define UNITY_MIN_ROUGHNESS 0.002

float ClampRoughnessForAnalyticalLights(float roughness)
{
    return max(roughness, UNITY_MIN_ROUGHNESS);
}

float D_GGXNoPI(float NdotH, float roughness)
{
    float a2 = roughness * roughness;
    float f = (NdotH * a2 - NdotH) * NdotH + 1.0;
    return a2 / (f * f);
}

float D_GGX(float NdotH, float roughness)
{
    return INV_PI * D_GGXNoPI(NdotH, roughness);
}

// Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, p. 19, 29.
float G_MaskingSmithGGX(float NdotV, float VdotH, float roughness)
{
    // G1(V, H)    = HeavisideStep(VdotH) / (1 + Λ(V)).
    // Λ(V)        = -0.5 + 0.5 * sqrt(1 + 1 / a²).
    // a           = 1 / (roughness * tan(theta)).
    // 1 + Λ(V)    = 0.5 + 0.5 * sqrt(1 + roughness² * tan²(theta)).
    // tan²(theta) = (1 - cos²(theta)) / cos²(theta) = 1 / cos²(theta) - 1.

    float hs = VdotH > 0.0 ? 1.0 : 0.0;
    float a2 = roughness * roughness;
    float z2 = NdotV * NdotV;

    return hs / (0.5 + 0.5 * sqrt(1.0 + a2 * (1.0 / z2 - 1.0)));
}

// Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, p. 12.
float D_GGX_Visible(float NdotH, float NdotV, float VdotH, float roughness)
{
    // Note that we pass 1.0 instead of 'VdotH' since the multiplication will already clamp.
    return D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, 1.0, roughness) * VdotH / NdotV;
}

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
float V_SmithJointGGX(float NdotL, float NdotV, float roughness)
{
    // Original formulation:
    //  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
    //  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
    //  G           = 1 / (1 + lambda_v + lambda_l);

    float a = roughness;
    float a2 = a * a;
    // Reorder code to be more optimal
    float lambdaV = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
    float lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0 * NdotL * NdotV) /  ((4.0 * NdotL * NdotV) * (lambda_v + lambda_l));
    return 0.5 / (lambdaV + lambdaL);
}

// Precompute part of lambdaV
float GetSmithJointGGXLambdaV(float NdotV, float roughness)
{
    float a = roughness;
    float a2 = a * a;
    return sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
}

float V_SmithJointGGX(float NdotL, float NdotV, float roughness, float lambdaV)
{
    float a = roughness;
    float a2 = a * a;
    // Reorder code to be more optimal
    lambdaV *= NdotL;
    float lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0 * NdotL * NdotV) /  ((4.0 * NdotL * NdotV) * (lambda_v + lambda_l));
    return 0.5 / (lambdaV + lambdaL);
}

float V_SmithJointGGXApprox(float NdotL, float NdotV, float roughness)
{
    float a = roughness;
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    float lambdaV = NdotL * (NdotV * (1 - a) + a);
    float lambdaL = NdotV * (NdotL * (1 - a) + a);

    return 0.5 / (lambdaV + lambdaL);
}

// Precompute part of LambdaV
float GetSmithJointGGXApproxLambdaV(float NdotV, float roughness)
{
    float a = roughness;
    return NdotV * (1 - a) + a;
}

float V_SmithJointGGXApprox(float NdotL, float NdotV, float roughness, float lambdaV)
{
    float a = roughness;
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    lambdaV *= NdotL;
    float lambdaL = NdotV * (NdotL * (1 - a) + a);

    return 0.5 / (lambdaV + lambdaL);
}

// roughnessT -> roughness in tangent direction
// roughnessB -> roughness in bitangent direction
float D_GGXAnisoNoPI(float TdotH, float BdotH, float NdotH, float roughnessT, float roughnessB)
{
    float f = TdotH * TdotH / (roughnessT * roughnessT) + BdotH * BdotH / (roughnessB * roughnessB) + NdotH * NdotH;
    return 1.0 / (roughnessT * roughnessB * f * f);
}

float D_GGXAniso(float TdotH, float BdotH, float NdotH, float roughnessT, float roughnessB)
{
    return INV_PI * D_GGXAnisoNoPI(TdotH, BdotH, NdotH, roughnessT, roughnessB);
}

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4
float V_SmithJointGGXAniso(float TdotV, float BdotV, float NdotV, float TdotL, float BdotL, float NdotL, float roughnessT, float roughnessB)
{
    float aT = roughnessT;
    float aT2 = aT * aT;
    float aB = roughnessB;
    float aB2 = aB * aB;

    float lambdaV = NdotL * sqrt(aT2 * TdotV * TdotV + aB2 * BdotV * BdotV + NdotV * NdotV);
    float lambdaL = NdotV * sqrt(aT2 * TdotL * TdotL + aB2 * BdotL * BdotL + NdotL * NdotL);

    return 0.5 / (lambdaV + lambdaL);
}

float GetSmithJointGGXAnisoLambdaV(float TdotV, float BdotV, float NdotV, float roughnessT, float roughnessB)
{
    float aT = roughnessT;
    float aT2 = aT * aT;
    float aB = roughnessB;
    float aB2 = aB * aB;

    return sqrt(aT2 * TdotV * TdotV + aB2 * BdotV * BdotV + NdotV * NdotV);
}

float V_SmithJointGGXAnisoLambdaV(float TdotV, float BdotV, float NdotV, float TdotL, float BdotL, float NdotL, float roughnessT, float roughnessB, float lambdaV)
{
    float aT = roughnessT;
    float aT2 = aT * aT;
    float aB = roughnessB;
    float aB2 = aB * aB;

    lambdaV *= NdotL;
    float lambdaL = NdotV * sqrt(aT2 * TdotL * TdotL + aB2 * BdotL * BdotL + NdotL * NdotL);

    return 0.5 / (lambdaV + lambdaL);
}

//-----------------------------------------------------------------------------
// Diffuse BRDF - diffuseColor is expected to be multiply by the caller
//-----------------------------------------------------------------------------

float LambertNoPI()
{
    return 1.0;
}

float Lambert()
{
    return INV_PI;
}

float DisneyDiffuseNoPI(float NdotV, float NdotL, float LdotH, float perceptualRoughness)
{
    float fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
    // Two schlick fresnel term
    float lightScatter = F_Schlick(1.0, fd90, NdotL);
    float viewScatter = F_Schlick(1.0, fd90, NdotV);

    return lightScatter * viewScatter;
}

float DisneyDiffuse(float NdotV, float NdotL, float LdotH, float perceptualRoughness)
{
    return INV_PI * DisneyDiffuseNoPI(NdotV, NdotL, LdotH, perceptualRoughness);
}

// Ref: Diffuse Lighting for GGX + Smith Microsurfaces, p. 113.
float3 DiffuseGGXNoPI(float3 albedo, float NdotV, float NdotL, float NdotH, float LdotV, float perceptualRoughness)
{
    float facing    = 0.5 + 0.5 * LdotV;
    float rough     = facing * (0.9 - 0.4 * facing) * ((0.5 + NdotH) / NdotH);
    float transmitL = F_Transm_Schlick(0, NdotL);
    float transmitV = F_Transm_Schlick(0, NdotV);
    float smooth    = transmitL * transmitV * 1.05;             // Normalize F_t over the hemisphere
    float single    = lerp(smooth, rough, perceptualRoughness); // Rescaled by PI
    // This constant is picked s.t. setting perceptualRoughness, albedo and all angles to 1
    // allows us to match the Lambertian and the Disney Diffuse models. Original value: 0.1159.
    float multiple  = perceptualRoughness * (0.079577 * PI);    // Rescaled by PI

    return single + albedo * multiple;
}

float3 DiffuseGGX(float3 albedo, float NdotV, float NdotL, float NdotH, float LdotV, float perceptualRoughness)
{
    // Note that we could save 2 cycles by inlining the multiplication by INV_PI.
    return INV_PI * DiffuseGGXNoPI(albedo, NdotV, NdotL, NdotH, LdotV, perceptualRoughness);
}

#endif // UNITY_BSDF_INCLUDED
