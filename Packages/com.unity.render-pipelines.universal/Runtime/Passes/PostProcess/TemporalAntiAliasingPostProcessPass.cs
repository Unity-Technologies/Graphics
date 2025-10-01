using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class TemporalAntiAliasingPostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const string k_TargetName = "_TemporalAATarget";

        Material m_Material;
        bool m_IsValid;

        // Input
        public TextureHandle sourceTexture { get; set; }

        // Output
        public TextureHandle destinationTexture { get; set; }

        public TemporalAntiAliasingPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for TemporalAntiAliasingPostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for TemporalAntiAliasingPostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            TextureHandle cameraDepth = resourceData.cameraDepth;
            TextureHandle motionVectors = resourceData.motionVectorColor;

            Debug.Assert(motionVectors.IsValid(), "MotionVectors are invalid. TAA requires a motion vector texture.");

            TemporalAA.Render(renderGraph, m_Material, cameraData, sourceTexture, in cameraDepth, in motionVectors, destinationTexture);
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
