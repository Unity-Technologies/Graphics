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
        Material m_BlitMaterial;
        Material m_BlitHDRMaterial;
        RTHandle m_CameraTargetHandle;

        /// <summary>
        /// Creates a new <c>FinalBlitPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="blitMaterial">The <c>Material</c> to use for copying the executing the final blit.</param>
        /// <param name="blitHDRMaterial">The <c>Material</c> to use for copying the executing the final blit when HDR output is active.</param>
        /// <seealso cref="RenderPassEvent"/>
        public FinalBlitPass(RenderPassEvent evt, Material blitMaterial, Material blitHDRMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(FinalBlitPass));
            base.useNativeRenderPass = false;

            m_BlitMaterial = blitMaterial;
            m_BlitHDRMaterial = blitHDRMaterial;
            renderPassEvent = evt;
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
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

        static void SetupHDROutput(Material material, HDROutputUtils.Operation hdrOperation, Vector4 hdrOutputParameters)
        {
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputParameters);
            HDROutputUtils.ConfigureHDROutput(material, HDROutputSettings.main.displayColorGamut, hdrOperation);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool outputsToHDR = renderingData.cameraData.isHDROutputActive;
            Material blitMaterial = outputsToHDR ? m_BlitHDRMaterial : m_BlitMaterial;
            if (blitMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", blitMaterial, GetType().Name);
                return;
            }

            ref CameraData cameraData = ref renderingData.cameraData;

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            DebugHandler debugHandler = GetActiveDebugHandler(ref renderingData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref cameraData);

            if (!resolveToDebugScreen)
            {
                // Create RTHandle alias to use RTHandle apis
                if (m_CameraTargetHandle != cameraTarget)
                {
                    m_CameraTargetHandle?.Release();
                    m_CameraTargetHandle = RTHandles.Alloc(cameraTarget);
                }
            }

            var cmd = renderingData.commandBuffer;

            if (m_Source == cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.FinalBlit)))
            {
                blitMaterial.enabledKeywords = null;

                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref cameraData, !resolveToDebugScreen);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LinearToSRGBConversion,
                    cameraData.requireSrgbConversion);

                if (outputsToHDR)
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();

                    Vector4 hdrOutputLuminanceParams;
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(tonemapping, out hdrOutputLuminanceParams);
                    
                    HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.None;
                    // If the HDRDebugView is on, we don't want the encoding
                    if (debugHandler == null || !debugHandler.HDRDebugViewIsActive(ref cameraData))
                        hdrOperation |= HDROutputUtils.Operation.ColorEncoding;
                    // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                    if (!cameraData.postProcessEnabled)
                        hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                    SetupHDROutput(blitMaterial, hdrOperation, hdrOutputLuminanceParams);
                }

                if (resolveToDebugScreen)
                {
                    debugHandler.BlitTextureToDebugScreenTexture(cmd, m_Source, blitMaterial, m_Source.rt?.filterMode == FilterMode.Bilinear ? 1 : 0);
                }
                else
                {
                    FinalBlitPass.ExecutePass(ref renderingData, blitMaterial, m_CameraTargetHandle, m_Source);
                    cameraData.renderer.ConfigureCameraTarget(m_CameraTargetHandle, m_CameraTargetHandle);
                }
            }
        }

        private static void ExecutePass(ref RenderingData renderingData, Material blitMaterial, RTHandle cameraTarget, RTHandle source)
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
            
            RenderingUtils.FinalBlit(cmd, ref cameraData, source, cameraTarget, loadAction, RenderBufferStoreAction.Store, blitMaterial, source.rt?.filterMode == FilterMode.Bilinear ? 1 : 0);
        }

        private class PassData
        {
            internal Material blitMaterial;
            internal TextureHandle source;
            internal TextureHandle destination;

            internal int sourceID;
            internal Vector4 hdrOutputLuminanceParams;

            internal RenderingData renderingData;
        }

        internal void Render(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle src, TextureHandle dest, TextureHandle overlayUITexture)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Final Blit", out var passData, base.profilingSampler))
            {
                bool outputsToHDR = renderingData.cameraData.isHDROutputActive;

                passData.blitMaterial = outputsToHDR ? m_BlitHDRMaterial : m_BlitMaterial;
                passData.source = src;
                passData.destination = dest;
                passData.renderingData = renderingData;
                passData.sourceID = ShaderPropertyId.sourceTex;

                builder.UseColorBuffer(passData.destination, 0);
                builder.ReadTexture(passData.source);

                if (outputsToHDR && overlayUITexture.IsValid())
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(tonemapping, out passData.hdrOutputLuminanceParams);

                    builder.ReadTexture(overlayUITexture);
                }
                else
                {
                    passData.hdrOutputLuminanceParams = new Vector4(-1.0f, -1.0f, -1.0f, -1.0f);
                }

                CoreUtils.SetKeyword(renderingData.commandBuffer, ShaderKeywordStrings.LinearToSRGBConversion,
                    renderingData.cameraData.requireSrgbConversion);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    data.blitMaterial.SetTexture(data.sourceID, data.source);

                    // TODO RENDERGRAPH: this should ideally be shared in ExecutePass to avoid code duplication
                    if (data.hdrOutputLuminanceParams.w >= 0)
                    {
                        // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                        HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.ColorEncoding;
                        if (!data.renderingData.cameraData.postProcessEnabled)
                            hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                        SetupHDROutput(data.blitMaterial, hdrOperation, data.hdrOutputLuminanceParams);
                    }

                    ExecutePass(ref data.renderingData, data.blitMaterial, data.destination, data.source);
                });
            }
        }
    }
}
