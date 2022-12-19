using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        // Shading Atlas
        private RenderTexture[] m_LineShadingAtlas = new RenderTexture[]{null, null};
        private int m_CurrentWriteAtlasIndex = 0;
        private int m_AtlasUpdateCount = 0;

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
                Debug.LogError("Line Rendering has not been initialized first before calling cleanup.");
                return;
            }

            m_IsInitialized = false;
        }

        internal void PrepareShadingAtlas()
        {
            //ensure shading atlas is alive
            bool shadingAtlasNeedsRecreation = m_LineShadingAtlas[0] == null || m_LineShadingAtlas[1] == null || !m_LineShadingAtlas[0].IsCreated() || !m_LineShadingAtlas[1].IsCreated();

            if (shadingAtlasNeedsRecreation)
            {
                CreateShadingAtlas();
                m_CurrentWriteAtlasIndex = 0;
                m_AtlasUpdateCount = 0;
            }
            else
            {
                m_AtlasUpdateCount++;
            }
            m_CurrentWriteAtlasIndex = (m_CurrentWriteAtlasIndex + 1) % 2;
        }

        internal ShadingSampleAtlas GetShadingSampleAtlas()
        {
            int readIndex = (m_CurrentWriteAtlasIndex + 1) % 2;
            ShadingSampleAtlas shadingSampleAtlas = new ShadingSampleAtlas()
            {
                currentAtlas = m_LineShadingAtlas[m_CurrentWriteAtlasIndex],
                previousAtlas = m_LineShadingAtlas[readIndex],
                currentAtlasAllocationSize = 0,
                historyValid = m_AtlasUpdateCount > 0
            };
            return shadingSampleAtlas;
        }

        private void DrawGroup(RenderGraph renderGraph, Vector2 viewport, SystemSettings settings, TextureHandle depthTexture, ShadingSampleAtlas shadingSampleAtlas, RenderTargets targets, RenderData[] renderDatas)
        {
            if (renderDatas.Length == 0)
                return;



            using (var builder = renderGraph.AddRenderPass<RasterizerResources>("Render Line", out var passData, k_LineRenderingSampler))
            {
                // TODO: Will it always be needed?
                builder.AllowPassCulling(false);

                int groupShadingSampleOffset = shadingSampleAtlas.currentAtlasAllocationSize;
                UpdateShadingAtlasAllocations(shadingSampleAtlas, renderDatas);

                RenderData[] ImportRenderDatas()
                {
                    var importedRenderers = renderDatas;

                    for (uint i = 0; i < importedRenderers.Length; ++i)
                    {
                        // TODO: Get rid of this...
                        importedRenderers[i].rendererData.indexBuffer = builder.ReadBuffer(renderDatas[i].rendererData.indexBuffer);
                        importedRenderers[i].rendererData.lodBuffer = builder.ReadBuffer(renderDatas[i].rendererData.lodBuffer);
                    }

                    return importedRenderers;
                }

                passData.depthTexture = builder.ReadTexture(depthTexture);

                passData.renderTargets = new RenderTargets
                {
                    color  = builder.WriteTexture(targets.color),
                    depth  = builder.WriteTexture(targets.depth),
                    motion = builder.WriteTexture(targets.motion)
                };

                passData.systemResources  = m_SystemResources;
                passData.rendererData     = ImportRenderDatas();
                passData.offsetsVertex    = PrefixSum(renderDatas.Select(o => o.rendererData.mesh.vertexCount).ToArray());
                passData.offsetsSegment   = PrefixSum(renderDatas.Select(o => (int)o.rendererData.mesh.GetIndexCount(0) / 2).ToArray());
                passData.debugModeIndex   = (int)settings.debugMode;
                passData.qualityModeIndex = (int)settings.sortingQuality;

                passData.ShadingSampleAtlas = shadingSampleAtlas;

                passData.shaderVariables = new ShaderVariables
                {
                    _SegmentCount         = renderDatas.Sum(o => (int)o.rendererData.mesh.GetIndexCount(0) / 2),
                    _VertexCount          = renderDatas.Sum(o => o.rendererData.mesh.vertexCount),
                    _DimBin               = new Vector2(DivRoundUp((int)viewport.x, Budgets.TileSizeBin) , DivRoundUp((int)viewport.y, Budgets.TileSizeBin)),
                    _SizeScreen           = new Vector4(viewport.x, viewport.y, 1 + (1f / viewport.x), 1 + (1f / viewport.y)),
                    _SizeBin              = new Vector3(Budgets.TileSizeBin, 2f * Budgets.TileSizeBin / viewport.x, 2f * Budgets.TileSizeBin / viewport.y),
                    _ClusterDepth         = settings.clusterCount,
                    _TileOpacityThreshold = settings.tileOpacityThreshold,
                    _GroupShadingSampleOffset = groupShadingSampleOffset
                };

                // Set up the various bin and clustering counts.
                {
                    passData.shaderVariables._BinCount = ((int)(passData.shaderVariables._DimBin.x * passData.shaderVariables._DimBin.y));
                    passData.shaderVariables._ClusterCount = passData.shaderVariables._BinCount * passData.shaderVariables._ClusterDepth;

                    // Round up the bin count to the next power of two due to our sorting algorithm.
                    // Common resolutions usually result in being very close to the next power of two so this isn't so bad.
                    passData.shaderVariables._BinCount = NextPowerOfTwo(passData.shaderVariables._BinCount);
                }

                passData.buffers = Buffers.Allocate(renderGraph, builder, new Buffers.AllocationParameters
                {
                    countVertex  = passData.shaderVariables._VertexCount,
                    countVertexMaxPerRenderer = renderDatas.Max(o => o.rendererData.mesh.vertexCount),
                    countSegment = passData.shaderVariables._SegmentCount,
                    countBin     = passData.shaderVariables._BinCount,
                    countCluster = passData.shaderVariables._ClusterCount,
                    depthCluster = passData.shaderVariables._ClusterDepth
                });

                builder.SetRenderFunc((RasterizerResources data, RenderGraphContext context) =>
                {
                    Rasterize(context.cmd, data);
                });
            }
        }

        internal void UpdateShadingAtlasAllocations(ShadingSampleAtlas sampleAtlas, RenderData[] renderDatas)
        {
            int currentAtlasOffset = sampleAtlas.currentAtlasAllocationSize;
            foreach (var renderData in renderDatas)
            {

                PerRendererPersistentData persistentRenderData = renderData.persistentData;
                persistentRenderData.shadingAtlasAllocation.previousAllocationOffset = persistentRenderData.shadingAtlasAllocation.currentAllocationOffset;
                persistentRenderData.shadingAtlasAllocation.previousAllocationSize = persistentRenderData.shadingAtlasAllocation.currentAllocationSize;

                int shadingSamplesNeeded = renderData.rendererData.mesh.vertexCount;

                persistentRenderData.shadingAtlasAllocation.currentAllocationOffset = currentAtlasOffset;
                persistentRenderData.shadingAtlasAllocation.currentAllocationSize = shadingSamplesNeeded;

                persistentRenderData.updateCount = sampleAtlas.historyValid ? persistentRenderData.updateCount + 1 : 0;

                currentAtlasOffset += shadingSamplesNeeded;
            }

            sampleAtlas.currentAtlasAllocationSize = currentAtlasOffset;
        }

        internal void Draw(Camera camera, RenderGraph renderGraph, TextureHandle depthTexture, SystemSettings settings, ShadingSampleAtlas shadingSampleAtlas, Vector2 viewport, RenderTargets targets)
        {
            if (!HasRenderDatas())
                return;

            var renderDatas = GetValidRenderDatas(renderGraph, camera);

            if (renderDatas.Length == 0)
                return;

            foreach (var group in Enum.GetValues(typeof(RendererGroup)).Cast<RendererGroup>())
            {
                // Skip the non-grouped renderers as those will be done individually in the next pass.
                if (group == RendererGroup.None)
                    continue;

                // Renderer components with grouping will be merged into one draw call to get proper inter-renderer sorting.
                RenderData[] groupRenderData = GetRenderDatasInGroup(renderDatas, group);
                DrawGroup(renderGraph, viewport, settings, depthTexture, shadingSampleAtlas, targets, groupRenderData);
            }

            foreach (var renderData in GetRenderDatasNoGroup(renderDatas))
            {
                // Renderer components without grouping are drawn individually and not merged.
                RenderData[] individualRenderData = new[] {renderData};
                DrawGroup(renderGraph, viewport, settings, depthTexture, shadingSampleAtlas, targets, individualRenderData);
            }
        }

        internal void CreateShadingAtlas()
        {
            RenderTexture CreateRenderTexture(GraphicsFormat format, int width, int height, string name)
            {
                RenderTextureDescriptor textureDesc = new RenderTextureDescriptor()
                {
                    dimension = TextureDimension.Tex2D,
                    width = width,
                    height = height,
                    volumeDepth = 1,
                    graphicsFormat = format,
                    enableRandomWrite = true,
                    msaaSamples = 1,
                };
                RenderTexture tex = new RenderTexture(textureDesc);
                tex.name = name;
                tex.Create();
                return tex;
            }

            DestroyShadingAtlas();

            int atlasDim = 4096; //hardcoded for now (note that the shaders also assume this)

            m_LineShadingAtlas[0] = CreateRenderTexture(GraphicsFormat.R32G32B32A32_SFloat, atlasDim, atlasDim,
                "Line Rasterizer Shading Atlas 0");
            m_LineShadingAtlas[1] = CreateRenderTexture(GraphicsFormat.R32G32B32A32_SFloat, atlasDim, atlasDim,
                "Line Rasterizer Shading Atlas 1");
        }

        internal void DestroyShadingAtlas()
        {
            if (m_LineShadingAtlas[0] != null)
            {
                m_LineShadingAtlas[0].Release();
            }
            if (m_LineShadingAtlas[1] != null)
            {
                m_LineShadingAtlas[1].Release();
            }

            m_LineShadingAtlas[0] = null;
            m_LineShadingAtlas[1] = null;
        }
    }
}
