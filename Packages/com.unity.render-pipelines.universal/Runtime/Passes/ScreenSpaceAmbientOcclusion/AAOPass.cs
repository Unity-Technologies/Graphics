using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Alchemy Ambient Occlusion (AAO) Pass - Standard SSAO mode using raster shaders.
    internal class AAOPass : ScriptableRenderPass, IDisposable
    {
        private readonly bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
        private int m_BlueNoiseTextureIndex = 0;
        private Material m_Material;
        private Texture2D[] m_BlueNoiseTextures;
        private SSAOUtils.CameraViewData m_CameraViewData = SSAOUtils.CameraViewData.Create();
        private SSAOUtils.BlurTypes m_BlurType = SSAOUtils.BlurTypes.Bilateral;
        private ProfilingSampler m_ProfilingSampler = URPProfilingSamplers.SSAO;
        private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;
        private SSAOUtils.SSAOMaterialParams m_SSAOParamsPrev = new SSAOUtils.SSAOMaterialParams();

        internal AAOPass(Shader shader, Texture2D[] blueNoiseTextures)
        {
            m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            Debug.Assert(m_Material != null, "AAOPass: Failed to create material from shader.");
            m_BlueNoiseTextures = blueNoiseTextures;
        }

        // Sets up AAO pass using renderer feature settings. Returns false if the pass should be skipped.
        internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings, ScreenSpaceAmbientOcclusionSettings.DepthSource depthSource)
        {
            m_CurrentSettings = featureSettings;
            m_CurrentSettings.Source = depthSource;
#if MODERN_SSAO
            m_CurrentSettings.Mode = ScreenSpaceAmbientOcclusionMode.Standard;
#endif
            m_BlurType = SSAOUtils.GetBlurType(m_CurrentSettings.BlurQuality);

            return m_CurrentSettings.Intensity > 0.0f
                   && m_CurrentSettings.Radius > 0.0f
                   && m_CurrentSettings.Falloff > 0.0f;
        }

#if MODERN_SSAO
        // Sets up AAO pass using active volume for Standard mode. Returns false if the pass should be skipped.
        internal bool Setup(ScreenSpaceAmbientOcclusionVolumeOverride ssaoVolume, bool usesDeferredLighting)
        {
            ApplyVolumeSettings(m_CurrentSettings, ssaoVolume, usesDeferredLighting);
            m_BlurType = SSAOUtils.GetBlurType(m_CurrentSettings.BlurQuality);
            return ssaoVolume.IsActive();
        }

        private static void ApplyVolumeSettings(ScreenSpaceAmbientOcclusionSettings settings, ScreenSpaceAmbientOcclusionVolumeOverride volume, bool usesDeferredLighting)
        {
            settings.Mode = ScreenSpaceAmbientOcclusionMode.Standard;
            settings.Intensity = volume.intensity;
            settings.Radius = volume.radius;
            settings.DirectLightingStrength = volume.directLightingStrength;
            settings.Falloff = volume.falloffDistance;
            settings.Downsample = volume.downsample;
            settings.AfterOpaque = volume.afterOpaque;
            settings.AOMethod = volume.method == ScreenSpaceAmbientOcclusionNoiseMethod.BlueNoise ? ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise : ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
            settings.Samples = volume.sampleCount switch
            {
                ScreenSpaceAmbientOcclusionSampleCount.Low => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low,
                ScreenSpaceAmbientOcclusionSampleCount.High => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High,
                _ => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium
            };

            settings.Source = volume.depthSource == ScreenSpaceAmbientOcclusionDepthSource.Depth ? ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth : ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
            settings.NormalSamples = volume.normalQuality switch
            {
                ScreenSpaceAmbientOcclusionNormalQuality.Low => ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low,
                ScreenSpaceAmbientOcclusionNormalQuality.High => ScreenSpaceAmbientOcclusionSettings.NormalQuality.High,
                _ => ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium
            };
            settings.BlurQuality = volume.blurQuality switch
            {
                ScreenSpaceAmbientOcclusionBlurQuality.Low => ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low,
                ScreenSpaceAmbientOcclusionBlurQuality.High => ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High,
                _ => ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium
            };

            // Force to use Depth + Normals textures if deferred rendering
            if (usesDeferredLighting)
                settings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
        }
#endif

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            // Builds a TextureDesc compatible with the active camera color target, safe for both intermediate render textures and imported back-buffer handles.
            TextureDesc cameraColorDesc = SSAOUtils.GetCameraColorDescriptor(renderGraph, resourceData.activeColorTexture, cameraData.camera.allowDynamicResolution);

            SSAOUtils.CreateRenderTextureHandles(renderGraph, resourceData, cameraColorDesc,
                m_CurrentSettings, m_SupportsR8RenderTextureFormat, m_BlurType, enableRandomWrite: false,
                out TextureHandle aoTexture, out TextureHandle blurTexture, out TextureHandle temporalTexture, out TextureHandle finalTexture);

            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

            SSAOUtils.SetupCameraViewMatrices(cameraData, ref m_CameraViewData);
            m_BlueNoiseTextureIndex = SSAOUtils.AdvanceBlueNoiseIndex(m_CurrentSettings, m_BlueNoiseTextures, m_BlueNoiseTextureIndex);

            SSAOUtils.SetupKeywordsAndParameters(m_Material, m_CurrentSettings, cameraData, cameraColorDesc, ref m_CameraViewData, m_BlueNoiseTextures, m_BlueNoiseTextureIndex, ref m_SSAOParamsPrev);
            SSAOUtils.RecordRasterAOPass(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, aoTexture, cameraDepthTexture, cameraNormalsTexture, cameraColorDesc);
            SSAOUtils.RecordBlurChain(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, cameraData, aoTexture, blurTexture, finalTexture);

            if (!m_CurrentSettings.AfterOpaque)
                SSAOUtils.RecordCleanupPass(renderGraph, m_ProfilingSampler, m_CurrentSettings.DirectLightingStrength, finalTexture);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (!m_CurrentSettings.AfterOpaque)
                cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, false);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_SSAOParamsPrev = default;
        }
    }
}
