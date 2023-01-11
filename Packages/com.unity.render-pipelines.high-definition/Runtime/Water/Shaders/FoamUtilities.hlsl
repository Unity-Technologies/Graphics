#ifndef FOAM_UTILITIES_H_
#define FOAM_UTILITIES_H_

Texture2D<float4> _FoamTexture;

float InvLerp(float from, float to, float value)
{
    return (value - from) / (to - from);
}

// float Remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
// {
//     float rel = InvLerp(origFrom, origTo, value);
//     return lerp(targetFrom, targetTo, rel);
// }

float SurfaceFoam(float2 _UV, float foamTime)
{
    float4 foamMasks = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, _UV * _FoamTilling);
    float microDistanceField = foamMasks.r;
    float temporalNoise = foamMasks.g;
    float foamNoise = saturate(foamMasks.b);
    float macroDistanceField = foamMasks.a;

    foamTime = saturate(foamTime);
    foamTime = pow(foamTime, 4.0);

    // Time offsets
    float microDistanceFieldInfluenceMin = 0.05;
    float microDistanceFieldInfluenceMax = 0.6;
    float MicroDistanceFieldInfluence = lerp(microDistanceFieldInfluenceMin, microDistanceFieldInfluenceMax, foamTime);
    foamTime += (2.0*(1.0f - microDistanceField) - 1.0) * MicroDistanceFieldInfluence;

    float temporalNoiseInfluenceMin = 0.1;
    float temporalNoiseInfluenceMax = 0.2;
    float temporalNoiseInfluence = -lerp(temporalNoiseInfluenceMin, temporalNoiseInfluenceMax, foamTime);
    foamTime += (2.0 * temporalNoise - 1.0) * temporalNoiseInfluence;

    foamTime = saturate(foamTime);

    foamTime = Remap(0.0, 1.0, 0.0, 2.2, foamTime); // easy way to make sure the erosion is over (there are many time offsets)
    foamTime = saturate(foamTime);

    // sharpness
    float sharpnessMin = 0.1;
    float sharpnessMax = 5.0;
    float sharpness = lerp(sharpnessMax, sharpnessMin, foamTime);
    sharpness = max(0.0f, sharpness);

    float alpha = Remap(foamTime, 1.0f, 0.0f, 1.0f, macroDistanceField);
    alpha *= sharpness;
    alpha = saturate(alpha);

    // detail in alpha
    float distanceFieldInAlpha = lerp(macroDistanceField, microDistanceField, 0.5f) * 0.45f;
    distanceFieldInAlpha = 1.0f - distanceFieldInAlpha;
    float noiseInAlpha = pow(foamNoise, 0.3f);

    // fade
    float fadeOverTime = 1.0 - foamTime;

    return (alpha * distanceFieldInAlpha * noiseInAlpha * fadeOverTime * 0.5);
}

float DeepFoam(float2 _UV, float foamTime)
{
    float4 foamMasks = SAMPLE_TEXTURE2D(_FoamTexture, s_linear_repeat_sampler, _UV * _FoamTilling);
    float microDistanceField = foamMasks.r;
    float temporalNoise = foamMasks.g;
    float macroDistanceField = foamMasks.a;

    float noOffsetedTime = foamTime;

    foamTime = saturate(foamTime);
    foamTime = pow(foamTime, 4.0);

    // Time offsets
    float microDistanceFieldInfluenceMin = 0.2;
    float microDistanceFieldInfluenceMax = 0.4;
    float MicroDistanceFieldInfluence = lerp(microDistanceFieldInfluenceMin, microDistanceFieldInfluenceMax, foamTime);
    foamTime += (2.0 * (1.0f - microDistanceField) - 1.0) * MicroDistanceFieldInfluence;

    float temporalNoiseInfluenceMin = 0.15;
    float temporalNoiseInfluenceMax = 0.6;
    float temporalNoiseInfluence = lerp(temporalNoiseInfluenceMin, temporalNoiseInfluenceMax, foamTime);
    foamTime += (2.0 * temporalNoise - 1.0) * -temporalNoiseInfluence;
    foamTime = saturate(foamTime);

    foamTime = Remap(0.0, 1.0, 0.0, 2.0, foamTime);

    foamTime *= 10.0;
    foamTime = saturate(foamTime);

    // sharpness
    float sharpnessMin = 1.0;
    float sharpnessMax = 1.0;
    float sharpness = lerp(sharpnessMin, sharpnessMax, foamTime);
    sharpness = max(0.0f, sharpness);

    float globalTimeOffset = 0.0;
    foamTime -= globalTimeOffset;
    float alpha = Remap(foamTime, 1.0f, 0.0f, 1.0f, macroDistanceField);
    alpha *= sharpness;
    alpha = saturate(alpha);

    float distanceFieldInAlpha = lerp(macroDistanceField, microDistanceField, 0.5f) * 1.0f;
    distanceFieldInAlpha = 1.0f - distanceFieldInAlpha;
    float noiseInAlpha = pow(saturate(foamMasks.b), 4.0f);

    alpha *= noiseInAlpha * distanceFieldInAlpha * 18.0f;
    alpha += lerp(1.0 * temporalNoise, 0.0, saturate(noOffsetedTime * 2.0f));

    return  alpha;
}

#endif // FOAM_UTILITIES_H_
