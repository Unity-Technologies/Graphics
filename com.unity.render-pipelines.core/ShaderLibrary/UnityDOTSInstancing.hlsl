#ifndef UNITY_DOTS_INSTANCING_INCLUDED
#define UNITY_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

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

#define UNITY_DOTS_INSTANCING_CONCAT2(a, b) a ## b
#define UNITY_DOTS_INSTANCING_CONCAT4(a, b, c, d) a ## b ## c ## d
#define UNITY_DOTS_INSTANCING_CONCAT_WITHOUT_METADATA(metadata_prefix, typespec, metadata_underscore_var) UNITY_DOTS_INSTANCING_CONCAT4(metadata_prefix, typespec, _, metadata_underscore_var)
#define UNITY_DOTS_INSTANCING_CONCAT_WITH_METADATA(metadata_prefix, typespec, name) UNITY_DOTS_INSTANCING_CONCAT4(metadata_prefix, typespec, _Metadata_, name)

// Metadata constants for properties have the following name format:
// unity_DOTSInstancing_<Type><Size>_Metadata_<Name>
// where
// <Type> is a single character element type specifier (e.g. F for float4x4)
//          F = float, I = int, U = uint, H = half
// <Size> is the total size of the property in bytes (e.g. 64 for float4x4)
// <Name> is the name of the property
#define UNITY_DOTS_INSTANCED_METADATA_NAME(type, name) UNITY_DOTS_INSTANCING_CONCAT_WITH_METADATA(unity_DOTSInstancing_, UNITY_DOTS_INSTANCING_CONCAT2(UNITY_DOTS_INSTANCING_TYPESPEC_, type), name)
#define UNITY_DOTS_INSTANCED_METADATA_NAME_FROM_MACRO(type, metadata_underscore_var) UNITY_DOTS_INSTANCING_CONCAT_WITHOUT_METADATA(unity_DOTSInstancing_, UNITY_DOTS_INSTANCING_CONCAT2(UNITY_DOTS_INSTANCING_TYPESPEC_, type), metadata_underscore_var)

#define UNITY_DOTS_INSTANCING_START(name) cbuffer UnityDOTSInstancing_##name {
#define UNITY_DOTS_INSTANCING_END(name)   }
#define UNITY_DOTS_INSTANCED_PROP(type, name) uint UNITY_DOTS_INSTANCED_METADATA_NAME(type, name);

// There is a separate FROM_MACRO variant to be used in macros of the form
// #define <MACRO_NAME> UNITY_DOTS_INSTANCED_METADATA_NAME_FROM_MACRO(float4, Metadata_<MACRO_NAME>)
// These kinds of macros can be used to have shader code load constants from DOTS instancing
// as if they were normal constants.
// The reason for having the FROM_MACRO variant is that the fxc shader preprocessor is buggy,
// and refuses to expand macros correctly if the macro body contains the macro itself.
// The correct behavior would be for the macro name to appear in the expansion as is (i.e. no recursion),
// but fxc's preprocessor completely breaks down in this situation.

#define UNITY_ACCESS_DOTS_INSTANCED_PROP(type, var) LoadDOTSInstancedData_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var))
#define UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(type, metadata_underscore_var) LoadDOTSInstancedData_##type(UNITY_DOTS_INSTANCED_METADATA_NAME_FROM_MACRO(type, metadata_underscore_var))
#define UNITY_ACCESS_DOTS_AND_TRADITIONAL_INSTANCED_PROP(type, arr, var) LoadDOTSInstancedData_##type(UNITY_DOTS_INSTANCED_METADATA_NAME(type, var))

// TODO: Shader feature level to compute only
ByteAddressBuffer unity_DOTSInstanceData;


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
    DOTSVisibleData unity_DOTSVisibleInstances[UNITY_INSTANCED_ARRAY_SIZE];
CBUFFER_END

uint GetDOTSInstanceIndex()
{
    return unity_DOTSVisibleInstances[unity_InstanceID].VisibleData.x;
}

uint ComputeDOTSInstanceDataAddress(uint metadata, uint stride)
{
    uint isOverridden = metadata & 0x80000000;
    uint baseAddress  = metadata & 0x7fffffff;
    uint offset       = isOverridden
        ? (GetDOTSInstanceIndex() * stride)
        : 0;
    return baseAddress + offset;
}

#define DEFINE_DOTS_LOAD_INSTANCE_SCALAR(type, conv, sizeof_type) \
type LoadDOTSInstancedData_##type(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, sizeof_type); \
    return conv(unity_DOTSInstanceData.Load(address)); \
}

#define DEFINE_DOTS_LOAD_INSTANCE_VECTOR(type, width, conv, sizeof_type) \
type##width LoadDOTSInstancedData_##type##width(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, sizeof_type * width); \
    return conv(unity_DOTSInstanceData.Load##width(address)); \
}

DEFINE_DOTS_LOAD_INSTANCE_SCALAR(float, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(int,   int,     4)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(uint,  uint,    4)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(half,  half,    2)

DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 2, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 3, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 4, asfloat, 4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   2, int2,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   3, int3,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   4, int4,    4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  2, uint2,   4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  3, uint3,   4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  4, uint4,   4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  2, half2,   2)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  3, half3,   2)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(half,  4, half4,   2)

// TODO: Other matrix sizes
float4x4 LoadDOTSInstancedData_float4x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4 * 16);
    float4 p1 = asfloat(unity_DOTSInstanceData.Load4(address + 0 * 16));
    float4 p2 = asfloat(unity_DOTSInstanceData.Load4(address + 1 * 16));
    float4 p3 = asfloat(unity_DOTSInstanceData.Load4(address + 2 * 16));
    float4 p4 = asfloat(unity_DOTSInstanceData.Load4(address + 3 * 16));
    return float4x4(
        p1.x, p2.x, p3.x, p4.x,
        p1.y, p2.y, p3.y, p4.y,
        p1.z, p2.z, p3.z, p4.z,
        p1.w, p2.w, p3.w, p4.w);
}
float4x4 LoadDOTSInstancedData(float4x4 dummy, uint metadata) { return LoadDOTSInstancedData_float4x4(metadata); }

float4x4 LoadDOTSInstancedData_float4x4_from_float3x4(uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 3 * 16);
    float4 p1 = asfloat(unity_DOTSInstanceData.Load4(address + 0 * 16));
    float4 p2 = asfloat(unity_DOTSInstanceData.Load4(address + 1 * 16));
    float4 p3 = asfloat(unity_DOTSInstanceData.Load4(address + 2 * 16));

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
        asfloat(unity_DOTSInstanceData.Load4(address + 0 * 8)),
        asfloat(unity_DOTSInstanceData.Load4(address + 1 * 8)));
}
float2x4 LoadDOTSInstancedData(float2x4 dummy, uint metadata) { return LoadDOTSInstancedData_float2x4(metadata); }

#undef DEFINE_DOTS_LOAD_INSTANCE_SCALAR
#undef DEFINE_DOTS_LOAD_INSTANCE_VECTOR

#endif // UNITY_DOTS_INSTANCING_ENABLED

#endif // UNITY_DOTS_INSTANCING_INCLUDED
