using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.RadeonRays
{

    internal struct AABB
    {
        public float3 Min;
        public float3 Max;

        public static readonly AABB Empty = new AABB(
            new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));

        public AABB(float3 min, float3 max)
        {
            Min = min;
            Max = max;
        }

        public void Encapsulate(AABB aabb)
        {
            Min = math.min(Min, aabb.Min);
            Max = math.max(Max, aabb.Max);
        }

        public void Encapsulate(float3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }

        public bool Contains(AABB rhs)
        {
            return rhs.Min.x >= Min.x && rhs.Min.y >= Min.y && rhs.Min.z >= Min.z &&
                   rhs.Max.x <= Max.x && rhs.Max.y <= Max.y && rhs.Max.z <= Max.z;
        }

        public bool IsValid()
        {
            return Min.x <= Max.x && Min.y <= Max.y && Min.z <= Max.z;
        }
    }


    internal class BvhCheck
    {
        const uint kInvalidID = ~0u;

        public class VertexBuffers
        {
            public GraphicsBuffer vertices;
            public GraphicsBuffer indices;
            public uint vertexBufferOffset = 0;
            public uint vertexCount;
            public uint vertexStride = 3;
            public uint indexBufferOffset = 0;
            public IndexFormat indexFormat = IndexFormat.Int32;
            public uint indexCount;

        };

        public static VertexBuffers Convert(MeshBuildInfo info)
        {
            var res = new VertexBuffers()
            {
                vertices = info.vertices,
                indices = info.triangleIndices,
                vertexBufferOffset = (uint)info.verticesStartOffset,
                vertexCount = info.vertexCount,
                vertexStride = info.vertexStride,
                indexBufferOffset = (uint)info.indicesStartOffset,
                indexCount = info.triangleCount * 3,
                indexFormat = info.indexFormat
            };

            return res;
        }

        public static double SurfaceArea(AABB aabb)
        {
            float3 edges = aabb.Max - aabb.Min;
            return 2.0f * (edges.x * edges.y + edges.x * edges.z + edges.z * edges.y);
        }

        public static double NodeSahCost(uint nodeAddr, AABB nodeAabb, AABB parentAabb)
        {
            double cost = IsLeafNode(nodeAddr) ? GetLeafNodePrimCount(nodeAddr) : 1.2f;
            var a = SurfaceArea(nodeAabb);
            var b = SurfaceArea(parentAabb);
            return cost * a / b;
        }

        public static double CheckConsistency(VertexBuffers bvhVertexBuffers, BottomLevelAccelStruct bvh, uint primitiveCount)
        {
            var header = new BvhHeader[1];
            bvh.bvh.GetData(header, 0, (int)bvh.bvhOffset, 1);

            var vertexBuffers = DownloadVertexData(bvhVertexBuffers);
            AABB[] primitiveAabbs = ComputePrimitiveAabbList(vertexBuffers, primitiveCount);

            return CheckConsistencyInternal(primitiveAabbs, bvh.bvh, bvh.bvhOffset + 1, bvh.bvhLeaves, bvh.bvhLeavesOffset, header[0], primitiveCount);
        }

        public static double CheckConsistency(GraphicsBuffer primitiveAabbsBuffer, BottomLevelAccelStruct bvh, uint primitiveCount)
        {
            var header = new BvhHeader[1];
            bvh.bvh.GetData(header, 0, (int)bvh.bvhOffset, 1);

            AABB[] primitiveAabbs = new AABB[primitiveCount];
            primitiveAabbsBuffer.GetData(primitiveAabbs);

            return CheckConsistencyInternal(primitiveAabbs, bvh.bvh, bvh.bvhOffset + 1, bvh.bvhLeaves, bvh.bvhLeavesOffset, header[0], primitiveCount);
        }

        public static double CheckConsistency(GraphicsBuffer bvhBuffer, uint bvhBufferOffset, uint primitiveCount)
        {
            var header = new BvhHeader[1];
            bvhBuffer.GetData(header, 0, (int)bvhBufferOffset, 1);

            return CheckConsistencyInternal(null, bvhBuffer, bvhBufferOffset + 1, null, 0, header[0], primitiveCount);
        }


        public static int ExtractBits(uint value, int startBit, int count)
        {
            uint mask = (uint)(((1 << count) - 1) << startBit);
            return ((int)(mask & value)) >> startBit;
        }

        public static bool IsLeafNode(uint nodeAddr)
        {
            return (nodeAddr & (1 << 31)) != 0;
        }

        public static uint GetLeafNodeFirstPrim(uint nodeAddr)
        {
            return (nodeAddr & ~0xE0000000);
        }

        public static uint GetLeafNodePrimCount(uint nodeAddr)
        {
            return (uint)ExtractBits(nodeAddr, 29, 2) + 1;
        }

        static double CheckConsistencyInternal(
            AABB[] primitiveAabbs,
            GraphicsBuffer bvhBuffer, uint bvhBufferOffset, GraphicsBuffer bvhLeavesBuffer, uint bvhLeavesBufferOffset,
            BvhHeader header, uint primitiveCount)
        {
            uint leafCount = header.leafNodeCount;
            uint rootAddr = header.root;
            bool isTopLevel = primitiveAabbs == null;
            var nodeCount = isTopLevel ? HlbvhTopLevelBuilder.GetBvhNodeCount(leafCount) : HlbvhBuilder.GetBvhNodeCount(leafCount);
            Assert.AreEqual(header.internalNodeCount, nodeCount);

            var bvhNodes = new BvhNode[nodeCount];
            bvhBuffer.GetData(bvhNodes, 0, (int)bvhBufferOffset, (int)nodeCount);

            uint4[] bvhLeafNodes = null;
            if (!isTopLevel)
            {
                bvhLeafNodes = new uint4[primitiveCount];
                bvhLeavesBuffer.GetData(bvhLeafNodes, 0, (int)bvhLeavesBufferOffset, (int)primitiveCount);
            }

            uint countedPrimitives = 0;

            var rootAabb = GetAabb(primitiveAabbs, bvhNodes, bvhLeafNodes, rootAddr, isTopLevel);
            double sahCost = 0.0f;

            var q = new Queue<(uint Addr, uint Parent)>();
            q.Enqueue((Addr: rootAddr, Parent: kInvalidID));
            while (q.Count != 0)
            {
                var current = q.Dequeue();
                uint addr = current.Addr;
                uint parent = current.Parent;

                AABB aabb = GetAabb(primitiveAabbs, bvhNodes, bvhLeafNodes, addr, isTopLevel);
                sahCost += NodeSahCost(addr, aabb, rootAabb);

                if (!(isTopLevel && IsLeafNode(addr)))
                    Assert.IsTrue(aabb.IsValid());

                if (IsLeafNode(addr))
                {
                    countedPrimitives += isTopLevel ? 1 : GetLeafNodePrimCount(addr);
                }
                else // internal node
                {
                    var node = bvhNodes[addr];
                    Assert.AreEqual(parent, node.parent);
                    var leftAabb = GetAabb(primitiveAabbs, bvhNodes, bvhLeafNodes, node.child0, isTopLevel);
                    var rightAabb = GetAabb(primitiveAabbs, bvhNodes, bvhLeafNodes, node.child1, isTopLevel);

                    bool leftOk = (aabb.Contains(leftAabb));
                    bool rightOk = (aabb.Contains(rightAabb));

                    Assert.IsTrue(leftOk);
                    Assert.IsTrue(rightOk);

                    if (node.child0  != kInvalidID)
                        q.Enqueue((Addr: node.child0, Parent: addr));

                    if (node.child1 != kInvalidID)
                        q.Enqueue((Addr: node.child1, Parent: addr));
                }
            }

            Assert.AreEqual(countedPrimitives, primitiveCount);

            return sahCost;
        }

        private sealed class VertexBuffersCPU
        {
            public float[] vertices;
            public uint[] indices;
            public uint vertexStride;
        };


        static uint3 GetFaceIndices(uint[] indices, uint triangleIdx)
        {
            return new uint3(
                indices[3 * triangleIdx],
                indices[3 * triangleIdx + 1],
                indices[3 * triangleIdx + 2]);
        }

        static float3 GetVertex(float[] vertices, uint stride, uint idx)
        {
            uint indexInFloats = idx * stride;
            return new float3(
                vertices[indexInFloats],
                vertices[indexInFloats + 1],
                vertices[indexInFloats + 2]);
        }

        struct Triangle
        {
            public float3 v0;
            public float3 v1;
            public float3 v2;
        };

        static Triangle GetTriangle(float[] vertices, uint stride, uint3 idx)
        {
            Triangle tri;
            tri.v0 = GetVertex(vertices, stride, idx.x);
            tri.v1 = GetVertex(vertices, stride, idx.y);
            tri.v2 = GetVertex(vertices, stride, idx.z);
            return tri;
        }

        static VertexBuffersCPU DownloadVertexData(VertexBuffers vertexBuffers)
        {
            var result = new VertexBuffersCPU();
            result.vertices = new float[vertexBuffers.vertexCount * vertexBuffers.vertexStride];
            result.indices = new uint[vertexBuffers.indexCount];
            result.vertexStride = vertexBuffers.vertexStride;

            if (vertexBuffers.indexFormat == IndexFormat.Int32)
            {
                vertexBuffers.indices.GetData(result.indices, 0, (int)vertexBuffers.indexBufferOffset, (int)vertexBuffers.indexCount);
            }
            else
            {
                var tmp = new ushort[vertexBuffers.indexCount];
                vertexBuffers.indices.GetData(tmp, 0, (int)vertexBuffers.indexBufferOffset, (int)vertexBuffers.indexCount);
                for (int i = 0; i < vertexBuffers.indexCount; ++i)
                    result.indices[i] = tmp[i];
            }

            vertexBuffers.vertices.GetData(result.vertices, 0, (int)vertexBuffers.vertexBufferOffset, (int)(vertexBuffers.vertexCount * vertexBuffers.vertexStride));

            return result;
        }

        static AABB GetAabb(AABB[] primitiveAabbs, BvhNode[] bvhNodes, uint4[] bvhLeafNodes, uint nodeAddr, bool isTopLevel)
        {
            var aabb = AABB.Empty;

            if (!IsLeafNode(nodeAddr))
            {
                var node = bvhNodes[nodeAddr];
                AABB left = new AABB(node.aabb0_min, node.aabb0_max);
                aabb.Encapsulate(left);

                AABB right = new AABB(node.aabb1_min, node.aabb1_max);
                aabb.Encapsulate(right);
            }
            else if (!isTopLevel)
            {
                int firstIndex = (int)GetLeafNodeFirstPrim(nodeAddr);
                int primCount = (int)GetLeafNodePrimCount(nodeAddr);
                for (int i = 0; i < primCount; ++i)
                {
                    uint index = (uint)(i + firstIndex);
                    uint primIndex = bvhLeafNodes[index].w;
                    aabb.Encapsulate(primitiveAabbs[primIndex]);
                }
            }

            return aabb;
        }

        static AABB[] ComputePrimitiveAabbList(VertexBuffersCPU bvhVertexBuffers, uint primitiveCount)
        {
            var res = new AABB[primitiveCount];
            for (uint i = 0; i < primitiveCount; ++i)
            {
                uint3 meshTriangleindices = GetFaceIndices(bvhVertexBuffers.indices, i);
                var triangle = GetTriangle(bvhVertexBuffers.vertices, bvhVertexBuffers.vertexStride, meshTriangleindices);

                var aabb = AABB.Empty;
                aabb.Encapsulate(triangle.v0);
                aabb.Encapsulate(triangle.v1);
                aabb.Encapsulate(triangle.v2);

                res[i] = aabb;
            }

            return res;
        }
    }
}

