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
        ProfilingSampler m_ProfilingSampler;

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

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
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
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
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
            m_ProfilingSampler = ProfilingSampler.Get(profileId);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            Rect pixelRect = renderingData.cameraData.pixelRect;
            float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                if (m_CameraSettings.overrideCamera)
                {
                    if (cameraData.xr.enabled)
                    {
                        Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overridden.");
                    }
                    else
                    {
                        Matrix4x4 projectionMatrix = Matrix4x4.Perspective(m_CameraSettings.cameraFieldOfView, cameraAspect,
                            camera.nearClipPlane, camera.farClipPlane);
                        projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, cameraData.IsCameraProjectionMatrixFlipped());

                        Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                        Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                        viewMatrix.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                        RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                    }
                }

                var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
                if (activeDebugHandler != null)
                {
                    activeDebugHandler.DrawWithDebugRenderState(context, cmd, ref renderingData, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock,
                        (ScriptableRenderContext ctx, ref RenderingData data, ref DrawingSettings ds, ref FilteringSettings fs, ref RenderStateBlock rsb) =>
                        {
                            ctx.DrawRenderers(data.cullResults, ref ds, ref fs, ref rsb);
                        });
                }
                else
                {
                    // Ensure we flush our command-buffer before we render...
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Render the objects...
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                }

                if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xr.enabled)
                {
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
                }
            }
        }

        private class PassData
        {
            internal RenderObjectsPass pass;
            internal RenderingData renderingData;
        }

        internal override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            using (var builder = renderGraph.AddRenderPass<PassData>("Render Objects Pass", out var passData, m_ProfilingSampler))
            {
                TextureHandle color = UniversalRenderer.m_ActiveRenderGraphColor;
                builder.UseColorBuffer(color, 0);
                builder.UseDepthBuffer(UniversalRenderer.m_ActiveRenderGraphDepth, DepthAccess.Write);
                builder.ReadTexture(renderer.frameResources.mainShadowsTexture);

                builder.AllowPassCulling(false);

                passData.pass = this;
                passData.renderingData = renderingData;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    data.pass.Execute(rgContext.renderContext, ref data.renderingData);
                });
            }
        }
    }
}
