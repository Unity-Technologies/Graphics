#ifndef UNITY_BSDF_INCLUDED
#define UNITY_BSDF_INCLUDED

// Note: All NDF and diffuse term have a version with and without divide by PI.
// Version with divide by PI are use for direct lighting.
// Version without divide by PI are use for image based lighting where often the PI cancel during importance sampling

//-----------------------------------------------------------------------------
// Fresnel term
//-----------------------------------------------------------------------------

REAL F_Schlick(REAL f0, REAL f90, REAL u)
{
    REAL x  = 1.0 - u;
    REAL x2 = x * x;
    REAL x5 = x * x2 * x2;
    return (f90 - f0) * x5 + f0;                // sub mul mul mul sub mad
}

REAL F_Schlick(REAL f0, REAL u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad
}

REAL3 F_Schlick(REAL3 f0, REAL f90, REAL u)
{
    REAL x  = 1.0 - u;
    REAL x2 = x * x;
    REAL x5 = x * x2 * x2;
    return f0 * (1.0 - x5) + (f90 * x5);        // sub mul mul mul sub mul mad*3
}

REAL3 F_Schlick(REAL3 f0, REAL u)
{
    return F_Schlick(f0, 1.0, u);               // sub mul mul mul sub mad*3
}

// Does not handle TIR.
REAL F_Transm_Schlick(REAL f0, REAL f90, REAL u)
{
    REAL x  = 1.0 - u;
    REAL x2 = x * x;
    REAL x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad
}

// Does not handle TIR.
REAL F_Transm_Schlick(REAL f0, REAL u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad
}

// Does not handle TIR.
REAL3 F_Transm_Schlick(REAL3 f0, REAL f90, REAL u)
{
    REAL x  = 1.0 - u;
    REAL x2 = x * x;
    REAL x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad*3
}

// Does not handle TIR.
REAL3 F_Transm_Schlick(REAL3 f0, REAL u)
{
    return F_Transm_Schlick(f0, 1.0, u);        // sub mul mul mad mad*3
}

//-----------------------------------------------------------------------------
// Specular BRDF
//-----------------------------------------------------------------------------

REAL D_GGXNoPI(REAL NdotH, REAL roughness)
{
    REAL a2 = Sq(roughness);
    REAL s  = (NdotH * a2 - NdotH) * NdotH + 1.0;
    return a2 / (s * s);
}

REAL D_GGX(REAL NdotH, REAL roughness)
{
    return INV_PI * D_GGXNoPI(NdotH, roughness);
}

// Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, p. 19, 29.
REAL G_MaskingSmithGGX(REAL NdotV, REAL roughness)
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
REAL D_GGX_Visible(REAL NdotH, REAL NdotV, REAL VdotH, REAL roughness)
{
    return D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness) * VdotH / NdotV;
}

// Precompute part of lambdaV
REAL GetSmithJointGGXPartLambdaV(REAL NdotV, REAL roughness)
{
    REAL a2 = Sq(roughness);
    return sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
}

// Note: V = G / (4 * NdotL * NdotV)
// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
REAL V_SmithJointGGX(REAL NdotL, REAL NdotV, REAL roughness, REAL partLambdaV)
{
    REAL a2 = Sq(roughness);

    // Original formulation:
    // lambda_v = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5
    // lambda_l = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5
    // G        = 1 / (1 + lambda_v + lambda_l);

    // Reorder code to be more optimal:
    REAL lambdaV = NdotL * partLambdaV;
    REAL lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0 * NdotL * NdotV) /  ((4.0 * NdotL * NdotV) * (lambda_v + lambda_l));
    return 0.5 / (lambdaV + lambdaL);
}

REAL V_SmithJointGGX(REAL NdotL, REAL NdotV, REAL roughness)
{
    REAL partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
    return V_SmithJointGGX(NdotL, NdotV, roughness, partLambdaV);
}

