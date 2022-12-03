using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    internal partial struct GPUPrefixSum
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
            public uint count;
            public uint offset;
            public uint parentOffset;
        }

        internal struct RenderGraphResources
        {
            public int alignedElementCount;
            public int maxBufferCount;
            public int maxLevelCount;

            public BufferHandle prefixBuffer0;
            public BufferHandle prefixBuffer1;
            public BufferHandle totalLevelCountBuffer;
            public BufferHandle levelOffsetBuffer;
            public BufferHandle indirectDispatchArgsBuffer;
            public BufferHandle output => prefixBuffer0;

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

        internal struct SupportResources
        {
            public bool ownsResources;
            public int  alignedElementCount;
            public int  maxBufferCount;
            public int  maxLevelCount;

            public GraphicsBuffer prefixBuffer0;
            public GraphicsBuffer prefixBuffer1;
            public GraphicsBuffer totalLevelCountBuffer;
            public GraphicsBuffer levelOffsetBuffer;
            public GraphicsBuffer indirectDispatchArgsBuffer;
            public GraphicsBuffer output => prefixBuffer0;

            public static SupportResources Create(int maxElementCount)
            {
                var resources = new SupportResources() { alignedElementCount = 0, ownsResources = true };
                resources.Resize(maxElementCount);
                return resources;
            }

            public static SupportResources Load(RenderGraphResources shaderGraphResources)
            {
                var resources = new SupportResources() { alignedElementCount = 0, ownsResources = false };
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

        internal struct DirectArgs
        {
            public bool             exclusive;
            public int              inputCount;
            public GraphicsBuffer   input;
            public SupportResources supportResources;
        }

        internal struct IndirectDirectArgs
        {
            public bool             exclusive;
            public int              inputCountBufferByteOffset;
            public ComputeBuffer    inputCountBuffer;
            public GraphicsBuffer   input;
            public SupportResources supportResources;
        }

        internal struct SystemResources
        {
            public ComputeShader computeAsset;

            public int kernelCalculateLevelDispatchArgsFromConst;
            public int kernelCalculateLevelDispatchArgsFromBuffer;
            public int kernelPrefixSumOnGroup;
            public int kernelPrefixSumOnGroupExclusive;
            public int kernelPrefixSumNextInput;
            public int kernelPrefixSumResolveParent;
            public int kernelPrefixSumResolveParentExclusive;

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
