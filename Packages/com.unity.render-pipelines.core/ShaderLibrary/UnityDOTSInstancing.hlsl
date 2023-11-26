#ifndef UNITY_DOTS_INSTANCING_INCLUDED
#define UNITY_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

#if UNITY_OLD_PREPROCESSOR
#error DOTS Instancing requires the new shader preprocessor. Please enable Caching Preprocessor in the Editor settings!
#endif

// Config defines
// ==========================================================================================
// #define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT





/*
Here's a bit of python code to generate these repetitive typespecs without
a lot of C macro magic

def print_dots_instancing_typespecs(elem_type, id_char, elem_size):
    print(f"#define UNITY_DOTS_INSTANCING_TYPESPEC_{elem_type} {id_char}{elem_size}")
    for y in range(1, 5):
        for x in range(1, 5):
            rows = "" if y == 1 else f"x{y}"
            size = elem_size * x * y
            print(f"#define UNITY_DOTS_INSTANCING_TYPESPEC_{elem_type}{x}{rows} {id_char}{size}")

for t, c, sz in (
        ('float', 'F', 4),
        ('int',   'I', 4),
        ('uint',  'U', 4),
        ('half',  'H', 2)
        ):
    print_dots_instancing_typespecs(t, c, sz)
*/

#define UNITY_DOTS_INSTANCING_TYPESPEC_float F4
#define UNITY_DOTS_INSTANCING_TYPESPEC_float1 F4
#define UNITY_DOTS_INSTANCING_TYPESPEC_float2 F8
#define UNITY_DOTS_INSTANCING_TYPESPEC_float3 F12
#define UNITY_DOTS_INSTANCING_TYPESPEC_float4 F16
#define UNITY_DOTS_INSTANCING_TYPESPEC_float1x2 F8
#define UNITY_DOTS_INSTANCING_TYPESPEC_float2x2 F16
#define UNITY_DOTS_INSTANCING_TYPESPEC_float3x2 F24
#define UNITY_DOTS_INSTANCING_TYPESPEC_float4x2 F32
#define UNITY_DOTS_INSTANCING_TYPESPEC_float1x3 F12
#define UNITY_DOTS_INSTANCING_TYPESPEC_float2x3 F24
#define UNITY_DOTS_INSTANCING_TYPESPEC_float3x3 F36
#define UNITY_DOTS_INSTANCING_TYPESPEC_float4x3 F48
#define UNITY_DOTS_INSTANCING_TYPESPEC_float1x4 F16
#define UNITY_DOTS_INSTANCING_TYPESPEC_float2x4 F32
#define UNITY_DOTS_INSTANCING_TYPESPEC_float3x4 F48
#define UNITY_DOTS_INSTANCING_TYPESPEC_float4x4 F64
#define UNITY_DOTS_INSTANCING_TYPESPEC_int I4
#define UNITY_DOTS_INSTANCING_TYPESPEC_int1 I4
#define UNITY_DOTS_INSTANCING_TYPESPEC_int2 I8
#define UNITY_DOTS_INSTANCING_TYPESPEC_int3 I12
#define UNITY_DOTS_INSTANCING_TYPESPEC_int4 I16
#define UNITY_DOTS_INSTANCING_TYPESPEC_int1x2 I8
#define UNITY_DOTS_INSTANCING_TYPESPEC_int2x2 I16
#define UNITY_DOTS_INSTANCING_TYPESPEC_int3x2 I24
#define UNITY_DOTS_INSTANCING_TYPESPEC_int4x2 I32
#define UNITY_DOTS_INSTANCING_TYPESPEC_int1x3 I12
#define UNITY_DOTS_INSTANCING_TYPESPEC_int2x3 I24
#define UNITY_DOTS_INSTANCING_TYPESPEC_int3x3 I36
#define UNITY_DOTS_INSTANCING_TYPESPEC_int4x3 I48
#define UNITY_DOTS_INSTANCING_TYPESPEC_int1x4 I16
#define UNITY_DOTS_INSTANCING_TYPESPEC_int2x4 I32
#define UNITY_DOTS_INSTANCING_TYPESPEC_int3x4 I48
#define UNITY_DOTS_INSTANCING_TYPESPEC_int4x4 I64
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint U4
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint1 U4
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint2 U8
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint3 U12
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint4 U16
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint1x2 U8
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint2x2 U16
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint3x2 U24
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint4x2 U32
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint1x3 U12
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint2x3 U24
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint3x3 U36
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint4x3 U48
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint1x4 U16
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint2x4 U32
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint3x4 U48
#define UNITY_DOTS_INSTANCING_TYPESPEC_uint4x4 U64
#define UNITY_DOTS_INSTANCING_TYPESPEC_half H2
#define UNITY_DOTS_INSTANCING_TYPESPEC_half1 H2
#define UNITY_DOTS_INSTANCING_TYPESPEC_half2 H4
#define UNITY_DOTS_INSTANCING_TYPESPEC_half3 H6
#define UNITY_DOTS_INSTANCING_TYPESPEC_half4 H8
#define UNITY_DOTS_INSTANCING_TYPESPEC_half1x2 H4
#define UNITY_DOTS_INSTANCING_TYPESPEC_half2x2 H8
#define UNITY_DOTS_INSTANCING_TYPESPEC_half3x2 H12
#define UNITY_DOTS_INSTANCING_TYPESPEC_half4x2 H16
#define UNITY_DOTS_INSTANCING_TYPESPEC_half1x3 H6
#define UNITY_DOTS_INSTANCING_TYPESPEC_half2x3 H12
#define UNITY_DOTS_INSTANCING_TYPESPEC_half3x3 H18
#define UNITY_DOTS_INSTANCING_TYPESPEC_half4x3 H24
#define UNITY_DOTS_INSTANCING_TYPESPEC_half1x4 H8
#define UNITY_DOTS_INSTANCING_TYPESPEC_half2x4 H16
#define UNITY_DOTS_INSTANCING_TYPESPEC_half3x4 H24
#define UNITY_DOTS_INSTANCING_TYPESPEC_half4x4 H32
#define UNITY_DOTS_INSTANCING_TYPESPEC_min16float H2
#define UNITY_DOTS_INSTANCING_TYPESPEC_min16float4 H8
#define UNITY_DOTS_INSTANCING_TYPESPEC_SH F128

