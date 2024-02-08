#ifndef SHADER_DEBUG_PRINT_INCLUDED
#define SHADER_DEBUG_PRINT_INCLUDED

// NOTE: For URP - set ENABLE_SHADER_DEBUG_PRINT in the project to enable CPU-side integration.
// NOTE: Currently works in game view/play mode.
//
// Include this header to any shader to enable debug printing values from shader code to console.
//
// Select threads/pixels to print using plain 'if'.
//
// Example:
// float4 colorRGBA = float4(0.1, 0.2, 0.3, 0.4);
// if(all(int2(pixel.xy) == int2(100, 100)))
//     ShaderDebugPrint(ShaderDebugTag('C','o','l'), colorRGBA);
// ----
// Output:
// Frame #270497: Col  float4(0.1f, 0.2f, 0.3f, 0.4f)
// ----
//
// Example:
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ShaderDebugPrint.hlsl"
//
// Print pixel at mouse position.
// ShaderDebugPrintMouseOver(int2(thisPixel.xy), pixelColor);
//
// Print pixel at mouse position on button press.
// ShaderDebugPrintMouseButtonOver(int2(thisPixel.xy), pixelColor);

// Output buffer bound with cmd.SetGlobalTexture().
RWStructuredBuffer<uint> shaderDebugOutputData;

static const uint MaxShaderDebugOutputElements = 1024 * 16; // Must match the C# side buffer size (16K elems / 6 (header+tag+payload) ~= 2730 uint4s)

// Input Constants
CBUFFER_START(ShaderDebugPrintInput)
float4 _ShaderDebugPrintInputMouse;
int    _ShaderDebugPrintInputFrame;
CBUFFER_END

// Mouse coordinates in pixels
// Relative to game view surface/rendertarget
// (Typically (0,0) is bottom-left in Unity. TIP: print mouse coords to check if unsure.)
int2 ShaderDebugMouseCoords()           { return _ShaderDebugPrintInputMouse.xy; }

// Mouse buttons
// Returns true on button down.
int  ShaderDebugMouseButtonLeft()       { return _ShaderDebugPrintInputMouse.z;  }
int  ShaderDebugMouseButtonRight()      { return _ShaderDebugPrintInputMouse.w;  }
int  ShaderDebugMouseButton(int button) { return button == 0 ? ShaderDebugMouseButtonLeft() : ShaderDebugMouseButtonRight(); }

int  ShaderDebugFrameNumber()           { return _ShaderDebugPrintInputFrame; }

// Output Data type encodings
// Must match C# side decoding
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

// Data-buffer format
// 1    uint    header
// 1    uint    tag (optional)
// 1-4  uint    value (type dependent)
//
// Header format
// 1    byte        Type id + tag flag
//      bits 0..6   value type id/enum
//      bit  7      has tag flag
// 3    bytes       (empty)
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

// Create 1-4 letter tags encoded into a uint
// For example
// ShaderDebugTag( 'M', 'y', 'I', 'd' );
uint ShaderDebugTag(uint a, uint b, uint c, uint d) { return a | (b << 8) | (c << 16) | (d << 24); }
uint ShaderDebugTag(uint a, uint b, uint c)         { return ShaderDebugTag( a, b, c, 0); }
uint ShaderDebugTag(uint a, uint b)                 { return ShaderDebugTag( a, b, 0); }
uint ShaderDebugTag(uint a)                         { return ShaderDebugTag( a, 0); }

// Print value to (Unity) console
// Be careful to not print all N threads (thousands). Use if statements and thread ids to pick values only from a few threads.
// (tag), an optional text tag for the print. Use ShaderDebugTag() helper to create.
// value, to be printed
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
void ShaderDebugPrint(bool   value) PRINT1(ValueTypeBool,   uint(value),   0, ShaderDebugNoTag)
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

// Print value for pixel under mouse cursor
// pixelPos, screen pixel coordinates for this fragment shader thread. Typically .xy of fragment shader input parameter with SV_Position semantic.
//           NOTE: Any render target scaling (or offset) is NOT taken into account as that can be arbitrary. You must correct scaling manually.
//                 (For example: fragment.xy * _ScreenParams.xy / _ScaledScreenParams.xy or similar)
//           TIP: Color the pixel (or a box) at mouse coords to debug scaling/offset.
// (tag), an optional text tag for the print. Use ShaderDebugTag() helper to create.
// value, to be printed
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

#define PRINT_MOUSE_BUTTON(BUTTON, VALUE)                        \
{                                                 \
    if(ShaderDebugMouseButton(BUTTON) && all(pixelPos == ShaderDebugMouseCoords())) \
        ShaderDebugPrint(VALUE);                  \
}

#define PRINT_MOUSE_BUTTON_WITH_TAG(BUTTON, VALUE, TAG)          \
{                                                 \
    if(ShaderDebugMouseButton(BUTTON) && all(pixelPos == ShaderDebugMouseCoords())) \
        ShaderDebugPrint(TAG, VALUE);             \
}

// Print value for pixel under mouse cursor when mouse left button is pressed
// pixelPos, screen pixel coordinates for this fragment shader thread. Typically .xy of fragment shader input parameter with SV_Position semantic.
// (tag), an optional text tag for the print. Use ShaderDebugTag() helper to create.
// value, to be printed
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, bool   value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, uint   value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, int    value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, float  value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, uint2  value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, int2   value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, float2 value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, uint3  value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, int3   value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, float3 value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, uint4  value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, int4   value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint tag, float4 value) PRINT_MOUSE_BUTTON_WITH_TAG(0, value, tag);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, bool   value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint   value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, int    value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, float  value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint2  value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, int2   value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, float2 value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint3  value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, int3   value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, float3 value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, uint4  value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, int4   value) PRINT_MOUSE_BUTTON(0, value);
void ShaderDebugPrintMouseButtonOver(int2 pixelPos, float4 value) PRINT_MOUSE_BUTTON(0, value);

#undef PRINT_MOUSE_BUTTON
#undef PRINT_MOUSE_BUTTON_WITH_TAG

#endif // SHADER_DEBUG_PRINT_INCLUDED
