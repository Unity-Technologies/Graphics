using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal class LightmappingContext: IDisposable
    {
        UnityComputeDeviceContext _deviceContext;
        public UnityComputeWorld World;
        public GraphicsBuffer TraceScratchBuffer;
        public LightmapIntegratorContext IntegratorContext;
        public LightmapIntegrationResourceCache ResourceCache;
        public RenderTexture AccumulatedOutput;
        public RenderTexture AccumulatedDirectionalOutput;
        public GraphicsBuffer ExpandedOutput;
        public GraphicsBuffer ExpandedDirectional;
        public GraphicsBuffer GBuffer;
        public GraphicsBuffer CompactedTexelIndices;
        public GraphicsBuffer CompactedGBufferLength;
        public GraphicsBuffer IndirectDispatchBuffer;
        public GraphicsBuffer IndirectDispatchRayTracingBuffer;

        public ChartRasterizer ChartRasterizer;
        // Temporary buffers required for chart rasterization
        public ChartRasterizer.Buffers ChartRasterizerBuffers;

        private int _width;
        private int _height;
        public int Width => _width;
        public int Height => _height;

        public void ClearOutputs()
        {
            CommandBuffer cmd = GetCommandBuffer();
            if (AccumulatedOutput is not null)
            {
                cmd.SetRenderTarget(AccumulatedOutput);
                cmd.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            }
            if (AccumulatedDirectionalOutput is not null)
            {
                cmd.SetRenderTarget(AccumulatedDirectionalOutput);
                cmd.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            }
        }

        static public RenderTexture MakeRenderTexture(int width, int height, string name)
        {
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGBFloat, 0)
            {
                sRGB = false,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.None,
                memoryless = RenderTextureMemoryless.None,
                useDynamicScale = false,
                depthBufferBits = 0
            };
            RenderTexture renderTexture = new RenderTexture(rtDesc)
            {
                name = name,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            return renderTexture;
        }

        internal bool ExpandedBufferNeedsUpdating(UInt64 expandedSize)
        {
            if (ExpandedOutput is not null &&
                ExpandedDirectional is not null &&
                CompactedTexelIndices is not null &&
                GBuffer is not null &&
                expandedSize == (UInt64)ExpandedOutput.count &&
                expandedSize == (UInt64)ExpandedDirectional.count &&
                expandedSize == (UInt64)CompactedTexelIndices.count &&
                expandedSize == (UInt64)CompactedTexelIndices.count)
            {
                return false;
            }
            return true;
        }

        internal bool InitializeExpandedBuffer(UInt64 expandedSize)
        {
            if (!ExpandedBufferNeedsUpdating(expandedSize))
                return true;

            ExpandedOutput?.Dispose();
            ExpandedOutput = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(expandedSize), sizeof(float) * 4);
            if (ExpandedOutput is null)
            {
                Dispose();
                return false;
            }

            ExpandedDirectional?.Dispose();
            ExpandedDirectional = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(expandedSize), sizeof(float) * 4);
            if (ExpandedDirectional is null)
            {
                Dispose();
                return false;
            }

            CompactedTexelIndices?.Dispose();
            CompactedTexelIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(expandedSize), sizeof(uint));
            if (CompactedTexelIndices is null)
            {
                Dispose();
                return false;
            }

            GBuffer?.Dispose();
            GBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(expandedSize), 16); // the GBuffer is used to store the UV samples as HitEntry (StochasticLightmapSampling.hlsl) hence the stride

            if (GBuffer is null)
            {
                Dispose();
                return false;
            }

            return true;
        }

        internal bool Initialize(UnityComputeDeviceContext deviceContext, int width, int height, UnityComputeWorld world, uint maxIndexCount, uint maxVertexCount, LightmapResourceLibrary resources)
        {
            _deviceContext = deviceContext;
            World = world;
            IntegratorContext = new LightmapIntegratorContext();
            ResourceCache = new LightmapIntegrationResourceCache();

            ChartRasterizer = new ChartRasterizer(resources.SoftwareChartRasterizationShader, resources.HardwareChartRasterizationShader);
            InitializeChartRasterizationBuffers(maxIndexCount, maxVertexCount);

            return SetOutputResolution(width, height);
        }

        internal bool SetOutputResolution(int width, int height) // In case of failure, the context is disposed and is thus not usable anymore
        {
            _width = width;
            _height = height;

            ReleaseAndDestroy(ref AccumulatedOutput);
            AccumulatedOutput = MakeRenderTexture(width, height, "AccumulatedOutput");
            if (AccumulatedOutput == null || !AccumulatedOutput.Create())
            {
                Dispose();
                return false;
            }

            ReleaseAndDestroy(ref AccumulatedDirectionalOutput);
            AccumulatedDirectionalOutput = MakeRenderTexture(width, height, "AccumulatedDirectionalOutput");
            if (AccumulatedDirectionalOutput == null || !AccumulatedDirectionalOutput.Create())
            {
                Dispose();
                return false;
            }

            CompactedGBufferLength?.Dispose();
            IndirectDispatchBuffer?.Dispose();
            IndirectDispatchRayTracingBuffer?.Dispose();
            CompactedGBufferLength = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, 1, sizeof(uint));
            IndirectDispatchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 3, sizeof(uint));
            IndirectDispatchRayTracingBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();

            return true;
        }

        public void InitializeTraceScratchBuffer(uint width, uint height, uint expandedSampleWidth)
        {
            TraceScratchBuffer?.Dispose();
            TraceScratchBuffer = null;

            Debug.Assert(World is not null);
            ulong scratchSize = World.RayTracingContext.GetRequiredTraceScratchBufferSizeInBytes(width, height, expandedSampleWidth);
            uint scratchStride = RayTracingContext.GetScratchBufferStrideInBytes();
            if (scratchSize > 0)
            {
                TraceScratchBuffer = new GraphicsBuffer(RayTracingHelper.ScratchBufferTarget, (int)(scratchSize / scratchStride), 4);
                Debug.Assert(TraceScratchBuffer == null || TraceScratchBuffer.target == RayTracingHelper.ScratchBufferTarget);
            }
        }

        private void InitializeChartRasterizationBuffers(uint maxIndexCount, uint maxVertexCount)
        {
            ChartRasterizerBuffers.vertex?.Dispose();
            ChartRasterizerBuffers.vertex = null;
            ChartRasterizerBuffers.vertexToOriginalVertex?.Dispose();
            ChartRasterizerBuffers.vertexToOriginalVertex = null;
            ChartRasterizerBuffers.vertexToChartID?.Dispose();
            ChartRasterizerBuffers.vertexToChartID = null;

            // We base the size of the temporary buffers on the vertex count or index count of the mesh with the most vertices / indices, to avoid constant reallocations.
            uint maxCount = Math.Max(maxIndexCount, maxVertexCount);
            ChartRasterizerBuffers.vertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)maxCount, UnsafeUtility.SizeOf<Vector2>());
            ChartRasterizerBuffers.vertexToOriginalVertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)maxCount, sizeof(uint));
            ChartRasterizerBuffers.vertexToChartID = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)maxCount, sizeof(uint));
        }

        public CommandBuffer GetCommandBuffer()
        {
            Debug.Assert(_deviceContext != null);
            return _deviceContext.GetCommandBuffer();
        }

        public void Dispose()
        {
            TraceScratchBuffer?.Dispose();
            ResourceCache?.Dispose();
            IntegratorContext?.Dispose();
            ReleaseAndDestroy(ref AccumulatedOutput);
            ReleaseAndDestroy(ref AccumulatedDirectionalOutput);
            ExpandedOutput?.Dispose();
            ExpandedDirectional?.Dispose();
            CompactedTexelIndices?.Dispose();
            GBuffer?.Dispose();
            CompactedGBufferLength?.Dispose();
            IndirectDispatchBuffer?.Dispose();
            IndirectDispatchRayTracingBuffer?.Dispose();

            ChartRasterizerBuffers.vertex?.Dispose();
            ChartRasterizerBuffers.vertexToOriginalVertex?.Dispose();
            ChartRasterizerBuffers.vertexToChartID?.Dispose();

            ChartRasterizer?.Dispose();
            ChartRasterizer = null;
        }

        private static void ReleaseAndDestroy(ref RenderTexture tex)
        {
            if (tex == null)
                return;

            tex.Release();
            CoreUtils.Destroy(tex);
            tex = null;
        }
    }
}
