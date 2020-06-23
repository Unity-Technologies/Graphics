#ifndef SHADER_DEBUG_PRINT_INCLUDED
#define SHADER_DEBUG_PRINT_INCLUDED

RWStructuredBuffer<uint> shaderDebugOutputData : register(u7);

static const int MaxShaderDebugOutputElements = 1024 * 1024; // 1M - must match the C# side buffer size

// Input Constants
float4 _ShaderDebugPrintInputMouse;
float4 _ShaderDebugPrintInputExtras;

static const int ValueTypeUint   = 1;
static const int ValueTypeInt    = 2;
static const int ValueTypeFloat  = 3;
static const int ValueTypeUint2  = 4;
static const int ValueTypeInt2   = 5;
static const int ValueTypeFloat2 = 6;
static const int ValueTypeUint3  = 7;
static const int ValueTypeInt3   = 8;
static const int ValueTypeFloat3 = 9;
static const int ValueTypeUint4  = 10;
static const int ValueTypeInt4   = 11;
static const int ValueTypeFloat4 = 12;

#define PRINT1(TYPE, VALUE) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        InterlockedAdd(shaderDebugOutputData[0], 2, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index + 0] = TYPE; \
            shaderDebugOutputData[index + 1] = VALUE; \
        } \
    } \
}

#define PRINT2(TYPE, VALUE) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        InterlockedAdd(shaderDebugOutputData[0], 3, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index + 0] = TYPE; \
            shaderDebugOutputData[index + 1] = VALUE.x; \
            shaderDebugOutputData[index + 2] = VALUE.y; \
        } \
    } \
}

#define PRINT3(TYPE, VALUE) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        InterlockedAdd(shaderDebugOutputData[0], 4, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index + 0] = TYPE; \
            shaderDebugOutputData[index + 1] = VALUE.x; \
            shaderDebugOutputData[index + 2] = VALUE.y; \
            shaderDebugOutputData[index + 3] = VALUE.z; \
        } \
    } \
}

#define PRINT4(TYPE, VALUE) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        InterlockedAdd(shaderDebugOutputData[0], 5, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index + 0] = TYPE; \
            shaderDebugOutputData[index + 1] = VALUE.x; \
            shaderDebugOutputData[index + 2] = VALUE.y; \
            shaderDebugOutputData[index + 3] = VALUE.z; \
            shaderDebugOutputData[index + 4] = VALUE.w; \
        } \
    } \
}

void ShaderDebugPrint(uint value)   PRINT1(ValueTypeUint, value)
void ShaderDebugPrint(int value)    PRINT1(ValueTypeInt, asuint(value))
void ShaderDebugPrint(float value)  PRINT1(ValueTypeFloat, asuint(value))
void ShaderDebugPrint(uint2 value)  PRINT2(ValueTypeUint2, value)
void ShaderDebugPrint(int2 value)   PRINT2(ValueTypeInt2, asuint(value))
void ShaderDebugPrint(float2 value) PRINT2(ValueTypeFloat2, asuint(value))
void ShaderDebugPrint(uint3 value)  PRINT3(ValueTypeUint3, value)
void ShaderDebugPrint(int3 value)   PRINT3(ValueTypeInt3, asuint(value))
void ShaderDebugPrint(float3 value) PRINT3(ValueTypeFloat3, asuint(value))
void ShaderDebugPrint(uint4 value)  PRINT4(ValueTypeUint4, value)
void ShaderDebugPrint(int4 value)   PRINT4(ValueTypeInt4, asuint(value))
void ShaderDebugPrint(float4 value) PRINT4(ValueTypeFloat4, asuint(value))

#undef PRINT1
#undef PRINT2
#undef PRINT3
#undef PRINT4

#endif // SHADER_DEBUG_PRINT_INCLUDED
