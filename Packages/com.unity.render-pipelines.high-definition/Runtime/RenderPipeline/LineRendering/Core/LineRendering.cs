using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility for rendering high quality lines.
    /// </summary>
    public partial class LineRendering
    {
        /// <summary>
        /// Defines where in the render pipeline the line color will be composed.
        /// </summary>
        public enum CompositionMode
        {
            /// <summary>Composition will occur before the color pyramid is generated.</summary>
            BeforeColorPyramid,
            /// <summary>Composition will occur after temporal anti-aliasing.</summary>
            AfterTemporalAntialiasing,
            /// <summary>Composition will occur after depth of field.</summary>
            AfterDepthOfField
        }

        /// <summary>
        /// Defines how many segments can be sorted in a single tile.
        /// </summary>
        public enum SortingQuality
        {
            /// <summary>Sorts a low number of segments per tile cluster (256).</summary>
            Low,
            /// <summary>Sorts a medium number of segments per tile cluster (512).</summary>
            Medium,
            /// <summary>Sorts a high number of segments per tile cluster (1024).</summary>
            High,
            /// <summary>Sorts a very high number of segments per tile cluster (2048).</summary>
            Ultra
        }

        static LineRendering s_Instance = new LineRendering();

        internal static LineRendering Instance
        {
            get
            {
                return s_Instance;
            }
        }

        private bool m_IsInitialized = false;

        // Compute resources and utility container.
        private SystemResources m_SystemResources;

        static readonly ProfilingSampler k_LineRenderingSampler = new ProfilingSampler("LineRendering");

        internal void Initialize(SystemResources parameters)
        {
            if (m_IsInitialized)
            {
                Debug.LogError("Line Rendering has already been initialized.");
                return;
            }

            m_SystemResources = parameters;

            m_IsInitialized = true;
        }

        internal void Cleanup()
        {
            if (!m_IsInitialized)
            {
                Debug.LogError("Line Rendering has not been initialized, nothing to cleanup.");
                return;
            }

            CleanupShadingAtlas();

            m_IsInitialized = false;
        }

        ShaderVariables ComputeShaderVariables(Arguments args, RendererData[] renderDatas)
        {
            var vars = new ShaderVariables
            {
                _SegmentCount         = renderDatas.Sum(o => (int)o.mesh.GetIndexCount(0) / 2),
                _VertexCount          = renderDatas.Sum(o => o.mesh.vertexCount),
                _DimBin               = new Vector2(DivRoundUp((int)args.viewport.x, Budgets.TileSizeBin) , DivRoundUp((int)args.viewport.y, Budgets.TileSizeBin)),
                _SizeScreen           = new Vector4(args.viewport.x, args.viewport.y, 1 + (1f / args.viewport.x), 1 + (1f / args.viewport.y)),
                _SizeBin              = new Vector4(Budgets.TileSizeBin, 2f * Budgets.TileSizeBin / args.viewport.x, 2f * Budgets.TileSizeBin / args.viewport.y, 0),
                _ClusterDepth         = args.settings.clusterCount,
                _TileOpacityThreshold = args.settings.tileOpacityThreshold
            };

            // Set up the various bin and clustering counts.
            {
                vars._BinCount = ((int)(vars._DimBin.x * vars._DimBin.y));
                vars._ClusterCount = vars._BinCount * vars._ClusterDepth;

                // Round up the bin count to the next power of two due to our sorting algorithm.
                // Common resolutions usually result in being very close to the next power of two so this isn't so bad.
                vars._BinCount = NextPowerOfTwo(vars._BinCount);
            }

            return vars;
        }

        void DrawInternal(RendererData[] renderDatas, ref Arguments args)
        {
            if (renderDatas.Length == 0)
                return;

            ComputeShadingAtlasAllocations(renderDatas, ref args.shadingAtlas);

            var shaderVariables = ComputeShaderVariables(args, renderDatas);

            // Allocate the buffer resources that will be shared between passes one and two.
            var sharedBuffers = SharedPassData.Buffers.Allocate(args.renderGraph, new SharedPassData.Buffers.AllocationParameters
            {
                countVertex  = shaderVariables._VertexCount,
                countSegment = shaderVariables._SegmentCount
            });

            shaderVariables._ShadingAtlasDimensions = sharedBuffers.groupShadingSampleAtlasDimensions;

#if UNITY_EDITOR
            var shadersStillCompiling = renderDatas.Any(o => !ShaderUtil.IsPassCompiled(o.material, o.offscreenShadingPass));
#endif

            // Utility for binding the common buffers between passes one and two.
            void UseSharedBuffers(RenderGraphBuilder builder, SharedPassData.Buffers buff)
            {
                builder.WriteBuffer(buff.constantBuffer);
                builder.WriteBuffer(buff.counterBuffer);
                builder.WriteBuffer(buff.vertexStream0);
                builder.WriteBuffer(buff.vertexStream1);
                builder.WriteBuffer(buff.recordBufferSegment);
                builder.WriteBuffer(buff.viewSpaceDepthRange);
                builder.ReadWriteTexture(buff.groupShadingSampleAtlas);
            }

            // Pass 1: Geometry Processing and Shading
            using (var builder = args.renderGraph.AddRenderPass<GeometryPassData>("Geometry Processing", out var passData, k_LineRenderingSampler))
            {
                // TODO: Get rid of this...
                // Unfortunately we currently need this utility to "reimport" some buffers.
                RendererData[] ImportRenderDatas()
                {
                    var importedRenderers = renderDatas;

                    for (uint i = 0; i < importedRenderers.Length; ++i)
                    {
                        importedRenderers[i].indexBuffer = builder.ReadBuffer(renderDatas[i].indexBuffer);
                        importedRenderers[i].lodBuffer   = builder.ReadBuffer(renderDatas[i].lodBuffer);
                    }

                    return importedRenderers;
                }

                // Set up other various dependent data.
                passData.depthRT          = builder.ReadTexture(args.depthTexture);
                passData.systemResources  = m_SystemResources;
                passData.rendererData     = ImportRenderDatas();
                passData.offsetsVertex    = PrefixSum(renderDatas.Select(o => o.mesh.vertexCount).ToArray());
                passData.offsetsSegment   = PrefixSum(renderDatas.Select(o => (int)o.mesh.GetIndexCount(0) / 2).ToArray());
                passData.matrixIVP        = args.matrixIVP;
                passData.shadingAtlas     = args.shadingAtlas;
                passData.shaderVariables  = shaderVariables;

                // Set up the shared resources.
                passData.sharedBuffers = sharedBuffers;
                UseSharedBuffers(builder, sharedBuffers);

                // Then set up the resources specific to this pass.
                passData.transientBuffers = GeometryPassData.Buffers.Allocate(args.renderGraph, builder, new GeometryPassData.Buffers.AllocationParameters
                {
                    countVertex  = passData.shaderVariables._VertexCount,
                    countVertexMaxPerRenderer = renderDatas.Max(o => o.mesh.vertexCount),
                });

                builder.SetRenderFunc((GeometryPassData data, RenderGraphContext context) =>
                {
                    // Upload the constant buffer before the geometry pass.
                    var constantBufferData = new NativeArray<ShaderVariables>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    {
                        constantBufferData[0] = shaderVariables;
                        context.cmd.SetBufferData(data.sharedBuffers.constantBuffer, constantBufferData);
                        constantBufferData.Dispose();
                    }

                    ExecuteGeometryPass(context.cmd, data);
                });
            }

            // Pass 2: Rasterization
            using (var builder = args.renderGraph.AddRenderPass<RasterizationPassData>("Rasterization", out var passData, k_LineRenderingSampler))
            {
                // Optionally schedule this pass in async. (This is actually the whole reason we split this process into two passes).
                builder.EnableAsyncCompute(args.settings.executeAsync);

                // Set up other various dependent data.
                passData.systemResources  = m_SystemResources;
                passData.debugModeIndex   = (int)args.settings.debugMode;
                passData.qualityModeIndex = (int)args.settings.sortingQuality;
                passData.shaderVariables  = shaderVariables;
#if UNITY_EDITOR
                passData.renderDataStillHasShadersCompiling = shadersStillCompiling;
#endif
                // Configure the render targets that the rasterizer will draw to.
                passData.renderTargets = new RenderTargets
                {
                    color  = builder.WriteTexture(args.targets.color),
                    depth  = builder.WriteTexture(args.targets.depth),
                    motion = builder.WriteTexture(args.targets.motion)
                };

                // Set up the shared resources.
                passData.sharedBuffers = sharedBuffers;
                UseSharedBuffers(builder, sharedBuffers);

                // Then set up the resources specific to this pass.
                passData.transientBuffers = RasterizationPassData.Buffers.Allocate(args.renderGraph, builder, new RasterizationPassData.Buffers.AllocationParameters
                {
                    countBin     = passData.shaderVariables._BinCount,
                    countCluster = passData.shaderVariables._ClusterCount,
                    depthCluster = passData.shaderVariables._ClusterDepth,

                    countBinRecords = ComputeBinningRecordCapacity(args.settings.memoryBudget),
                    countWorkQUeue  = ComputeWorkQueueCapacity(args.settings.memoryBudget)
                });

                builder.SetRenderFunc((RasterizationPassData data, RenderGraphContext context) =>
                {
                    ExecuteRasterizationPass(context.cmd, data);
                });
            }
        }

        internal void Draw(Arguments args)
        {
            if (!HasRenderDatas())
                return;

            var renderDatas = GetValidRenderDatas(args.renderGraph, args.camera);

            if (renderDatas.Length == 0)
                return;

            foreach (var renderData in SortRenderDatasByCameraDistance(renderDatas, args.camera))
            {
                DrawInternal(renderData, ref args);
            }
        }
    }
}