static const int kDotsInstancedPropOverrideDisabled = 0;
static const int kDotsInstancedPropOverrideSupported = 1;
static const int kDotsInstancedPropOverrideRequired = 2;

#define UNITY_DOTS_INSTANCING_CONCAT2(a, b) a ## b
#define UNITY_DOTS_INSTANCING_CONCAT4(a, b, c, d) a ## b ## c ## d
#define UNITY_DOTS_INSTANCING_CONCAT_WITH_METADATA(metadata_prefix, typespec, name) UNITY_DOTS_INSTANCING_CONCAT4(metadata_prefix, typespec, _Metadata, name)

// Metadata constants for properties have the following name format:
// unity_DOTSInstancing<Type><Size>_Metadata<Name>
// where
// <Type> is a single character element type specifier (e.g. F for float4x4)
//          F = float, I = int, U = uint, H = half
// <Size> is the total size of the property in bytes (e.g. 64 for float4x4)
// <Name> is the name of the property
// NOTE: There is no underscore between 'Metadata' and <Name> to avoid a double
//       underscore in the common case where the property name starts with an underscore.
//       A prefix double underscore is illegal on some platforms like OpenGL.
#define UNITY_DOTS_INSTANCED_METADATA_NAME(type, name) UNITY_DOTS_INSTANCING_CONCAT_WITH_METADATA(unity_DOTSInstancing, UNITY_DOTS_INSTANCING_CONCAT2(UNITY_DOTS_INSTANCING_TYPESPEC_, type), name)
#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) UNITY_DOTS_INSTANCING_CONCAT2(name, _DOTSInstancingOverrideMode)

#define UNITY_DOTS_INSTANCING_START(name) cbuffer UnityDOTSInstancing_##name {
#define UNITY_DOTS_INSTANCING_END(name)   }

#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED(type, name) static const uint UNITY_DOTS_INSTANCED_METADATA_NAME(type, name) = 0; \
static const int UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) = kDotsInstancedPropOverrideDisabled;

#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(type, name) uint UNITY_DOTS_INSTANCED_METADATA_NAME(type, name); \
static const int UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) = kDotsInstancedPropOverrideSupported;

#define UNITY_DOTS_INSTANCED_PROP_OVERRIDE_REQUIRED(type, name) uint UNITY_DOTS_INSTANCED_METADATA_NAME(type, name); \
static const int UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) = kDotsInstancedPropOverrideRequired;

#ifdef UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED_BY_DEFAULT
#define UNITY_DOTS_INSTANCED_PROP(type, name) UNITY_DOTS_INSTANCED_PROP_OVERRIDE_DISABLED(type, name)
#else
#define UNITY_DOTS_INSTANCED_PROP(type, name) UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(type, name)
#endif

