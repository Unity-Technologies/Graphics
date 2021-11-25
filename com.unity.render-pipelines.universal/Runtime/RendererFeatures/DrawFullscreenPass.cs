using System;

namespace UnityEngine.Rendering.Universal
{
    internal enum DrawFullscreenBufferType
    {
        CameraColor,
        Custom
    }

    [Serializable]
    internal class DrawFullscreenSettings
    {
        [SerializeField] internal RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField] internal bool requiresNormalTexture = false;
        [SerializeField] internal bool requiresMotionVectorTexture = false;
        [SerializeField] internal Material blitMaterial;
        [SerializeField] internal int blitMaterialPassIndex;
        [SerializeField] internal DrawFullscreenBufferType source = DrawFullscreenBufferType.CameraColor;
        [SerializeField] internal DrawFullscreenBufferType destination = DrawFullscreenBufferType.CameraColor;
    }

    [ExcludeFromPreset]
    [Tooltip("Draw a fullscreen effect on screen using the material in parameter.")]
    internal class DrawFullscreenPass : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField] private DrawFullscreenSettings m_Settings = new DrawFullscreenSettings();

        private FullscreenPass m_FullscreenPass = null;

        /// <inheritdoc/>
        public override void Create()
        {
            if (m_FullscreenPass == null)
                m_FullscreenPass = new FullscreenPass();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool shouldAdd = m_FullscreenPass.Setup(m_Settings, renderer, name);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_FullscreenPass);
            }
        }

        // The Fullscreen Pass
        private class FullscreenPass : ScriptableRenderPass
        {
            static readonly int k_TemporaryRTId = Shader.PropertyToID("_TempFullscreenRT");
            static readonly int k_CustomRTId = Shader.PropertyToID("_CustomColorRT");

            ScriptableRenderer m_Renderer = null;
            DrawFullscreenSettings m_Settings;
            bool m_IsSourceAndDestinationSameTarget;
            RenderTargetIdentifier m_Source;
            RenderTargetIdentifier m_Destination;
            RenderTargetIdentifier m_Temp;
            string m_ProfilerTagName;

            internal bool Setup(DrawFullscreenSettings featureSettings, ScriptableRenderer renderer, string profilerTagName)
            {
                m_Renderer = renderer;
                m_Settings = featureSettings;
                m_ProfilerTagName = profilerTagName;
                renderPassEvent = featureSettings.injectionPoint;

                ConfigureInput(ScriptableRenderPassInput.Depth); // We need the depth for fullscreen effects that relies on position
                if (featureSettings.requiresNormalTexture)
                    ConfigureInput(ScriptableRenderPassInput.Normal);
                if (featureSettings.requiresMotionVectorTexture)
                    ConfigureInput(ScriptableRenderPassInput.Motion);
                if (featureSettings.source == DrawFullscreenBufferType.CameraColor)
                    ConfigureInput(ScriptableRenderPassInput.Color);

                return m_Settings.blitMaterial != null
                    && m_Settings.blitMaterial.passCount > m_Settings.blitMaterialPassIndex
                    && m_Settings.blitMaterialPassIndex >= 0;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                blitTargetDescriptor.depthBufferBits = 0;

                m_IsSourceAndDestinationSameTarget = m_Settings.source == m_Settings.destination &&
                    (m_Settings.source == DrawFullscreenBufferType.CameraColor);

                var renderer = renderingData.cameraData.renderer;

                if (m_Settings.source == DrawFullscreenBufferType.Custom || m_Settings.destination == DrawFullscreenBufferType.Custom)
                    cmd.GetTemporaryRT(k_CustomRTId, blitTargetDescriptor, FilterMode.Bilinear);

                if (m_Settings.source == DrawFullscreenBufferType.CameraColor)
                    m_Source = renderer.cameraColorTargetHandle;
                else
                    m_Source = new RenderTargetIdentifier(k_CustomRTId);

                if (m_Settings.destination == DrawFullscreenBufferType.CameraColor)
                    m_Destination = renderer.cameraColorTargetHandle;
                else
                    m_Destination = new RenderTargetIdentifier(k_CustomRTId);

                if (m_IsSourceAndDestinationSameTarget)
                {
                    cmd.GetTemporaryRT(k_TemporaryRTId, blitTargetDescriptor, FilterMode.Bilinear);
                    m_Temp = new RenderTargetIdentifier(k_TemporaryRTId);
                }
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTagName);

                cmd.SetGlobalTexture("_CameraOpaqueTexture", renderingData.cameraData.renderer.cameraColorTargetHandle);
                // Can't read and write to same color target, create a temp render target to blit.
                if (m_IsSourceAndDestinationSameTarget)
                {
                    Blitter.BlitTexture(cmd, m_Source, m_Temp, m_Settings.blitMaterial, m_Settings.blitMaterialPassIndex);
                    cmd.Blit(m_Temp, m_Destination);
                }
                else
                {
                    Blitter.BlitTexture(cmd, m_Source, m_Destination, m_Settings.blitMaterial, m_Settings.blitMaterialPassIndex);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <inheritdoc/>
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                    throw new ArgumentNullException("cmd");

                if (m_IsSourceAndDestinationSameTarget)
                    cmd.ReleaseTemporaryRT(k_TemporaryRTId);

                if (m_Settings.source == DrawFullscreenBufferType.Custom || m_Settings.destination == DrawFullscreenBufferType.Custom)
                    cmd.ReleaseTemporaryRT(k_CustomRTId);
            }
        }
    }
}
