#ifdef UNITY_INSTANCING_ENABLED
    TEXTURE2D(_TerrainHeightmapTexture);
    TEXTURE2D(_TerrainNormalmapTexture);
    float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif

#define API_HAS_GURANTEED_R16_SUPPORT 0 //!(SHADER_API_VULKAN)
float UnpackHeightmap(float4 height)
{
#if (API_HAS_GURANTEED_R16_SUPPORT)
    return height.r;
#else
    return (height.r + height.g * 256.0f) / 257.0f; // (255.0f * height.r + 255.0f * 256.0f * height.g) / 65535.0f
#endif
}

UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

void ApplyPreVertexModification(inout AttributesMesh input)
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = input.positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    input.positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    input.positionOS.y = height * _TerrainHeightmapScale.y;

    #ifdef ATTRIBUTES_NEED_NORMAL
        input.normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
        input.uv0 = sampleCoords * _TerrainHeightmapRecipSize.zw;
    #endif
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    input.tangentOS.xyz = cross(float3(0,0,1), input.normalOS);
    input.tangentOS.w = 1;
#endif
}

void ApplyVertexModification(AttributesMesh input, float3 normalWS, inout float3 positionWS, float4 time)
{
}