#define UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_DISABLED(name) (UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) == kDotsInstancedPropOverrideDisabled)
#define UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_ENABLED(name) (UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) == kDotsInstancedPropOverrideSupported)
#define UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_REQUIRED(name) (UNITY_DOTS_INSTANCED_PROP_OVERRIDE_MODE_NAME(name) == kDotsInstancedPropOverrideRequired)

#define UNITY_ACCESS_DOTS_INSTANCED_PROP(type, var) ( /* Compile-time branches */ \
UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_ENABLED(var) ? LoadDOTSInstancedData_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_REQUIRED(var) ? LoadDOTSInstancedDataOverridden_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: ((type)0) \
)

#define UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var) ( /* Compile-time branches */ \
UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_ENABLED(var) ? LoadDOTSInstancedData_##type(var, UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_REQUIRED(var) ? LoadDOTSInstancedDataOverridden_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: (var) \
)

#define UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(type, var, default_value) ( /* Compile-time branches */ \
UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_ENABLED(var) ? LoadDOTSInstancedData_##type(default_value, UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: UNITY_DOTS_INSTANCED_PROP_IS_OVERRIDE_REQUIRED(var) ? LoadDOTSInstancedDataOverridden_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var)) \
: (default_value) \
)

#define UNITY_ACCESS_DOTS_AND_TRADITIONAL_INSTANCED_PROP(type, arr, var) UNITY_ACCESS_DOTS_INSTANCED_PROP(type, var)
#define UNITY_ACCESS_DOTS_AND_TRADITIONAL_INSTANCED_PROP_WITH_DEFAULT(type, arr, var) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
#define UNITY_ACCESS_DOTS_AND_TRADITIONAL_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(type, arr, var, default_value) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(type, var, default_value)

#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() // No-op by default

#ifdef UNITY_DOTS_INSTANCING_UNIFORM_BUFFER
CBUFFER_START(unity_DOTSInstanceData)
    float4 unity_DOTSInstanceDataRaw[1024];	// warning: if you change 1024 value, you should also change BatchRendererGroup::GetConstantBufferMaxWindowSize() function in the c++ code base
CBUFFER_END
#else
ByteAddressBuffer unity_DOTSInstanceData;
#endif

// DOTS instanced shaders do not get globals from UnityPerDraw automatically.
// Instead, the BatchRendererGroup user must provide this cbuffer and/or
// set up DOTS instanced properties for the values.
// NOTE: Do *NOT* use the string "Globals" in this cbuffer name, cbuffers
// with those kinds of names will be automatically renamed.
CBUFFER_START(unity_DOTSInstanceGlobalValues)
    float4 unity_DOTS_ProbesOcclusion;
    float4 unity_DOTS_SpecCube0_HDR;
    float4 unity_DOTS_SpecCube1_HDR;
    float4 unity_DOTS_SHAr;
    float4 unity_DOTS_SHAg;
    float4 unity_DOTS_SHAb;
    float4 unity_DOTS_SHBr;
    float4 unity_DOTS_SHBg;
    float4 unity_DOTS_SHBb;
    float4 unity_DOTS_SHC;
CBUFFER_END

// The data has to be wrapped inside a struct, otherwise the instancing code path
// on some platforms does not trigger.
struct DOTSVisibleData
{
    uint4 VisibleData;
};

// The name of this cbuffer has to start with "UnityInstancing" and a struct so it's
// detected as an "instancing cbuffer" by some platforms that use string matching
// to detect this.
CBUFFER_START(UnityInstancingDOTS_InstanceVisibility)
    DOTSVisibleData unity_DOTSVisibleInstances[256];	// warning: if you change 256 value you should also change kBRGVisibilityGLESMaxElementCount in c++ code base
CBUFFER_END

// Keep these in sync with SRP Batcher DOTSInstancingFlags
static const uint kDOTSInstancingFlagFlipWinding      = (1 << 0); // Flip triangle winding when rendering, e.g. when the scale is negative
static const uint kDOTSInstancingFlagForceZeroMotion  = (1 << 1); // Object should produce zero motion vectors when rendered in the motion pass
static const uint kDOTSInstancingFlagCameraMotion     = (1 << 2); // Object uses Camera motion (i.e. not per-Object motion)
static const uint kDOTSInstancingFlagHasPrevPosition  = (1 << 3); // Object has a separate previous frame position vertex streams (e.g. for deformed objects)
static const uint kDOTSInstancingFlagMainLightEnabled = (1 << 4); // Object should receive direct lighting from the main light (e.g. light not baked into lightmap)

