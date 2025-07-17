using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.RadeonRays
{
    internal struct TopLevelAccelStruct : IDisposable
    {
        public const GraphicsBuffer.Target topLevelBvhTarget = GraphicsBuffer.Target.Structured;
        public const GraphicsBuffer.Target instanceInfoTarget = GraphicsBuffer.Target.Structured;

        public GraphicsBuffer topLevelBvh;
        public GraphicsBuffer bottomLevelBvhs;
        public GraphicsBuffer instanceInfos;
        public uint instanceCount;

        public void Dispose()
        {
            topLevelBvh?.Dispose();
            instanceInfos?.Dispose();
        }
    }

    internal class HlbvhTopLevelBuilder
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

        public HlbvhTopLevelBuilder(RadeonRaysShaders shaders)
        {
            shaderBuildHlbvh = shaders.buildHlbvh;
            kernelInit = shaderBuildHlbvh.FindKernel("Init");
            kernelCalculateAabb = shaderBuildHlbvh.FindKernel("CalculateAabb");
            kernelCalculateMortonCodes = shaderBuildHlbvh.FindKernel("CalculateMortonCodes");
            kernelBuildTreeBottomUp = shaderBuildHlbvh.FindKernel("BuildTreeBottomUp");

            radixSort = new RadixSort(shaders);
        }

        public ulong GetScratchDataSizeInDwords(uint instanceCount)
        {
            var scratchLayout = ScratchBufferLayout.Create(instanceCount);
            return scratchLayout.TotalSize;
        }

        public static uint GetBvhNodeCount(uint leafCount)
        {
            return leafCount - 1;
        }

        public void AllocateResultBuffers(uint instanceCount, ref TopLevelAccelStruct accelStruct)
        {
            var bvhNodeCount = GetBvhNodeCount(instanceCount);

            accelStruct.Dispose();
            accelStruct.instanceInfos = new GraphicsBuffer(TopLevelAccelStruct.instanceInfoTarget, (int)instanceCount, Marshal.SizeOf<InstanceInfo>());
            accelStruct.topLevelBvh = new GraphicsBuffer(TopLevelAccelStruct.topLevelBvhTarget, (int)bvhNodeCount + 1, Marshal.SizeOf<BvhNode>()); // plus one for header
        }

        public void CreateEmpty(ref TopLevelAccelStruct accelStruct)
        {
            accelStruct.Dispose();
            accelStruct.topLevelBvh = new GraphicsBuffer(TopLevelAccelStruct.topLevelBvhTarget, 2, Marshal.SizeOf<BvhNode>());
            accelStruct.instanceInfos = accelStruct.topLevelBvh;
            accelStruct.bottomLevelBvhs = accelStruct.topLevelBvh;
            accelStruct.instanceCount = 0;

            var top = new BvhNode[2];
            top[0].child0 = 0;
            top[0].child1 = 0;
            top[0].parent = 0;

            top[1].child0 = 0;
            top[1].child1 = 0;
            top[1].parent = 0xFFFFFFFF;
            top[1].update = 0;
            top[1].aabb0_min = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            top[1].aabb0_max = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            top[1].aabb1_min = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            top[1].aabb1_max = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            accelStruct.topLevelBvh.SetData(top);
        }

        public void Execute(CommandBuffer cmd, GraphicsBuffer scratch, ref TopLevelAccelStruct accelStruct)
        {
            Common.EnableKeyword(cmd, shaderBuildHlbvh, "TOP_LEVEL", true);
            Common.EnableKeyword(cmd, shaderBuildHlbvh, "UINT16_INDICES", false);
            uint instanceCount = accelStruct.instanceCount;
            var scratchLayout = ScratchBufferLayout.Create(instanceCount);

            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_constants_vertex_stride, 0);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_constants_triangle_count, (int)instanceCount);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_bvh_offset, 0);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_internal_node_range_offset, (int)scratchLayout.InternalNodeRange);
            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_aabb_offset, (int)scratchLayout.Aabb);

            BindKernelArguments(cmd, kernelInit, scratch, scratchLayout, accelStruct, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelInit, 1, 1, 1);

            BindKernelArguments(cmd, kernelCalculateAabb, scratch, scratchLayout, accelStruct, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelCalculateAabb, (int)Common.CeilDivide(instanceCount, kTrianglesPerGroup), 1, 1);

            BindKernelArguments(cmd, kernelCalculateMortonCodes, scratch, scratchLayout, accelStruct, false);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelCalculateMortonCodes, (int)Common.CeilDivide(instanceCount, kTrianglesPerGroup), 1, 1);

            radixSort.Execute(cmd, scratch,
                scratchLayout.MortonCodes, scratchLayout.SortedMortonCodes,
                scratchLayout.PrimitiveRefs, scratchLayout.SortedPrimitiveRefs,
                scratchLayout.SortMemory, instanceCount);

            BindKernelArguments(cmd, kernelBuildTreeBottomUp, scratch, scratchLayout, accelStruct, true);
            cmd.DispatchCompute(shaderBuildHlbvh, kernelBuildTreeBottomUp, (int)Common.CeilDivide(instanceCount, kTrianglesPerGroup), 1, 1);
        }

        struct ScratchBufferLayout
        {
            public uint Aabb;
            public uint MortonCodes;
            public uint PrimitiveRefs;
            public uint SortedMortonCodes;
            public uint SortedPrimitiveRefs;
            public uint SortMemory;
            public uint InternalNodeRange;
            public uint TotalSize;

            public static ScratchBufferLayout Create(uint instanceCount)
            {
                var result = new ScratchBufferLayout();
                result.Aabb = result.Reserve(6);
                result.MortonCodes = result.Reserve(instanceCount);
                result.PrimitiveRefs = result.Reserve(instanceCount);
                result.SortedMortonCodes = result.Reserve(instanceCount);
                result.SortedPrimitiveRefs = result.Reserve(instanceCount);
                result.SortMemory = result.Reserve((uint)RadixSort.GetScratchDataSizeInDwords(instanceCount));

                // overlaps with MortonCodes and PrimitiveRefs
                result.InternalNodeRange = result.MortonCodes;

                return result;
            }

            uint Reserve(uint size)
            {
                var temp = TotalSize;
                TotalSize += size;
                return temp;
            }
        }

        private void BindKernelArguments(
            CommandBuffer cmd,
            int kernel,
            GraphicsBuffer scratch,
            ScratchBufferLayout scratchLayout,
            TopLevelAccelStruct accelStruct,
            bool setSortedCodes)
        {
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_scratch_buffer, scratch);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_bvh, accelStruct.topLevelBvh);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_bottom_bvhs, accelStruct.bottomLevelBvhs);
            cmd.SetComputeBufferParam(shaderBuildHlbvh, kernel, SID.g_instance_infos, accelStruct.instanceInfos);

            cmd.SetComputeIntParam(shaderBuildHlbvh, SID.g_aabb_offset, (int)scratchLayout.Aabb);

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
    }
}
