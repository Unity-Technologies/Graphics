using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class TemporalAntiAliasingPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "_TemporalAATarget";

        Material m_Material;
        bool m_IsValid;

        public TemporalAntiAliasingPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
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
