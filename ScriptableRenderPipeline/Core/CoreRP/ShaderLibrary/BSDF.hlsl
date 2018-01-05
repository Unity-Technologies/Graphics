#ifndef UNITY_BSDF_INCLUDED
#define UNITY_BSDF_INCLUDED

// Note: All NDF and diffuse term have a version with and without divide by PI.
// Version with divide by PI are use for direct lighting.
// Version without divide by PI are use for image based lighting where often the PI cancel during importance sampling

//-----------------------------------------------------------------------------
// Fresnel term
//-----------------------------------------------------------------------------

real F_Schlick(real f0, real f90, real u)
{
    real x  = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (f90 - f0) * x5 + f0;                // sub mul mul mul sub mad
}

real F_Schlick(real f0, real u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad
}

real3 F_Schlick(real3 f0, real f90, real u)
{
    real x  = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return f0 * (1.0 - x5) + (f90 * x5);        // sub mul mul mul sub mul mad*3
}

real3 F_Schlick(real3 f0, real u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad*3
}

// Does not handle TIR.
real F_Transm_Schlick(real f0, real f90, real u)
{
    real x  = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad
}

// Does not handle TIR.
real F_Transm_Schlick(real f0, real u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad
}

// Does not handle TIR.
real3 F_Transm_Schlick(real3 f0, real f90, real u)
{
    real x  = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad*3
}

// Does not handle TIR.
real3 F_Transm_Schlick(real3 f0, real u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad*3
}

//-----------------------------------------------------------------------------
// Specular BRDF
//-----------------------------------------------------------------------------

real D_GGXNoPI(real NdotH, real roughness)
{
    real a2 = Sq(roughness);
    real s  = (NdotH * a2 - NdotH) * NdotH + 1.0;
    return a2 / (s * s);
}

real D_GGX(real NdotH, real roughness)
{
    return INV_PI * D_GGXNoPI(NdotH, roughness);
}

// Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, p. 19, 29.
real G_MaskingSmithGGX(real NdotV, real roughness)
{
    // G1(V, H)    = HeavisideStep(VdotH) / (1 + Λ(V)).
    // Λ(V)        = -0.5 + 0.5 * sqrt(1 + 1 / a²).
    // a           = 1 / (roughness * tan(theta)).
    // 1 + Λ(V)    = 0.5 + 0.5 * sqrt(1 + roughness² * tan²(theta)).
    // tan²(theta) = (1 - cos²(theta)) / cos²(theta) = 1 / cos²(theta) - 1.
    // Assume that (VdotH > 0), e.i. (acos(LdotV) < Pi).

    return 1.0 / (0.5 + 0.5 * sqrt(1.0 + Sq(roughness) * (1.0 / Sq(NdotV) - 1.0)));
}

// Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, p. 12.
real D_GGX_Visible(real NdotH, real NdotV, real VdotH, real roughness)
{
    return D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness) * VdotH / NdotV;
}

// Precompute part of lambdaV
real GetSmithJointGGXPartLambdaV(real NdotV, real roughness)
{
    real a2 = Sq(roughness);
    return sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
}

// Note: V = G / (4 * NdotL * NdotV)
// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
real V_SmithJointGGX(real NdotL, real NdotV, real roughness, real partLambdaV)
{
    real a2 = Sq(roughness);

    // Original formulation:
    // lambda_v = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5
    // lambda_l = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5
    // G        = 1 / (1 + lambda_v + lambda_l);

    // Reorder code to be more optimal:
    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0 * NdotL * NdotV) /  ((4.0 * NdotL * NdotV) * (lambda_v + lambda_l));
    return 0.5 / (lambdaV + lambdaL);
}

real V_SmithJointGGX(real NdotL, real NdotV, real roughness)
{
    real partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
    return V_SmithJointGGX(NdotL, NdotV, roughness, partLambdaV);
}

// Inline D_GGX() * V_SmithJointGGX() together for better code generation.
real DV_SmithJointGGX(real NdotH, real NdotL, real NdotV, real roughness, real partLambdaV)
{
    real a2 = Sq(roughness);
    real s  = (NdotH * a2 - NdotH) * NdotH + 1.0;

    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    real2 D = real2(a2, s * s);            // Fraction without the multiplier (1/Pi)
    real2 G = real2(1, lambdaV + lambdaL); // Fraction without the multiplier (1/2)

    return (INV_PI * 0.5) * (D.x * G.x) / (D.y * G.y);
}

real DV_SmithJointGGX(real NdotH, real NdotL, real NdotV, real roughness)
{
    real partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
    return DV_SmithJointGGX(NdotH, NdotL, NdotV, roughness, partLambdaV);
}

// Precompute a part of LambdaV.
// Note on this linear approximation.
// Exact for roughness values of 0 and 1. Also, exact when the cosine is 0 or 1.
// Otherwise, the worst case relative error is around 10%.
// https://www.desmos.com/calculator/wtp8lnjutx
real GetSmithJointGGXPartLambdaVApprox(real NdotV, real roughness)
{
    real a = roughness;
    return NdotV * (1 - a) + a;
}

real V_SmithJointGGXApprox(real NdotL, real NdotV, real roughness, real partLambdaV)
{
    real a = roughness;

    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * (NdotL * (1 - a) + a);

    return 0.5 / (lambdaV + lambdaL);
}

