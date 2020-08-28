using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Render all opaque forward objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names LightweightForward or SRPDefaultUnlit. The pass only
    /// renders objects in the rendering queue range of Opaque objects.
    /// </summary>
    internal class DebugShadowCascadesPass : ScriptableRenderPass
    {
        const string k_RenderOpaquesTag = "Debug Render Shadow Cascades";
        //FilteringSettings m_OpaqueFilterSettings;

        RenderTargetHandle colorAttachmentHandle { get; set; }
        RenderTargetHandle depthAttachmentHandle { get; set; }
        RenderTextureDescriptor descriptor { get; set; }
        int m_ShadowCasterCascadesCount;
        //ClearFlag clearFlag { get; set; }
        //Color clearColor { get; set; }
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DebugShadowCascades.Execute()");
        Material debugMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/ShadowCascadeDebug"));
        internal DebugShadowCascadesPass()
        {
            //RegisterShaderPassName("UniversalForward");
            //RegisterShaderPassName("SRPDefaultUnlit");

            //m_OpaqueFilterSettings = new FilteringSettings(RenderQueueRange.opaque);
        }

        /// <summary>
        /// Configure the pass before execution
        /// </summary>
        /// <param name="baseDescriptor">Current target descriptor</param>
        /// <param name="colorAttachmentHandle">Color attachment to render into</param>
        /// <param name="depthAttachmentHandle">Depth attachment to render into</param>
        /// <param name="clearFlag">Camera clear flag</param>
        /// <param name="clearColor">Camera clear color</param>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle,
            ClearFlag clearFlag,
            Color clearColor)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.depthAttachmentHandle = depthAttachmentHandle;
            //clearColor = CoreUtils.ConvertSRGBToActiveColorSpace(clearColor);
            ConfigureClear(clearFlag, clearColor);
            //clearFlag = clearFlag;
            descriptor = baseDescriptor;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            int shadowLightIndex = renderingData.lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return false;

            VisibleLight shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            if (light.shadows == LightShadows.None)
                return false;

            // m_ShadowCasterCascadesCount = renderingData.shadowData.mainLightShadowCascadesCount;
            //
            // int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(renderingData.shadowData.mainLightShadowmapWidth,
            //     renderingData.shadowData.mainLightShadowmapHeight, m_ShadowCasterCascadesCount);
            // m_ShadowmapWidth = renderingData.shadowData.mainLightShadowmapWidth;
            // m_ShadowmapHeight = (m_ShadowCasterCascadesCount == 2) ?
            //     renderingData.shadowData.mainLightShadowmapHeight >> 1 :
            //     renderingData.shadowData.mainLightShadowmapHeight;
            //
            // for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            // {
            //     bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
            //         shadowLightIndex, cascadeIndex, m_ShadowmapWidth, m_ShadowmapHeight, shadowResolution, light.shadowNearPlane,
            //         out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex], out m_CascadeSlices[cascadeIndex].viewMatrix, out m_CascadeSlices[cascadeIndex].projectionMatrix);
            //
            //     if (!success)
            //         return false;
            // }

            return true;
        }

        /// <inheritdoc/>
        //public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // if (renderer == null)
            //     throw new ArgumentNullException("renderer");

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderOpaquesTag);
            //using (new ProfilingSample(cmd, k_RenderOpaquesTag))
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                RenderBufferLoadAction colorLoadOp = (CoreUtils.HasFlag(clearFlag, ClearFlag.Color))
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load;
                RenderBufferStoreAction colorStoreOp = RenderBufferStoreAction.Store;

                RenderBufferLoadAction depthLoadOp = (CoreUtils.HasFlag(clearFlag, ClearFlag.Depth))
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load;

                RenderBufferStoreAction depthStoreOp = RenderBufferStoreAction.Store;

                //CoreUtils.SetRenderTarget(cmd, colorAttachmentHandle.Identifier(), colorLoadOp, colorStoreOp,
                //    depthAttachmentHandle.Identifier(), depthLoadOp, depthStoreOp, clearFlag, clearColor, descriptor.dimension);
                CoreUtils.SetRenderTarget(cmd, colorAttachmentHandle.Identifier(), colorLoadOp, colorStoreOp,
                    depthAttachmentHandle.Identifier(), depthLoadOp, depthStoreOp, clearFlag, clearColor);

                // TODO: We need a proper way to handle multiple camera/ camera stack. Issue is: multiple cameras can share a same RT
                // (e.g, split screen games). However devs have to be dilligent with it and know when to clear/preserve color.
                // For now we make it consistent by resolving viewport with a RT until we can have a proper camera management system
                //if (colorAttachmentHandle == -1 && !cameraData.isDefaultViewport)
                //    cmd.SetViewport(camera.pixelRect);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                /////
                Camera camera = renderingData.cameraData.camera;
                SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("LightweightForward"), sortingSettings);
                drawingSettings.overrideMaterial = debugMaterial;
                drawingSettings.perObjectData = PerObjectData.None;
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
                context.SetupCameraProperties(camera);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                /////
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
