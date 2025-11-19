using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class StpPostProcessPass : PostProcessPass
    {
        public const string k_UpscaledColorTargetName = "_CameraColorUpscaledSTP";
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

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
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