static const uint kPerInstanceDataBit = 0x80000000;
static const uint kAddressMask        = 0x7fffffff;

static DOTSVisibleData unity_SampledDOTSVisibleData;
static real4 unity_DOTS_Sampled_SHAr;
static real4 unity_DOTS_Sampled_SHAg;
static real4 unity_DOTS_Sampled_SHAb;
static real4 unity_DOTS_Sampled_SHBr;
static real4 unity_DOTS_Sampled_SHBg;
static real4 unity_DOTS_Sampled_SHBb;
static real4 unity_DOTS_Sampled_SHC;
static real4 unity_DOTS_Sampled_ProbesOcclusion;

uint GetDOTSInstanceIndex()
{
    return unity_SampledDOTSVisibleData.VisibleData.x;
}

#ifdef UNITY_DOTS_INSTANCING_UNIFORM_BUFFER
// In UBO mode we precompute our select masks based on our instance index.
// All base addresses are aligned by 16, so we already know which offsets
// the instance index will load (modulo 16).
// All float1 loads will share the select4 masks, and all float2 loads
// will share the select2 mask.
// These variables are single assignment only, and should hopefully be well
// optimizable and dead code eliminatable for the compiler.
static uint unity_DOTSInstanceData_Select4_Mask0;
static uint unity_DOTSInstanceData_Select4_Mask1;
static uint unity_DOTSInstanceData_Select2_Mask;

// The compiler should dead code eliminate the parts of this that are not used by the shader.
void SetupDOTSInstanceSelectMasks()
{
    uint instanceIndex = GetDOTSInstanceIndex();
    uint offsetSingleChannel = instanceIndex << 2; // float: stride 4 bytes

    // x = 0 = 00
    // y = 1 = 01
    // z = 2 = 10
    // w = 3 = 11
    // Lowest 2 bits are zero, all accesses are aligned,
    // and base addresses are aligned by 16.
    // Bits 29 and 28 give the channel index.
    // NOTE: Mask generation was rewritten to this form specifically to avoid codegen
    // correctness issues on GLES.
    unity_DOTSInstanceData_Select4_Mask0 = (offsetSingleChannel & 0x4) ? 0xffffffff : 0;
    unity_DOTSInstanceData_Select4_Mask1 = (offsetSingleChannel & 0x8) ? 0xffffffff : 0;
    // Select2 mask is the same as the low bit mask of select4, since
    // (x << 3) << 28 == (x << 2) << 29
    unity_DOTSInstanceData_Select2_Mask = unity_DOTSInstanceData_Select4_Mask0;
}

#else

// This is a no-op in SSBO mode
void SetupDOTSInstanceSelectMasks() {}

#endif

void SetDOTSVisibleData(DOTSVisibleData visibleData)
{
    unity_SampledDOTSVisibleData = visibleData;
    SetupDOTSInstanceSelectMasks();
}

void SetupDOTSVisibleInstancingData()
{
    SetDOTSVisibleData(unity_DOTSVisibleInstances[unity_InstanceID]);
}

int GetDOTSInstanceCrossfadeSnorm8()
{
    return unity_SampledDOTSVisibleData.VisibleData.y;
}

bool IsDOTSInstancedProperty(uint metadata)
{
    return (metadata & kPerInstanceDataBit) != 0;
}

// Stride is typically expected to be a compile-time literal here, so this should
// be optimized into shifts and other cheap ALU ops by the compiler.
uint ComputeDOTSInstanceOffset(uint instanceIndex, uint stride)
{
    return instanceIndex * stride;
}

uint ComputeDOTSInstanceDataAddress(uint metadata, uint stride)
{
    uint isOverridden = metadata & kPerInstanceDataBit;
    // Sign extend per-instance data bit so it can just be ANDed with the offset
    uint offsetMask  = (uint)((int)isOverridden >> 31);
    uint baseAddress = metadata & kAddressMask;
    uint offset      = ComputeDOTSInstanceOffset(GetDOTSInstanceIndex(), stride);
    offset          &= offsetMask;
    return baseAddress + offset;
}

// This version assumes that the high bit of the metadata is set (= per instance data).
// Useful if the call site has already branched over this.
uint ComputeDOTSInstanceDataAddressOverridden(uint metadata, uint stride)
{
    uint baseAddress = metadata & kAddressMask;
    uint offset      = ComputeDOTSInstanceOffset(GetDOTSInstanceIndex(), stride);
    return baseAddress + offset;
}

