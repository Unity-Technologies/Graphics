RW_TEXTURE2D_X(float4, _AccumulatedVariance);

void UpdatePerPixelVariance(uint2 pixelCoords, uint sampleCount, float4 rad)
{
    // Radiance is measured in gamma/perceptual space
    float L = Luminance(LinearToGamma22(rad));

    // Welford's algorithm
    if (sampleCount > 0)
    {
        float4 accVariance = _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)];
        float delta = L - accVariance.x;
        accVariance.x += delta / sampleCount;
        float delta2 = L - accVariance.x;
        accVariance.y += delta * delta2;
        _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
    }
}
