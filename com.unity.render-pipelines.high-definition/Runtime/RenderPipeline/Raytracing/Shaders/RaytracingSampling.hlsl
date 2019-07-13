// We need a noise texture for sampling
Texture2D<float2>                       _OwenScrambledRGTexture;
Texture2D<float>                        _OwenScrambledTexture;
Texture2D<float4>                       _ScramblingTexture;

uint2 ScramblingValue(uint i, uint j)
{
    i = i % 256;
    j = j % 256;
    return clamp((uint2)(_ScramblingTexture[uint2(i, j)] * 256.0f), uint2(0,0), uint2(255, 255));
}

float2 GetRaytracingNoiseSampleRG(uint sampleIndex, uint2 scramblingValue)
{
    // Make sure arguments are in the right range
    sampleIndex = sampleIndex % 256;

    // Fetch the matching Value sequence
    uint2 value = clamp((uint)(_OwenScrambledRGTexture[uint2(sampleIndex, 0)].xy * 256.0f), 0, 255);

    // Scramble the value
    value = value ^ scramblingValue;

    // convert to float and return
    float2 v = (0.5 + float2(value)) / 256.0;
    return v;
}

float GetRaytracingNoiseSample(uint sampleIndex, uint sampleDimension, uint scramblingValue)
{
    // Make sure arguments are in the right range
    sampleIndex = sampleIndex % 256;
    sampleDimension = sampleDimension % 256;

    // Fetch the matching Value sequence
    uint value = clamp((uint)(_OwenScrambledTexture[uint2(sampleDimension, sampleIndex)].x * 256.0f), 0, 255);

    // Scramble the value
    value = value ^ scramblingValue;

    // convert to float and return
    float v = (0.5f + value) / 256.0f;
    return v;
}