#ifdef UNITY_DOTS_INSTANCING_UNIFORM_BUFFER
uint DOTSInstanceData_Select(uint addressOrOffset, uint4 v)
{
    uint mask0 = unity_DOTSInstanceData_Select4_Mask0;
    uint mask1 = unity_DOTSInstanceData_Select4_Mask1;
    return
        (((v.w & mask0) | (v.z & ~mask0)) & mask1) |
        (((v.y & mask0) | (v.x & ~mask0)) & ~mask1);
}

uint2 DOTSInstanceData_Select2(uint addressOrOffset, uint4 v)
{
    uint mask0 = unity_DOTSInstanceData_Select2_Mask;
    return (v.zw & mask0) | (v.xy & ~mask0);
}

uint DOTSInstanceData_Load(uint address)
{
    uint float4Index = address >> 4;
    uint4 raw = asuint(unity_DOTSInstanceDataRaw[float4Index]);
    return DOTSInstanceData_Select(address, raw);
}
uint2 DOTSInstanceData_Load2(uint address)
{
    uint float4Index = address >> 4;
    uint4 raw = asuint(unity_DOTSInstanceDataRaw[float4Index]);
    return DOTSInstanceData_Select2(address, raw);
}
uint4 DOTSInstanceData_Load4(uint address)
{
    uint float4Index = address >> 4;
    return asuint(unity_DOTSInstanceDataRaw[float4Index]);
}
uint3 DOTSInstanceData_Load3(uint address)
{
    // This is likely to be slow, tightly packed float3s are tricky
    switch (address & 0xf)
    {
    default:
    case 0:
        return DOTSInstanceData_Load4(address).xyz;
    case 4:
        return DOTSInstanceData_Load4(address).yzw;
    case 8:
        {
            uint float4Index = address >> 4;
            uint4 raw0 = asuint(unity_DOTSInstanceDataRaw[float4Index]);
            uint4 raw1 = asuint(unity_DOTSInstanceDataRaw[float4Index + 1]);
            uint3 v;
            v.xy = raw0.zw;
            v.z  = raw1.x;
            return v;
        }
    case 12:
        {
            uint float4Index = address >> 4;
            uint4 raw0 = asuint(unity_DOTSInstanceDataRaw[float4Index]);
            uint4 raw1 = asuint(unity_DOTSInstanceDataRaw[float4Index + 1]);
            uint3 v;
            v.x  = raw0.w;
            v.yz = raw1.xy;
            return v;
        }
    }
}
#else
uint DOTSInstanceData_Load(uint address)
{
    return unity_DOTSInstanceData.Load(address);
}
uint2 DOTSInstanceData_Load2(uint address)
{
    return unity_DOTSInstanceData.Load2(address);
}
uint3 DOTSInstanceData_Load3(uint address)
{
    return unity_DOTSInstanceData.Load3(address);
}
uint4 DOTSInstanceData_Load4(uint address)
{
    return unity_DOTSInstanceData.Load4(address);
}
#endif

#define DEFINE_DOTS_LOAD_INSTANCE_SCALAR(type, conv, sizeof_type) \
type LoadDOTSInstancedData_##type(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, sizeof_type); \
    return conv(DOTSInstanceData_Load(address)); \
} \
type LoadDOTSInstancedDataOverridden_##type(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, sizeof_type); \
    return conv(DOTSInstanceData_Load(address)); \
} \
type LoadDOTSInstancedData_##type(type default_value, uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, sizeof_type); \
    return IsDOTSInstancedProperty(metadata) ? \
        conv(DOTSInstanceData_Load(address)) : default_value; \
}

#define DEFINE_DOTS_LOAD_INSTANCE_VECTOR(type, width, conv, sizeof_type) \
type##width LoadDOTSInstancedData_##type##width(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, sizeof_type * width); \
    return conv(DOTSInstanceData_Load##width(address)); \
} \
type##width LoadDOTSInstancedDataOverridden_##type##width(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, sizeof_type * width); \
    return conv(DOTSInstanceData_Load##width(address)); \
} \
type##width LoadDOTSInstancedData_##type##width(type##width default_value, uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, sizeof_type * width); \
    return IsDOTSInstancedProperty(metadata) ? \
        conv(DOTSInstanceData_Load##width(address)) : default_value; \
}

