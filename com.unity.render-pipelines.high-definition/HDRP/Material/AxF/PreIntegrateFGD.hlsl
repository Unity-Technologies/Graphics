///////////////////////////////////////////////////////////////////////////////////////////////////
// Pre-Integrated FDG term for the Ward BRDF as used by the AxF format
///////////////////////////////////////////////////////////////////////////////////////////////////
//

// ----------------------------------------------------------------------------
// Importance sampling BSDF functions
// ----------------------------------------------------------------------------

void SampleWardDir( real2 u, real3 V, real3x3 localToWorld, real roughness,
                    out real3 L, out real NdotL, out real NdotH, out real VdotH, bool VeqN = false)
{
    // GGX NDF sampling
    real cosTheta = sqrt((1.0 - u.x) / (1.0 + (roughness * roughness - 1.0) * u.x));
    real phi = TWO_PI * u.y;

    real3 localH = SphericalToCartesian(phi, cosTheta);

    NdotH = cosTheta;

    real3 localV;

    if (VeqN)
    {
        // localV == localN
        localV = real3(0.0, 0.0, 1.0);
        VdotH = NdotH;
    }
    else
    {
        localV = mul(V, transpose(localToWorld));
        VdotH = saturate(dot(localV, localH));
    }

    // Compute { localL = reflect(-localV, localH) }
    real3 localL = -localV + 2.0 * VdotH * localH;
    NdotL = localL.z;

    L = mul(localL, localToWorld);
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleWard(  real2 u, real3 V, real3x3 localToWorld, real roughness, real NdotV,
                            out real3 L, out real VdotH, out real NdotL, out real weightOverPdf)
{
    real NdotH;
    SampleWardDir(u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH);

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // Remind (L.H) == (V.H)
    // F is apply outside the function
    real Vis = V_SmithJointGGX(NdotL, NdotV, roughness);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#if !defined SHADER_API_GLES

real4 IntegrateWardFGD(real3 V, real3 N, real roughness, uint sampleCount = 8192)
{
    real NdotV = ClampNdotV(dot(N, V));
    real4 acc = 0.0;
    real2 randNum = InitRandom(V.xy * 0.5 + 0.5);    // Add some jittering on Hammersley2d

    real3x3 localToWorld = GetLocalFrame(N);

    for (uint i = 0; i < sampleCount; ++i)
    {
        real2 u = frac(randNum + Hammersley2d(i, sampleCount));

        real VdotH;
        real NdotL;
        real weightOverPdf;

        real3 L; // Unused
        ImportanceSampleWard(   u, V, localToWorld, roughness, NdotV,
                                L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Integral{BSDF * <N,L> dw} =
            // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
            // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
            // (1 - F0) * x + F0 * y = lerp(x, y, F0)
            acc.x += weightOverPdf * pow(1 - VdotH, 5);
            acc.y += weightOverPdf;
        }

        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            real LdotV = dot(L, V);
            real disneyDiffuse = DisneyDiffuseNoPI(NdotV, NdotL, LdotV, RoughnessToPerceptualRoughness(roughness));

            acc.z += disneyDiffuse * weightOverPdf;
        }
    }

    acc /= sampleCount;

    // Remap from the [0.5, 1.5] to the [0, 1] range.
    acc.z -= 0.5;

    return acc;
}
#else
// Not supported due to lack of random library in GLES 2
#define IntegrateGGXAndDisneyFGD ERROR_ON_UNSUPPORTED_FUNCTION(IntegrateGGXAndDisneyFGD)
#endif