// Inline D_GGX() * V_SmithJointGGX() together for better code generation.
REAL DV_SmithJointGGX(REAL NdotH, REAL NdotL, REAL NdotV, REAL roughness, REAL partLambdaV)
{
    REAL a2 = Sq(roughness);
    REAL s  = (NdotH * a2 - NdotH) * NdotH + 1.0;

    REAL lambdaV = NdotL * partLambdaV;
    REAL lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    REAL2 D = REAL2(a2, s * s);            // Fraction without the multiplier (1/Pi)
    REAL2 G = REAL2(1, lambdaV + lambdaL); // Fraction without the multiplier (1/2)

    return (INV_PI * 0.5) * (D.x * G.x) / (D.y * G.y);
}

REAL DV_SmithJointGGX(REAL NdotH, REAL NdotL, REAL NdotV, REAL roughness)
{
    REAL partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
    return DV_SmithJointGGX(NdotH, NdotL, NdotV, roughness, partLambdaV);
}

// Precompute a part of LambdaV.
// Note on this linear approximation.
// Exact for roughness values of 0 and 1. Also, exact when the cosine is 0 or 1.
// Otherwise, the worst case relative error is around 10%.
// https://www.desmos.com/calculator/wtp8lnjutx
REAL GetSmithJointGGXPartLambdaVApprox(REAL NdotV, REAL roughness)
{
    REAL a = roughness;
    return NdotV * (1 - a) + a;
}

REAL V_SmithJointGGXApprox(REAL NdotL, REAL NdotV, REAL roughness, REAL partLambdaV)
{
    REAL a = roughness;

    REAL lambdaV = NdotL * partLambdaV;
    REAL lambdaL = NdotV * (NdotL * (1 - a) + a);

    return 0.5 / (lambdaV + lambdaL);
}

REAL V_SmithJointGGXApprox(REAL NdotL, REAL NdotV, REAL roughness)
{
    REAL partLambdaV = GetSmithJointGGXPartLambdaVApprox(NdotV, roughness);
    return V_SmithJointGGXApprox(NdotL, NdotV, roughness, partLambdaV);
}

// roughnessT -> roughness in tangent direction
// roughnessB -> roughness in bitangent direction
REAL D_GGXAnisoNoPI(REAL TdotH, REAL BdotH, REAL NdotH, REAL roughnessT, REAL roughnessB)
{
    REAL a2 = roughnessT * roughnessB;
    REAL3 v = REAL3(roughnessB * TdotH, roughnessT * BdotH, a2 * NdotH);
    REAL  s = dot(v, v);

    return a2 * Sq(a2 / s);
}

REAL D_GGXAniso(REAL TdotH, REAL BdotH, REAL NdotH, REAL roughnessT, REAL roughnessB)
{
    return INV_PI * D_GGXAnisoNoPI(TdotH, BdotH, NdotH, roughnessT, roughnessB);
}

REAL GetSmithJointGGXAnisoPartLambdaV(REAL TdotV, REAL BdotV, REAL NdotV, REAL roughnessT, REAL roughnessB)
{
    return length(REAL3(roughnessT * TdotV, roughnessB * BdotV, NdotV));
}

// Note: V = G / (4 * NdotL * NdotV)
// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4
REAL V_SmithJointGGXAniso(REAL TdotV, REAL BdotV, REAL NdotV, REAL TdotL, REAL BdotL, REAL NdotL, REAL roughnessT, REAL roughnessB, REAL partLambdaV)
{
    REAL lambdaV = NdotL * partLambdaV;
    REAL lambdaL = NdotV * length(REAL3(roughnessT * TdotL, roughnessB * BdotL, NdotL));

    return 0.5 / (lambdaV + lambdaL);
}

REAL V_SmithJointGGXAniso(REAL TdotV, REAL BdotV, REAL NdotV, REAL TdotL, REAL BdotL, REAL NdotL, REAL roughnessT, REAL roughnessB)
{
    REAL partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB);
    return V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB, partLambdaV);
}