DEFINE_DOTS_LOAD_INSTANCE_SCALAR(float, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(int,   int,     4)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(uint,  uint,    4)
//DEFINE_DOTS_LOAD_INSTANCE_SCALAR(half,  half,    2)

DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 2, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 3, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 4, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   2, int2,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   3, int3,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   4, int4,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  2, uint2,   4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  3, uint3,   4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  4, uint4,   4)
//DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  2, half2,   2)
//DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  3, half3,   2)
//DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  4, half4,   2)

half LoadDOTSInstancedData_half(uint metadata)
{
    float f = LoadDOTSInstancedData_float(metadata);
    min16float f16 = min16float(f);
    return f16;
}
half LoadDOTSInstancedDataOverridden_half(uint metadata)
{
    float f = LoadDOTSInstancedDataOverridden_float(metadata);
    min16float f16 = min16float(f);
    return f16;
}

half4 LoadDOTSInstancedData_half4(uint metadata)
{
    float4 f = LoadDOTSInstancedData_float4(metadata);
    min16float4 f16x4 = min16float4(f.x, f.y, f.z, f.w);
    return f16x4;
}
half4 LoadDOTSInstancedDataOverridden_half4(uint metadata)
{
    float4 f = LoadDOTSInstancedDataOverridden_float4(metadata);
    min16float4 f16x4 = min16float4(f.x, f.y, f.z, f.w);
    return f16x4;
}

min16float LoadDOTSInstancedData_min16float(uint metadata)
{
    return min16float(LoadDOTSInstancedData_half(metadata));
}
min16float LoadDOTSInstancedDataOverridden_min16float(uint metadata)
{
    return min16float(LoadDOTSInstancedDataOverridden_half(metadata));
}

min16float4 LoadDOTSInstancedData_min16float4(uint metadata)
{
    return min16float4(LoadDOTSInstancedData_half4(metadata));
}
min16float4 LoadDOTSInstancedDataOverridden_min16float4(uint metadata)
{
    return min16float4(LoadDOTSInstancedDataOverridden_half4(metadata));
}

min16float LoadDOTSInstancedData_min16float(min16float default_value, uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_min16float(metadata) : default_value;
}

min16float4 LoadDOTSInstancedData_min16float4(min16float4 default_value, uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_min16float4(metadata) : default_value;
}

// TODO: Other matrix sizes
float4x4 LoadDOTSInstancedData_float4x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4 * 16);
    float4 p1 = asfloat(DOTSInstanceData_Load4(address + 0 * 16));
    float4 p2 = asfloat(DOTSInstanceData_Load4(address + 1 * 16));
    float4 p3 = asfloat(DOTSInstanceData_Load4(address + 2 * 16));
    float4 p4 = asfloat(DOTSInstanceData_Load4(address + 3 * 16));
    return float4x4(
        p1.x, p2.x, p3.x, p4.x,
        p1.y, p2.y, p3.y, p4.y,
        p1.z, p2.z, p3.z, p4.z,
        p1.w, p2.w, p3.w, p4.w);
}
float4x4 LoadDOTSInstancedDataOverridden_float4x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, 4 * 16);
    float4 p1 = asfloat(DOTSInstanceData_Load4(address + 0 * 16));
    float4 p2 = asfloat(DOTSInstanceData_Load4(address + 1 * 16));
    float4 p3 = asfloat(DOTSInstanceData_Load4(address + 2 * 16));
    float4 p4 = asfloat(DOTSInstanceData_Load4(address + 3 * 16));
    return float4x4(
        p1.x, p2.x, p3.x, p4.x,
        p1.y, p2.y, p3.y, p4.y,
        p1.z, p2.z, p3.z, p4.z,
        p1.w, p2.w, p3.w, p4.w);
}

float4x4 LoadDOTSInstancedData_float4x4_from_float3x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 3 * 16);
    float4 p1 = asfloat(DOTSInstanceData_Load4(address + 0 * 16));
    float4 p2 = asfloat(DOTSInstanceData_Load4(address + 1 * 16));
    float4 p3 = asfloat(DOTSInstanceData_Load4(address + 2 * 16));

    return float4x4(
        p1.x, p1.w, p2.z, p3.y,
        p1.y, p2.x, p2.w, p3.z,
        p1.z, p2.y, p3.x, p3.w,
        0.0,  0.0,  0.0,  1.0
    );
}
float4x4 LoadDOTSInstancedDataOverridden_float4x4_from_float3x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, 3 * 16);
    float4 p1 = asfloat(DOTSInstanceData_Load4(address + 0 * 16));
    float4 p2 = asfloat(DOTSInstanceData_Load4(address + 1 * 16));
    float4 p3 = asfloat(DOTSInstanceData_Load4(address + 2 * 16));

    return float4x4(
        p1.x, p1.w, p2.z, p3.y,
        p1.y, p2.x, p2.w, p3.z,
        p1.z, p2.y, p3.x, p3.w,
        0.0,  0.0,  0.0,  1.0
    );
}

