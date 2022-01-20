using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal static class GpuPrefixSumDefs
    {
        public const int GroupSize = 128;

        public static int DivUpGroup(int value)
        {
            return (value + GroupSize - 1) / GroupSize;
        }

        public static int AlignUpGroup(int value)
        {
            return DivUpGroup(value) * GroupSize;
        }

        public static void CalculateTotalBufferSize(int maxElementCount, out int totalSize, out int levelCounts)
        {
            int alignedSupportMaxCount = GpuPrefixSumDefs.AlignUpGroup(maxElementCount);
            totalSize = alignedSupportMaxCount;
            levelCounts = 1;
            while (alignedSupportMaxCount > GpuPrefixSumDefs.GroupSize)
            {
                alignedSupportMaxCount = GpuPrefixSumDefs.AlignUpGroup(GpuPrefixSumDefs.DivUpGroup(alignedSupportMaxCount));
                totalSize += alignedSupportMaxCount;
                ++levelCounts;
            }
        }

    }

    [GenerateHLSL(PackingRules.Exact, false, false)]
    internal struct GpuPrefixSumLevelOffsets
    {
        public uint count;
        public uint offset;
        public uint parentOffset;
    }

    internal struct GpuPrefixSumRenderGraphResources
    {
        public int alignedElementCount;
        public int maxBufferCount;
        public int maxLevelCount;
        public ComputeBufferHandle prefixBuffer0;
        public ComputeBufferHandle prefixBuffer1;
        public ComputeBufferHandle totalLevelCountBuffer;
        public ComputeBufferHandle levelOffsetBuffer;
        public ComputeBufferHandle indirectDispatchArgsBuffer;

        public ComputeBufferHandle output => prefixBuffer0;

        public static GpuPrefixSumRenderGraphResources Create(int newMaxElementCount, RenderGraph renderGraph, RenderGraphBuilder builder, bool outputIsTemp = false)
        {
            var resources = new GpuPrefixSumRenderGraphResources();
            resources.Initialize(newMaxElementCount, renderGraph, builder, outputIsTemp);
            return resources;
        }

        void Initialize(int newMaxElementCount, RenderGraph renderGraph, RenderGraphBuilder builder, bool outputIsTemp = false)
        {
            newMaxElementCount = Math.Max(newMaxElementCount, 1);
            GpuPrefixSumDefs.CalculateTotalBufferSize(newMaxElementCount, out int totalSize, out int levelCounts);

            var prefixBuffer0Desc = new ComputeBufferDesc(totalSize, 4, ComputeBufferType.Raw) { name = "prefixBuffer0" };
            prefixBuffer0 = outputIsTemp ? builder.CreateTransientComputeBuffer(prefixBuffer0Desc) : builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(prefixBuffer0Desc));
            prefixBuffer1 = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(newMaxElementCount, 4, ComputeBufferType.Raw) { name = "prefixBuffer1" });
            totalLevelCountBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(1, 4, ComputeBufferType.Raw) { name = "totalLevelCountBuffer" });
            levelOffsetBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(levelCounts, System.Runtime.InteropServices.Marshal.SizeOf<GpuPrefixSumLevelOffsets>(), ComputeBufferType.Structured) { name = "levelOffsetBuffer" });
            indirectDispatchArgsBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(levelCounts, 4 * 6, ComputeBufferType.Structured | ComputeBufferType.IndirectArguments) { name = "indirectDispatchArgsBuffer" });//3 arguments for upp dispatch, 3 arguments for lower dispatch
            alignedElementCount = GpuPrefixSumDefs.AlignUpGroup(newMaxElementCount);
            maxBufferCount = totalSize;
            maxLevelCount = levelCounts;
        }
    }

    internal struct GpuPrefixSumSupportResources
    {
        public bool ownsResources;
        public int alignedElementCount;
        public int maxBufferCount;
        public int maxLevelCount;
        public ComputeBuffer prefixBuffer0;
        public ComputeBuffer prefixBuffer1;
        public ComputeBuffer totalLevelCountBuffer;
        public ComputeBuffer levelOffsetBuffer;
        public ComputeBuffer indirectDispatchArgsBuffer;
        public ComputeBuffer output => prefixBuffer0;

        public static GpuPrefixSumSupportResources Create(int maxElementCount)
        {
            var resources = new GpuPrefixSumSupportResources() { alignedElementCount = 0, ownsResources = true };
            resources.Resize(maxElementCount);
            return resources;
        }

        public static GpuPrefixSumSupportResources Load(GpuPrefixSumRenderGraphResources shaderGraphResources)
        {
            var resources = new GpuPrefixSumSupportResources() { alignedElementCount = 0, ownsResources = false };
            resources.LoadFromShaderGraph(shaderGraphResources);
            return resources;
        }

        public void Resize(int newMaxElementCount)
        {
            if (!ownsResources)
                throw new Exception("Cannot resize resources unless they are owned. Use GpuPrefixSumSupportResources.Create() for this.");

            newMaxElementCount = Math.Max(newMaxElementCount, 1); //at bare minimum support a single group.
            if (alignedElementCount >= newMaxElementCount)
                return;

            Dispose();
            GpuPrefixSumDefs.CalculateTotalBufferSize(newMaxElementCount, out int totalSize, out int levelCounts);
            prefixBuffer0 = new ComputeBuffer(totalSize, 4, ComputeBufferType.Raw);
            prefixBuffer1 = new ComputeBuffer(newMaxElementCount, 4, ComputeBufferType.Raw);
            totalLevelCountBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Raw);
            levelOffsetBuffer = new ComputeBuffer(levelCounts, System.Runtime.InteropServices.Marshal.SizeOf<GpuPrefixSumLevelOffsets>(), ComputeBufferType.Structured);
            indirectDispatchArgsBuffer = new ComputeBuffer(levelCounts, 4 * 6, ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);//3 arguments for upp dispatch, 3 arguments for lower dispatch
            alignedElementCount = GpuPrefixSumDefs.AlignUpGroup(newMaxElementCount);
            maxBufferCount = totalSize;
            maxLevelCount = levelCounts;
        }

        void LoadFromShaderGraph(GpuPrefixSumRenderGraphResources shaderGraphResources)
        {
            alignedElementCount = shaderGraphResources.alignedElementCount;
            maxBufferCount = shaderGraphResources.maxBufferCount;
            maxLevelCount = shaderGraphResources.maxLevelCount;
            prefixBuffer0 = (ComputeBuffer)shaderGraphResources.prefixBuffer0;
            prefixBuffer1 = (ComputeBuffer)shaderGraphResources.prefixBuffer1;
            totalLevelCountBuffer = (ComputeBuffer)shaderGraphResources.totalLevelCountBuffer;
            levelOffsetBuffer = (ComputeBuffer)shaderGraphResources.levelOffsetBuffer;
            indirectDispatchArgsBuffer = (ComputeBuffer)shaderGraphResources.indirectDispatchArgsBuffer;
        }

        public void Dispose()
        {
            if (alignedElementCount == 0 || !ownsResources)
                return;

            alignedElementCount = 0;
            if (prefixBuffer0 != null)
            {
                prefixBuffer0.Dispose();
                prefixBuffer0 = null;
            }

            if (prefixBuffer1 != null)
            {
                prefixBuffer1.Dispose();
                prefixBuffer1 = null;
            }

            if (levelOffsetBuffer != null)
            {
                levelOffsetBuffer.Dispose();
                levelOffsetBuffer = null;
            }

            if (indirectDispatchArgsBuffer != null)
            {
                indirectDispatchArgsBuffer.Dispose();
                indirectDispatchArgsBuffer = null;
            }

            if (totalLevelCountBuffer != null)
            {
                totalLevelCountBuffer.Dispose();
                totalLevelCountBuffer = null;
            }
        }
    }

    internal struct GpuPrefixSumDirectArgs
    {
        public bool exclusive;
        public int inputCount;
        public ComputeBuffer input;
        public GpuPrefixSumSupportResources supportResources;
    }

    internal struct GpuPrefixSumIndirectDirectArgs
    {
        public bool exclusive;
        public int inputCountBufferByteOffset;
        public ComputeBuffer inputCountBuffer;
        public ComputeBuffer input;
        public GpuPrefixSumSupportResources supportResources;
    }

    internal struct GpuPrefixSum
    {
        ComputeShader m_PrefixSumCS;
        private int m_KernelMainCalculateLevelDispatchArgsFromConst;
        private int m_KernelMainCalculateLevelDispatchArgsFromBuffer;
        private int m_KernelMainPrefixSumOnGroup;
        private int m_KernelMainPrefixSumOnGroupExclusive;
        private int m_KernelMainPrefixSumNextInput;
        private int m_KernelMainPrefixSumResolveParent;
        private int m_KernelMainPrefixSumResolveParentExclusive;

        private void LoadShaders(HDRenderPipelineRuntimeResources resources)
        {
            m_PrefixSumCS = resources.shaders.gpuPrefixSumCS;
            m_KernelMainCalculateLevelDispatchArgsFromConst = m_PrefixSumCS.FindKernel("MainCalculateLevelDispatchArgsFromConst");
            m_KernelMainCalculateLevelDispatchArgsFromBuffer = m_PrefixSumCS.FindKernel("MainCalculateLevelDispatchArgsFromBuffer");
            m_KernelMainPrefixSumOnGroup = m_PrefixSumCS.FindKernel("MainPrefixSumOnGroup");
            m_KernelMainPrefixSumOnGroupExclusive = m_PrefixSumCS.FindKernel("MainPrefixSumOnGroupExclusive");
            m_KernelMainPrefixSumNextInput = m_PrefixSumCS.FindKernel("MainPrefixSumNextInput");
            m_KernelMainPrefixSumResolveParent = m_PrefixSumCS.FindKernel("MainPrefixSumResolveParent");
            m_KernelMainPrefixSumResolveParentExclusive = m_PrefixSumCS.FindKernel("MainPrefixSumResolveParentExclusive");
        }

        Vector4 PackPrefixSumArgs(int a, int b, int c, int d)
        {
            unsafe
            {
                return new Vector4(
                    *((float*)&a),
                    *((float*)&b),
                    *((float*)&c),
                    *((float*)&d));
            }
        }

        private void ExecuteCommonIndirect(CommandBuffer cmdBuffer, ComputeBuffer inputBuffer, in GpuPrefixSumSupportResources supportResources, bool isExclusive)
        {
            int sumOnGroupKernel = isExclusive ? m_KernelMainPrefixSumOnGroupExclusive : m_KernelMainPrefixSumOnGroup;
            int sumResolveParentKernel = isExclusive ? m_KernelMainPrefixSumResolveParentExclusive : m_KernelMainPrefixSumResolveParent;

            //hierarchy up
            for (int levelId = 0; levelId < supportResources.maxLevelCount; ++levelId)
            {
                var packedArgs = PackPrefixSumArgs(0, 0, 0, levelId);
                cmdBuffer.SetComputeVectorParam(m_PrefixSumCS, HDShaderIDs._PrefixSumIntArgs, packedArgs);

                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumOnGroupKernel, HDShaderIDs._InputBuffer, levelId == 0 ? inputBuffer : supportResources.prefixBuffer1);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumOnGroupKernel, HDShaderIDs._TotalLevelsBuffer, supportResources.totalLevelCountBuffer);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumOnGroupKernel, HDShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumOnGroupKernel, HDShaderIDs._OutputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.DispatchCompute(m_PrefixSumCS, sumOnGroupKernel, supportResources.indirectDispatchArgsBuffer, (uint)(levelId * 6 * 4));

                if (levelId == supportResources.maxLevelCount - 1)
                    continue;

                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainPrefixSumNextInput, HDShaderIDs._InputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainPrefixSumNextInput, HDShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainPrefixSumNextInput, HDShaderIDs._OutputBuffer, supportResources.prefixBuffer1);
                cmdBuffer.DispatchCompute(m_PrefixSumCS, m_KernelMainPrefixSumNextInput, supportResources.indirectDispatchArgsBuffer, (uint)((levelId + 1) * 6 * 4));
            }

            //down the hierarchy
            for (int levelId = supportResources.maxLevelCount - 1; levelId >= 1; --levelId)
            {
                var packedArgs = PackPrefixSumArgs(0, 0, 0, levelId);
                cmdBuffer.SetComputeVectorParam(m_PrefixSumCS, HDShaderIDs._PrefixSumIntArgs, packedArgs);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumResolveParentKernel, HDShaderIDs._InputBuffer, inputBuffer);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumResolveParentKernel, HDShaderIDs._OutputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, sumResolveParentKernel, HDShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.DispatchCompute(m_PrefixSumCS, sumResolveParentKernel, supportResources.indirectDispatchArgsBuffer, (uint)(((levelId - 1) * 6 + 3) * 4));
            }
        }

        public void DispatchDirect(CommandBuffer cmdBuffer, in GpuPrefixSumDirectArgs arguments)
        {
            if (arguments.supportResources.prefixBuffer0 == null || arguments.supportResources.prefixBuffer1 == null)
                throw new Exception("Support resources are not valid.");

            if (arguments.input == null)
                throw new Exception("Input source buffer cannot be null.");

            if (arguments.inputCount > arguments.supportResources.alignedElementCount)
                throw new Exception("Input count exceeds maximum count of support resources. Ensure to create support resources with enough space.");

            //Generate level offsets first, from const value.
            var packedArgs = PackPrefixSumArgs(arguments.inputCount, arguments.supportResources.maxLevelCount, 0, 0);
            cmdBuffer.SetComputeVectorParam(m_PrefixSumCS, HDShaderIDs._PrefixSumIntArgs, packedArgs);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromConst, HDShaderIDs._OutputLevelsOffsetsBuffer, arguments.supportResources.levelOffsetBuffer);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromConst, HDShaderIDs._OutputDispatchLevelArgsBuffer, arguments.supportResources.indirectDispatchArgsBuffer);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromConst, HDShaderIDs._OutputTotalLevelsBuffer, arguments.supportResources.totalLevelCountBuffer);
            cmdBuffer.DispatchCompute(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromConst, 1, 1, 1);

            ExecuteCommonIndirect(cmdBuffer, arguments.input, arguments.supportResources, arguments.exclusive);
        }

        internal void DispatchIndirect(CommandBuffer cmdBuffer, in GpuPrefixSumIndirectDirectArgs arguments)
        {
            if (arguments.supportResources.prefixBuffer0 == null || arguments.supportResources.prefixBuffer1 == null)
                throw new Exception("Support resources are not valid.");

            if (arguments.input == null || arguments.inputCountBuffer == null)
                throw new Exception("Input source buffer and inputCountBuffer cannot be null.");

            //Generate level offsets first, from const value.
            var packedArgs = PackPrefixSumArgs(0, arguments.supportResources.maxLevelCount, arguments.inputCountBufferByteOffset, 0);
            cmdBuffer.SetComputeVectorParam(m_PrefixSumCS, HDShaderIDs._PrefixSumIntArgs, packedArgs);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromBuffer, HDShaderIDs._InputCountBuffer, arguments.inputCountBuffer);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromBuffer, HDShaderIDs._OutputLevelsOffsetsBuffer, arguments.supportResources.levelOffsetBuffer);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromBuffer, HDShaderIDs._OutputDispatchLevelArgsBuffer, arguments.supportResources.indirectDispatchArgsBuffer);
            cmdBuffer.SetComputeBufferParam(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromBuffer, HDShaderIDs._OutputTotalLevelsBuffer, arguments.supportResources.totalLevelCountBuffer);
            cmdBuffer.DispatchCompute(m_PrefixSumCS, m_KernelMainCalculateLevelDispatchArgsFromBuffer, 1, 1, 1);

            ExecuteCommonIndirect(cmdBuffer, arguments.input, arguments.supportResources, arguments.exclusive);
        }

        internal void Initialize(HDRenderPipelineRuntimeResources defaultResources)
        {
            LoadShaders(defaultResources);
        }

        internal void Dispose()
        {
            m_PrefixSumCS = null;
        }
    }
}