real V_SmithJointGGXApprox(real NdotL, real NdotV, real roughness)
{
    real partLambdaV = GetSmithJointGGXPartLambdaVApprox(NdotV, roughness);
    return V_SmithJointGGXApprox(NdotL, NdotV, roughness, partLambdaV);
}

// roughnessT -> roughness in tangent direction
// roughnessB -> roughness in bitangent direction
real D_GGXAnisoNoPI(real TdotH, real BdotH, real NdotH, real roughnessT, real roughnessB)
{
    real a2 = roughnessT * roughnessB;
    real3 v = real3(roughnessB * TdotH, roughnessT * BdotH, a2 * NdotH);
    real  s = dot(v, v);

    return a2 * Sq(a2 / s);
}

real D_GGXAniso(real TdotH, real BdotH, real NdotH, real roughnessT, real roughnessB)
{
    return INV_PI * D_GGXAnisoNoPI(TdotH, BdotH, NdotH, roughnessT, roughnessB);
}

real GetSmithJointGGXAnisoPartLambdaV(real TdotV, real BdotV, real NdotV, real roughnessT, real roughnessB)
{
    return length(real3(roughnessT * TdotV, roughnessB * BdotV, NdotV));
}

// Note: V = G / (4 * NdotL * NdotV)
// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4
real V_SmithJointGGXAniso(real TdotV, real BdotV, real NdotV, real TdotL, real BdotL, real NdotL, real roughnessT, real roughnessB, real partLambdaV)
{
    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * length(real3(roughnessT * TdotL, roughnessB * BdotL, NdotL));

    return 0.5 / (lambdaV + lambdaL);
}

real V_SmithJointGGXAniso(real TdotV, real BdotV, real NdotV, real TdotL, real BdotL, real NdotL, real roughnessT, real roughnessB)
{
    real partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB);
    return V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB, partLambdaV);
}

// Inline D_GGXAniso() * V_SmithJointGGXAniso() together for better code generation.
real DV_SmithJointGGXAniso(real TdotH, real BdotH, real NdotH, real NdotV,
                           real TdotL, real BdotL, real NdotL,
                           real roughnessT, real roughnessB, real partLambdaV)
{
    real a2 = roughnessT * roughnessB;
    real3 v = real3(roughnessB * TdotH, roughnessT * BdotH, a2 * NdotH);
    real  s = dot(v, v);

    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * length(real3(roughnessT * TdotL, roughnessB * BdotL, NdotL));

    real2 D = real2(a2 * a2 * a2, s * s);  // Fraction without the multiplier (1/Pi)
    real2 G = real2(1, lambdaV + lambdaL); // Fraction without the multiplier (1/2)

    return (INV_PI * 0.5) * (D.x * G.x) / (D.y * G.y);
}

real DV_SmithJointGGXAniso(real TdotH, real BdotH, real NdotH,
                           real TdotV, real BdotV, real NdotV,
                           real TdotL, real BdotL, real NdotL,
                           real roughnessT, real roughnessB)
{
    real partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB);
    return DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                 roughnessT, roughnessB, partLambdaV);
}

//-----------------------------------------------------------------------------
// Diffuse BRDF - diffuseColor is expected to be multiply by the caller
//-----------------------------------------------------------------------------

real LambertNoPI()
{
    return 1.0;
}

real Lambert()
{
    return INV_PI;
}

real DisneyDiffuseNoPI(real NdotV, real NdotL, real LdotV, real perceptualRoughness)
{
    // (2 * LdotH * LdotH) = 1 + LdotV
    // real fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
    real fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
    // Two schlick fresnel term
    real lightScatter = F_Schlick(1.0, fd90, NdotL);
    real viewScatter  = F_Schlick(1.0, fd90, NdotV);

    // Normalize the BRDF for polar view angles of up to (Pi/4).
    // We use the worst case of (roughness = albedo = 1), and, for each view angle,
    // integrate (brdf * cos(theta_light)) over all light directions.
    // The resulting value is for (theta_view = 0), which is actually a little bit larger
    // than the value of the integral for (theta_view = Pi/4).
    // Hopefully, the compiler folds the constant together with (1/Pi).
    return rcp(1.03571) * (lightScatter * viewScatter);
}

real DisneyDiffuse(real NdotV, real NdotL, real LdotV, real perceptualRoughness)
{
    return INV_PI * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, perceptualRoughness);
}

// Ref: Diffuse Lighting for GGX + Smith Microsurfaces, p. 113.
real3 DiffuseGGXNoPI(real3 albedo, real NdotV, real NdotL, real NdotH, real LdotV, real roughness)
{
    real facing    = 0.5 + 0.5 * LdotV;              // (LdotH)^2
    real rough     = facing * (0.9 - 0.4 * facing) * (0.5 / NdotH + 1);
    real transmitL = F_Transm_Schlick(0, NdotL);
    real transmitV = F_Transm_Schlick(0, NdotV);
    real smooth    = transmitL * transmitV * 1.05;   // Normalize F_t over the hemisphere
    real single    = lerp(smooth, rough, roughness); // Rescaled by PI
    real multiple  = roughness * (0.1159 * PI);      // Rescaled by PI

    return single + albedo * multiple;
}

real3 DiffuseGGX(real3 albedo, real NdotV, real NdotL, real NdotH, real LdotV, real roughness)
{
    // Note that we could save 2 cycles by inlining the multiplication by INV_PI.
    return INV_PI * DiffuseGGXNoPI(albedo, NdotV, NdotL, NdotH, LdotV, roughness);
}

#endif // UNITY_BSDF_INCLUDED
