#ifndef SHADER_DEBUG_PRINT_INCLUDED
#define SHADER_DEBUG_PRINT_INCLUDED

RWStructuredBuffer<uint> shaderDebugOutputData : register(u7);

static const int MaxShaderDebugOutputElements = 1024 * 1024; // 1M - must match the C# side buffer size

// Input Constants
float4 _ShaderDebugPrintInputMouse;
int    _ShaderDebugPrintInputFrame;

int2 ShaderDebugMouseCoords()      { return _ShaderDebugPrintInputMouse.xy; }
int  ShaderDebugMouseButtonLeft()  { return _ShaderDebugPrintInputMouse.z;  }
int  ShaderDebugMouseButtonRight() { return _ShaderDebugPrintInputMouse.w;  }
int  ShaderDebugFrameNumber()      { return _ShaderDebugPrintInputFrame; }

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
static const int ValueTypeHasTag = 128;

#define PRINT1(TYPE, VALUE, HASTAG, TAG) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        uint elements = 2; \
        if (HASTAG) elements++; \
        InterlockedAdd(shaderDebugOutputData[0], elements, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index++] = TYPE | HASTAG; \
            if (HASTAG) shaderDebugOutputData[index++] = TAG[0] | (TAG[1] << 8) | (TAG[2] << 16) | (TAG[3] << 24); \
            shaderDebugOutputData[index++] = VALUE; \
        } \
    } \
}

#define PRINT2(TYPE, VALUE, HASTAG, TAG) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        uint elements = 3; \
        if (HASTAG) elements++; \
        InterlockedAdd(shaderDebugOutputData[0], elements, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index++] = TYPE | HASTAG; \
            if (HASTAG) shaderDebugOutputData[index++] = TAG[0] | (TAG[1] << 8) | (TAG[2] << 16) | (TAG[3] << 24); \
            shaderDebugOutputData[index++] = VALUE.x; \
            shaderDebugOutputData[index++] = VALUE.y; \
        } \
    } \
}

#define PRINT3(TYPE, VALUE, HASTAG, TAG) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        uint elements = 4; \
        if (HASTAG) elements++; \
        InterlockedAdd(shaderDebugOutputData[0], elements, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index++] = TYPE | HASTAG; \
            if (HASTAG) shaderDebugOutputData[index++] = TAG[0] | (TAG[1] << 8) | (TAG[2] << 16) | (TAG[3] << 24); \
            shaderDebugOutputData[index++] = VALUE.x; \
            shaderDebugOutputData[index++] = VALUE.y; \
            shaderDebugOutputData[index++] = VALUE.z; \
        } \
    } \
}

#define PRINT4(TYPE, VALUE, HASTAG, TAG) \
{ \
    if (shaderDebugOutputData[0] < MaxShaderDebugOutputElements) \
    { \
        uint index; \
        uint elements = 5; \
        if (HASTAG) elements++; \
        InterlockedAdd(shaderDebugOutputData[0], elements, index); \
        index++; \
        if (index < MaxShaderDebugOutputElements) \
        { \
            shaderDebugOutputData[index++] = TYPE | HASTAG; \
            if (HASTAG) shaderDebugOutputData[index++] = TAG[0] | (TAG[1] << 8) | (TAG[2] << 16) | (TAG[3] << 24); \
            shaderDebugOutputData[index++] = VALUE.x; \
            shaderDebugOutputData[index++] = VALUE.y; \
            shaderDebugOutputData[index++] = VALUE.z; \
            shaderDebugOutputData[index++] = VALUE.w; \
        } \
    } \
}

uint ShaderDebugNoTag[4];

void ShaderDebugPrint(uint tag[4], uint value) PRINT1(ValueTypeUint, value, ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag[4], int value) PRINT1(ValueTypeInt, value, ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag[4], float value) PRINT1(ValueTypeFloat, value, ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag[4], uint2 value)  PRINT2(ValueTypeUint2, value, ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], int2 value)   PRINT2(ValueTypeInt2, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], float2 value) PRINT2(ValueTypeFloat2, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], uint3 value)  PRINT3(ValueTypeUint3, value, ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], int3 value)   PRINT3(ValueTypeInt3, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], float3 value) PRINT3(ValueTypeFloat3, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], uint4 value)  PRINT4(ValueTypeUint4, value, ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], int4 value)   PRINT4(ValueTypeInt4, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag[4], float4 value) PRINT4(ValueTypeFloat4, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint value)   PRINT1(ValueTypeUint, value, 0, ShaderDebugNoTag)
void ShaderDebugPrint(int value)    PRINT1(ValueTypeInt, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float value)  PRINT1(ValueTypeFloat, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint2 value)  PRINT2(ValueTypeUint2, value, 0, ShaderDebugNoTag)
void ShaderDebugPrint(int2 value)   PRINT2(ValueTypeInt2, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float2 value) PRINT2(ValueTypeFloat2, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint3 value)  PRINT3(ValueTypeUint3, value, 0, ShaderDebugNoTag)
void ShaderDebugPrint(int3 value)   PRINT3(ValueTypeInt3, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float3 value) PRINT3(ValueTypeFloat3, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint4 value)  PRINT4(ValueTypeUint4, value, 0, ShaderDebugNoTag)
void ShaderDebugPrint(int4 value)   PRINT4(ValueTypeInt4, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float4 value) PRINT4(ValueTypeFloat4, asuint(value), 0, ShaderDebugNoTag)

#undef PRINT1
#undef PRINT2
#undef PRINT3
#undef PRINT4

#define PRINT_MOUSE(VALUE)                        \
{                                                 \
    if(all(pixelPos == ShaderDebugMouseCoords())) \
        ShaderDebugPrint(value);                  \
}

void ShaderDebugPrintMouseOver(int2 pixelPos, uint   value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, int    value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, float  value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint2  value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, int2   value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, float2 value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint3  value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, int3   value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, float3 value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint4  value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, int4   value) PRINT_MOUSE(value);
void ShaderDebugPrintMouseOver(int2 pixelPos, float4 value) PRINT_MOUSE(value);

#undef PRINT_MOUSE

#endif // SHADER_DEBUG_PRINT_INCLUDED
