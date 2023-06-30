using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Generate HDR debug data into the given color target
    /// </summary>
    internal class HDRDebugViewPass : ScriptableRenderPass
    {
        private enum HDRDebugPassId
        {
            CIExyPrepass = 0,
            DebugViewPass = 1
        }

        PassDataCIExy m_PassDataCIExy;
        PassDataDebugView m_PassDataDebugView;
        RTHandle m_CIExyTarget;     // xyBuffer;
        RTHandle m_PassthroughRT;
        RTHandle m_CameraTargetHandle;
        Material m_material;

        /// <summary>
        /// Creates a new <c>HDRDebugViewPass</c> instance.
        /// </summary>
        /// <param name="mat">The <c>Material</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public HDRDebugViewPass(Material mat)
        {
            base.profilingSampler = new ProfilingSampler(nameof(HDRDebugViewPass));
            renderPassEvent = RenderPassEvent.AfterRendering + 3;
            m_PassDataCIExy = new PassDataCIExy() { material = mat };
            m_PassDataDebugView = new PassDataDebugView() { material = mat };
            m_material = mat;
        }

        // Common to RenderGraph and non-RenderGraph paths
        private class PassDataCIExy
        {
            internal CommandBuffer cmd;
            internal Material material;
            internal Vector4 luminanceParameters;
            internal TextureHandle srcColor;
            internal TextureHandle xyBuffer;
            internal TextureHandle passThrough;
        }

        private class PassDataDebugView
        {
            internal CommandBuffer cmd;
            internal Material material;
            internal HDRDebugMode hdrDebugMode;
            internal CameraData cameraData;
            internal Vector4 luminanceParameters;
            internal TextureHandle overlayUITexture;
            internal TextureHandle xyBuffer;
            internal TextureHandle passThrough;
            internal TextureHandle dstColor;
        }

        public static void ConfigureDescriptorForCIEPrepass(ref RenderTextureDescriptor descriptor)
        {
            descriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
            descriptor.width = descriptor.height = ShaderConstants._SizeOfHDRXYMapping;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.useDynamicScale = true;
            descriptor.depthBufferBits = (int)DepthBits.None;
            descriptor.enableRandomWrite = true;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;
            descriptor.vrUsage = VRTextureUsage.None; // We only need one for both eyes in VR
        }

        internal static Vector4 GetLuminanceParameters(CameraData cameraData)
        {
            var luminanceParams = Vector4.zero;
            if (cameraData.isHDROutputActive)
            {
                Tonemapping tonemapping = VolumeManager.instance.stack.GetComponent<Tonemapping>();
                UniversalRenderPipeline.GetHDROutputLuminanceParameters(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, tonemapping, out luminanceParams);
            }
            else
            {
                luminanceParams.z = 1.0f;
            }
            return luminanceParams;
        }

        private void ExecutePass(PassDataCIExy dataCIExy, PassDataDebugView dataDebugView, RTHandle sourceTexture, RTHandle xyTarget, RTHandle destTexture)
        {
            //CIExyPrepass
            ExecuteCIExyPrepass(dataCIExy, sourceTexture, m_PassthroughRT, xyTarget);

            //HDR DebugView - should always be the last stack of the camera
            RTHandle overlayUITexture = null; // this is null when not using rendergraph path, as it is bound earlier to the CommandBuffer
            ExecuteHDRDebugViewFinalPass(dataDebugView, m_PassthroughRT, xyTarget, overlayUITexture, destTexture);
            dataDebugView.cameraData.renderer.ConfigureCameraTarget(destTexture, destTexture);
        }

        private static void ExecuteCIExyPrepass(PassDataCIExy data, RTHandle sourceTexture, RTHandle passThroughRT, RTHandle xyTarget)
        {
            var cmd = data.cmd;
            Vector2 viewportScale = sourceTexture.useScaling ? new Vector2(sourceTexture.rtHandleProperties.rtHandleScale.x, sourceTexture.rtHandleProperties.rtHandleScale.y) : Vector2.one;

            using (new ProfilingScope(cmd, new ProfilingSampler("Generate HDR DebugView CIExy")))
            {
                var debugParameters = new Vector4(ShaderConstants._SizeOfHDRXYMapping, ShaderConstants._SizeOfHDRXYMapping, 0, data.luminanceParameters.w /*colorPrimaries*/);
                cmd.SetRandomWriteTarget(ShaderConstants._CIExyUAVIndex, xyTarget);
                cmd.SetGlobalTexture(ShaderConstants._DebugScreenTexturePropertyId, sourceTexture);
                cmd.SetGlobalVector(ShaderConstants._HDRDebugParamsId, debugParameters);

                CoreUtils.SetRenderTarget(cmd, passThroughRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
                Blitter.BlitTexture(cmd, viewportScale, data.material, 0);
            }
        }

        private static void ExecuteHDRDebugViewFinalPass(PassDataDebugView data, RTHandle sourceTexture, RTHandle xyTarget, RTHandle overlayUITexture, RTHandle destTexture)
        {
            var cmd = data.cmd;

            using (new ProfilingScope(cmd, new ProfilingSampler("HDR DebugView")))
            {
                if (data.cameraData.isHDROutputActive)
                {
                    HDROutputUtils.ConfigureHDROutput(data.material, data.cameraData.hdrDisplayColorGamut, HDROutputUtils.Operation.ColorEncoding);
                }

                cmd.ClearRandomWriteTargets();
                cmd.SetGlobalTexture(ShaderConstants._SourceTextureId, sourceTexture);
                cmd.SetGlobalTexture(ShaderConstants._xyTextureId, xyTarget);
                if (overlayUITexture != null) // this is null when not using rendergraph path, as it is bound earlier to the CommandBuffer
                    cmd.SetGlobalTexture(ShaderPropertyId.overlayUITexture, overlayUITexture);
                cmd.SetGlobalVector(ShaderConstants._HDRDebugParamsId, data.luminanceParameters);
                cmd.SetGlobalInteger(ShaderConstants._DebugHDRModeId, (int)data.hdrDebugMode);
                RenderingUtils.FinalBlit(cmd, ref data.cameraData, sourceTexture, destTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material, 1);
            }
        }

        // Non-RenderGraph path
        public void Dispose()
        {
            m_CIExyTarget?.Release();
            m_PassthroughRT?.Release();
            m_CameraTargetHandle?.Release();
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="cameraData">Descriptor for the color buffer.</param>
        /// <param name="hdrdebugMode">Active DebugMode for HDR.</param>
        public void Setup(ref CameraData cameraData, HDRDebugMode hdrdebugMode)
        {
            m_PassDataDebugView.hdrDebugMode = hdrdebugMode;

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, cameraData.pixelWidth, cameraData.pixelHeight);
            RenderingUtils.ReAllocateIfNeeded(ref m_PassthroughRT, descriptor, name: "_HDRDebugDummyRT");

            RenderTextureDescriptor descriptorCIE = cameraData.cameraTargetDescriptor;
            HDRDebugViewPass.ConfigureDescriptorForCIEPrepass(ref descriptorCIE);
            RenderingUtils.ReAllocateIfNeeded(ref m_CIExyTarget, descriptorCIE, name: "_xyBuffer");
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = m_PassDataCIExy.cmd = m_PassDataDebugView.cmd = renderingData.commandBuffer;
            m_PassDataCIExy.luminanceParameters = m_PassDataDebugView.luminanceParameters = GetLuminanceParameters(renderingData.cameraData);
            m_PassDataDebugView.cameraData = renderingData.cameraData;

            var sourceTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            // Create RTHandle alias to use RTHandle apis
            if (m_CameraTargetHandle != cameraTarget)
            {
                m_CameraTargetHandle?.Release();
                m_CameraTargetHandle = RTHandles.Alloc(cameraTarget);
            }

            m_material.enabledKeywords = null;
            GetActiveDebugHandler(ref renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref renderingData.cameraData, true);

            CoreUtils.SetRenderTarget(cmd, m_CIExyTarget, ClearFlag.Color, Color.clear);

            ExecutePass(m_PassDataCIExy, m_PassDataDebugView, sourceTexture, m_CIExyTarget, m_CameraTargetHandle);
        }

        //RenderGraph path
        internal void RenderHDRDebug(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle srcColor, TextureHandle overlayUITexture, TextureHandle dstColor, HDRDebugMode hDRDebugMode)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
            var passThroughRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_HDRDebugDummyRT", false);

            ConfigureDescriptorForCIEPrepass(ref descriptor);
            var xyBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_xyBuffer", true);

            var luminanceParameters = GetLuminanceParameters(renderingData.cameraData);

            using (var builder = renderGraph.AddRasterRenderPass<PassDataCIExy>("Generate HDR DebugView CIExy", out var passData, base.profilingSampler))
            {
                passData.cmd = renderingData.commandBuffer;
                passData.material = m_material;
                passData.luminanceParameters = luminanceParameters;
                passData.srcColor = srcColor;
                passData.xyBuffer = xyBuffer;
                passData.passThrough = passThroughRT;
                builder.UseTextureFragment(passThroughRT, 0);
                builder.UseTexture(xyBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(srcColor);
                builder.SetRenderFunc((PassDataCIExy data, RasterGraphContext context) =>
                {
                    ExecuteCIExyPrepass(data, data.srcColor, data.passThrough, data.xyBuffer);
                });
            }
            using (var builder = renderGraph.AddRasterRenderPass<PassDataDebugView>("HDR DebugView", out var passData, base.profilingSampler))
            {
                passData.cmd = renderingData.commandBuffer;
                passData.material = m_material;
                passData.hdrDebugMode = hDRDebugMode;
                passData.luminanceParameters = luminanceParameters;
                passData.cameraData = renderingData.cameraData;
                passData.overlayUITexture = overlayUITexture;
                passData.xyBuffer = xyBuffer;
                passData.passThrough = passThroughRT;
                passData.dstColor = dstColor;
                builder.UseTextureFragment(dstColor, 0);
                builder.UseTexture(passThroughRT, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(xyBuffer);
                if (overlayUITexture.IsValid())
                    builder.UseTexture(overlayUITexture);
                builder.SetRenderFunc((PassDataDebugView data, RasterGraphContext context) =>
                {
                    ExecuteHDRDebugViewFinalPass(data, data.passThrough, data.xyBuffer, data.overlayUITexture, data.dstColor);
                });
            }
        }

        internal class ShaderConstants
        {
            public static readonly int _DebugHDRModeId = Shader.PropertyToID("_DebugHDRMode");
            public static readonly int _HDRDebugParamsId = Shader.PropertyToID("_HDRDebugParams");
            public static readonly int _SourceTextureId = Shader.PropertyToID("_SourceTexture");
            public static readonly int _xyTextureId = Shader.PropertyToID("_xyBuffer");
            public static readonly int _DebugScreenTexturePropertyId = Shader.PropertyToID("_DebugScreenTexture");
            public static readonly int _SizeOfHDRXYMapping = 512;
            public static readonly int _CIExyUAVIndex = 1;
        }
    }
}
