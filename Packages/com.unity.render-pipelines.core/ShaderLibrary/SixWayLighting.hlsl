#ifndef UNITY_SIX_WAY_COMMON_INCLUDED
#define UNITY_SIX_WAY_COMMON_INCLUDED

#define ABSORPTION_EPSILON max(REAL_MIN, 1e-5)

real3 ComputeDensityScales(real3 absorptionColor)
{
    absorptionColor.rgb = max(ABSORPTION_EPSILON, absorptionColor.rgb);

    // Empirical value used to parametrize absorption from color
    const real absorptionStrength = 0.2f;
    return 1.0f + log2(absorptionColor.rgb) / log2(absorptionStrength);
}

real3 GetTransmissionWithAbsorption(real transmission, real3 densityScales, real absorptionRange)
{
    // Recompute transmission based on density scaling
    return pow(saturate(transmission / absorptionRange), densityScales);
}

real3 GetTransmissionWithAbsorption(real transmission, real4 absorptionColor, real absorptionRange, bool alphaPremultiplied)
{
#if defined(_SIX_WAY_COLOR_ABSORPTION)
    real3 densityScales = ComputeDensityScales(absorptionColor.rgb);

    if(alphaPremultiplied)
        absorptionRange *= (absorptionColor.a > 0) ? absorptionColor.a : 1.0f;

    real3 outTransmission = GetTransmissionWithAbsorption(transmission, densityScales, absorptionRange);
    outTransmission *= absorptionRange;

    return outTransmission;
#else
    return transmission.xxx * absorptionColor.rgb; // simple multiply
#endif
}


real3 GetSixWayDiffuseContributions(real3 rightTopBack, real3 leftBottomFront, real4 baseColor, real3 L0, real3 diffuseGIData[3], real absorptionRange, bool alphaPremultiplied)
{
    real3 giColor = real3(0,0,0);

    // Scale to be energy conserving: Total energy = 4*pi; divided by 6 directions
    real scale = 4.0f * PI / 6.0f;
    #if defined(_SIX_WAY_COLOR_ABSORPTION)
        real3 densityScales = ComputeDensityScales(baseColor.rgb);
        if(alphaPremultiplied)
            absorptionRange *= (baseColor.a > 0) ? baseColor.a : 1.0f;
        for(int i = 0; i < 3; i++)
        {
            real3 bakeDiffuseLighting = L0 + diffuseGIData[i];
            giColor += GetTransmissionWithAbsorption(rightTopBack[i], densityScales, absorptionRange) * bakeDiffuseLighting;
            bakeDiffuseLighting = L0 - diffuseGIData[i];
            giColor += GetTransmissionWithAbsorption(leftBottomFront[i], densityScales, absorptionRange) * bakeDiffuseLighting;
        }
        giColor *= absorptionRange;
    #else
        for(int i = 0; i < 3; i++)
        {
            real3 bakeDiffuseLighting = L0 + diffuseGIData[i];
            giColor += rightTopBack[i] * bakeDiffuseLighting;
            bakeDiffuseLighting = L0 - diffuseGIData[i];
            giColor += leftBottomFront[i] * bakeDiffuseLighting;
        }
        giColor *= baseColor.rgb;
    #endif
    return giColor * scale;
}

#endif // UNITY_SIX_WAY_COMMON_INCLUDED
