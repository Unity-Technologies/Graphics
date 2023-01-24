using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    public partial struct GPUPrefixSum
    {
        [GenerateHLSL]
        internal static class ShaderDefs
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
                int alignedSupportMaxCount = AlignUpGroup(maxElementCount);
                totalSize = alignedSupportMaxCount;
                levelCounts = 1;
                while (alignedSupportMaxCount > GroupSize)
                {
                    alignedSupportMaxCount = AlignUpGroup(DivUpGroup(alignedSupportMaxCount));
                    totalSize += alignedSupportMaxCount;
                    ++levelCounts;
                }
            }
        }

        [GenerateHLSL(PackingRules.Exact, false, false)]
        internal struct LevelOffsets
        {
            internal uint count;
            internal uint offset;
            internal uint parentOffset;
        }

        /// <summary>
        /// Utility for adapting to render graph usage.
        /// </summary>
        public struct RenderGraphResources
        {
            internal int alignedElementCount;
            internal int maxBufferCount;
            internal int maxLevelCount;

            internal BufferHandle prefixBuffer0;
            internal BufferHandle prefixBuffer1;
            internal BufferHandle totalLevelCountBuffer;
            internal BufferHandle levelOffsetBuffer;
            internal BufferHandle indirectDispatchArgsBuffer;

            /// <summary>The prefix sum result.</summary>
            public BufferHandle output => prefixBuffer0;

            /// <summary>
            /// Creates the render graph buffer resources from an input count.
            /// </summary>
            /// <param name="newMaxElementCount"></param>
            /// <param name="renderGraph">Render Graph</param>
            /// <param name="builder">Render Graph Builder</param>
            /// <param name="outputIsTemp">Whether or not to allocate a transient resource.</param>
            /// <returns>The created Render Graph Resources.</returns>
            public static RenderGraphResources Create(int newMaxElementCount, RenderGraph renderGraph, RenderGraphBuilder builder, bool outputIsTemp = false)
            {
                var resources = new RenderGraphResources();
                resources.Initialize(newMaxElementCount, renderGraph, builder, outputIsTemp);
                return resources;
            }

            void Initialize(int newMaxElementCount, RenderGraph renderGraph, RenderGraphBuilder builder, bool outputIsTemp = false)
            {
                newMaxElementCount = Math.Max(newMaxElementCount, 1);
                ShaderDefs.CalculateTotalBufferSize(newMaxElementCount, out int totalSize, out int levelCounts);

                var prefixBuffer0Desc = new BufferDesc(totalSize, 4, GraphicsBuffer.Target.Raw) { name = "prefixBuffer0" };
                prefixBuffer0 = outputIsTemp ? builder.CreateTransientBuffer(prefixBuffer0Desc) : builder.WriteBuffer(renderGraph.CreateBuffer(prefixBuffer0Desc));
                prefixBuffer1 = builder.CreateTransientBuffer(new BufferDesc(newMaxElementCount, 4, GraphicsBuffer.Target.Raw) { name = "prefixBuffer1" });
                totalLevelCountBuffer = builder.CreateTransientBuffer(new BufferDesc(1, 4, GraphicsBuffer.Target.Raw) { name = "totalLevelCountBuffer" });
                levelOffsetBuffer = builder.CreateTransientBuffer(new BufferDesc(levelCounts, System.Runtime.InteropServices.Marshal.SizeOf<LevelOffsets>(), GraphicsBuffer.Target.Structured) { name = "levelOffsetBuffer" });
                indirectDispatchArgsBuffer = builder.CreateTransientBuffer(new BufferDesc(6 * levelCounts, sizeof(uint), GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.IndirectArguments) { name = "indirectDispatchArgsBuffer" });//3 arguments for upp dispatch, 3 arguments for lower dispatch
                alignedElementCount = ShaderDefs.AlignUpGroup(newMaxElementCount);
                maxBufferCount = totalSize;
                maxLevelCount = levelCounts;
            }
        }

        /// <summary>
        /// Data structure containing the runtime resources that are bound by the command buffer.
        /// </summary>
        public struct SupportResources
        {
            internal bool ownsResources;
            internal int  alignedElementCount;
            internal int  maxBufferCount;
            internal int  maxLevelCount;

            internal GraphicsBuffer prefixBuffer0;
            internal GraphicsBuffer prefixBuffer1;
            internal GraphicsBuffer totalLevelCountBuffer;
            internal GraphicsBuffer levelOffsetBuffer;
            internal GraphicsBuffer indirectDispatchArgsBuffer;

            /// <summary>The prefix sum result.</summary>
            public GraphicsBuffer output => prefixBuffer0;

            /// <summary>
            /// Allocate support resources to accomodate a max count.
            /// </summary>
            /// <param name="maxElementCount">The max element count.</param>
            /// <returns>The created support resources.</returns>
            public static SupportResources Create(int maxElementCount)
            {
                var resources = new SupportResources() { alignedElementCount = 0, ownsResources = true };
                resources.Resize(maxElementCount);
                return resources;
            }

            /// <summary>
            /// Load supporting resources from Render Graph Resources.
            /// </summary>
            /// <param name="shaderGraphResources">Render Graph Resources</param>
            /// <returns>The created support resources.</returns>
            public static SupportResources Load(RenderGraphResources shaderGraphResources)
            {
                var resources = new SupportResources() { alignedElementCount = 0, ownsResources = false };
                resources.LoadFromShaderGraph(shaderGraphResources);
                return resources;
            }

            internal void Resize(int newMaxElementCount)
            {
                if (!ownsResources)
                    throw new Exception("Cannot resize resources unless they are owned. Use GpuPrefixSumSupportResources.Create() for this.");

                newMaxElementCount = Math.Max(newMaxElementCount, 1); //at bare minimum support a single group.
                if (alignedElementCount >= newMaxElementCount)
                    return;

                Dispose();
                ShaderDefs.CalculateTotalBufferSize(newMaxElementCount, out int totalSize, out int levelCounts);

                alignedElementCount = ShaderDefs.AlignUpGroup(newMaxElementCount);
                maxBufferCount      = totalSize;
                maxLevelCount       = levelCounts;

                prefixBuffer0              = new GraphicsBuffer(GraphicsBuffer.Target.Raw, totalSize, 4);
                prefixBuffer1              = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newMaxElementCount, 4);
                totalLevelCountBuffer      = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, 4);
                levelOffsetBuffer          = new GraphicsBuffer(GraphicsBuffer.Target.Structured, levelCounts, System.Runtime.InteropServices.Marshal.SizeOf<LevelOffsets>());
                indirectDispatchArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 6 * levelCounts, sizeof(uint));//3 arguments for upp dispatch, 3 arguments for lower dispatch
            }

            void LoadFromShaderGraph(RenderGraphResources shaderGraphResources)
            {
                alignedElementCount = shaderGraphResources.alignedElementCount;
                maxBufferCount      = shaderGraphResources.maxBufferCount;
                maxLevelCount       = shaderGraphResources.maxLevelCount;

                prefixBuffer0              = (GraphicsBuffer)shaderGraphResources.prefixBuffer0;
                prefixBuffer1              = (GraphicsBuffer)shaderGraphResources.prefixBuffer1;
                totalLevelCountBuffer      = (GraphicsBuffer)shaderGraphResources.totalLevelCountBuffer;
                levelOffsetBuffer          = (GraphicsBuffer)shaderGraphResources.levelOffsetBuffer;
                indirectDispatchArgsBuffer = (GraphicsBuffer)shaderGraphResources.indirectDispatchArgsBuffer;
            }

            /// <summary>
            /// Dispose the supporting resources.
            /// </summary>
            public void Dispose()
            {
                if (alignedElementCount == 0 || !ownsResources)
                    return;

                alignedElementCount = 0;

                void TryFreeBuffer(GraphicsBuffer resource)
                {
                    if (resource != null)
                    {
                        resource.Dispose();
                        resource = null;
                    }
                }

                TryFreeBuffer(prefixBuffer0);
                TryFreeBuffer(prefixBuffer1);
                TryFreeBuffer(levelOffsetBuffer);
                TryFreeBuffer(indirectDispatchArgsBuffer);
                TryFreeBuffer(totalLevelCountBuffer);
            }
        }

        /// <summary>
        /// Arguments for a direct prefix sum.
        /// </summary>
        public struct DirectArgs
        {
            /// <summary>An inclusive or exclusive prefix sum.</summary>
            public bool             exclusive;
            /// <summary>The size of the input list.</summary>
            public int              inputCount;
            /// <summary>The input list.</summary>
            public GraphicsBuffer   input;
            /// <summary>Required runtime resources.</summary>
            public SupportResources supportResources;
        }

        /// <summary>
        /// Arguments for an indirect prefix sum.
        /// </summary>
        public struct IndirectDirectArgs
        {
            /// <summary>An inclusive or exclusive prefix sum.</summary>
            public bool             exclusive;
            /// <summary>Byte offset of the count inside the input count buffer.</summary>
            public int              inputCountBufferByteOffset;
            /// <summary>GPU buffer defining the size of the input list.</summary>
            public ComputeBuffer    inputCountBuffer;
            /// <summary>The input list.</summary>
            public GraphicsBuffer   input;
            /// <summary>Required runtime resources.</summary>
            public SupportResources supportResources;
        }

        /// <summary>
        /// Structure defining any required assets used by the GPU sort.
        /// </summary>
        public struct SystemResources
        {
            /// <summary>
            /// The compute asset that defines all of the kernels for the GPU prefix sum.
            /// </summary>
            public ComputeShader computeAsset;

            internal int kernelCalculateLevelDispatchArgsFromConst;
            internal int kernelCalculateLevelDispatchArgsFromBuffer;
            internal int kernelPrefixSumOnGroup;
            internal int kernelPrefixSumOnGroupExclusive;
            internal int kernelPrefixSumNextInput;
            internal int kernelPrefixSumResolveParent;
            internal int kernelPrefixSumResolveParentExclusive;

            internal void LoadKernels()
            {
                if (computeAsset == null)
                    return;

                kernelCalculateLevelDispatchArgsFromConst  = computeAsset.FindKernel("MainCalculateLevelDispatchArgsFromConst");
                kernelCalculateLevelDispatchArgsFromBuffer = computeAsset.FindKernel("MainCalculateLevelDispatchArgsFromBuffer");
                kernelPrefixSumOnGroup                     = computeAsset.FindKernel("MainPrefixSumOnGroup");
                kernelPrefixSumOnGroupExclusive            = computeAsset.FindKernel("MainPrefixSumOnGroupExclusive");
                kernelPrefixSumNextInput                   = computeAsset.FindKernel("MainPrefixSumNextInput");
                kernelPrefixSumResolveParent               = computeAsset.FindKernel("MainPrefixSumResolveParent");
                kernelPrefixSumResolveParentExclusive      = computeAsset.FindKernel("MainPrefixSumResolveParentExclusive");
            }
        }
    }
}
