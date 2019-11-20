#ifndef UNITY_DOTS_INSTANCING_INCLUDED
#define UNITY_DOTS_INSTANCING_INCLUDED

#ifdef UNITY_DOTS_INSTANCING_ENABLED

// TODO: Shader feature level to compute only
ByteAddressBuffer unity_DOTSInstanceData;

CBUFFER_START(UnityInstanceVisibility)
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

// TODO: types that are not float/int/uint
#define DEFINE_DOTS_LOAD_INSTANCE_SCALAR(type, conv) \
type LoadDOTSInstancedData(type dummy, uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4); \
    return conv(unity_DOTSInstanceData.Load(address)); \
}

#define DEFINE_DOTS_LOAD_INSTANCE_VECTOR(type, width, conv) \
type##width LoadDOTSInstancedData(type##width dummy, uint metadata) \
{ \
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4 * width); \
    return conv(unity_DOTSInstanceData.Load##width(address)); \
}

DEFINE_DOTS_LOAD_INSTANCE_SCALAR(float, asfloat)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(int,   int)
DEFINE_DOTS_LOAD_INSTANCE_SCALAR(uint,  uint)

DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 2, asfloat)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 3, asfloat)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(float, 4, asfloat)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   2, int2)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   3, int3)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(int,   4, int4)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  2, uint2)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  3, uint3)
DEFINE_DOTS_LOAD_INSTANCE_VECTOR(uint,  4, uint4)

float4x4 LoadDOTSInstancedData(float4x4 dummy, uint metadata)
{
    uint address = ComputeDOTSInstanceDataAddress(metadata, 4 * 16);
    return float4x4(
        asfloat(unity_DOTSInstanceData.Load4(address + 0 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 1 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 2 * 16)),
        asfloat(unity_DOTSInstanceData.Load4(address + 3 * 16)));
}

#undef DEFINE_DOTS_LOAD_INSTANCE_SCALAR
#undef DEFINE_DOTS_LOAD_INSTANCE_VECTOR

#endif // UNITY_DOTS_INSTANCING_ENABLED


#endif // UNITY_DOTS_INSTANCING_INCLUDED

