using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
            internal UniversalCameraData cameraData;
            internal Vector4 luminanceParameters;
            internal TextureHandle overlayUITexture;
            internal TextureHandle xyBuffer;
            internal TextureHandle srcColor;
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

        internal static Vector4 GetLuminanceParameters(UniversalCameraData cameraData)
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
                CoreUtils.SetRenderTarget(cmd, destTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, ClearFlag.None, Color.clear);

                Vector4 debugParameters = new Vector4(ShaderConstants._SizeOfHDRXYMapping, ShaderConstants._SizeOfHDRXYMapping, 0, 0);

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

                Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(sourceTexture, destination, data.cameraData);

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
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="cameraData">Descriptor for the color buffer.</param>
        /// <param name="hdrdebugMode">Active DebugMode for HDR.</param>
        public void Setup(UniversalCameraData cameraData, HDRDebugMode hdrdebugMode)
        {
            m_PassDataDebugView.hdrDebugMode = hdrdebugMode;

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, cameraData.pixelWidth, cameraData.pixelHeight);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_PassthroughRT, descriptor, name: "_HDRDebugDummyRT");

            RenderTextureDescriptor descriptorCIE = cameraData.cameraTargetDescriptor;
            HDRDebugViewPass.ConfigureDescriptorForCIEPrepass(ref descriptorCIE);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_CIExyTarget, descriptorCIE, name: "_xyBuffer");
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            var cmd = renderingData.commandBuffer;
            m_PassDataCIExy.luminanceParameters = m_PassDataDebugView.luminanceParameters = GetLuminanceParameters(cameraData);
            m_PassDataDebugView.cameraData = cameraData;

            var sourceTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            // Get RTHandle alias to use RTHandle apis
            RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
            var cameraTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

            m_material.enabledKeywords = null;
            GetActiveDebugHandler(cameraData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, cameraData, true);

            CoreUtils.SetRenderTarget(cmd, m_CIExyTarget, ClearFlag.Color, Color.clear);

            ExecutePass(cmd, m_PassDataCIExy, m_PassDataDebugView, sourceTexture, m_CIExyTarget, cameraTargetHandle);
        }

        private void ExecutePass(CommandBuffer cmd, PassDataCIExy dataCIExy, PassDataDebugView dataDebugView, RTHandle sourceTexture, RTHandle xyTarget, RTHandle destTexture)
        {
            RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);

            //CIExyPrepass
            bool requiresCIExyData = dataDebugView.hdrDebugMode != HDRDebugMode.ValuesAbovePaperWhite;
            if (requiresCIExyData)
            {
                ExecuteCIExyPrepass(cmd, dataCIExy, sourceTexture, xyTarget, m_PassthroughRT);
            }

            //HDR DebugView - should always be the last stack of the camera
            CoreUtils.SetRenderTarget(cmd, destTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            ExecuteHDRDebugViewFinalPass(rasterCmd, dataDebugView, sourceTexture, destTexture, xyTarget);

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            dataDebugView.cameraData.renderer.ConfigureCameraTarget(destTexture, destTexture);
            #pragma warning restore CS0618
        }

        //RenderGraph path
        internal void RenderHDRDebug(RenderGraph renderGraph, UniversalCameraData cameraData, TextureHandle srcColor, TextureHandle overlayUITexture, TextureHandle dstColor, HDRDebugMode hdrDebugMode)
        {
            bool requiresCIExyData = hdrDebugMode != HDRDebugMode.ValuesAbovePaperWhite;
            Vector4 luminanceParameters = GetLuminanceParameters(cameraData);

            TextureHandle intermediateRT = srcColor;
            TextureHandle xyBuffer = TextureHandle.nullHandle;

            if (requiresCIExyData)
            {
                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, cameraData.pixelWidth, cameraData.pixelHeight);
                intermediateRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_HDRDebugDummyRT", false);

                ConfigureDescriptorForCIEPrepass(ref descriptor);
                xyBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_xyBuffer", true);

                // Using low level pass because of random UAV support, and since this is a debug view, we don't care much about merging passes or optimizing for TBDR.
                // This could be a compute pass (like in HDRP) but doing it in pixel is compatible with devices that might support HDR output but not compute shaders.
                using (var builder = renderGraph.AddUnsafePass<PassDataCIExy>("Generate HDR DebugView CIExy", out var passData, base.profilingSampler))
                {
                    passData.material = m_material;
                    passData.luminanceParameters = luminanceParameters;
                    passData.srcColor = srcColor;
                    builder.UseTexture(srcColor);
                    passData.xyBuffer = xyBuffer;
                    builder.UseTexture(xyBuffer, AccessFlags.Write);
                    passData.passThrough = intermediateRT;
                    builder.UseTexture(intermediateRT, AccessFlags.Write);

                    builder.SetRenderFunc((PassDataCIExy data, UnsafeGraphContext context) =>
                    {
                        ExecuteCIExyPrepass(CommandBufferHelpers.GetNativeCommandBuffer(context.cmd), data, data.srcColor, data.xyBuffer, data.passThrough);
                    });
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassDataDebugView>("HDR DebugView", out var passData, base.profilingSampler))
            {
                passData.material = m_material;
                passData.hdrDebugMode = hdrDebugMode;
                passData.luminanceParameters = luminanceParameters;
                passData.cameraData = cameraData;

                if (requiresCIExyData)
                {
                    passData.xyBuffer = xyBuffer;
                    builder.UseTexture(xyBuffer);
                }

                passData.srcColor = srcColor;
                builder.UseTexture(srcColor);
                passData.dstColor = dstColor;
                builder.SetRenderAttachment(dstColor, 0, AccessFlags.WriteAll);

                if (overlayUITexture.IsValid())
                {
                    passData.overlayUITexture = overlayUITexture;
                    builder.UseTexture(overlayUITexture);
                }

                builder.SetRenderFunc((PassDataDebugView data, RasterGraphContext context) =>
                {
                    data.material.enabledKeywords = null;
                    ExecuteHDRDebugViewFinalPass(context.cmd, data, data.srcColor, data.dstColor, data.xyBuffer);
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
