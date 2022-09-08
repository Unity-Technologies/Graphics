using System;
using System.Collections.Generic;
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
        [Obsolete("Use SetDepthState instead", false)]
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
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            ConfigureTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);

            ExecutePass(context, m_PassData, ref renderingData, renderingData.commandBuffer, renderingData.cameraData.IsCameraProjectionMatrixFlipped());
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, CommandBuffer cmd, bool isYFlipped)
        {
            var cameraData = renderingData.cameraData;

            SortingCriteria sortingCriteria = (passData.renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = passData.overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = passData.overrideMaterialPassIndex;
            drawingSettings.overrideShader = passData.overrideShader;
            drawingSettings.overrideShaderPassIndex = passData.overrideShaderPassIndex;

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
                    activeDebugHandler.DrawWithDebugRenderState(context, cmd, ref renderingData, ref drawingSettings, ref passData.filteringSettings, ref passData.renderStateBlock,
                        (ScriptableRenderContext ctx, CommandBuffer cmd, ref RenderingData data, ref DrawingSettings ds, ref FilteringSettings fs, ref RenderStateBlock rsb) =>
                        {
                            RenderingUtils.DrawRendererListWithRenderStateBlock(ctx, cmd, data, ds, fs, rsb);

                        });
                }
                else
                {
                    // Ensure we flush our command-buffer before we render...
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Render the objects...
                   RenderingUtils.DrawRendererListWithRenderStateBlock(context, cmd, renderingData, drawingSettings, passData.filteringSettings, passData.renderStateBlock);
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
            internal FilteringSettings filteringSettings;
            internal RenderStateBlock renderStateBlock;
            internal RenderPassEvent renderPassEvent;
            internal RenderQueueType renderQueueType;
            internal Material overrideMaterial;
            internal int overrideMaterialPassIndex;
            internal Shader overrideShader;
            internal List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
            internal int overrideShaderPassIndex;

            internal TextureHandle color;
            internal RenderingData renderingData;
        }

        void InitPassData(ref PassData passData)
        {
            passData.filteringSettings = m_FilteringSettings;
            passData.cameraSettings = m_CameraSettings;
            passData.renderStateBlock = m_RenderStateBlock;
            passData.overrideMaterial = overrideMaterial;
            passData.renderPassEvent = renderPassEvent;
            passData.renderQueueType = renderQueueType;
            passData.overrideShaderPassIndex = overrideShaderPassIndex;
            passData.overrideMaterialPassIndex = overrideMaterialPassIndex;
            passData.overrideShader = overrideShader;
            passData.shaderTagIdList = m_ShaderTagIdList;
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            using (var builder = renderGraph.AddRenderPass<PassData>("Render Objects Pass", out var passData, s_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                TextureHandle color = UniversalRenderer.m_ActiveRenderGraphColor;
                passData.color = builder.UseColorBuffer(color, 0);
                builder.UseDepthBuffer(UniversalRenderer.m_ActiveRenderGraphDepth, DepthAccess.Write);

                UniversalRenderer.RenderGraphFrameResources frameResources = renderer.frameResources;

                if (frameResources.mainShadowsTexture.IsValid())
                    builder.ReadTexture(frameResources.mainShadowsTexture);
                if (frameResources.additionalShadowsTexture.IsValid())
                    builder.ReadTexture(frameResources.additionalShadowsTexture);

                builder.AllowPassCulling(false);


                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    var isYFlipped = data.renderingData.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(rgContext.renderContext, data, ref data.renderingData, data.renderingData.commandBuffer, isYFlipped);
                });
            }
        }
    }
}
