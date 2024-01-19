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
        private PassData m_PassData;

        // Use specialed URP fragment shader pass for debug draw support and color space conversion/encoding support.
        // See CoreBlit.shader and BlitHDROverlay.shader
        static class BlitPassNames
        {
            public const string NearestSampler = "NearestDebugDraw";
            public const string BilinearSampler = "BilinearDebugDraw";
        }

        enum BlitType
        {
            Core = 0, // Core blit
            HDR = 1, // Blit with HDR encoding and overlay UI compositing
            Count = 2
        }

        struct BlitMaterialData
        {
            public Material material;
            public int nearestSamplerPass;
            public int bilinearSamplerPass;
        }

        BlitMaterialData[] m_BlitMaterialData;

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
            m_PassData = new PassData();
            renderPassEvent = evt;

            // Find sampler passes by name
            const int blitTypeCount = (int)BlitType.Count;
            m_BlitMaterialData = new BlitMaterialData[blitTypeCount];
            for (int i = 0; i < blitTypeCount; ++i)
            {
                m_BlitMaterialData[i].material = i == (int)BlitType.Core ? blitMaterial : blitHDRMaterial;
                m_BlitMaterialData[i].nearestSamplerPass = m_BlitMaterialData[i].material?.FindPass(BlitPassNames.NearestSampler) ?? -1;
                m_BlitMaterialData[i].bilinearSamplerPass = m_BlitMaterialData[i].material?.FindPass(BlitPassNames.BilinearSampler) ?? -1;
            }
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
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

        static void SetupHDROutput(ColorGamut hdrDisplayColorGamut, Material material, HDROutputUtils.Operation hdrOperation, Vector4 hdrOutputParameters)
        {
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputParameters);
            HDROutputUtils.ConfigureHDROutput(material, hdrDisplayColorGamut, hdrOperation);
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            DebugHandler debugHandler = GetActiveDebugHandler(ref renderingData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref cameraData);

            if (resolveToDebugScreen)
            {
                ConfigureTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool outputsToHDR = renderingData.cameraData.isHDROutputActive;
            InitPassData(ref renderingData, ref m_PassData, outputsToHDR ? BlitType.HDR : BlitType.Core);

            if (m_PassData.blitMaterialData.material == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_PassData.blitMaterialData, GetType().Name);
                return;
            }

            ref CameraData cameraData = ref renderingData.cameraData;

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            DebugHandler debugHandler = GetActiveDebugHandler(ref renderingData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref cameraData);

            // Get RTHandle alias to use RTHandle apis
            RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
            var cameraTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

            var cmd = renderingData.commandBuffer;

            if (m_Source == cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.FinalBlit)))
            {
                m_PassData.blitMaterialData.material.enabledKeywords = null;

                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref cameraData, !resolveToDebugScreen);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LinearToSRGBConversion,
                    m_PassData.requireSrgbConversion);

                if (outputsToHDR)
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();

                    Vector4 hdrOutputLuminanceParams;
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, tonemapping, out hdrOutputLuminanceParams);

                    HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.None;
                    // If the HDRDebugView is on, we don't want the encoding
                    if (debugHandler == null || !debugHandler.HDRDebugViewIsActive(ref cameraData))
                        hdrOperation |= HDROutputUtils.Operation.ColorEncoding;
                    // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                    if (!cameraData.postProcessEnabled)
                        hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                    SetupHDROutput(cameraData.hdrDisplayColorGamut, m_PassData.blitMaterialData.material, hdrOperation, hdrOutputLuminanceParams);
                }

                if (resolveToDebugScreen)
                {
                    int shaderPassIndex = m_Source.rt?.filterMode == FilterMode.Bilinear ? m_PassData.blitMaterialData.bilinearSamplerPass : m_PassData.blitMaterialData.nearestSamplerPass;

                    Vector2 viewportScale = m_Source.useScaling ? new Vector2(m_Source.rtHandleProperties.rtHandleScale.x, m_Source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, m_Source, viewportScale, m_PassData.blitMaterialData.material, shaderPassIndex);

                    cameraData.renderer.ConfigureCameraTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                }
                else
                {
                    FinalBlitPass.ExecutePass(ref renderingData, m_PassData.blitMaterialData, cameraTargetHandle, m_Source);
                    cameraData.renderer.ConfigureCameraTarget(cameraTargetHandle, cameraTargetHandle);
                }
            }
        }

        private static void ExecutePass(ref RenderingData renderingData, in BlitMaterialData blitMaterialData, RTHandle cameraTarget, RTHandle source)
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

            int shaderPassIndex = source.rt?.filterMode == FilterMode.Bilinear ? blitMaterialData.bilinearSamplerPass : blitMaterialData.nearestSamplerPass;
            RenderingUtils.FinalBlit(cmd, ref cameraData, source, cameraTarget, loadAction, RenderBufferStoreAction.Store, blitMaterialData.material, shaderPassIndex);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;

            internal int sourceID;
            internal Vector4 hdrOutputLuminanceParams;
            internal bool requireSrgbConversion;
            internal BlitMaterialData blitMaterialData;
            internal RenderingData renderingData;
        }

        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        private void InitPassData(ref RenderingData renderingData, ref PassData passData, BlitType blitType)
        {
            passData.renderingData = renderingData;
            passData.requireSrgbConversion = renderingData.cameraData.requireSrgbConversion;

            passData.blitMaterialData = m_BlitMaterialData[(int)blitType];
        }

        internal void Render(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle src, TextureHandle dest, TextureHandle overlayUITexture)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Final Blit", out var passData, base.profilingSampler))
            {
                bool outputsToHDR = renderingData.cameraData.isHDROutputActive;
                passData.source = src;
                passData.destination = dest;
                passData.sourceID = ShaderPropertyId.sourceTex;

                InitPassData(ref renderingData, ref passData, outputsToHDR ? BlitType.HDR : BlitType.Core);

                builder.UseColorBuffer(passData.destination, 0);
                builder.ReadTexture(passData.source);

                if (outputsToHDR && overlayUITexture.IsValid())
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();
                    ref CameraData cameraData = ref renderingData.cameraData;
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, tonemapping, out passData.hdrOutputLuminanceParams);

                    builder.ReadTexture(overlayUITexture);
                }
                else
                {
                    passData.hdrOutputLuminanceParams = new Vector4(-1.0f, -1.0f, -1.0f, -1.0f);
                }

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    data.blitMaterialData.material.enabledKeywords = null;

                    CoreUtils.SetKeyword(context.cmd, ShaderKeywordStrings.LinearToSRGBConversion, data.requireSrgbConversion);
                    data.blitMaterialData.material.SetTexture(data.sourceID, data.source);

                    DebugHandler debugHandler = GetActiveDebugHandler(ref data.renderingData);
                    bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref data.renderingData.cameraData);

                    // TODO RENDERGRAPH: this should ideally be shared in ExecutePass to avoid code duplication
                    if (data.hdrOutputLuminanceParams.w >= 0)
                    {
                        HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.None;
                        // If the HDRDebugView is on, we don't want the encoding
                        if (debugHandler == null || !debugHandler.HDRDebugViewIsActive(ref data.renderingData.cameraData))
                            hdrOperation |= HDROutputUtils.Operation.ColorEncoding;

                        // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                        if (!data.renderingData.cameraData.postProcessEnabled)
                            hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                        SetupHDROutput(data.renderingData.cameraData.hdrDisplayColorGamut, data.blitMaterialData.material, hdrOperation, data.hdrOutputLuminanceParams);
                    }

                    if (resolveToDebugScreen)
                    {
                        RTHandle sourceTex = data.source;
                        Vector2 viewportScale = sourceTex.useScaling ? new Vector2(sourceTex.rtHandleProperties.rtHandleScale.x, sourceTex.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                        int shaderPassIndex = sourceTex.rt?.filterMode == FilterMode.Bilinear ? data.blitMaterialData.bilinearSamplerPass : data.blitMaterialData.nearestSamplerPass;
                        Blitter.BlitTexture(context.cmd, sourceTex, viewportScale, data.blitMaterialData.material, shaderPassIndex);
                    }
                    else
                        ExecutePass(ref data.renderingData, data.blitMaterialData, data.destination, data.source);
                });
            }
        }
    }
}
