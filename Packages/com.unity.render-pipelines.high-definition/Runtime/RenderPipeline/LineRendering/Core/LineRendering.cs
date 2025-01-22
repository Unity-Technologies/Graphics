using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Profiling;
using UnityEngine.Rendering.HighDefinition;

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
            BeforeColorPyramid = 0,
            /// <summary>Composition will occur before the color pyramid is generated but after clouds are composited.</summary>
            BeforeColorPyramidAfterClouds = 3,
            /// <summary>Composition will occur after temporal anti-aliasing.</summary>
            AfterTemporalAntialiasing = 1,
            /// <summary>Composition will occur after depth of field.</summary>
            AfterDepthOfField = 2
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
        private List<RendererData> m_VisibleDatas = new List<RendererData>();

        // Compute resources and utility container.
        private SystemResources m_SystemResources;

        static readonly ProfilingSampler k_LineRenderingGeometrySampler      = new ProfilingSampler("LineRenderingGeometry");
        static readonly ProfilingSampler k_LineRenderingRasterizationSampler = new ProfilingSampler("LineRenderingRasterization");

        private ConstantBuffer<ShaderVariables> m_ShaderVariablesBuffer;

        // System keywords
        private LocalKeyword[] m_SegmentIndicesKeywords;

        internal void Initialize(SystemResources parameters)
        {
            if (m_IsInitialized)
            {
                Debug.LogError("Line Rendering has already been initialized.");
                return;
            }

            m_SystemResources = parameters;

            m_SegmentIndicesKeywords = new LocalKeyword[]
            {
                new(m_SystemResources.stageSetupSegmentCS, "INDEX_FORMAT_UINT_16"),
                new(m_SystemResources.stageSetupSegmentCS, "INDEX_FORMAT_UINT_32")
            };

            m_ShaderVariablesBuffer = new ConstantBuffer<ShaderVariables>();

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

            m_ShaderVariablesBuffer?.Release();
            m_ShaderVariablesBuffer = null;

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
                _TileOpacityThreshold = args.settings.tileOpacityThreshold,
                _ViewIndex            = args.viewIndex
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

            // Disable the compiling indication if we are in XR.
            shadersStillCompiling &= args.viewCount <= 1;
#endif

            // Utility for binding the common buffers between passes one and two.
            void UseSharedBuffers(RenderGraphBuilder builder, SharedPassData.Buffers buff)
            {
                builder.WriteBuffer(buff.counterBuffer);
                builder.WriteBuffer(buff.vertexStream0);
                builder.WriteBuffer(buff.vertexStream1);
                builder.WriteBuffer(buff.vertexStream2);
                builder.WriteBuffer(buff.vertexStream3);
                builder.WriteBuffer(buff.recordBufferSegment);
                builder.WriteBuffer(buff.viewSpaceDepthRange);
                builder.ReadWriteTexture(buff.groupShadingSampleAtlas);
            }

            // Pass 1: Geometry Processing and Shading
            using (var builder = args.renderGraph.AddRenderPass<GeometryPassData>("Geometry Processing", out var passData, k_LineRenderingGeometrySampler))
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
                passData.shaderVariables       = shaderVariables;
                passData.shaderVariablesBuffer = m_ShaderVariablesBuffer;

                passData.systemResources  = m_SystemResources;
                passData.rendererData     = ImportRenderDatas();
                passData.offsetsVertex    = PrefixSum(renderDatas.Select(o => o.mesh.vertexCount).ToArray());
                passData.offsetsSegment   = PrefixSum(renderDatas.Select(o => (int)o.mesh.GetIndexCount(0) / 2).ToArray());
                passData.matrixIVP        = args.matrixIVP;
                passData.shadingAtlas     = args.shadingAtlas;

                // Set up the shared resources.
                passData.sharedBuffers = sharedBuffers;
                passData.depthRT       = builder.ReadTexture(args.depthTexture);
                UseSharedBuffers(builder, sharedBuffers);

                // Then set up the resources specific to this pass.
                passData.transientBuffers = GeometryPassData.Buffers.Allocate(args.renderGraph, builder, new GeometryPassData.Buffers.AllocationParameters
                {
                    countVertex  = shaderVariables._VertexCount,
                    countVertexMaxPerRenderer = renderDatas.Max(o => o.mesh.vertexCount),
                });

                builder.SetRenderFunc((GeometryPassData data, RenderGraphContext context) =>
                {
                    // Upload the constants to device.
                    data.shaderVariablesBuffer.UpdateData(context.cmd, data.shaderVariables);

                    // Render-graph provides a scratch MPB for our needs.
                    data.materialPropertyBlock = context.renderGraphPool.GetTempMaterialPropertyBlock();

                    ExecuteGeometryPass(context.cmd, data);
                });
            }

            // Pass 2: Rasterization
            using (var builder = args.renderGraph.AddRenderPass<RasterizationPassData>("Rasterization", out var passData, k_LineRenderingRasterizationSampler))
            {
                // Optionally schedule this pass in async. (This is actually the whole reason we split this process into two passes).
                builder.EnableAsyncCompute(args.settings.executeAsync);

                // Set up other various dependent data.
                passData.shaderVariables       = shaderVariables;
                passData.shaderVariablesBuffer = m_ShaderVariablesBuffer;

                passData.binCount         = shaderVariables._BinCount;
                passData.clusterCount     = shaderVariables._ClusterCount;
                passData.clusterDepth     = shaderVariables._ClusterDepth;
                passData.systemResources  = m_SystemResources;
                passData.debugModeIndex   = (int)args.settings.debugMode;
                passData.qualityModeIndex = (int)args.settings.sortingQuality;
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
                passData.depthRT       = builder.ReadTexture(args.depthTexture);
                UseSharedBuffers(builder, sharedBuffers);

                // Then set up the resources specific to this pass.
                passData.transientBuffers = RasterizationPassData.Buffers.Allocate(args.renderGraph, builder, new RasterizationPassData.Buffers.AllocationParameters
                {
                    countBin     = shaderVariables._BinCount,
                    countCluster = shaderVariables._ClusterCount,
                    depthCluster = shaderVariables._ClusterDepth,

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

            // Cull the render datas to lighten the CPU and GPU load further down the line
            Vector3 cameraOffset = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0) // TODO: ShaderConfig is HDRP-specific, we should not use it here.
                cameraOffset = args.cameraPosition;

            m_VisibleDatas.Clear();
            foreach (var renderData in renderDatas)
            {
                // We're using the OrientedBBox although it is in world space and really a AABB
                OrientedBBox obb;
                Bounds worldBounds = renderData.bounds;

                obb.center = worldBounds.center;
                obb.center -= cameraOffset;

                obb.right = Vector3.right;
                obb.up = Vector3.up;

                obb.extentX = worldBounds.extents.x;
                obb.extentY = worldBounds.extents.y;
                obb.extentZ = worldBounds.extents.z;

                if (GeometryUtils.Overlap(obb, args.cameraFrustum, 6, 8))
                {
                    m_VisibleDatas.Add(renderData);
                }
            }

            if (m_VisibleDatas.Count == 0)
                return;

            foreach (var renderData in SortRenderDatasByCameraDistance(renderDatas, args.camera))
            {
                // [NOTE-HQ-LINES-SINGLE-PASS-STEREO]
                // The software rasterizer doesn't support instancing and is thus incompatible with
                // single-pass mode for XR stereo rendering. This is a problem since that is what HDRP
                // use by default, so we have to manually support it with a multi-pass approach.
                for (int viewIndex = 0; viewIndex < args.viewCount; ++viewIndex)
                {
                    args.viewIndex = viewIndex;
                    DrawInternal(renderData, ref args);
                }
            }
        }
    }
}