float2x4 LoadDOTSInstancedData_float2x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4 * 8);
    return float2x4(
        asfloat(DOTSInstanceData_Load4(address + 0 * 8)),
        asfloat(DOTSInstanceData_Load4(address + 1 * 8)));
}
float2x4 LoadDOTSInstancedDataOverridden_float2x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddressOverridden(metadata, 4 * 8);
    return float2x4(
        asfloat(DOTSInstanceData_Load4(address + 0 * 8)),
        asfloat(DOTSInstanceData_Load4(address + 1 * 8)));
}

float4x4 LoadDOTSInstancedData_float4x4(float4x4 default_value, uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_float4x4(metadata) : default_value;
}

float4x4 LoadDOTSInstancedData_float4x4_from_float3x4(float4x4 default_value, uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_float4x4_from_float3x4(metadata) : default_value;
}

float2x4 LoadDOTSInstancedData_float2x4(float4 default_value[2], uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_float2x4(metadata) : float2x4(default_value[0], default_value[1]);
}

float2x4 LoadDOTSInstancedData_float2x4(float2x4 default_value, uint metadata)
{
    return IsDOTSInstancedProperty(metadata) ?
        LoadDOTSInstancedData_float2x4(metadata) : default_value;
}

float4 LoadDOTSInstancedData_RenderingLayer()
{
    return float4(asfloat(unity_DOTSVisibleInstances[0].VisibleData.z), 0,0,0);
}

float3 LoadDOTSInstancedData_MeshLocalBoundCenter()
{
    return float3(asfloat(unity_DOTSVisibleInstances[1].VisibleData.z), asfloat(unity_DOTSVisibleInstances[1].VisibleData.w), asfloat(unity_DOTSVisibleInstances[2].VisibleData.z));
}

float3 LoadDOTSInstancedData_MeshLocalBoundExtent()
{
    return float3(asfloat(unity_DOTSVisibleInstances[2].VisibleData.w), asfloat(unity_DOTSVisibleInstances[3].VisibleData.z), asfloat(unity_DOTSVisibleInstances[3].VisibleData.w));
}

float4 LoadDOTSInstancedData_MotionVectorsParams()
{
    uint flags = unity_DOTSVisibleInstances[0].VisibleData.w;
    return float4(0, flags & kDOTSInstancingFlagForceZeroMotion ? 0.0f : 1.0f, -0.001f, flags & kDOTSInstancingFlagCameraMotion ? 0.0f : 1.0f);
}

float4 LoadDOTSInstancedData_WorldTransformParams()
{
    uint flags = unity_DOTSVisibleInstances[0].VisibleData.w;
    return float4(0, 0, 0, flags & kDOTSInstancingFlagFlipWinding ? -1.0f : 1.0f);
}

float4 LoadDOTSInstancedData_LightData()
{
    uint flags = unity_DOTSVisibleInstances[0].VisibleData.w;
    // X channel = light start index (not supported in DOTS instancing)
    // Y channel = light count (not supported in DOTS instancing)
    // Z channel = main light strength
    return float4(0, 0, flags & kDOTSInstancingFlagMainLightEnabled ? 1.0f : 0.0f, 0);
}

float4 LoadDOTSInstancedData_LODFade()
{
    int crossfadeSNorm8 = GetDOTSInstanceCrossfadeSnorm8();
    float crossfade = clamp((float)crossfadeSNorm8, -127, 127);
    crossfade *= 1.0 / 127;
    return crossfade;
}

