#ifndef SHADER_DEBUG_PRINT_INCLUDED
#define SHADER_DEBUG_PRINT_INCLUDED

RWStructuredBuffer<uint> shaderDebugOutputData : register(u7);

static const uint MaxShaderDebugOutputElements = 1024 * 1024; // 1M - must match the C# side buffer size

// Input Constants
float4 _ShaderDebugPrintInputMouse;
int    _ShaderDebugPrintInputFrame;

int2 ShaderDebugMouseCoords()      { return _ShaderDebugPrintInputMouse.xy; }
int  ShaderDebugMouseButtonLeft()  { return _ShaderDebugPrintInputMouse.z;  }
int  ShaderDebugMouseButtonRight() { return _ShaderDebugPrintInputMouse.w;  }
int  ShaderDebugFrameNumber()      { return _ShaderDebugPrintInputFrame; }

static const uint ValueTypeUint   = 1;
static const uint ValueTypeInt    = 2;
static const uint ValueTypeFloat  = 3;
static const uint ValueTypeUint2  = 4;
static const uint ValueTypeInt2   = 5;
static const uint ValueTypeFloat2 = 6;
static const uint ValueTypeUint3  = 7;
static const uint ValueTypeInt3   = 8;
static const uint ValueTypeFloat3 = 9;
static const uint ValueTypeUint4  = 10;
static const uint ValueTypeInt4   = 11;
static const uint ValueTypeFloat4 = 12;
static const uint ValueTypeBool   = 13;
static const uint ValueTypeHasTag = 128;

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
            if (HASTAG) shaderDebugOutputData[index++] = TAG; \
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
            if (HASTAG) shaderDebugOutputData[index++] = TAG; \
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
            if (HASTAG) shaderDebugOutputData[index++] = TAG; \
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
            if (HASTAG) shaderDebugOutputData[index++] = TAG; \
            shaderDebugOutputData[index++] = VALUE.x; \
            shaderDebugOutputData[index++] = VALUE.y; \
            shaderDebugOutputData[index++] = VALUE.z; \
            shaderDebugOutputData[index++] = VALUE.w; \
        } \
    } \
}

static const uint ShaderDebugNoTag;

uint Tag(uint a, uint b, uint c, uint d)
{
    return a | (b << 8) | (c << 16) | (d << 24);
}

void ShaderDebugPrint(uint tag, bool   value) PRINT1(ValueTypeBool,   uint(value),   ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag, uint   value) PRINT1(ValueTypeUint,   value,         ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag, int    value) PRINT1(ValueTypeInt,    asuint(value), ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag, float  value) PRINT1(ValueTypeFloat,  asuint(value), ValueTypeHasTag, tag);
void ShaderDebugPrint(uint tag, uint2  value) PRINT2(ValueTypeUint2,  value,         ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, int2   value) PRINT2(ValueTypeInt2,   asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, float2 value) PRINT2(ValueTypeFloat2, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, uint3  value) PRINT3(ValueTypeUint3,  value,         ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, int3   value) PRINT3(ValueTypeInt3,   asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, float3 value) PRINT3(ValueTypeFloat3, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, uint4  value) PRINT4(ValueTypeUint4,  value,         ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, int4   value) PRINT4(ValueTypeInt4,   asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(uint tag, float4 value) PRINT4(ValueTypeFloat4, asuint(value), ValueTypeHasTag, tag)
void ShaderDebugPrint(bool   value) PRINT1(ValueTypeUint,   uint(value),   0, ShaderDebugNoTag)
void ShaderDebugPrint(uint   value) PRINT1(ValueTypeUint,   value,         0, ShaderDebugNoTag)
void ShaderDebugPrint(int    value) PRINT1(ValueTypeInt,    asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float  value) PRINT1(ValueTypeFloat,  asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint2  value) PRINT2(ValueTypeUint2,  value,         0, ShaderDebugNoTag)
void ShaderDebugPrint(int2   value) PRINT2(ValueTypeInt2,   asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float2 value) PRINT2(ValueTypeFloat2, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint3  value) PRINT3(ValueTypeUint3,  value,         0, ShaderDebugNoTag)
void ShaderDebugPrint(int3   value) PRINT3(ValueTypeInt3,   asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float3 value) PRINT3(ValueTypeFloat3, asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(uint4  value) PRINT4(ValueTypeUint4,  value,         0, ShaderDebugNoTag)
void ShaderDebugPrint(int4   value) PRINT4(ValueTypeInt4,   asuint(value), 0, ShaderDebugNoTag)
void ShaderDebugPrint(float4 value) PRINT4(ValueTypeFloat4, asuint(value), 0, ShaderDebugNoTag)

#undef PRINT1
#undef PRINT2
#undef PRINT3
#undef PRINT4

#define PRINT_MOUSE(VALUE)                        \
{                                                 \
    if(all(pixelPos == ShaderDebugMouseCoords())) \
        ShaderDebugPrint(VALUE);                  \
}

#define PRINT_MOUSE_WITH_TAG(VALUE, TAG)          \
{                                                 \
    if(all(pixelPos == ShaderDebugMouseCoords())) \
        ShaderDebugPrint(TAG, VALUE);             \
}

void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, bool   value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, uint   value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, int    value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, float  value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, uint2  value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, int2   value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, float2 value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, uint3  value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, int3   value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, float3 value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, uint4  value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, int4   value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, uint tag, float4 value) PRINT_MOUSE_WITH_TAG(value, tag);
void ShaderDebugPrintMouseOver(int2 pixelPos, bool   value) PRINT_MOUSE(value);
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
#undef PRINT_MOUSE_WITH_TAG

#endif // SHADER_DEBUG_PRINT_INCLUDED
