using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering.FrustumPlanes
{
    public struct FrustumPlanes
    {
        public enum IntersectResult
        {
            Out,
            In,
            Partial
        };

        public struct PlanePacket4
        {
            public float4 Xs;
            public float4 Ys;
            public float4 Zs;
            public float4 Distances;
        }

        public static NativeArray<PlanePacket4> BuildSOAPlanePacketsMulti(NativeArray<Plane> cullingPlanes, NativeArray<int> splitCounts, Allocator allocator)
        {
            int totalPacketCount = 0;
            for (int s = 0; s < splitCounts.Length; ++s)
            {
                totalPacketCount += (splitCounts[s] + 3) >> 2;
            }

            var planes = new NativeArray<PlanePacket4>(totalPacketCount, allocator, NativeArrayOptions.UninitializedMemory);

            int dstOffset = 0;
            int srcOffset = 0;
            for (int s = 0; s < splitCounts.Length; ++s)
            {
                int cullingPlaneCount = splitCounts[s];
                int packetCount = (cullingPlaneCount + 3) >> 2;

                for (int i = 0; i < cullingPlaneCount; i++)
                {
                    var p = planes[dstOffset + (i >> 2)];
                    p.Xs[i & 3] = cullingPlanes[srcOffset + i].normal.x;
                    p.Ys[i & 3] = cullingPlanes[srcOffset + i].normal.y;
                    p.Zs[i & 3] = cullingPlanes[srcOffset + i].normal.z;
                    p.Distances[i & 3] = cullingPlanes[srcOffset + i].distance;
                    planes[dstOffset + (i >> 2)] = p;
                }

                // Populate the remaining planes with values that are always "in"
                for (int i = cullingPlaneCount; i < 4 * packetCount; ++i)
                {
                    var p = planes[dstOffset + (i >> 2)];
                    p.Xs[i & 3] = 1.0f;
                    p.Ys[i & 3] = 0.0f;
                    p.Zs[i & 3] = 0.0f;

                    // This value was before hardcoded to 32786.0f.
                    // It was causing the culling system to discard the rendering of entities having a X coordinate approximately less than -32786.
                    // We could not find anything relying on this number, so the value has been increased to 1 billion
                    p.Distances[i & 3] = 1e9f;

                    planes[dstOffset + (i >> 2)] = p;
                }

                srcOffset += cullingPlaneCount;
                dstOffset += packetCount;
            }
            return planes;
        }

        // Returns a bitmask (one 1 per split): 1 = visible, 0 = not visible in that split.
        static public uint Intersect2NoPartialMulti(NativeArray<PlanePacket4> cullingPlanePackets, NativeArray<int> splitCounts, AABB a)
        {
            float4 mx = a.Center.xxxx;
            float4 my = a.Center.yyyy;
            float4 mz = a.Center.zzzz;

            float4 ex = a.Extents.xxxx;
            float4 ey = a.Extents.yyyy;
            float4 ez = a.Extents.zzzz;

            uint outMask = 0;

            int offset = 0;
            for (int s = 0; s < splitCounts.Length; ++s)
            {
                int packetCount = (splitCounts[s] + 3) >> 2;

                int4 masks = 0;
                for (int i = 0; i < packetCount; i++)
                {
                    var p = cullingPlanePackets[offset + i];
                    float4 distances = dot4(p.Xs, p.Ys, p.Zs, mx, my, mz) + p.Distances;
                    float4 radii = dot4(ex, ey, ez, math.abs(p.Xs), math.abs(p.Ys), math.abs(p.Zs));
                    masks += (int4)(distances + radii <= 0);
                }

                int outCount = math.csum(masks);
                if (outCount == 0) outMask |= 1u << s;

                offset += packetCount;
            }

            return outMask;
        }

        public static NativeArray<PlanePacket4> BuildSOAPlanePackets(NativeArray<Plane> cullingPlanes, int offset, int count, Allocator allocator)
        {
            int cullingPlaneCount = count;
            int packetCount = (cullingPlaneCount + 3) >> 2;
            var planes = new NativeArray<PlanePacket4>(packetCount, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < cullingPlaneCount; i++)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = cullingPlanes[i + offset].normal.x;
                p.Ys[i & 3] = cullingPlanes[i + offset].normal.y;
                p.Zs[i & 3] = cullingPlanes[i + offset].normal.z;
                p.Distances[i & 3] = cullingPlanes[i + offset].distance;
                planes[i >> 2] = p;
            }

            // Populate the remaining planes with values that are always "in"
            for (int i = cullingPlaneCount; i < 4 * packetCount; ++i)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = 1.0f;
                p.Ys[i & 3] = 0.0f;
                p.Zs[i & 3] = 0.0f;

                // This value was before hardcoded to 32786.0f.
                // It was causing the culling system to discard the rendering of entities having a X coordinate approximately less than -32786.
                // We could not find anything relying on this number, so the value has been increased to 1 billion
                p.Distances[i & 3] = 1e9f;

                planes[i >> 2] = p;
            }

            return planes;
        }

        static public IntersectResult Intersect2NoPartial(NativeArray<PlanePacket4> cullingPlanePackets, AABB a)
        {
            float4 mx = a.Center.xxxx;
            float4 my = a.Center.yyyy;
            float4 mz = a.Center.zzzz;

            float4 ex = a.Extents.xxxx;
            float4 ey = a.Extents.yyyy;
            float4 ez = a.Extents.zzzz;

            int4 masks = 0;

            for (int i = 0; i < cullingPlanePackets.Length; i++)
            {
                var p = cullingPlanePackets[i];
                float4 distances = dot4(p.Xs, p.Ys, p.Zs, mx, my, mz) + p.Distances;
                float4 radii = dot4(ex, ey, ez, math.abs(p.Xs), math.abs(p.Ys), math.abs(p.Zs));

                masks += (int4)(distances + radii <= 0);
            }

            int outCount = math.csum(masks);
            return outCount > 0 ? IntersectResult.Out : IntersectResult.In;
        }

        private static float4 dot4(float4 xs, float4 ys, float4 zs, float4 mx, float4 my, float4 mz)
        {
            return xs * mx + ys * my + zs * mz;
        }
    }
}
