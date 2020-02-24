using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    class ScreenSpaceAOPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier screenSpaceOcclusionTexture;

        private Material m_ScreenSpaceAOMaterial;
        private RenderTargetHandle m_ScreenSpaceAmbientOcclusion;
        private const string m_ProfilerTag = "Screen Space Ambient Occlusion";
        private bool m_Enabled;
        private bool m_IsDeferred = false;
        private bool m_HasDepthNormalTexture = false;
        private int m_DownScale = 1;

        public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
        public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

        private enum SSAOShaderPasses
        {
            OcclusionDepth = 0,
            OcclusionDepthNormals = 1,
            OcclusionGbuffer = 2,
            HorizontalBlurDepth = 3,
            HorizontalBlurDepthNormals = 4,
            HorizontalBlurGBuffer = 5,
            VerticalBlur = 6,
            FinalComposition = 7,
        }

        public ScreenSpaceAOPass(Material screenspaceAOMaterial, bool enabled)
        {
            m_Enabled = enabled;
            m_ScreenSpaceAOMaterial = screenspaceAOMaterial;
            m_ScreenSpaceAmbientOcclusion.Init("_ScreenSpaceAOTexture");
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        }

        public bool Setup(ref RenderingData renderingData, bool isDeferred, bool hasDepthNormalTexture)
        {
            m_IsDeferred = isDeferred;
            m_HasDepthNormalTexture = hasDepthNormalTexture;

            return m_Enabled;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            AmbientOcclusion ambientOcclusion = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            m_DownScale = ambientOcclusion.downSample.value ? 2 : 1;

            // Setup descriptor
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / m_DownScale;
            m_Descriptor.height = m_Descriptor.height / m_DownScale;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;

            // SSAO settings
            m_ScreenSpaceAOMaterial.SetFloat("_SSAO_Intensity", ambientOcclusion.intensity.value);
            m_ScreenSpaceAOMaterial.SetFloat("_SSAO_Radius", ambientOcclusion.radius.value);
            m_ScreenSpaceAOMaterial.SetInt("_SSAO_Samples", ambientOcclusion.sampleCount.value);

            cmd.GetTemporaryRT(m_ScreenSpaceAmbientOcclusion.id, m_Descriptor, FilterMode.Bilinear);

            var desc = GetStereoCompatibleDescriptor(m_Descriptor.width * m_DownScale, m_Descriptor.height * m_DownScale, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(_TempTarget, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_TempTarget2, desc, FilterMode.Bilinear);

            screenSpaceOcclusionTexture = m_ScreenSpaceAmbientOcclusion.Identifier();

            ConfigureTarget(screenSpaceOcclusionTexture);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ScreenSpaceAOMaterial == null)
            {
                Debug.LogErrorFormat(
                    "Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.",
                    m_ScreenSpaceAOMaterial, GetType().Name);
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            m_ScreenSpaceAOMaterial.SetMatrix("ProjectionMatrix", camera.projectionMatrix);
            bool stereo = renderingData.cameraData.isStereoEnabled;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            if (!stereo)
            {
                if (m_IsDeferred)
                {
                    ExecuteGBuffer(camera, cmd);
                }
                else if (!m_HasDepthNormalTexture)
                {
                    ExecuteDepthTexture(camera, cmd);
                }
                else
                {
                    ExecuteDepthNormalTexture(camera, cmd);
                }
            }
            else
            {
                ExecuteBlit(camera, cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteGBuffer(Camera camera, CommandBuffer cmd)
        {
            Debug.LogError("GBUFFER is not Implemented!!!!");
        }

        private void ExecuteDepthTexture(Camera camera, CommandBuffer cmd)
        {
            Debug.LogError("Only DEPTH is not Implemented!!!!");
        }

        private void ExecuteDepthNormalTexture(Camera camera, CommandBuffer cmd)
        {
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // Occlusion pass
            cmd.Blit(_TempTarget, _TempTarget, m_ScreenSpaceAOMaterial, (int)SSAOShaderPasses.OcclusionDepthNormals);

            // Horizontal Blur
            cmd.SetGlobalTexture("_MainTex", _TempTarget);
            cmd.Blit(_TempTarget, _TempTarget2, m_ScreenSpaceAOMaterial, (int)SSAOShaderPasses.HorizontalBlurDepthNormals);

            // Vertical Blur
            cmd.SetGlobalTexture("_MainTex", _TempTarget2);
            cmd.Blit(_TempTarget2, _TempTarget, m_ScreenSpaceAOMaterial, (int)SSAOShaderPasses.VerticalBlur);

            // Final Composition
            cmd.SetGlobalTexture("_MainTex", _TempTarget);
            cmd.Blit(_TempTarget, screenSpaceOcclusionTexture, m_ScreenSpaceAOMaterial, (int)SSAOShaderPasses.FinalComposition);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        private void ExecuteBlit(Camera camera, CommandBuffer cmd)
        {
            // Avoid setting and restoring camera view and projection matrices when in stereo.
            Blit(cmd, screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceAOMaterial);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            cmd.ReleaseTemporaryRT(m_ScreenSpaceAmbientOcclusion.id);
            cmd.ReleaseTemporaryRT(_TempTarget);
            cmd.ReleaseTemporaryRT(_TempTarget2);
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            // Inherit the VR setup from the camera descriptor
            var desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}

