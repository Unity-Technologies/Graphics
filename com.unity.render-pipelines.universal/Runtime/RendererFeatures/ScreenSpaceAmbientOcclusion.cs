using System;

namespace UnityEngine.Rendering.Universal
{
    class ScreenSpaceAOPass : ScriptableRenderPass
    {
        Material m_ScreenSpaceAOMaterial;
        Texture2D m_NoiseTexture;
        RenderTargetHandle m_ScreenSpaceAmbientOcclusion;
        const string m_ProfilerTag = "Ambient Occlusion";
        bool m_Enabled;

        public ScreenSpaceAOPass(bool enabled, Material screenspaceAOMaterial, Texture2D noiseAsset)
        {
            m_Enabled = enabled;
            m_ScreenSpaceAOMaterial = screenspaceAOMaterial;
            m_NoiseTexture = noiseAsset;
            m_ScreenSpaceAmbientOcclusion.Init("_ScreenSpaceAOTexture");
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            return m_Enabled;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            AmbientOcclusion ambientOcclusion = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            // Setup descriptor
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            //descriptor.width /= 2;
            //descriptor.height /= 2;

            // Ambient Occlusion Settings
            m_ScreenSpaceAOMaterial.SetTexture("_NoiseTex", m_NoiseTexture);
            m_ScreenSpaceAOMaterial.SetFloat("_AO_Intensity", ambientOcclusion.intensity.value);
            m_ScreenSpaceAOMaterial.SetFloat("_AO_Radius", ambientOcclusion.radius.value);

            // SSAO settings
            m_ScreenSpaceAOMaterial.SetInt("_SSAO_Samples", ambientOcclusion.sampleCount.value);
            m_ScreenSpaceAOMaterial.SetFloat("_SSAO_Area", ambientOcclusion.area.value);

            cmd.GetTemporaryRT(m_ScreenSpaceAmbientOcclusion.id, descriptor, FilterMode.Point);

            RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceAmbientOcclusion.Identifier();
            ConfigureTarget(screenSpaceOcclusionTexture);
            ConfigureClear(ClearFlag.All, Color.white);
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
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ScreenSpaceAOMaterial, 0, 0);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }
            else
            {
                // Avoid setting and restoring camera view and projection matrices when in stereo.
                RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceAmbientOcclusion.Identifier();
                Blit(cmd, screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceAOMaterial);
            }
            //context.ExecuteCommandBuffer(cmd);
            //cmd.Clear();
            //md.Blit(m_ScreenSpaceAmbientOcclusion.Identifier(), m_ScreenSpaceAmbientOcclusion.Identifier(), m_ScreenSpaceAOMaterial, 1);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_ScreenSpaceAmbientOcclusion.id);
        }
    }
}

