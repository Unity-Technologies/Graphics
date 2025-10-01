using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class LensFlareScreenSpacePostProcessPass : ScriptableRenderPass, IDisposable
    {
        Material m_Material;
        bool m_IsValid;

        // Settings
        public ScreenSpaceLensFlare lensFlareScreenSpace { get; set; }

        public bool sameSourceDestinationTexture { get; set; } = false;

        // Input

        // Post-processing main color-buffer texture desc. Can be different from downsampled bloom/flare textures.
        public TextureDesc colorBufferTextureDesc { get; set; }

        // Flare streaks are generated from this texture. Typically, a lower resolution downsampled bloom (mipN) texture.
        public TextureHandle sourceTexture { get; set; }

        // Flare is blended to the destination. Typically, the bloom (mip0) texture. Bloom can be at different resolution compared to the main color source.
        public TextureHandle destinationTexture { get; set; }

        public LensFlareScreenSpacePostProcessPass(Shader shader)
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

        private class LensFlareScreenSpacePassData
        {
            internal TextureHandle streakTmpTexture;
            internal TextureHandle streakTmpTexture2;
            internal TextureHandle flareResultTmp;
            internal TextureHandle flareDestinationBloomTexture;
            internal TextureHandle flareSourceBloomMipTexture;
            internal int actualColorWidth;
            internal int actualColorHeight;
            internal Camera camera;
            internal Material material;
            internal ScreenSpaceLensFlare lensFlareScreenSpace;
            internal int downsample;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for LensFlareScreenSpacePostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for LensFlareScreenSpacePostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            Camera camera = cameraData.camera;

            var downsample = (int) lensFlareScreenSpace.resolution.value;

            int flareRenderWidth = Math.Max( colorBufferTextureDesc.width / downsample, 1);
            int flareRenderHeight = Math.Max( colorBufferTextureDesc.height / downsample, 1);

            var streakTextureDesc = PostProcessUtils.GetCompatibleDescriptor(colorBufferTextureDesc, flareRenderWidth, flareRenderHeight, colorBufferTextureDesc.colorFormat);
            var streakTmpTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture", true, FilterMode.Bilinear);
            var streakTmpTexture2 = PostProcessUtils.CreateCompatibleTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture2", true, FilterMode.Bilinear);

            // NOTE: Result texture is the result of the flares/streaks only. Not the final output which is "bloom + flares".
            var resultTmpTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, streakTextureDesc, "_LensFlareScreenSpace", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<LensFlareScreenSpacePassData>("Blit Lens Flare Screen Space", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareScreenSpace)))
            {
                // Use WriteTexture here because DoLensFlareScreenSpaceCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lensflare to be rendergraph friendly
                passData.streakTmpTexture = streakTmpTexture;
                builder.UseTexture(streakTmpTexture, AccessFlags.ReadWrite);
                passData.streakTmpTexture2 = streakTmpTexture2;
                builder.UseTexture(streakTmpTexture2, AccessFlags.ReadWrite);
                passData.flareSourceBloomMipTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.ReadWrite);
                passData.flareDestinationBloomTexture = destinationTexture;
                // Input/Output can be the same texture. There's a temp texture in between. Avoid RG double write error.
                if(!sameSourceDestinationTexture)
                    builder.UseTexture(destinationTexture, AccessFlags.ReadWrite);
                passData.actualColorWidth = colorBufferTextureDesc.width;
                passData.actualColorHeight = colorBufferTextureDesc.height;
                passData.camera = camera;
                passData.material = m_Material;
                passData.lensFlareScreenSpace = lensFlareScreenSpace; // NOTE: reference, assumed constant until executed.
                passData.downsample = downsample;
                passData.flareResultTmp = resultTmpTexture;
                builder.UseTexture(resultTmpTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (LensFlareScreenSpacePassData data, UnsafeGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var camera = data.camera;
                    var lensFlareScreenSpace = data.lensFlareScreenSpace;

                    LensFlareCommonSRP.DoLensFlareScreenSpaceCommon(
                        data.material,
                        camera,
                        (float)data.actualColorWidth,
                        (float)data.actualColorHeight,
                        data.lensFlareScreenSpace.tintColor.value,
                        data.flareDestinationBloomTexture,
                        data.flareSourceBloomMipTexture,
                        null, // We don't have any spectral LUT in URP
                        data.streakTmpTexture,
                        data.streakTmpTexture2,
                        new Vector4(
                            lensFlareScreenSpace.intensity.value,
                            lensFlareScreenSpace.firstFlareIntensity.value,
                            lensFlareScreenSpace.secondaryFlareIntensity.value,
                            lensFlareScreenSpace.warpedFlareIntensity.value),
                        new Vector4(
                            lensFlareScreenSpace.vignetteEffect.value,
                            lensFlareScreenSpace.startingPosition.value,
                            lensFlareScreenSpace.scale.value,
                            0), // Free slot, not used
                        new Vector4(
                            lensFlareScreenSpace.samples.value,
                            lensFlareScreenSpace.sampleDimmer.value,
                            lensFlareScreenSpace.chromaticAbberationIntensity.value,
                            0), // No need to pass a chromatic aberration sample count, hardcoded at 3 in shader
                        new Vector4(
                            lensFlareScreenSpace.streaksIntensity.value,
                            lensFlareScreenSpace.streaksLength.value,
                            lensFlareScreenSpace.streaksOrientation.value,
                            lensFlareScreenSpace.streaksThreshold.value),
                        new Vector4(
                            data.downsample,
                            lensFlareScreenSpace.warpedFlareScale.value.x,
                            lensFlareScreenSpace.warpedFlareScale.value.y,
                            0), // Free slot, not used
                        cmd,
                        data.flareResultTmp,
                        false);
                });
            }
        }
    }
}
