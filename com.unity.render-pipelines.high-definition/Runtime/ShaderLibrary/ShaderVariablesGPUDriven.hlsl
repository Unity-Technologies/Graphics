// UNITY_SHADER_NO_UPGRADE

//#ifndef UNITY_SHADER_VARIABLES_INCLUDED
//#define UNITY_SHADER_VARIABLES_INCLUDED

#ifdef UNITY_GPU_DRIVEN_PIPELINE
ByteAddressBuffer _InstancingPageBuffer;
float4 LoadInstancingBuffer_float4(uint index, uint pageOffset)
{
    if (pageOffset == ~0u)
        return (float4) 1;

    uint address = _InstancingPageBuffer.Load(pageOffset + (index + 1) * 4);
    return asfloat(_InstancingPageBuffer.Load4(address));
}

float4x4 LoadInstancingBuffer_float4x4(uint index, uint pageOffset)
{
    if (pageOffset == ~0u)
        return (float4x4) 0;

    uint address = _InstancingPageBuffer.Load(pageOffset + (index + 1) * 4);
    float4 p1 = asfloat(_InstancingPageBuffer.Load4(address + 0 * 16));
    float4 p2 = asfloat(_InstancingPageBuffer.Load4(address + 1 * 16));
    float4 p3 = asfloat(_InstancingPageBuffer.Load4(address + 2 * 16));
    float4 p4 = asfloat(_InstancingPageBuffer.Load4(address + 3 * 16));
    return float4x4(
                p1.x, p1.y, p1.z, p1.w,
                p2.x, p2.y, p2.z, p2.w,
                p3.x, p3.y, p3.z, p3.w,
                p4.x, p4.y, p4.z, p4.w);
}

uint LoadInstancingBuffer_uint(uint index, uint pageOffset)
{
    if (pageOffset == ~0u)
        return 0;

            //uint count = _InstancingPageBuffer.Load(pageOffset);
            //if (index >= count)
            //    return (float4) 1;
    uint address = _InstancingPageBuffer.Load(pageOffset + (index + 1) * 4);
    return asuint(_InstancingPageBuffer.Load(address));
}


#define UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(Type, Index, Offset) LoadInstancingBuffer_##Type(Index, Offset)
#define unity_RenderingLayer                UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(uint, 0, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT (1)       // the persistent count of properties

#define unity_LightmapST                    UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 0+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_LightmapIndex                 UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 1+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
//#define unity_DynamicLightmapST           UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, instancePageOffset)
#define unity_SHAr                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 1+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHAg                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 2+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHAb                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 3+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHBr                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 4+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHBg                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 5+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHBb                          UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 6+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_SHC                           UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 7+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_ProbeVolumeParams             UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 0+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_ProbeVolumeWorldToObject      UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4x4, 1+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_ProbeVolumeSizeInv            UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 2+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)
#define unity_ProbeVolumeMin                UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 3+UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_PERSISTENT, UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET)

//#define unity_ProbesOcclusion       UNITY_GPU_DRIVEN_INSTANCE_PROPERTY_LOAD(float4, 1+UNITY_GPU_DRIVEN_INSTANCE_BUFFER_PERSISTENT, instancePageOffset)
//#define unity_MatrixPreviousM       LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME_FROM_MACRO(float3x4, Metadata_unity_MatrixPreviousM))
//#define unity_MatrixPreviousMI      LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME_FROM_MACRO(float3x4, Metadata_unity_MatrixPreviousMI))

uint GetMeshRenderingLightLayerGPUDriven(UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET_DECLARE)
{ 
    return _EnableLightLayers ? (asuint(unity_RenderingLayer.x) & RENDERING_LIGHT_LAYERS_MASK) >> RENDERING_LIGHT_LAYERS_MASK_SHIFT : DEFAULT_LIGHT_LAYERS;
}

uint GetMeshRenderingDecalLayerGPUDriven(UNITY_GPU_DRIVEN_PROPERTY_BUFFER_OFFSET_DECLARE)
{
    return _EnableDecalLayers ? ((asuint(unity_RenderingLayer.x) & RENDERING_DECAL_LAYERS_MASK) >> RENDERING_DECAL_LAYERS_MASK_SHIFT) : DEFAULT_DECAL_LAYERS;
}

#endif

//#endif // UNITY_SHADER_VARIABLES_INCLUDED
