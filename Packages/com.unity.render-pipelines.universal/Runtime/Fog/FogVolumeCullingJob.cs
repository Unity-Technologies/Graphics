#if VOLUMETRIC_FOG

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct FogVolumeProperties
    {
        public float4 scattering;
        public float3 densityTextureTiling;
        public float3 densityTextureScrollSpeed;
        public int blendPriority;
        public EntityId entityId;
        public FogVolumeDensityMode densityMode;
        public FogVolumeBlendMode blendMode;
#if UNITY_EDITOR
        public ulong sceneCullingMask;
#endif
    }

    // Must match FogVolumeData in FogVolume.shader.
    struct FogVolumeData
    {
        public float4 axisX;
        public float4 axisY;
        public float4 axisZ;
        public float4 center;
        public float4 scattering;
        public float4 clipBounds;
        public float3 densityTextureTiling;
        public uint startSlice; // 16-byte boundary
        public float3 densityTextureScrollSpeed;
        public uint sliceCount; // 16-byte boundary
        public uint densityMode;
        public uint3 padding; // 16-byte boundary
    }

    struct FogVolumeSortKey : IComparable<FogVolumeSortKey>
    {
        public int blendPriority;
        public EntityId entityId;
        public int fogVolumeIndex;

        public int CompareTo(FogVolumeSortKey other)
        {
            // Blend priority, ascending
            int result = blendPriority.CompareTo(other.blendPriority);

            // EntityId, ascending (tie-break)
            if (result == 0)
                result = entityId.CompareTo(other.entityId);

            return result;
        }
    }

    struct FogVolumeBox
    {
        public float3 center;
        public float3x3 halfAxes;
        public float3x3 worldToLocal;

        public FogVolumeBox(in float4x4 localToWorld)
        {
            center = localToWorld.c3.xyz;
            halfAxes = 0.5f * math.float3x3(localToWorld.c0.xyz, localToWorld.c1.xyz, localToWorld.c2.xyz);
            worldToLocal = math.inverse(halfAxes);
        }

        public float3 GetCorner(int index)
        {
            float3 signs = math.select(math.float3(-1.0f), math.float3(1.0f), (index & math.int3(1, 2, 4)) != 0);
            return center + math.mul(halfAxes, signs);
        }

        public bool IsBehindPlane(float4 plane)
        {
            float radius = math.csum(math.abs(math.mul(plane.xyz, halfAxes)));
            return math.dot(plane.xyz, center) + plane.w < -radius;
        }

        public float GetDistance(float3 point)
        {
            float3 offset = point - center;
            float3 pointLS = math.mul(worldToLocal, offset);
            float3 excess = pointLS - math.clamp(pointLS, -1.0f, 1.0f);
            if (math.all(excess == 0.0f))
                return 0.0f;

            // Exact for orthogonal axes and never overestimates for sheared ones.
            float3 direction = math.normalize(math.mul(halfAxes, excess));
            return math.max(0.0f, math.dot(direction, offset) - math.csum(math.abs(math.mul(direction, halfAxes))));
        }
    }

    [BurstCompile]
    struct FogVolumeCullingJob : IJob
    {
        const float k_MinHalfExtent = 0.0005f;

        [ReadOnly] public NativeArray<FogVolumeBox> boxes;
        [ReadOnly] public NativeArray<FogVolumeProperties> properties;
        [ReadOnly] public NativeArray<int> layers;
        public int fogVolumeCount;

        public int cullingMask;
#if UNITY_EDITOR
        public ulong sceneCullingMask;
#endif
        public float4x4 worldToClip;
        public float4x4 worldToGPUClip;
        public float3 cameraPosition;
        public float maxDistance;
        public float4 encodingParams;
        public int sliceCount;
        public float2 voxelSizeNDC;

        public NativeArray<FogVolumeSortKey> sortKeys;
        public NativeArray<FogVolumeData> fogVolumeData;
        public NativeArray<FogVolumeBlendMode> blendModes;
        public NativeReference<int> visibleCount;

        public void Execute()
        {
            var rows = math.transpose(worldToClip);
            var left = rows.c3 + rows.c0;
            var right = rows.c3 - rows.c0;
            var bottom = rows.c3 + rows.c1;
            var top = rows.c3 - rows.c1;

            int count = 0;
            for (int i = 0; i < fogVolumeCount; ++i)
            {
                if ((cullingMask & (1 << layers[i])) == 0)
                    continue;

#if UNITY_EDITOR
                if ((sceneCullingMask & properties[i].sceneCullingMask) == 0)
                    continue;
#endif

                var box = boxes[i];

                if (math.abs(math.determinant(box.halfAxes)) < k_MinHalfExtent * k_MinHalfExtent * k_MinHalfExtent)
                    continue;

                if (box.IsBehindPlane(left) ||
                    box.IsBehindPlane(right) ||
                    box.IsBehindPlane(bottom) ||
                    box.IsBehindPlane(top))
                    continue;

                if (box.GetDistance(cameraPosition) >= maxDistance)
                    continue;

                sortKeys[count++] = new FogVolumeSortKey
                {
                    blendPriority = properties[i].blendPriority,
                    entityId = properties[i].entityId,
                    fogVolumeIndex = i,
                };
            }

            sortKeys.GetSubArray(0, count).Sort();

            for (int i = 0; i < count; ++i)
            {
                int fogVolumeIndex = sortKeys[i].fogVolumeIndex;
                var fogVolumeProperties = properties[fogVolumeIndex];
                fogVolumeData[i] = ConvertToGPUData(boxes[fogVolumeIndex], fogVolumeProperties);
                blendModes[i] = fogVolumeProperties.blendMode;
            }

            visibleCount.Value = count;
        }

        FogVolumeData ConvertToGPUData(in FogVolumeBox box, in FogVolumeProperties fogVolumeProperties)
        {
            float nearestDistance = box.GetDistance(cameraPosition);
            float farthestDistance = 0.0f;
            float2 clipMin = float.MaxValue;
            float2 clipMax = float.MinValue;
            bool coversScreen = nearestDistance == 0.0f;

            for (int corner = 0; corner < 8; ++corner)
            {
                float3 cornerWS = box.GetCorner(corner);
                farthestDistance = math.max(farthestDistance, math.distance(cornerWS, cameraPosition));

                float4 cornerCS = math.mul(worldToGPUClip, math.float4(cornerWS, 1.0f));
                if (cornerCS.w <= 0.0f)
                {
                    coversScreen = true;
                    continue;
                }

                float2 cornerNDC = cornerCS.xy / cornerCS.w;
                clipMin = math.min(clipMin, cornerNDC);
                clipMax = math.max(clipMax, cornerNDC);
            }

            if (coversScreen)
            {
                clipMin = -1.0f;
                clipMax = 1.0f;
            }
            else
            {
                clipMin = math.max(clipMin - voxelSizeNDC, -1.0f);
                clipMax = math.min(clipMax + voxelSizeNDC, 1.0f);
            }

            int startSlice = math.clamp(GetSlice(nearestDistance) - 1, 0, sliceCount - 1);
            int endSlice = math.clamp(GetSlice(farthestDistance) + 1, 0, sliceCount - 1);

            var worldToLocalRows = math.transpose(box.worldToLocal);

            return new FogVolumeData
            {
                axisX = math.float4(worldToLocalRows.c0, 0.0f),
                axisY = math.float4(worldToLocalRows.c1, 0.0f),
                axisZ = math.float4(worldToLocalRows.c2, 0.0f),
                center = math.float4(box.center, 0.0f),
                scattering = fogVolumeProperties.scattering,
                clipBounds = math.float4(clipMin, clipMax - clipMin),
                densityTextureTiling = fogVolumeProperties.densityTextureTiling,
                startSlice = (uint)startSlice,
                densityTextureScrollSpeed = fogVolumeProperties.densityTextureScrollSpeed,
                sliceCount = (uint)(endSlice - startSlice + 1),
                densityMode = (uint)fogVolumeProperties.densityMode,
            };
        }

        // Mirrors EncodeLogarithmicDepthGeneralized() in Common.hlsl.
        int GetSlice(float distance)
        {
            float encoded = encodingParams.x +
                encodingParams.y * math.log2(math.max(0.0f, distance - encodingParams.z));
            return (int)math.floor(math.saturate(encoded) * sliceCount);
        }
    }
}

#endif
