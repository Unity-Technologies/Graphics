using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.RadeonRays
{
    internal sealed class RestructureBvh : IDisposable
    {
        readonly ComputeShader shader;
        readonly int kernelInitPrimitiveCounts;
        readonly int kernelFindTreeletRoots;
        readonly int kernelRestructure;
        readonly int kernelPrepareTreeletsDispatchSize;
        const int numIterations = 3;
        readonly GraphicsBuffer treeletDispatchIndirectBuffer;

        const uint kGroupSize = 256u;
        const uint kTrianglesPerThread = 8u;
        const uint kTrianglesPerGroup = kTrianglesPerThread * kGroupSize;
        const uint kMinPrimitivesPerTreelet = 64u;
        const int kMaxThreadGroupsPerDispatch = 65535;

        public RestructureBvh(RadeonRaysShaders shaders)
        {
            shader = shaders.restructureBvh;
            kernelInitPrimitiveCounts = shader.FindKernel("InitPrimitiveCounts");
            kernelFindTreeletRoots = shader.FindKernel("FindTreeletRoots");
            kernelRestructure = shader.FindKernel("Restructure");
            kernelPrepareTreeletsDispatchSize = shader.FindKernel("PrepareTreeletsDispatchSize");

            treeletDispatchIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 6, sizeof(uint));
        }
        public void Dispose()
        {
            treeletDispatchIndirectBuffer.Dispose();
        }

        public ulong GetScratchDataSizeInDwords(uint triangleCount)
        {
            var scratchLayout = ScratchBufferLayout.Create(triangleCount);
            return scratchLayout.TotalSize;
        }

        public static uint GetBvhNodeCount(uint leafCount)
        {
            return leafCount - 1;
        }

        public void Execute(
            CommandBuffer cmd,
            GraphicsBuffer vertices, int verticesOffset, uint vertexStride, uint triangleCount,
            GraphicsBuffer scratch, in BottomLevelLevelAccelStruct result)
        {
            var scratchLayout = ScratchBufferLayout.Create(triangleCount);

            cmd.SetComputeIntParam(shader, SID.g_vertices_offset, verticesOffset);
            cmd.SetComputeIntParam(shader, SID.g_constants_vertex_stride, (int)vertexStride);
            cmd.SetComputeIntParam(shader, SID.g_constants_triangle_count, (int)triangleCount);
            cmd.SetComputeIntParam(shader, SID.g_treelet_count_offset, (int)scratchLayout.TreeletCount);
            cmd.SetComputeIntParam(shader, SID.g_treelet_roots_offset, (int)scratchLayout.TreeletRoots);
            cmd.SetComputeIntParam(shader, SID.g_primitive_counts_offset, (int)scratchLayout.PrimitiveCounts);
            cmd.SetComputeIntParam(shader, SID.g_leaf_parents_offset, (int)scratchLayout.LeafParents);
            cmd.SetComputeIntParam(shader, SID.g_bvh_offset, (int)result.bvhOffset);
            cmd.SetComputeIntParam(shader, SID.g_bvh_leaves_offset, (int)result.bvhLeavesOffset);

            uint minPrimitivePerTreelet = kMinPrimitivesPerTreelet;
            for (int i = 0; i < numIterations; ++i)
            {
                cmd.SetComputeIntParam(shader, SID.g_constants_min_prims_per_treelet, (int)minPrimitivePerTreelet);

                BindKernelArguments(cmd, kernelInitPrimitiveCounts, vertices, scratch, result);
                cmd.DispatchCompute(shader, kernelInitPrimitiveCounts, (int)Common.CeilDivide(kTrianglesPerGroup, kGroupSize), 1, 1);

                BindKernelArguments(cmd, kernelFindTreeletRoots, vertices, scratch, result);
                cmd.DispatchCompute(shader, kernelFindTreeletRoots, (int)Common.CeilDivide(kTrianglesPerGroup, kGroupSize), 1, 1);

                BindKernelArguments(cmd, kernelPrepareTreeletsDispatchSize, vertices, scratch, result);
                cmd.DispatchCompute(shader, kernelPrepareTreeletsDispatchSize, 1, 1, 1);

                BindKernelArguments(cmd, kernelRestructure, vertices, scratch, result);
                cmd.SetComputeIntParam(shader, SID.g_remainder_treelets, 0);
                cmd.DispatchCompute(shader, kernelRestructure, treeletDispatchIndirectBuffer, 0);

                if (Common.CeilDivide(triangleCount, minPrimitivePerTreelet) > kMaxThreadGroupsPerDispatch)
                {
                    cmd.SetComputeIntParam(shader, SID.g_remainder_treelets, 1);
                    cmd.DispatchCompute(shader, kernelRestructure, treeletDispatchIndirectBuffer, 3 * sizeof(uint));
                }

                minPrimitivePerTreelet *= 2;
            }
        }

        private void BindKernelArguments(
            CommandBuffer cmd, int kernel,
            GraphicsBuffer vertices,
            GraphicsBuffer scratch, BottomLevelLevelAccelStruct result)
        {
            cmd.SetComputeBufferParam(shader, kernel, SID.g_vertices, vertices);
            cmd.SetComputeBufferParam(shader, kernel, SID.g_scratch_buffer, scratch);
            cmd.SetComputeBufferParam(shader, kernel, SID.g_bvh, result.bvh);
            cmd.SetComputeBufferParam(shader, kernel, SID.g_bvh_leaves, result.bvhLeaves);
            cmd.SetComputeBufferParam(shader, kernel, SID.g_treelet_dispatch_buffer, treeletDispatchIndirectBuffer);
        }

        struct ScratchBufferLayout
        {
            public uint LeafParents;
            public uint TreeletCount;
            public uint TreeletRoots;
            public uint PrimitiveCounts;
            public uint TotalSize;

            public static ScratchBufferLayout Create(uint triangleCount)
            {
                var result = new ScratchBufferLayout();
                result.LeafParents     = result.Reserve(triangleCount);
                result.TreeletCount    = result.Reserve(1);
                result.TreeletRoots    = result.Reserve(triangleCount);
                result.PrimitiveCounts = result.Reserve(GetBvhNodeCount(triangleCount));

                return result;
            }

            uint Reserve(uint size)
            {
                var temp = TotalSize;
                TotalSize += size;
                return temp;
            }
        }
    }
}

