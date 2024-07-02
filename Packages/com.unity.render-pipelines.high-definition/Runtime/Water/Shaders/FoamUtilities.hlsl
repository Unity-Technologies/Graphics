#ifndef FOAM_UTILITIES_H_
#define FOAM_UTILITIES_H_

Texture2D<float4> _FoamTexture;

float FoamErosion(float foamTime, float2 position, bool isSurfaceFoam = true, float lod = 0)
{
    float2 currentDirection = OrientationToDirection(_PatchOrientation[0]);

#if defined(WATER_LOCAL_CURRENT)
    currentDirection = SampleWaterGroup0CurrentMap(position);

    // Apply the current orientation
    float sinC, cosC;
    sincos(_GroupOrientation[0], sinC, cosC);
    currentDirection = float2(cosC * currentDirection.x - sinC * currentDirection.y, sinC * currentDirection.x + cosC * currentDirection.y);
#endif

    // Magic number for the foam speed to match current speed.
    currentDirection *= 3.0f;
    float2 lerpFactors = frac(_SimulationTime * 0.5f * _FoamCurrentInfluence + float2(0.0, 0.5));
    float2 UVA = position - currentDirection * lerpFactors.x;
    float2 UVB = position - currentDirection * lerpFactors.y;

    float4 foamMasksA = float4(0,0,0,0);
    float4 foamMasksB = float4(0,0,0,0);
    float4 foamMasks = float4(0,0,0,0);

    float lerpFactor = 0;

    if(_FoamCurrentInfluence > 0)
        lerpFactor = pow(cos(lerpFactors.x * PI), 2);


    // We still use lodBias for surface foam but force the LOD for deep foam because we want it blurrier.
    if	(isSurfaceFoam)
    {
        foamMasksA = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, UVA * _WaterFoamTiling);
        if (_FoamCurrentInfluence > 0)
            foamMasksB = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, UVB * _WaterFoamTiling);
    }
    else
    {
        foamMasksA = SAMPLE_TEXTURE2D_LOD(_FoamTexture, s_linear_repeat_sampler, UVA * _WaterFoamTiling, lod);
        if (_FoamCurrentInfluence > 0)
            foamMasksB = SAMPLE_TEXTURE2D_LOD(_FoamTexture, s_linear_repeat_sampler, UVB * _WaterFoamTiling, lod);
    }

    foamMasks = lerp(foamMasksA, foamMasksB, lerpFactor);

    float microDistanceField = foamMasks.r;
    float temporalNoise = foamMasks.g;
    float foamNoise = saturate(foamMasks.b);
    float macroDistanceField = foamMasks.a;

    foamTime = saturate(foamTime);
    float initialFoamTime = pow(foamTime, 32);

    // Time offsets
    float microDistanceFieldInfluenceMin = 0.05;
    float microDistanceFieldInfluenceMax = 0.6;
    float microDistanceFieldInfluence = lerp(microDistanceFieldInfluenceMin, microDistanceFieldInfluenceMax, foamTime);

    float temporalNoiseInfluenceMin = 0.1;
    float temporalNoiseInfluenceMax = 0.2;
    float temporalNoiseInfluence = lerp(temporalNoiseInfluenceMin, temporalNoiseInfluenceMax, foamTime);

    float erosion = saturate(temporalNoise * temporalNoiseInfluence + microDistanceField * microDistanceFieldInfluence);

    foamTime -= erosion;
    foamTime = smoothstep(0.1,0.9,foamTime);

    float alpha, distanceFieldInAlpha = 0;

    // thoses type of erosions is only used for surface foam
    if (isSurfaceFoam)
    {
        // sharpness
        float sharpnessMin = 0.1;
        float sharpnessMax = 5.0;
        alpha = Remap(foamTime, 1.0f, 0.0f, 1.0f, macroDistanceField);
        alpha = saturate(alpha * lerp(sharpnessMax, sharpnessMin, foamTime));

        // detail in alpha
        distanceFieldInAlpha = lerp(macroDistanceField, microDistanceField, 0.5);
        distanceFieldInAlpha = 1.0f - 0.45*distanceFieldInAlpha;
    }

    // This is for the foam to disappear up to the end.
    foamTime += initialFoamTime;

    float fadeOverTime = saturate(1.0 - foamTime);

    if (isSurfaceFoam)
        return (alpha * distanceFieldInAlpha * foamNoise * fadeOverTime);
    else
        return fadeOverTime;
}


#endif // FOAM_UTILITIES_H_
