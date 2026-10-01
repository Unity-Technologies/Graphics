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
        const string k_QualityProfilePath = "Assets/URP/MigrationTests/SSAOMigrationQualityProfile.asset";

        const float k_Intensity = 4.25f;
        const float k_Radius = 0.125f;
        const float k_Falloff = 250f;
        const float k_DirectLightingStrength = 0.75f;

        // The migration adds the override as a sub-asset, so the profile has to exist on disk
        static VolumeProfile CreateProfileAsset(string path)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            AssetDatabase.CreateAsset(profile, path);
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
                SeedGlobalSettings(globalSettingsAsset, CreateProfileAsset(k_ProfilePath));
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

                return HasCopiedFeatureSettings(ssao, out message);
            }
        }

        class InactiveRendererFeature : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            private ScriptableRendererData m_RendererData;
            private ScreenSpaceAmbientOcclusion m_Feature;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Feature = AddFeature(renderPipelineAsset, active: false, out m_RendererData);
                SeedGlobalSettings(globalSettingsAsset, CreateProfileAsset(k_ProfilePath));
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

                var profile = CreateProfileAsset(k_ProfilePath);
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

        // Checks the values that AddFeature writes on the renderer feature
        static bool HasCopiedFeatureSettings(ScreenSpaceAmbientOcclusionVolumeOverride ssao, out string message)
        {
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

        // Version 13 lets the ambient occlusion step run again
        // The fresh global settings have no default profile, so only the quality profile is touched
        abstract class QualityProfileTestCase : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            private ScriptableRendererData m_RendererData;
            private ScreenSpaceAmbientOcclusion m_Feature;
            private VolumeProfile m_PreviousQualityProfile;
            protected VolumeProfile m_QualityProfile;

            protected abstract bool featureActive { get; }

            protected virtual void AuthorProfile(VolumeProfile profile)
            {
            }

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                m_Feature = AddFeature(renderPipelineAsset, featureActive, out m_RendererData);
                m_QualityProfile = CreateProfileAsset(k_QualityProfilePath);
                AuthorProfile(m_QualityProfile);

                m_PreviousQualityProfile = renderPipelineAsset.volumeProfile;
                renderPipelineAsset.volumeProfile = m_QualityProfile;
                globalSettingsAsset.m_AssetVersion = 13;
            }

            public void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
                renderPipelineAsset.volumeProfile = m_PreviousQualityProfile;
                RemoveFeature(m_RendererData, m_Feature);
                AssetDatabase.DeleteAsset(k_QualityProfilePath);
            }

            public abstract bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message);
        }

        class QualityProfileOfActiveRendererFeature : QualityProfileTestCase
        {
            protected override bool featureActive => true;

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!m_QualityProfile.TryGet(out ScreenSpaceAmbientOcclusionVolumeOverride ssao))
                {
                    message = "The quality volume profile of the pipeline asset has no ambient occlusion override";
                    return false;
                }

                return HasCopiedFeatureSettings(ssao, out message);
            }
        }

        class QualityProfileOfInactiveRendererFeature : QualityProfileTestCase
        {
            protected override bool featureActive => false;

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                message = "A quality volume profile must not receive an ambient occlusion override from an inactive feature";
                return !m_QualityProfile.Has<ScreenSpaceAmbientOcclusionVolumeOverride>();
            }
        }

        class QualityProfileAlreadyAuthored : QualityProfileTestCase
        {
            const float k_AuthoredIntensity = 9f;

            protected override bool featureActive => true;

            protected override void AuthorProfile(VolumeProfile profile)
            {
                var authored = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>();
                AssetDatabase.AddObjectToAsset(authored, profile);
                authored.intensity = k_AuthoredIntensity;
            }

            public override bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (!m_QualityProfile.TryGet(out ScreenSpaceAmbientOcclusionVolumeOverride ssao))
                {
                    message = "The quality volume profile lost its authored ambient occlusion override";
                    return false;
                }

                message = $"An override that was already on the quality profile must keep its authored intensity, got {ssao.intensity}";
                return Mathf.Approximately(ssao.intensity, k_AuthoredIntensity);
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
            new TestCaseData(new QualityProfileOfActiveRendererFeature())
                .SetName("When a pipeline asset has a quality volume profile, the active ambient occlusion renderer feature settings are copied into it"),
            new TestCaseData(new QualityProfileOfInactiveRendererFeature())
                .SetName("When a pipeline asset has a quality volume profile but an inactive ambient occlusion renderer feature, the profile gets no override"),
            new TestCaseData(new QualityProfileAlreadyAuthored())
                .SetName("When the quality volume profile already has an ambient occlusion override, the migration leaves it untouched"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
