#ifndef UNITY_DOTS_INSTANCING_INCLUDED
#define UNITY_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

// TODO: Shader feature level to compute only
ByteAddressBuffer unity_DOTSInstanceData;

CBUFFER_START(UnityDOTSInstancing_InstanceVisibility)
    uint4 unity_DOTSVisibleInstances[UNITY_INSTANCED_ARRAY_SIZE];
CBUFFER_END

uint GetDOTSInstanceIndex()
{
    return unity_DOTSVisibleInstances[unity_InstanceID].x;
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
} \
type LoadDOTSInstancedData(type dummy, uint metadata) { return LoadDOTSInstancedData_##type(metadata); }

#define DEFINE_DOTS_LOAD_INSTANCE_VECTOR(type, width, conv, sizeof_type) \
type##width LoadDOTSInstancedData_##type##width(uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, sizeof_type * width); \
    return conv(unity_DOTSInstanceData.Load##width(address)); \
} \
type##width LoadDOTSInstancedData(type##width dummy, uint metadata) { return LoadDOTSInstancedData_##type##width(metadata); }

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
    // TODO: Remove this transpose, do it on CPU side
    return transpose(float4x4(
        asfloat(unity_DOTSInstanceData.Load4(address + 0 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 1 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 2 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 3 * 16))));
}
float4x4 LoadDOTSInstancedData(float4x4 dummy, uint metadata) { return LoadDOTSInstancedData_float4x4(metadata); }

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

