using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// The scriptable render pass used with the render objects renderer feature.
    /// </summary>
    public class RenderObjectsPass : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjects.CustomCameraSettings m_CameraSettings;
        string m_ProfilerTag;
        static ProfilingSampler s_ProfilingSampler;

        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial { get; set; }

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private PassData m_PassData;

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        [Obsolete("Use SetDepthState instead", true)]
        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            SetDepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="cameraSettings">The settings for custom cameras values.</param>
        public RenderObjectsPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects.CustomCameraSettings cameraSettings)
        {
            base.profilingSampler = new ProfilingSampler(nameof(RenderObjectsPass));

            m_ProfilerTag = profilerTag;
            s_ProfilingSampler = new ProfilingSampler(profilerTag);

            m_PassData = new PassData();

            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_CameraSettings = cameraSettings;
        }

        internal RenderObjectsPass(URPProfileId profileId, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects.CustomCameraSettings cameraSettings)
            : this(profileId.GetType().Name, renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings)
        {
            s_ProfilingSampler = ProfilingSampler.Get(profileId);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref renderingData, ref m_PassData);
            InitRendererLists(ref renderingData, ref m_PassData, context, default(RenderGraph), false);

            ExecutePass(m_PassData, ref renderingData, CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData.rendererList, renderingData.cameraData.IsCameraProjectionMatrixFlipped());
        }

        private static void ExecutePass(PassData passData, ref RenderingData renderingData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            ref var cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            Rect pixelRect = renderingData.cameraData.pixelRect;
            float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                if (passData.cameraSettings.overrideCamera)
                {
                    if (cameraData.xr.enabled)
                    {
                        Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overridden.");
                    }
                    else
                    {
                        Matrix4x4 projectionMatrix = Matrix4x4.Perspective(passData.cameraSettings.cameraFieldOfView, cameraAspect,
                            camera.nearClipPlane, camera.farClipPlane);
                        projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);

                        Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                        Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                        viewMatrix.SetColumn(3, cameraTranslation + passData.cameraSettings.offset);

                        RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                    }
                }

                var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists.DrawWithRendererList(cmd);
                }
                else
                {
                    cmd.DrawRendererList(rendererList);
                }

                if (passData.cameraSettings.overrideCamera && passData.cameraSettings.restoreCamera && !cameraData.xr.enabled)
                {
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(0), isYFlipped), false);
                }
            }
        }

        private class PassData
        {
            internal RenderObjects.CustomCameraSettings cameraSettings;
            internal RenderPassEvent renderPassEvent;

            internal TextureHandle color;
            internal RenderingData renderingData;
            internal RendererListHandle rendererListHdl;
            internal DebugRendererLists debugRendererLists;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }

        private void InitPassData(ref RenderingData renderingData, ref PassData passData)
        {
            passData.cameraSettings = m_CameraSettings;
            passData.renderPassEvent = renderPassEvent;
            passData.renderingData = renderingData;
        }

        private void InitRendererLists(ref RenderingData renderingData, ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            ref var cameraData = ref renderingData.cameraData;
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

            var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
            var filterSettings = m_FilteringSettings;
            if (useRenderGraph)
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(renderGraph, ref renderingData, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, renderingData, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
                }
            }
            else
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context, ref renderingData, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(context, renderingData, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererList);
                }
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Objects Pass", out var passData, s_ProfilingSampler))
            {
                InitPassData(ref renderingData, ref passData);

                TextureHandle color = renderer.activeColorTexture;
                passData.color = builder.UseTextureFragment(color, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(renderer.activeDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);
                
                TextureHandle mainShadowsTexture = frameResources.GetTexture(UniversalResource.MainShadowsTexture);
                TextureHandle additionalShadowsTexture = frameResources.GetTexture(UniversalResource.AdditionalShadowsTexture);

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, IBaseRenderGraphBuilder.AccessFlags.Read);

                for (int i = 0; i < RenderGraphUtils.DBufferSize; ++i)
                {
                    var dbuffer = frameResources.GetTexture((UniversalResource) (UniversalResource.DBuffer0 + i));
                    if (dbuffer.IsValid())
                        builder.UseTexture(dbuffer, IBaseRenderGraphBuilder.AccessFlags.Read);
                }

                InitRendererLists(ref renderingData, ref passData, default(ScriptableRenderContext), renderGraph, true);
                var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.rendererListHdl);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.renderingData.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, ref data.renderingData, rgContext.cmd, data.rendererListHdl, isYFlipped);
                });
            }
        }
    }
}
