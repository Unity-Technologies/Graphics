#ifndef SHADER_DEBUG_PRINT_INCLUDED
#define SHADER_DEBUG_PRINT_INCLUDED

RWStructuredBuffer<uint> shaderDebugOutputData : register(u7);

static const int MaxShaderDebugOutputElements = 1024 * 1024; // 1M - must match the C# side buffer size

void ShaderDebugPrint(uint value)
{
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements)
    {
        uint index;
        InterlockedAdd(shaderDebugOutputData[0], 1, index);
        index++; // Skip the counter in the beginning
        if (index < MaxShaderDebugOutputElements)
        {
            shaderDebugOutputData[index] = value;
        }
    }
}

#endif // SHADER_DEBUG_PRINT_INCLUDED
