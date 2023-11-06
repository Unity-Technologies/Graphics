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

            // Disabling native render passes (for non-RG) because it renders to 2 different render targets
            useNativeRenderPass = false;
        }

        // Common to RenderGraph and non-RenderGraph paths
        private class PassDataCIExy
        {
            internal Material material;
            internal Vector4 luminanceParameters;
            internal TextureHandle srcColor;
            internal TextureHandle xyBuffer;
            internal TextureHandle passThrough;
        }

        private class PassDataDebugView
        {
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

        private static void ExecuteCIExyPrepass(CommandBuffer cmd, PassDataCIExy data, RTHandle sourceTexture, RTHandle xyTarget, RTHandle destTexture)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Generate HDR DebugView CIExy")))
            {
                CoreUtils.SetRenderTarget(cmd, destTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);

                var debugParameters = new Vector4(ShaderConstants._SizeOfHDRXYMapping, ShaderConstants._SizeOfHDRXYMapping, 0, 0);

                cmd.SetRandomWriteTarget(ShaderConstants._CIExyUAVIndex, xyTarget);
                data.material.SetVector(ShaderConstants._HDRDebugParamsId, debugParameters);
                data.material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, data.luminanceParameters);

                Vector2 viewportScale = sourceTexture.useScaling ? new Vector2(sourceTexture.rtHandleProperties.rtHandleScale.x, sourceTexture.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, sourceTexture, viewportScale, data.material, 0);

                cmd.ClearRandomWriteTargets();
            }
        }

        private static void ExecuteHDRDebugViewFinalPass(RasterCommandBuffer cmd, PassDataDebugView data, RTHandle sourceTexture, RTHandle destination, RTHandle xyTarget)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("HDR DebugView")))
            {
                if (data.cameraData.isHDROutputActive)
                {
                    HDROutputUtils.ConfigureHDROutput(data.material, data.cameraData.hdrDisplayColorGamut, HDROutputUtils.Operation.ColorEncoding);
                }

                data.material.SetTexture(ShaderConstants._xyTextureId, xyTarget);
                
                Vector4 debugParameters = new Vector4(ShaderConstants._SizeOfHDRXYMapping, ShaderConstants._SizeOfHDRXYMapping, 0, 0);
                data.material.SetVector(ShaderConstants._HDRDebugParamsId, debugParameters);
                data.material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, data.luminanceParameters);
                data.material.SetInteger(ShaderConstants._DebugHDRModeId, (int)data.hdrDebugMode);

                Vector2 viewportScale = sourceTexture.useScaling ? new Vector2(sourceTexture.rtHandleProperties.rtHandleScale.x, sourceTexture.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                bool yflip = data.cameraData.IsRenderTargetProjectionMatrixFlipped(destination);
                Vector4 scaleBias = !yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                RenderTargetIdentifier cameraTarget = BuiltinRenderTextureType.CameraTarget;
                #if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled)
                        cameraTarget = data.cameraData.xr.renderTarget;
                #endif

                if (destination.nameID == cameraTarget || data.cameraData.targetTexture != null)
                    cmd.SetViewport(data.cameraData.pixelRect);

                Blitter.BlitTexture(cmd, sourceTexture, scaleBias, data.material, 1);
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
            var cmd = renderingData.commandBuffer;
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

            ExecutePass(cmd, m_PassDataCIExy, m_PassDataDebugView, sourceTexture, m_CIExyTarget, m_CameraTargetHandle);
        }

        private void ExecutePass(CommandBuffer cmd, PassDataCIExy dataCIExy, PassDataDebugView dataDebugView, RTHandle sourceTexture, RTHandle xyTarget, RTHandle destTexture)
        {
            RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);

            //CIExyPrepass
            ExecuteCIExyPrepass(cmd, dataCIExy, sourceTexture, xyTarget, m_PassthroughRT);

            //HDR DebugView - should always be the last stack of the camera
            CoreUtils.SetRenderTarget(cmd, destTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            ExecuteHDRDebugViewFinalPass(rasterCmd, dataDebugView, m_PassthroughRT, destTexture, xyTarget);
            dataDebugView.cameraData.renderer.ConfigureCameraTarget(destTexture, destTexture);
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

            // Using low level pass because of random UAV support, and since this is a debug view, we don't care much about merging passes or optimizing for TBDR.
            // This could be a compute pass (like in HDRP) but doing it in pixel is compatible with devices that might support HDR output but not compute shaders.
            using (var builder = renderGraph.AddLowLevelPass<PassDataCIExy>("Generate HDR DebugView CIExy", out var passData, base.profilingSampler))
            {
                passData.material = m_material;
                passData.luminanceParameters = luminanceParameters;
                passData.srcColor = builder.UseTexture(srcColor);
                passData.xyBuffer = builder.UseTexture(xyBuffer, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                passData.passThrough = builder.UseTexture(passThroughRT, IBaseRenderGraphBuilder.AccessFlags.Write);

                builder.SetRenderFunc((PassDataCIExy data, LowLevelGraphContext context) =>
                {
                    ExecuteCIExyPrepass(context.legacyCmd, data, data.srcColor, data.xyBuffer, data.passThrough);
                });
            }
            using (var builder = renderGraph.AddRasterRenderPass<PassDataDebugView>("HDR DebugView", out var passData, base.profilingSampler))
            {
                passData.material = m_material;
                passData.hdrDebugMode = hDRDebugMode;
                passData.luminanceParameters = luminanceParameters;
                passData.cameraData = renderingData.cameraData;

                passData.xyBuffer = builder.UseTexture(xyBuffer);
                passData.passThrough = builder.UseTexture(passThroughRT);
                passData.dstColor = builder.UseTextureFragment(dstColor, 0, IBaseRenderGraphBuilder.AccessFlags.WriteAll);

                if (overlayUITexture.IsValid())
                    passData.overlayUITexture = builder.UseTexture(overlayUITexture);

                builder.SetRenderFunc((PassDataDebugView data, RasterGraphContext context) =>
                {
                    data.material.enabledKeywords = null;
                    ExecuteHDRDebugViewFinalPass(context.cmd, data, data.passThrough, data.dstColor, data.xyBuffer);
                });
            }
        }

        internal class ShaderConstants
        {
            public static readonly int _DebugHDRModeId = Shader.PropertyToID("_DebugHDRMode");
            public static readonly int _HDRDebugParamsId = Shader.PropertyToID("_HDRDebugParams");
            public static readonly int _xyTextureId = Shader.PropertyToID("_xyBuffer");
            public static readonly int _SizeOfHDRXYMapping = 512;
            public static readonly int _CIExyUAVIndex = 1;
        }
    }
}
