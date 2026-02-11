using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
#if ENABLE_UPSCALER_FRAMEWORK
    [Obsolete("StpPostProcessPass is replaced by STPIUpscaler #from(6000.3)")]
#endif
    internal sealed class StpPostProcessPass : PostProcessPass
    {
        public const string k_UpscaledColorTargetName = "CameraColorUpscaledSTP";
        Texture2D[] m_BlueNoise16LTex;
        bool m_IsValid;

        public StpPostProcessPass(Texture2D[] blueNoise16LTex)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;   // Use default name
            m_BlueNoise16LTex = blueNoise16LTex;

            m_IsValid = m_BlueNoise16LTex != null && m_BlueNoise16LTex.Length > 0;
        }

        public override void Dispose()
        {
            m_IsValid = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

#if ENABLE_UPSCALER_FRAMEWORK
            var postProcessingData = frameData.Get<UniversalPostProcessingData>();
            if (postProcessingData.activeUpscaler != null)
                return;
#endif

            // Note that enabling jitters uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides (like
            // disable useTemporalAA if another feature is disabled) then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking the value here.
            var cameraData = frameData.Get<UniversalCameraData>();
            bool useTemporalAA = cameraData.IsTemporalAAEnabled();

            // STP is only enabled when TAA is enabled and all of its runtime requirements are met.
            // Using IsSTPRequested() vs IsSTPEnabled() for perf reason here, as we already know TAA status
            bool isSTPRequested = cameraData.IsSTPRequested();
            bool useSTP = useTemporalAA && isSTPRequested;
            if (!useSTP)
            {
                // Warn users if TAA and STP are disabled despite being requested
                if (cameraData.IsTemporalAARequested())
                    TemporalAA.ValidateAndWarn(cameraData, isSTPRequested);
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;

            var srcDesc = renderGraph.GetTextureDesc(sourceTexture);
            var dstDesc = StpPostProcessPass.GetStpTargetDesc(srcDesc, cameraData);
            var destinationTexture =  PostProcessUtils.CreateCompatibleTexture(renderGraph, dstDesc, k_UpscaledColorTargetName, false, FilterMode.Bilinear);

            TextureHandle cameraDepth = resourceData.cameraDepthTexture;
            TextureHandle motionVectors = resourceData.motionVectorColor;

            Debug.Assert(motionVectors.IsValid(), "MotionVectors are invalid. STP requires a motion vector texture.");

            int frameIndex = Time.frameCount;
            var noiseTexture = m_BlueNoise16LTex[frameIndex & (m_BlueNoise16LTex.Length - 1)];

            StpUtils.Execute(renderGraph, resourceData, cameraData, sourceTexture, cameraDepth, motionVectors, destinationTexture, noiseTexture);

            // Update the camera resolution to reflect the upscaled size
            var destDesc = destinationTexture.GetDescriptor(renderGraph);
            UpscalerPostProcessPass.UpdateCameraResolution(renderGraph, cameraData, new Vector2Int(destDesc.width, destDesc.height));

            resourceData.cameraColor = destinationTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextureDesc GetStpTargetDesc(in TextureDesc sourceDesc, UniversalCameraData cameraData)
        {
            var targetDesc = PostProcessUtils.GetCompatibleDescriptor(sourceDesc,
                cameraData.pixelWidth,
                cameraData.pixelHeight,
                // Avoid enabling sRGB because STP works with compute shaders which can't output sRGB automatically.
                Experimental.Rendering.GraphicsFormatUtility.GetLinearFormat(sourceDesc.format));

            // STP uses compute shaders so all render textures must enable random writes
            targetDesc.enableRandomWrite = true;

            return targetDesc;
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