void SetupDOTSSHCoeffs(uint shMetadata)
{
    if (IsDOTSInstancedProperty(shMetadata))
    {
        uint address = ComputeDOTSInstanceDataAddressOverridden(shMetadata, 8 * 16);
        unity_DOTS_Sampled_SHAr = real4(asfloat(DOTSInstanceData_Load4(address + 0 * 16)));
        unity_DOTS_Sampled_SHAg = real4(asfloat(DOTSInstanceData_Load4(address + 1 * 16)));
        unity_DOTS_Sampled_SHAb = real4(asfloat(DOTSInstanceData_Load4(address + 2 * 16)));
        unity_DOTS_Sampled_SHBr = real4(asfloat(DOTSInstanceData_Load4(address + 3 * 16)));
        unity_DOTS_Sampled_SHBg = real4(asfloat(DOTSInstanceData_Load4(address + 4 * 16)));
        unity_DOTS_Sampled_SHBb = real4(asfloat(DOTSInstanceData_Load4(address + 5 * 16)));
        unity_DOTS_Sampled_SHC  = real4(asfloat(DOTSInstanceData_Load4(address + 6 * 16)));
        unity_DOTS_Sampled_ProbesOcclusion = real4(asfloat(DOTSInstanceData_Load4(address + 7 * 16)));
    }
    else
    {
        unity_DOTS_Sampled_SHAr = real4(unity_DOTS_SHAr);
        unity_DOTS_Sampled_SHAg = real4(unity_DOTS_SHAg);
        unity_DOTS_Sampled_SHAb = real4(unity_DOTS_SHAb);
        unity_DOTS_Sampled_SHBr = real4(unity_DOTS_SHBr);
        unity_DOTS_Sampled_SHBg = real4(unity_DOTS_SHBg);
        unity_DOTS_Sampled_SHBb = real4(unity_DOTS_SHBb);
        unity_DOTS_Sampled_SHC  = real4(unity_DOTS_SHC);
        unity_DOTS_Sampled_ProbesOcclusion = real4(unity_DOTS_ProbesOcclusion);
    }
}

real4 LoadDOTSInstancedData_SHAr() { return unity_DOTS_Sampled_SHAr; }
real4 LoadDOTSInstancedData_SHAg() { return unity_DOTS_Sampled_SHAg; }
real4 LoadDOTSInstancedData_SHAb() { return unity_DOTS_Sampled_SHAb; }
real4 LoadDOTSInstancedData_SHBr() { return unity_DOTS_Sampled_SHBr; }
real4 LoadDOTSInstancedData_SHBg() { return unity_DOTS_Sampled_SHBg; }
real4 LoadDOTSInstancedData_SHBb() { return unity_DOTS_Sampled_SHBb; }
real4 LoadDOTSInstancedData_SHC()  { return unity_DOTS_Sampled_SHC; }
real4 LoadDOTSInstancedData_ProbesOcclusion()  { return unity_DOTS_Sampled_ProbesOcclusion; }

float4 LoadDOTSInstancedData_SelectionValue(uint metadata, uint submeshIndex, float4 globalSelectionID)
{
    // If there is a DOTS instanced per-instance ID, get that.
    if (IsDOTSInstancedProperty(metadata))
    {
        // Add 1 to the EntityID, so the EntityID 0 gets a value that is not equal to the clear value.
        uint selectionID = LoadDOTSInstancedData_uint2(metadata).x;
        uint idValue = selectionID + 1;

        // 26 bits for the entity index.
        // 5 bits for the submesh index.
        // 1 bit which must be set when outputting an EntityID/SubmeshIndex bitpack to let Unity know that it is not a regular selection ID.
        // When the high-bit is set, Unity will internally interpret the data as a 26-5-1 encoded bitmask and extract the EntityIndex/SubmeshIndex accordingly.

        // Encode entity index with 26 bits. idValue & ((1 << 26) - 1) == idValue % (1 << 26)
        uint idValueBits = idValue & ((1 << 26) - 1);

        // Encode submesh index with 5 bits. submeshIndex & ((1 << 5) - 1) == submeshIndex % (1 << 5)
        uint submeshBits = submeshIndex & ((1 << 5) - 1);
        // Shift to high-bits. The 26 first bits are used by the entity index.
        submeshBits <<= 26;

        uint pickingID = (1 << 31) | submeshBits | idValueBits;

        // Pack a 32-bit integer into four 8-bit color channels such that the integer can be exactly reconstructed afterwards.
        return float4(uint4(pickingID >> 0, pickingID >> 8, pickingID >> 16, pickingID >> 24) & 0xFF) / 255.0f;
    }
    else
    {
        return globalSelectionID;
    }
}
#define UNITY_ACCESS_DOTS_INSTANCED_SELECTION_VALUE(name, submesh, selectionID) \
    LoadDOTSInstancedData_SelectionValue(UNITY_DOTS_INSTANCED_METADATA_NAME(uint2, name), submesh, selectionID)

#undef DEFINE_DOTS_LOAD_INSTANCE_SCALAR
#undef DEFINE_DOTS_LOAD_INSTANCE_VECTOR

#endif // UNITY_DOTS_INSTANCING_ENABLED

#endif // UNITY_DOTS_INSTANCING_INCLUDED
