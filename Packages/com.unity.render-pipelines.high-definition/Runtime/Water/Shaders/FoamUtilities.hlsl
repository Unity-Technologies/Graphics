#ifndef FOAM_UTILITIES_H_
#define FOAM_UTILITIES_H_

Texture2D<float4> _FoamTexture;

float SurfaceFoam(float2 _UV, float foamTime)
{
    float4 foamMasks = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, _UV * _FoamTiling);
    float microDistanceField = foamMasks.r;
    float temporalNoise = foamMasks.g;
    float foamNoise = saturate(foamMasks.b);
    float macroDistanceField = foamMasks.a;

    foamTime = saturate(foamTime);
    foamTime = pow(foamTime, 4.0);

    // Time offsets
    float microDistanceFieldInfluenceMin = 0.05;
    float microDistanceFieldInfluenceMax = 0.6;
    float microDistanceFieldInfluence = -lerp(microDistanceFieldInfluenceMin, microDistanceFieldInfluenceMax, foamTime);
    foamTime += (2.0 * microDistanceField - 1.0) * microDistanceFieldInfluence;

    float temporalNoiseInfluenceMin = 0.1;
    float temporalNoiseInfluenceMax = 0.2;
    float temporalNoiseInfluence = -lerp(temporalNoiseInfluenceMin, temporalNoiseInfluenceMax, foamTime);
    foamTime += (2.0 * temporalNoise - 1.0) * temporalNoiseInfluence;

    // easy way to make sure the erosion is over (there are many time offsets)
    foamTime = Remap(0.0, 1.0, 0.0, 2.2, saturate(foamTime));
    foamTime = saturate(foamTime);

    // sharpness
    float sharpnessMin = 0.1;
    float sharpnessMax = 5.0;
    float alpha = Remap(foamTime, 1.0f, 0.0f, 1.0f, macroDistanceField);
    alpha = saturate(alpha * lerp(sharpnessMax, sharpnessMin, foamTime));

    // detail in alpha
    float distanceFieldInAlpha = lerp(macroDistanceField, microDistanceField, 0.5f);
    distanceFieldInAlpha = 1.0f - 0.45f * distanceFieldInAlpha;
    float noiseInAlpha = pow(foamNoise, 0.3f);

    // fade
    float fadeOverTime = 1.0 - foamTime;

    return (alpha * distanceFieldInAlpha * noiseInAlpha * fadeOverTime * 0.5);
}

float DeepFoam(float2 _UV, float foamTime)
{
    float4 foamMasks = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, _UV * _FoamTiling);
    float microDistanceField = foamMasks.r;
    float temporalNoise = foamMasks.g;
    float foamNoise = saturate(foamMasks.b);
    float macroDistanceField = foamMasks.a;

    float noOffsetedTime = foamTime;

    foamTime = saturate(foamTime);
    foamTime = pow(foamTime, 4.0);

    // Time offsets
    float microDistanceFieldInfluenceMin = 0.2;
    float microDistanceFieldInfluenceMax = 0.4;
    float microDistanceFieldInfluence = -lerp(microDistanceFieldInfluenceMin, microDistanceFieldInfluenceMax, foamTime);
    foamTime += (2.0 * microDistanceField - 1.0) * microDistanceFieldInfluence;

    float temporalNoiseInfluenceMin = 0.15;
    float temporalNoiseInfluenceMax = 0.6;
    float temporalNoiseInfluence = -lerp(temporalNoiseInfluenceMin, temporalNoiseInfluenceMax, foamTime);
    foamTime += (2.0 * temporalNoise - 1.0) * temporalNoiseInfluence;

    foamTime = Remap(0.0, 1.0, 0.0, 2.0, saturate(foamTime));

    foamTime = saturate(10.0 * foamTime);

    float alpha = Remap(foamTime, 1.0f, 0.0f, 1.0f, macroDistanceField);
    alpha = saturate(alpha);

    // detail in alpha
    float distanceFieldInAlpha = lerp(macroDistanceField, microDistanceField, 0.5f);
    distanceFieldInAlpha = 1.0f - distanceFieldInAlpha;
    float noiseInAlpha = pow(foamNoise, 4.0f);

    alpha *= noiseInAlpha * distanceFieldInAlpha * 18.0f;
    alpha += lerp(temporalNoise, 0.0, saturate(noOffsetedTime * 2.0f));

    return  alpha;
}

#endif // FOAM_UTILITIES_H_
