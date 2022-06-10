using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    public class FinalBlitPass : ScriptableRenderPass
    {
        RTHandle m_Source;
        static Material m_BlitMaterial;
        RTHandle m_CameraTargetHandle;

        /// <summary>
        /// Creates a new <c>FinalBlitPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="blitMaterial">The <c>Material</c> to use for copying the executing the final blit.</param>
        /// <seealso cref="RenderPassEvent"/>
        public FinalBlitPass(RenderPassEvent evt, Material blitMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(FinalBlitPass));
            base.useNativeRenderPass = false;

            m_BlitMaterial = blitMaterial;
            renderPassEvent = evt;
        }

        public void Dispose()
        {
            m_CameraTargetHandle?.Release();
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        [Obsolete("Use RTHandles for colorHandle")]
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
        {
            if (m_Source?.nameID != colorHandle.Identifier())
                m_Source = RTHandles.Alloc(colorHandle.Identifier());
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle colorHandle)
        {
            m_Source = colorHandle;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_BlitMaterial, GetType().Name);
                return;
            }

            // Note: We need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            ref CameraData cameraData = ref renderingData.cameraData;

            RenderTargetIdentifier cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                int depthSlice = cameraData.xr.singlePassEnabled ? -1 : cameraData.xr.GetTextureArraySlice();
                cameraTarget = new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, depthSlice);
            }
#endif
            // Create RTHandle alias to use RTHandle apis
            if (m_CameraTargetHandle != cameraTarget)
            {
                m_CameraTargetHandle?.Release();
                m_CameraTargetHandle = RTHandles.Alloc(cameraTarget);
            }

            var cmd = renderingData.commandBuffer;

            if (m_Source == cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.FinalBlit)))
            {
                GetActiveDebugHandler(ref renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref cameraData, true);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LinearToSRGBConversion,
                    cameraData.requireSrgbConversion);

                FinalBlitPass.ExecutePass(ref renderingData, m_CameraTargetHandle, m_Source);
                cameraData.renderer.ConfigureCameraTarget(m_CameraTargetHandle, m_CameraTargetHandle);
            }
        }

        private static void ExecutePass(ref RenderingData renderingData, RTHandle cameraTarget, RTHandle source)
        {
            var cameraData = renderingData.cameraData;
            var cmd = renderingData.commandBuffer;

            // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
            // We need to keep in the pipeline of first render pass to each render target to properly set load/store actions.
            // meanwhile we set to load so split screen case works.
            var loadAction = RenderBufferLoadAction.DontCare;
            if (!cameraData.isSceneViewCamera && !cameraData.isDefaultViewport)
                loadAction = RenderBufferLoadAction.Load;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                loadAction = RenderBufferLoadAction.Load;
#endif

            RenderingUtils.FinalBlit(cmd, ref cameraData, source, cameraTarget, loadAction, RenderBufferStoreAction.Store, m_BlitMaterial, source.rt?.filterMode == FilterMode.Bilinear ? 1 : 0);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;

            internal int sourceID;

            internal RenderingData renderingData;
        }

        internal void Render(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle src, TextureHandle dest)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Final Blit", out var passData, base.profilingSampler))
            {
                passData.source = src;
                passData.destination = dest;
                passData.renderingData = renderingData;
                passData.sourceID = ShaderPropertyId.sourceTex;

                builder.UseColorBuffer(passData.destination, 0);
                builder.ReadTexture(passData.source);

                CoreUtils.SetKeyword(renderingData.commandBuffer, ShaderKeywordStrings.LinearToSRGBConversion,
                    renderingData.cameraData.requireSrgbConversion);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    m_BlitMaterial.SetTexture(data.sourceID, data.source);

                    ExecutePass(ref data.renderingData, data.destination, data.source);
                });
            }
        }
    }
}
