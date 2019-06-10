// We need a noise texture for sampling
Texture2D<float4>                       _OwenScrambledTexture;
Texture2D<float4>                       _ScramblingTexture;

// TODO: Use the tangent to create the local orthobasis
void CreatePixarOrthoNormalBasis(float3 n, out float3 tangent, out float3 bitangent)
{
    float sign = n.z > 0.0f ? 1.0f : -1.0f;
    float a = -1.0 / (sign + n.z);
    float b = n.x * n.y * a;
    tangent = float3(1.0 + sign * n.x * n.x * a, sign * b, -sign * n.x);
    bitangent = float3(b, sign + n.y * n.y * a, -n.y);
}

uint2 ScramblingValue(uint i, uint j)
{
    i = i % 256;
    j = j % 256;
    return clamp((uint2)(_ScramblingTexture[uint2(i, j)] * 256.0f), uint2(0,0), uint2(255, 255));
}

float GetRaytracingNoiseSample(uint sampleIndex, uint sampleDimension, uint scramblingValue)
{
    // Make sure arguments are in the right range
    sampleIndex = sampleIndex % 256;
    sampleDimension = sampleDimension % 4;

    // Fetch the matching Value sequence
    uint value = clamp((uint)(_OwenScrambledTexture[uint2(sampleIndex, 0)][sampleDimension] * 256.0f), 0, 255);

    // Scramble the value
    value = value ^ scramblingValue;

    // convert to float and return
    float v = (0.5f + value) / 256.0f;
    return v;
}
