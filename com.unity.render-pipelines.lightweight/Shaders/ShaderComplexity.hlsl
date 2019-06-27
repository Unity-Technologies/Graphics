#ifndef LIGHTWEIGHT_SHADERCOMPLEXITY_INCLUDED
#define LIGHTWEIGHT_SHADERCOMPLEXITY_INCLUDED

half4 GetComplexityColor(float4 instructionCount, half4 originalColor)
{
    half4 lut[5] = {
           half4(0, 1, 0, 0),
           half4(0.25, 0.75, 0, 0),
           half4(0.498, 0.5019, 0.0039, 0),
           half4(0.749, 0.247, 0, 0),
           half4(1, 0, 0, 0)
    };
    half cutoff[5] = {
        half(50),
        half(90), // Tweak these because we don't normalize yet. Tweaked for scene 26
        half(92),
        half(94),
        half(10000)
    };

    half ALU = instructionCount.x;
    for (int i = 0; i < 5; ++i)
    {
        if (ALU < cutoff[i])
        {
            return lut[i];
        }
    }

    return originalColor;
}

half4 OutputShaderComplexityColor(half4 shaderOutputColor)
{
    half4 colorCalculationsCompensation = half4(0, 0, 0, 0); // E.g., our color calc takes x instructions
    half4 complexity = unity_ShaderComplexity - colorCalculationsCompensation; // Substract these extra instructions
    half4 complexityColor = GetComplexityColor(complexity, shaderOutputColor);
    return complexityColor; // Make sure the compiler does not throw away the original shader just because we are outputting (mostly) a complexity color!   
}

#endif // LIGHTWEIGHT_SHADERCOMPLEXITY_INCLUDED
