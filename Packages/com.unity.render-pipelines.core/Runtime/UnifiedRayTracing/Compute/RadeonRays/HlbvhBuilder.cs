using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.RadeonRays
{
    internal struct BottomLevelLevelAccelStruct
    {
        public GraphicsBuffer bvh;
        public uint bvhOffset;
        public GraphicsBuffer bvhLeaves;
        public uint bvhLeavesOffset;
    }

    internal class HlbvhBuilder
    {
        readonly ComputeShader shaderBuildHlbvh;
        readonly int kernelInit;
        readonly int kernelCalculateAabb;
        readonly int kernelCalculateMortonCodes;
        readonly int kernelBuildTreeBottomUp;

        readonly RadixSort radixSort;

        const uint kTrianglesPerThread = 8u;
        const uint kGroupSize = 256u;
        const uint kTrianglesPerGroup = kTrianglesPerThread * kGroupSize;

        public HlbvhBuilder(RadeonRaysShaders shaders)
        {
            shaderBuildHlbvh = shaders.buildHlbvh;
            kernelInit = shaderBuildHlbvh.FindKernel("Init");
            kernelCalculateAabb = shaderBuildHlbvh.FindKernel("CalculateAabb");
            kernelCalculateMortonCodes = shaderBuildHlbvh.FindKernel("CalculateMortonCodes");
            kernelBuildTreeBottomUp = shaderBuildHlbvh.FindKernel("BuildTreeBottomUp");

            radixSort = new RadixSort(shaders);
        }

        public uint GetScratchDataSizeInDwords(uint triangleCount)
        {
            var scratchLayout = ScratchBufferLayout.Create(triangleCount);
            return scratchLayout.TotalSize;
        }

        public static uint GetBvhNodeCount(uint leafCount)
        {
            return leafCount - 1;
        }

        public uint GetResultDataSizeInDwords(uint triangleCount)
        {
            var bvhNodeCount = GetBvhNodeCount(triangleCount) + 1; // plus one for header
            uint sizeOfNode = 16;
            return bvhNodeCount * sizeOfNode;
        }

        public void Execute(
            CommandBuffer cmd,
            GraphicsBuffer vertices, int verticesOffset, uint vertexStride,
            GraphicsBuffer indices, int indicesOffset, int baseIndex, IndexFormat indexFormat, uint triangleCount,
            GraphicsBuffer scratch,
            in BottomLevelLevelAccelStruct result)
        {
            Common.EnableKeyword(cmd, shaderBuildHlbvh, "TOP_LEVEL", false);
            Common.EnableKeyword(cmd, shaderBuildHlbvh, "UINT16_INDICES", indexFormat == IndexFormat.Int16);
            var scratchLayout = ScratchBufferLayout.Create(triangleCount);

            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_indices_offset, indicesOffset);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_base_index, baseIndex);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_vertices_offset, verticesOffset);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_constants_vertex_stride, (int)vertexStride);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_constants_triangle_count, (int)triangleCount);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_bvh_offset, (int)result.bvhOffset);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_bvh_leaves_offset, (int)result.bvhLeavesOffset);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_internal_node_range_offset, (int)scratchLayout.InternalNodeRange);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_leaf_parents_offset, (int)scratchLayout.LeafParents);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_aabb_offset, (int)scratchLayout.Aabb);

            BindKernelArguments(cmd, kernelInit, vertices, indices, scratch, scratchLayout, result, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelInit, 1, 1, 1);

            BindKernelArguments(cmd, kernelCalculateAabb, vertices, indices, scratch, scratchLayout, result, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelCalculateAabb, (int)Common.CeilDivide(triangleCount, kTrianglesPerGroup), 1, 1);

            BindKernelArguments(cmd, kernelCalculateMortonCodes, vertices, indices, scratch, scratchLayout, result, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelCalculateMortonCodes, (int)Common.CeilDivide(triangleCount, kTrianglesPerGroup), 1, 1);

            radixSort.Execute(cmd, scratch,
                scratchLayout.MortonCodes, scratchLayout.SortedMortonCodes,
                scratchLayout.PrimitiveRefs, scratchLayout.SortedPrimitiveRefs,
                scratchLayout.SortMemory, triangleCount);

            BindKernelArguments(cmd, kernelBuildTreeBottomUp, vertices, indices, scratch, scratchLayout, result, true);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelBuildTreeBottomUp, (int)Common.CeilDivide(triangleCount, kTrianglesPerGroup), 1, 1);
        }

        private void BindKernelArguments(
            CommandBuffer cmd,
            int kernel,
            GraphicsBuffer vertices,
            GraphicsBuffer indices,
            GraphicsBuffer scratch,
            ScratchBufferLayout scratchLayout,
            BottomLevelLevelAccelStruct result,
            bool setSortedCodes)
        {
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_vertices, vertices);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_indices, indices);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_scratch_buffer, scratch);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_bvh, result.bvh);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_bvh_leaves, result.bvhLeaves);

            if (setSortedCodes)
            {
                cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_morton_codes_offset, (int)scratchLayout.SortedMortonCodes);
                cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_primitive_refs_offset, (int)scratchLayout.SortedPrimitiveRefs);
            }
            else
            {
                cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_morton_codes_offset, (int)scratchLayout.MortonCodes);
                cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_primitive_refs_offset, (int)scratchLayout.PrimitiveRefs);
            }
        }

        struct ScratchBufferLayout
        {
            public uint PrimitiveRefs;
            public uint MortonCodes;
            public uint SortedPrimitiveRefs;
            public uint SortedMortonCodes;
            public uint SortMemory;
            public uint Aabb;
            public uint LeafParents;
            public uint InternalNodeRange;
            public uint TotalSize;

            public static ScratchBufferLayout Create(uint triangleCount)
            {
                var result = new ScratchBufferLayout();
                result.SortMemory = result.Reserve(math.max((uint)RadixSort.GetScratchDataSizeInDwords(triangleCount), triangleCount));
                result.PrimitiveRefs = result.Reserve(triangleCount);
                result.MortonCodes = result.Reserve(triangleCount);
                result.SortedPrimitiveRefs = result.Reserve(triangleCount);
                result.SortedMortonCodes = result.Reserve(triangleCount);
                result.Aabb = result.Reserve(6);

                result.InternalNodeRange = result.PrimitiveRefs;
                result.LeafParents = result.SortMemory;

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
