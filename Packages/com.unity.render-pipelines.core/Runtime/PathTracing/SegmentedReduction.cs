using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Core
{
    internal class SegmentedReduction
    {
        private readonly ComputeShader _segmentedReductionShader;
        private readonly int _reductionKernel;
        private readonly uint _threadGroupSize;

        private static class ShaderProperties
        {
            public static readonly int SegmentWidth = Shader.PropertyToID("g_SegmentWidth");
            public static readonly int SegmentStride = Shader.PropertyToID("g_SegmentStride");
            public static readonly int SegmentCount = Shader.PropertyToID("g_SegmentCount");
            public static readonly int InputOffset = Shader.PropertyToID("g_InputOffset");
            public static readonly int OutputOffset = Shader.PropertyToID("g_OutputOffset");
            public static readonly int OverwriteOutput = Shader.PropertyToID("g_OverwriteOutput");
            public static readonly int TruncateInterval = Shader.PropertyToID("g_TruncateInterval");
            public static readonly int TruncatedSegmentWidth = Shader.PropertyToID("g_TruncatedSegmentWidth");
            public static readonly int InputFloatBuffer = Shader.PropertyToID("g_InputFloatBuffer");
            public static readonly int OutputFloatBuffer = Shader.PropertyToID("g_OutputFloatBuffer");
        }

        public SegmentedReduction(ComputeShader segmentedReductionShader)
        {
            _segmentedReductionShader = segmentedReductionShader;
            _reductionKernel = _segmentedReductionShader.FindKernel("SegmentedReductionFloat");
            _segmentedReductionShader.GetKernelThreadGroupSizes(_reductionKernel, out _threadGroupSize, out _, out _);
        }

#if UNITY_EDITOR
        public static ComputeShader LoadShader()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/SegmentedReduction.compute");
        }
#endif

        private static void CalculateParametersForTwoPassReduction(
            uint segmentWidth,
            uint segmentCount,
            out uint firstPassSegmentCount,
            out uint firstPassSegmentWidth,
            out uint truncateInterval,
            out uint truncatedSegmentWidth,
            out uint secondPassSegmentCount,
            out uint secondPassSegmentWidth)
        {
            // Split each segments into a number of segments with size close to sqrt(segmentWidth).
            firstPassSegmentWidth = (uint)Mathf.Floor(Mathf.Sqrt(segmentWidth));

            // Splitting may result in a truncated segment at the end of each original segment.
            // Calculate how often this happen, how wide the truncated segment will be.
            truncateInterval = GraphicsHelpers.DivUp(segmentWidth, firstPassSegmentWidth);
            truncatedSegmentWidth = segmentWidth % firstPassSegmentWidth;

            // Calculate total number of segments after splitting.
            firstPassSegmentCount = truncateInterval * segmentCount;

            // The second pass reduces the output of the first pass.
            // The first pass will produce segmentCount segments of width truncateInterval.
            secondPassSegmentCount = segmentCount;
            secondPassSegmentWidth = truncateInterval;
        }

        public static uint GetScratchBufferSizeInDwords(uint segmentWidth, uint segmentStride, uint segmentCount)
        {
            CalculateParametersForTwoPassReduction(segmentWidth, segmentCount, out uint firstPassSegmentCount, out _, out _, out _, out _, out _);
            return firstPassSegmentCount * segmentStride;
        }

        // Performs segmented reduction in 2 passes, each doing roughly sqrt(N) reductions.
        // This is almost always faster than single pass reduction, and should be preferred over it.
        public void TwoPassSegmentedReduction(
            CommandBuffer cmd,
            uint segmentWidth,
            uint segmentStride,
            uint segmentCount,
            uint inputOffset,
            uint outputOffset,
            GraphicsBuffer inputBuffer,
            GraphicsBuffer scratchBuffer,
            GraphicsBuffer outputBuffer,
            bool overwriteOutput)
        {
            CalculateParametersForTwoPassReduction(
                segmentWidth,
                segmentCount,
                out uint firstPassSegmentCount,
                out uint firstPassSegmentWidth,
                out uint truncateInterval,
                out uint truncatedSegmentWidth,
                out uint secondPassSegmentCount,
                out uint secondPassSegmentWidth);

            DispatchReductionKernel(cmd, firstPassSegmentWidth, segmentStride, firstPassSegmentCount, inputOffset, 0, inputBuffer, scratchBuffer, true, truncateInterval, truncatedSegmentWidth);
            DispatchReductionKernel(cmd, secondPassSegmentWidth, segmentStride, secondPassSegmentCount, inputOffset, outputOffset, scratchBuffer, outputBuffer, overwriteOutput, 0, 0);
        }

        // Performs segmented reduction in a single pass. This is slower than the 2 pass
        // variant in most cases, except for the case where each individual reduction is very small.
        // This function is mostly intended for small problem sizes and for debugging.
        public void SinglePassSegmentedReduction(
            CommandBuffer cmd,
            uint segmentWidth,
            uint segmentStride,
            uint segmentCount,
            uint inputOffset,
            uint outputOffset,
            GraphicsBuffer inputBuffer,
            GraphicsBuffer outputBuffer,
            bool overwriteOutput)
        {
            DispatchReductionKernel(cmd, segmentWidth, segmentStride, segmentCount, inputOffset, outputOffset, inputBuffer, outputBuffer, overwriteOutput, 0, 0);
        }

        private void DispatchReductionKernel(
            CommandBuffer cmd,
            uint segmentWidth,
            uint segmentStride,
            uint segmentCount,
            uint inputOffset,
            uint outputOffset,
            GraphicsBuffer inputBuffer,
            GraphicsBuffer outputBuffer,
            bool overwriteOutput,
            uint truncateInterval,
            uint truncatedSegmentWidth)
        {
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.SegmentWidth, (int)segmentWidth);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.SegmentStride, (int)segmentStride);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.SegmentCount, (int)segmentCount);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.InputOffset, (int)inputOffset);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.OutputOffset, (int)outputOffset);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.OverwriteOutput, overwriteOutput ? 1 : 0);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.TruncateInterval, (int)truncateInterval);
            cmd.SetComputeIntParam(_segmentedReductionShader, ShaderProperties.TruncatedSegmentWidth, (int)truncatedSegmentWidth);
            cmd.SetComputeBufferParam(_segmentedReductionShader, _reductionKernel, ShaderProperties.InputFloatBuffer, inputBuffer);
            cmd.SetComputeBufferParam(_segmentedReductionShader, _reductionKernel, ShaderProperties.OutputFloatBuffer, outputBuffer);
            cmd.DispatchCompute(_segmentedReductionShader, _reductionKernel, (int)GraphicsHelpers.DivUp(segmentCount, _threadGroupSize), 1, 1);
        }
    }
}
