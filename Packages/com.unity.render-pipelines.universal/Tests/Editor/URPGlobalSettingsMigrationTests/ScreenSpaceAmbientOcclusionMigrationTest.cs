using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class ScreenSpaceAmbientOcclusionMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<URPDefaultVolumeProfileSettings>
    {
        const string k_ProfilePath = "Assets/URP/MigrationTests/SSAOMigrationProfile.asset";

        const float k_Intensity = 4.25f;
        const float k_Radius = 0.125f;
        const float k_Falloff = 250f;
        const float k_DirectLightingStrength = 0.75f;

        // The migration adds the override as a sub-asset, so the profile has to exist on disk
        static VolumeProfile CreateProfileAsset()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            AssetDatabase.CreateAsset(profile, k_ProfilePath);
            return profile;
        }

        static ScreenSpaceAmbientOcclusion AddFeature(UniversalRenderPipelineAsset renderPipelineAsset, bool active, out ScriptableRendererData rendererData)
        {
            if (!renderPipelineAsset.TryGetRendererData(renderPipelineAsset.m_DefaultRendererIndex, out rendererData))
                return null;

            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.SetActive(active);

#pragma warning disable CS0618 // Type or member is obsolete
            var settings = feature.settings;
#pragma warning restore CS0618
            settings.Intensity = k_Intensity;
            settings.Radius = k_Radius;
            settings.Falloff = k_Falloff;
            settings.DirectLightingStrength = k_DirectLightingStrength;
            settings.Downsample = true;
            settings.AfterOpaque = true;
            settings.AOMethod = ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
            settings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
            settings.NormalSamples = ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;
            settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;
            settings.BlurQuality = ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low;

            rendererData.rendererFeatures.Add(feature);
            return feature;
        }

        static void RemoveFeature(ScriptableRendererData rendererData, ScreenSpaceAmbientOcclusion feature)
        {
            if (feature == null)
                return;

            rendererData.rendererFeatures.Remove(feature);
            Object.DestroyImmediate(feature);
        }

        // Version 4 lets the existing step move the profile into URPDefaultVolumeProfileSettings first
        static void SeedGlobalSettings(UniversalRenderPipelineGlobalSettings globalSettingsAsset, VolumeProfile profile)
        {
#pragma warning disable 618 // Type or member is obsolete
            globalSettingsAsset.m_ObsoleteDefaultVolumeProfile = profile;
            globalSettingsAsset.m_AssetVersion = 4;
#pragma warning restore 618
        }

        static bool TryGetMigratedOverride(URPDefaultVolumeProfileSettings settings, out ScreenSpaceAmbientOcclusionVolumeOverride ssao)
        {
            ssao = null;
            return settings.volumeProfile != null && settings.volumeProfile.TryGet(out ssao);
        }

        class ActiveRendererFeature : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            private ScriptableRendererData m_RendererData;
            private ScreenSpaceAmbientOcclusion m_Feature;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Feature = AddFeature(renderPipelineAsset, active: true, out m_RendererData);
                SeedGlobalSettings(globalSettingsAsset, CreateProfileAsset());
            }

            public void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                RemoveFeature(m_RendererData, m_Feature);
                AssetDatabase.DeleteAsset(k_ProfilePath);
            }

            public bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!TryGetMigratedOverride(settings, out var ssao))
                {
                    message = "The default volume profile has no ambient occlusion override";
                    return false;
                }

                if (!ssao.parameters.All(p => p.overrideState))
                {
                    message = "The override of an active feature must override every parameter";
                    return false;
                }

                if (ssao.mode != ScreenSpaceAmbientOcclusionMode.SSAO || ssao.quality != ScreenSpaceAmbientOcclusionQuality.Custom)
                {
                    message = $"Expected SSAO mode with Custom quality, got {ssao.mode} with {ssao.quality} quality";
                    return false;
                }

                if (!Mathf.Approximately(ssao.intensity, k_Intensity) ||
                    !Mathf.Approximately(ssao.radius, k_Radius) ||
                    !Mathf.Approximately(ssao.falloffDistance, k_Falloff) ||
                    !Mathf.Approximately(ssao.directLightingStrength, k_DirectLightingStrength))
                {
                    message = $"Values were not copied: intensity {ssao.intensity}, radius {ssao.radius}, falloff {ssao.falloffDistance}, direct lighting {ssao.directLightingStrength}";
                    return false;
                }

                if (!ssao.downsample || !ssao.afterOpaque)
                {
                    message = $"Expected downsample and after opaque to be on, got {ssao.downsample} and {ssao.afterOpaque}";
                    return false;
                }

                if (ssao.method != ScreenSpaceAmbientOcclusionNoiseMethod.InterleavedGradient ||
                    ssao.depthSource != ScreenSpaceAmbientOcclusionDepthSource.Depth ||
                    ssao.normalQuality != ScreenSpaceAmbientOcclusionNormalQuality.High ||
                    ssao.sampleCount != ScreenSpaceAmbientOcclusionSampleCount.High ||
                    ssao.blurQuality != ScreenSpaceAmbientOcclusionBlurQuality.Low)
                {
                    message = $"Enums were not mapped: method {ssao.method}, depth source {ssao.depthSource}, normal quality {ssao.normalQuality}, sample count {ssao.sampleCount}, blur quality {ssao.blurQuality}";
                    return false;
                }

                message = string.Empty;
                return true;
            }
        }

        class InactiveRendererFeature : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            private ScriptableRendererData m_RendererData;
            private ScreenSpaceAmbientOcclusion m_Feature;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Feature = AddFeature(renderPipelineAsset, active: false, out m_RendererData);
                SeedGlobalSettings(globalSettingsAsset, CreateProfileAsset());
            }

            public void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                RemoveFeature(m_RendererData, m_Feature);
                AssetDatabase.DeleteAsset(k_ProfilePath);
            }

            public bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!TryGetMigratedOverride(settings, out var ssao))
                {
                    message = "The default volume profile has no ambient occlusion override";
                    return false;
                }

                if (ssao.parameters.Any(p => p.overrideState))
                {
                    message = "The override of an inactive feature must not override any parameter";
                    return false;
                }

                if (Mathf.Approximately(ssao.intensity, k_Intensity))
                {
                    message = "An inactive feature must not have its intensity copied";
                    return false;
                }

                message = string.Empty;
                return true;
            }
        }

        class OverrideAlreadyAuthored : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            const float k_AuthoredIntensity = 9f;

            private ScriptableRendererData m_RendererData;
            private ScreenSpaceAmbientOcclusion m_Feature;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Feature = AddFeature(renderPipelineAsset, active: true, out m_RendererData);

                var profile = CreateProfileAsset();
                var authored = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>();
                AssetDatabase.AddObjectToAsset(authored, profile);
                authored.intensity = k_AuthoredIntensity;

                SeedGlobalSettings(globalSettingsAsset, profile);
            }

            public void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                RemoveFeature(m_RendererData, m_Feature);
                AssetDatabase.DeleteAsset(k_ProfilePath);
            }

            public bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!TryGetMigratedOverride(settings, out var ssao))
                {
                    message = "The default volume profile has no ambient occlusion override";
                    return false;
                }

                if (!Mathf.Approximately(ssao.intensity, k_AuthoredIntensity))
                {
                    message = $"An override that was already on the profile must keep its authored intensity, got {ssao.intensity}";
                    return false;
                }

                message = string.Empty;
                return true;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new ActiveRendererFeature())
                .SetName("When migrating an active ambient occlusion renderer feature, its settings are being transferred correctly"),
            new TestCaseData(new InactiveRendererFeature())
                .SetName("When migrating an inactive ambient occlusion renderer feature, the override is added without overriding anything"),
            new TestCaseData(new OverrideAlreadyAuthored())
                .SetName("When the default volume profile already has an ambient occlusion override, the migration leaves it untouched"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
