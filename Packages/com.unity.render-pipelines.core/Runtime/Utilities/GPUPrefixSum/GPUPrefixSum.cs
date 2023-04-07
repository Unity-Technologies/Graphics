using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class for computing inclusive or exclusive prefix sums, directly or indirectly dispatched on the GPU.
    /// </summary>
    public partial struct GPUPrefixSum
    {
        private SystemResources resources;

        /// <summary>
        /// Initializes a re-usable GPU prefix sum instance.
        /// </summary>
        /// <param name="resources">The required system resources.</param>
        public GPUPrefixSum(SystemResources resources)
        {
            this.resources = resources;
            this.resources.LoadKernels();
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

        internal void ExecuteCommonIndirect(CommandBuffer cmdBuffer, GraphicsBuffer inputBuffer, in SupportResources supportResources, bool isExclusive)
        {
            int sumOnGroupKernel = isExclusive ? resources.kernelPrefixSumOnGroupExclusive : resources.kernelPrefixSumOnGroup;
            int sumResolveParentKernel = isExclusive ? resources.kernelPrefixSumResolveParentExclusive : resources.kernelPrefixSumResolveParent;

            //hierarchy up
            for (int levelId = 0; levelId < supportResources.maxLevelCount; ++levelId)
            {
                var packedArgs = PackPrefixSumArgs(0, 0, 0, levelId);
                cmdBuffer.SetComputeVectorParam(resources.computeAsset, ShaderIDs._PrefixSumIntArgs, packedArgs);

                if (levelId == 0)
                    cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumOnGroupKernel, ShaderIDs._InputBuffer, inputBuffer);
                else
                    cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumOnGroupKernel, ShaderIDs._InputBuffer, supportResources.prefixBuffer1);

                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumOnGroupKernel, ShaderIDs._TotalLevelsBuffer, supportResources.totalLevelCountBuffer);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumOnGroupKernel, ShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumOnGroupKernel, ShaderIDs._OutputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.DispatchCompute(resources.computeAsset, sumOnGroupKernel, supportResources.indirectDispatchArgsBuffer, (uint)(levelId * 6 * 4));

                if (levelId == supportResources.maxLevelCount - 1)
                    continue;

                cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelPrefixSumNextInput, ShaderIDs._InputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelPrefixSumNextInput, ShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelPrefixSumNextInput, ShaderIDs._OutputBuffer, supportResources.prefixBuffer1);
                cmdBuffer.DispatchCompute(resources.computeAsset, resources.kernelPrefixSumNextInput, supportResources.indirectDispatchArgsBuffer, (uint)((levelId + 1) * 6 * 4));
            }

            //down the hierarchy
            for (int levelId = supportResources.maxLevelCount - 1; levelId >= 1; --levelId)
            {
                var packedArgs = PackPrefixSumArgs(0, 0, 0, levelId);
                cmdBuffer.SetComputeVectorParam(resources.computeAsset, ShaderIDs._PrefixSumIntArgs, packedArgs);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumResolveParentKernel, ShaderIDs._InputBuffer, inputBuffer);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumResolveParentKernel, ShaderIDs._OutputBuffer, supportResources.prefixBuffer0);
                cmdBuffer.SetComputeBufferParam(resources.computeAsset, sumResolveParentKernel, ShaderIDs._LevelsOffsetsBuffer, supportResources.levelOffsetBuffer);
                cmdBuffer.DispatchCompute(resources.computeAsset, sumResolveParentKernel, supportResources.indirectDispatchArgsBuffer, (uint)(((levelId - 1) * 6 + 3) * 4));
            }
        }

        /// <summary>
        /// Prefix sum a list of data from a CPU-defined count.
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer for recording the prefix sum commands.</param>
        /// <param name="arguments">Runtime arguments for the prefix sum.</param>
        /// <exception cref="Exception">When the input data is invalid.</exception>
        public void DispatchDirect(CommandBuffer cmdBuffer, in DirectArgs arguments)
        {
            if (arguments.supportResources.prefixBuffer0 == null || arguments.supportResources.prefixBuffer1 == null)
                throw new Exception("Support resources are not valid.");

            if (arguments.input == null)
                throw new Exception("Input source buffer cannot be null.");

            if (arguments.inputCount > arguments.supportResources.alignedElementCount)
                throw new Exception("Input count exceeds maximum count of support resources. Ensure to create support resources with enough space.");

            //Generate level offsets first, from const value.
            var packedArgs = PackPrefixSumArgs(arguments.inputCount, arguments.supportResources.maxLevelCount, 0, 0);
            cmdBuffer.SetComputeVectorParam(resources.computeAsset, ShaderIDs._PrefixSumIntArgs, packedArgs);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromConst, ShaderIDs._OutputLevelsOffsetsBuffer, arguments.supportResources.levelOffsetBuffer);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromConst, ShaderIDs._OutputDispatchLevelArgsBuffer, arguments.supportResources.indirectDispatchArgsBuffer);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromConst, ShaderIDs._OutputTotalLevelsBuffer, arguments.supportResources.totalLevelCountBuffer);
            cmdBuffer.DispatchCompute(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromConst, 1, 1, 1);

            ExecuteCommonIndirect(cmdBuffer, arguments.input, arguments.supportResources, arguments.exclusive);
        }

        /// <summary>
        /// Prefix sum a list of data from a GPU-defined count.
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer for recording the prefix sum commands.</param>
        /// <param name="arguments">Runtime arguments for the prefix sum.</param>
        /// <exception cref="Exception">When the input data is invalid.</exception>
        public void DispatchIndirect(CommandBuffer cmdBuffer, in IndirectDirectArgs arguments)
        {
            if (arguments.supportResources.prefixBuffer0 == null || arguments.supportResources.prefixBuffer1 == null)
                throw new Exception("Support resources are not valid.");

            if (arguments.input == null || arguments.inputCountBuffer == null)
                throw new Exception("Input source buffer and inputCountBuffer cannot be null.");

            //Generate level offsets first, from const value.
            var packedArgs = PackPrefixSumArgs(0, arguments.supportResources.maxLevelCount, arguments.inputCountBufferByteOffset, 0);
            cmdBuffer.SetComputeVectorParam(resources.computeAsset, ShaderIDs._PrefixSumIntArgs, packedArgs);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromBuffer, ShaderIDs._InputCountBuffer, arguments.inputCountBuffer);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromBuffer, ShaderIDs._OutputLevelsOffsetsBuffer, arguments.supportResources.levelOffsetBuffer);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromBuffer, ShaderIDs._OutputDispatchLevelArgsBuffer, arguments.supportResources.indirectDispatchArgsBuffer);
            cmdBuffer.SetComputeBufferParam(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromBuffer, ShaderIDs._OutputTotalLevelsBuffer, arguments.supportResources.totalLevelCountBuffer);
            cmdBuffer.DispatchCompute(resources.computeAsset, resources.kernelCalculateLevelDispatchArgsFromBuffer, 1, 1, 1);

            ExecuteCommonIndirect(cmdBuffer, arguments.input, arguments.supportResources, arguments.exclusive);
        }
    }
}
