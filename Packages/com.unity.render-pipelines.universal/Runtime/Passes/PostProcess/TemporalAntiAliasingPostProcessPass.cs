using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class TemporalAntiAliasingPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorTemporalAA";

        Material m_Material;
        bool m_IsValid;
        uint m_WarnCounter;

        public TemporalAntiAliasingPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;

            m_WarnCounter = 0;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var postProcessingData = frameData.Get<UniversalPostProcessingData>();
            if (postProcessingData.activeUpscaler != null && postProcessingData.activeUpscaler.isTemporal)
                return; // we are using a temporal upscaler that uses jitter, skip TAA pass

            // Note that enabling camera jitter uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides
            // then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking it locally here.
            if (!cameraData.IsTemporalAAEnabled())
            {
                // Warn users if TAA is disabled despite being requested
                if (cameraData.IsTemporalAARequested())
                    TemporalAA.ValidateAndWarn(cameraData, ref m_WarnCounter, false);
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, sourceTexture, k_TargetName, false, FilterMode.Bilinear);

            TextureHandle cameraDepth = resourceData.cameraDepth;
            TextureHandle motionVectors = resourceData.motionVectorColor;

            Debug.Assert(motionVectors.IsValid(), "MotionVectors are invalid. TAA requires a motion vector texture.");

            TemporalAA.Render(renderGraph, m_Material, cameraData, sourceTexture, in cameraDepth, in motionVectors, destinationTexture);

            resourceData.cameraColor = destinationTexture;
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