// Inline D_GGXAniso() * V_SmithJointGGXAniso() together for better code generation.
REAL DV_SmithJointGGXAniso(REAL TdotH, REAL BdotH, REAL NdotH, REAL NdotV,
                            REAL TdotL, REAL BdotL, REAL NdotL,
                            REAL roughnessT, REAL roughnessB, REAL partLambdaV)
{
    REAL a2 = roughnessT * roughnessB;
    REAL3 v = REAL3(roughnessB * TdotH, roughnessT * BdotH, a2 * NdotH);
    REAL  s = dot(v, v);

    REAL lambdaV = NdotL * partLambdaV;
    REAL lambdaL = NdotV * length(REAL3(roughnessT * TdotL, roughnessB * BdotL, NdotL));

    REAL2 D = REAL2(a2 * a2 * a2, s * s);  // Fraction without the multiplier (1/Pi)
    REAL2 G = REAL2(1, lambdaV + lambdaL); // Fraction without the multiplier (1/2)

    return (INV_PI * 0.5) * (D.x * G.x) / (D.y * G.y);
}

REAL DV_SmithJointGGXAniso(REAL TdotH, REAL BdotH, REAL NdotH,
                            REAL TdotV, REAL BdotV, REAL NdotV,
                            REAL TdotL, REAL BdotL, REAL NdotL,
                            REAL roughnessT, REAL roughnessB)
{
    REAL partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB);
    return DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                 roughnessT, roughnessB, partLambdaV);
}

//-----------------------------------------------------------------------------
// Diffuse BRDF - diffuseColor is expected to be multiply by the caller
//-----------------------------------------------------------------------------

REAL LambertNoPI()
{
    return 1.0;
}

REAL Lambert()
{
    return INV_PI;
}

REAL DisneyDiffuseNoPI(REAL NdotV, REAL NdotL, REAL LdotV, REAL perceptualRoughness)
{
    // (2 * LdotH * LdotH) = 1 + LdotV
    // REAL fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
    REAL fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
    // Two schlick fresnel term
    REAL lightScatter = F_Schlick(1.0, fd90, NdotL);
    REAL viewScatter  = F_Schlick(1.0, fd90, NdotV);

    // Normalize the BRDF for polar view angles of up to (Pi/4).
    // We use the worst case of (roughness = albedo = 1), and, for each view angle,
    // integrate (brdf * cos(theta_light)) over all light directions.
    // The resulting value is for (theta_view = 0), which is actually a little bit larger
    // than the value of the integral for (theta_view = Pi/4).
    // Hopefully, the compiler folds the constant together with (1/Pi).
    return rcp(1.03571) * (lightScatter * viewScatter);
}

REAL DisneyDiffuse(REAL NdotV, REAL NdotL, REAL LdotV, REAL perceptualRoughness)
{
    return INV_PI * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, perceptualRoughness);
}

// Ref: Diffuse Lighting for GGX + Smith Microsurfaces, p. 113.
REAL3 DiffuseGGXNoPI(REAL3 albedo, REAL NdotV, REAL NdotL, REAL NdotH, REAL LdotV, REAL roughness)
{
    REAL facing    = 0.5 + 0.5 * LdotV;              // (LdotH)^2
    REAL rough     = facing * (0.9 - 0.4 * facing) * (0.5 / NdotH + 1);
    REAL transmitL = F_Transm_Schlick(0, NdotL);
    REAL transmitV = F_Transm_Schlick(0, NdotV);
    REAL smooth    = transmitL * transmitV * 1.05;   // Normalize F_t over the hemisphere
    REAL single    = lerp(smooth, rough, roughness); // Rescaled by PI
    REAL multiple  = roughness * (0.1159 * PI);      // Rescaled by PI

    return single + albedo * multiple;
}

REAL3 DiffuseGGX(REAL3 albedo, REAL NdotV, REAL NdotL, REAL NdotH, REAL LdotV, REAL roughness)
{
    // Note that we could save 2 cycles by inlining the multiplication by INV_PI.
    return INV_PI * DiffuseGGXNoPI(albedo, NdotV, NdotL, NdotH, LdotV, roughness);
}

#endif // UNITY_BSDF_INCLUDED
