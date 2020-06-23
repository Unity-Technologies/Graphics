// Storage format:
// x: the mean value
// y: the squared distance to the mean
// z: sample count
RW_TEXTURE2D_X(float4, _AccumulatedVariance);

void UpdatePerPixelVariance(uint2 pixelCoords, uint iteration, float4 rad)
{
    // Radiance is measured in gamma/perceptual space
    float L = Luminance(LinearToGamma22(rad));

    // Welford's algorithm
    float4 accVariance = (iteration > 0) ? _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] : 0;
    float delta = L - accVariance.x;
    accVariance.x += delta / (accVariance.z + 1);
    float delta2 = L - accVariance.x;
    accVariance.y += delta * delta2;
    accVariance.z += 1;
    _AccumulatedVariance[COORD_TEXTURE2D_X(pixelCoords)] = accVariance;
}
