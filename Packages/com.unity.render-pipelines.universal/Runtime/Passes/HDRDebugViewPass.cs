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

        PassData m_PassData;
        RTHandle m_CIExyTarget;     // xyBuffer;
        RTHandle m_PassthroughRT;
        RTHandle m_CameraTargetHandle;

        /// <summary>
        /// Creates a new <c>HDRDebugViewPass</c> instance.
        /// </summary>
        /// <param name="mat">The <c>Material</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public HDRDebugViewPass(Material mat)
        {
            base.profilingSampler = new ProfilingSampler(nameof(HDRDebugViewPass));
            renderPassEvent = RenderPassEvent.AfterRendering + 3;
            m_PassData = new PassData() { material = mat };

            // Disabling native render passes (for non-RG) because it renders to 2 different render targets
            useNativeRenderPass = false;
        }

        // Common to RenderGraph and non-RenderGraph paths
        private class PassData
        {
            internal Material material;
            internal HDRDebugMode hdrDebugMode;
            internal Vector4 luminanceParameters;
            internal CameraData cameraData;
        }

        /// <summary>
        /// Get a descriptor for the required color texture for this pass
        /// </summary>
        /// <param name="descriptor"></param>
        /// <seealso cref="RenderTextureDescriptor"/>
        public static void ConfigureDescriptor(ref RenderTextureDescriptor descriptor)
        {
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.useDynamicScale = true;
            descriptor.depthBufferBits = (int)DepthBits.None;
        }

        public static void ConfigureDescriptorForCIEPrepass(ref RenderTextureDescriptor descriptor)
        {
            descriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
            descriptor.width = descriptor.height = ShaderConstants._SizeOfHDRXYMapping;
            descriptor.enableRandomWrite = true;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;
            descriptor.vrUsage = VRTextureUsage.None; // We only need one for both eyes in VR
        }

        internal static Vector4 GetLuminanceParameters(ref CameraData cameraData)
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

        private static void ExecuteCIExyPrepass(CommandBuffer cmd, PassData data, RTHandle sourceTexture, RTHandle xyTarget, RTHandle destTexture)
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

        private static void ExecuteHDRDebugViewFinalPass(CommandBuffer cmd, PassData data,RTHandle sourceTexture, RTHandle destination, RTHandle xyTarget)
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

                RenderingUtils.FinalBlit(cmd, ref data.cameraData, sourceTexture, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material, 1);
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
        /// <param name="hdrdebugMode">Active DebugMode for HDR.</param>
        public void Setup(ref CameraData cameraData, HDRDebugMode hdrdebugMode)
        {
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, cameraData.pixelWidth, cameraData.pixelHeight);
            RenderingUtils.ReAllocateIfNeeded(ref m_PassthroughRT, descriptor, name: "_HDRDebugDummyRT");

            ConfigureDescriptorForCIEPrepass(ref descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref m_CIExyTarget, descriptor, name: "_xyBuffer");

            m_PassData.hdrDebugMode = hdrdebugMode;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            m_PassData.luminanceParameters = GetLuminanceParameters(ref renderingData.cameraData);
            m_PassData.cameraData = renderingData.cameraData;

            var sourceTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            // Create RTHandle alias to use RTHandle apis
            if (m_CameraTargetHandle != cameraTarget)
            {
                m_CameraTargetHandle?.Release();
                m_CameraTargetHandle = RTHandles.Alloc(cameraTarget);
            }

            m_PassData.material.enabledKeywords = null;
            GetActiveDebugHandler(ref renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref m_PassData.cameraData, true);

            CoreUtils.SetRenderTarget(cmd, m_CIExyTarget, ClearFlag.Color, Color.clear);

            ExecutePass(cmd, m_PassData, sourceTexture, m_CIExyTarget);
        }
        
        private void ExecutePass(CommandBuffer cmd, PassData data, RTHandle sourceTexture, RTHandle xyTarget)
        {
            //CIExyPrepass
            ExecuteCIExyPrepass(cmd, data, sourceTexture, xyTarget, m_PassthroughRT);

            //HDR DebugView - should always be the last stack of the camera
            ExecuteHDRDebugViewFinalPass(cmd, data, m_PassthroughRT, m_CameraTargetHandle, xyTarget);
            data.cameraData.renderer.ConfigureCameraTarget(m_CameraTargetHandle, m_CameraTargetHandle);
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
