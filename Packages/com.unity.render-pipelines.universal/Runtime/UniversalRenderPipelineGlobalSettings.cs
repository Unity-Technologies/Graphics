using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Universal Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type. In URP, Global Settings contain:
    /// - light layer names
    /// </summary>
    [URPHelpURL("urp/urp-global-settings")]
    [DisplayInfo(name = "URP Global Settings Asset", order = CoreUtils.Sections.section4 + 2)]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [DisplayName("URP")]
    partial class UniversalRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<UniversalRenderPipelineGlobalSettings, UniversalRenderPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;

        #region Version system

        internal bool IsAtLastVersion() => k_LastVersion == m_AssetVersion;

        internal const int k_LastVersion = 17;

#pragma warning disable CS0414
        [SerializeField][FormerlySerializedAs("k_AssetVersion")]
        internal int m_AssetVersion = k_LastVersion;
#pragma warning restore CS0414

#if UNITY_EDITOR
        public static void UpgradeAsset(EntityId assetInstanceID)
        {
            if (EditorUtility.EntityIdToObject(assetInstanceID) is not UniversalRenderPipelineGlobalSettings asset)
                return;

            int assetVersionBeforeUpgrade = asset.m_AssetVersion;

            if (asset.m_AssetVersion < 2)
            {
#pragma warning disable 618 // Obsolete warning
                // Renamed supportRuntimeDebugDisplay => m_StripDebugVariants, which results in inverted logic
                asset.m_StripDebugVariants = !asset.supportRuntimeDebugDisplay;
                asset.m_AssetVersion = 2;
#pragma warning restore 618 // Obsolete warning

                // For old test projects lets keep post processing stripping enabled, as huge chance they did not used runtime profile creating
#if UNITY_INCLUDE_TESTS
#pragma warning disable 618 // Obsolete warning
                asset.m_StripUnusedPostProcessingVariants = true;
#pragma warning restore 618 // Obsolete warning
#endif
            }

            if (asset.m_AssetVersion < 3)
            {
                int index = 0;
#pragma warning disable 618 // Obsolete warning
                asset.m_RenderingLayerNames = new string[8];
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName0;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName1;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName2;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName3;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName4;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName5;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName6;
                asset.m_RenderingLayerNames[index++] = asset.lightLayerName7;
#pragma warning restore 618 // Obsolete warning
                asset.m_AssetVersion = 3;
                DecalProjector.UpdateAllDecalProperties();
            }

            if (asset.m_AssetVersion < 4)
            {
#pragma warning disable 618 // Type or member is obsolete
                asset.m_ShaderStrippingSetting.exportShaderVariants                 = asset.m_ExportShaderVariants;
                asset.m_ShaderStrippingSetting.shaderVariantLogLevel                = asset.m_ShaderVariantLogLevel;
                asset.m_ShaderStrippingSetting.stripRuntimeDebugShaders             = asset.m_StripDebugVariants;
                asset.m_URPShaderStrippingSetting.stripScreenCoordOverrideVariants  = asset.m_StripScreenCoordOverrideVariants;
                asset.m_URPShaderStrippingSetting.stripUnusedPostProcessingVariants = asset.m_StripUnusedPostProcessingVariants;
                asset.m_URPShaderStrippingSetting.stripUnusedVariants               = asset.m_StripUnusedVariants;
#pragma warning restore 618

                asset.m_AssetVersion = 4;
            }

            if (asset.m_AssetVersion < 5)
            {
#pragma warning disable 618 // Type or member is obsolete
                asset.m_ObsoleteDefaultVolumeProfile = GetOrCreateDefaultVolumeProfile(asset.m_ObsoleteDefaultVolumeProfile);
#pragma warning restore 618 // Type or member is obsolete
                asset.m_AssetVersion = 5;
            }

            if (asset.m_AssetVersion < 6)
            {
                MigrateToRenderPipelineGraphicsSettings(asset);
                asset.m_AssetVersion = 6;
            }

            if (asset.m_AssetVersion < 7)
            {
#pragma warning disable 618 // Type or member is obsolete
                if (asset.m_RenderingLayerNames is { Length: > 0 })
                {
                    // We can't output an error here because Inner migration could cause another migration due a Graphics Settings asset reimporting.
                    InternalRenderPipelineGlobalSettingsUtils
                        .TryMigrateRenderingLayersToTagManager<UniversalRenderPipeline>(asset.m_RenderingLayerNames);
                }
#pragma warning restore 618 // Type or member is obsolete
                asset.m_AssetVersion = 7;
            }

            // Reload PSDImporter and AsepriteImporter assets for 2D. Importers are triggered before graphics settings are loaded
            // This ensures affected assets dependent on default materials from graphics settings are loaded correctly
            if (asset.m_AssetVersion < 8)
            {
                var distinctGuids = AssetDatabase.FindAssets("", new[] { "Assets" });

                for (int i = 0; i < distinctGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(distinctGuids[i]);
                    var assetExt = Path.GetExtension(path);

                    if (assetExt == ".psb" || assetExt == ".psd" ||
                        assetExt == ".ase" || assetExt == ".aseprite")
                        AssetDatabase.ImportAsset(path);
                }

                asset.m_AssetVersion = 8;
            }

            // URPReflectionProbeSettings is introduced; disable rotation for older projects to preserve
            // pre-existing behavior (rotation was not supported before this version).
            if (asset.m_AssetVersion < 9)
            {
                var reflectionProbeSettings = GetOrCreateGraphicsSettings<URPReflectionProbeSettings>(asset);
                reflectionProbeSettings.UseReflectionProbeRotation = false;
                asset.m_AssetVersion = 9;
            }

            // Migrate terrain shader settings from UniversalRenderPipelineRuntimeShaders to UniversalRenderPipelineRuntimeTerrainShaders
            if (asset.m_AssetVersion < 10)
            {
                MigrateTerrainShaderSettings(asset);
                asset.m_AssetVersion = 10;
            }

            if (asset.m_AssetVersion < 11)
            {
                MigrateFilmGrainTextures();
                asset.m_AssetVersion = 11;
            }

            if (asset.m_AssetVersion < 12)
            {
                var exposureSettings = GetOrCreateGraphicsSettings<URPExposureSettings>(asset);
                exposureSettings.UseExposure = false;
                asset.m_AssetVersion = 12;
            }

            if (asset.m_AssetVersion < 13)
            {
                var shadowBiasSettings = GetOrCreateGraphicsSettings<URPShadowBiasSettings>(asset);
                shadowBiasSettings.depthBiasMode = ShadowDepthBiasMode.Legacy;
                asset.m_AssetVersion = 13;
            }

            if (asset.m_AssetVersion < 14)
            {
                MigrateScreenSpaceAmbientOcclusionToVolumeProfiles(asset);
                asset.m_AssetVersion = 14;
            }

            if (asset.m_AssetVersion < 15)
            {
                var shadowBiasSettings = GetOrCreateGraphicsSettings<URPShadowBiasSettings>(asset);
                shadowBiasSettings.normalBiasMode = ShadowNormalBiasMode.Caster;
                asset.m_AssetVersion = 15;
            }

            if (asset.m_AssetVersion < 16)
            {
                MigrateLightFalloffMode(asset);
                asset.m_AssetVersion = 16;
            }

            if (asset.m_AssetVersion < 17)
            {
                MigrateShadowSettingsToVolumeProfiles(asset);
                asset.m_AssetVersion = 17;
            }

            // If the asset version has changed, means that a migration step has been executed
            if (assetVersionBeforeUpgrade != asset.m_AssetVersion)
                EditorUtility.SetDirty(asset);
        }

        static void MigrateFilmGrainTextures()
        {
            // Film Grain textures have been moved from PostProcessData reference to Global Settings to allow stripping
            // the textures when unused. This migration step logs a user warning if they had customized the film grain
            // textures, and removes the deprecated texture references from PostProcessData.

            GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineFilmGrainResources>(out var filmGrainResources);
            var ppDataGuids = AssetDatabase.FindAssets("t:PostProcessData");
            foreach (var ppDataGuid in ppDataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(ppDataGuid);
                if (!path.StartsWith("Assets"))
                    continue; // We only care about mutable assets inside Assets

                var postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(path);
                ClearObsoleteFilmGrainTexturesAndLogWarnings(postProcessData, filmGrainResources);
                EditorUtility.SetDirty(postProcessData);
            }
        }

        internal static void ClearObsoleteFilmGrainTexturesAndLogWarnings(PostProcessData postProcessData, UniversalRenderPipelineFilmGrainResources filmGrainResources)
        {
#pragma warning disable 618
            var oldFilmGrainTextureArray = postProcessData.textures?.filmGrainTex;
            if (oldFilmGrainTextureArray != null && filmGrainResources != null)
            {
                List<string> unusedOldFilmGrainTexturePaths = new();
                foreach (var oldTex in oldFilmGrainTextureArray)
                {
                    if (oldTex != null && Array.IndexOf(filmGrainResources.textures, oldTex) == -1)
                    {
                        var oldPath = AssetDatabase.GetAssetPath(oldTex);
                        if (!string.IsNullOrEmpty(oldPath))
                            unusedOldFilmGrainTexturePaths.Add(oldPath);
                    }
                }

                if (unusedOldFilmGrainTexturePaths.Count > 0)
                {
                    Debug.LogWarning("Film Grain texture list has been moved from PostProcessData to URP " +
                                     "Graphics Settings, and it can no longer be edited. Use " +
                                     "`FilmGrain.type = FilmGrainLookup.Custom` instead. As a consequence, " +
                                     "following custom film grain textures are no longer referenced:\n" +
                                     $"{string.Join("\n", unusedOldFilmGrainTexturePaths)}.");
                }
            }

            // Clear the references so they are no longer referenced in the deprecated field.
            if (postProcessData.textures != null)
                postProcessData.textures.filmGrainTex = null;
#pragma warning restore 618
        }

        public static void MigrateToRenderPipelineGraphicsSettings(UniversalRenderPipelineGlobalSettings data)
        {
            MigrateToShaderStrippingSetting(data);
            MigrateToURPShaderStrippingSetting(data);
            MigrateDefaultVolumeProfile(data);
        }

        private static T GetOrCreateGraphicsSettings<T>(UniversalRenderPipelineGlobalSettings data)
            where T : class, IRenderPipelineGraphicsSettings, new()
        {
            T settings;

            if (data.TryGet(typeof(T), out var baseSettings))
            {
                settings = baseSettings as T;
            }
            else
            {
                settings = new T();
                data.Add(settings);
            }

            return settings;
        }

        static void MigrateToShaderStrippingSetting(UniversalRenderPipelineGlobalSettings data)
        {
            var shaderStrippingSetting = GetOrCreateGraphicsSettings<ShaderStrippingSetting>(data);

#pragma warning disable 618 // Type or member is obsolete
            shaderStrippingSetting.shaderVariantLogLevel    = data.m_ShaderStrippingSetting.shaderVariantLogLevel;
            shaderStrippingSetting.exportShaderVariants     = data.m_ShaderStrippingSetting.exportShaderVariants;
            shaderStrippingSetting.stripRuntimeDebugShaders = data.m_ShaderStrippingSetting.stripRuntimeDebugShaders;
#pragma warning restore 618
        }

        static void MigrateToURPShaderStrippingSetting(UniversalRenderPipelineGlobalSettings data)
        {
            var urpShaderStrippingSetting = GetOrCreateGraphicsSettings<URPShaderStrippingSetting>(data);

#pragma warning disable 618 // Type or member is obsolete
            urpShaderStrippingSetting.stripScreenCoordOverrideVariants  = data.m_URPShaderStrippingSetting.stripScreenCoordOverrideVariants;
            urpShaderStrippingSetting.stripUnusedPostProcessingVariants = data.m_URPShaderStrippingSetting.stripUnusedPostProcessingVariants;
            urpShaderStrippingSetting.stripUnusedVariants               = data.m_URPShaderStrippingSetting.stripUnusedVariants;
#pragma warning restore 618
        }

        static void MigrateDefaultVolumeProfile(UniversalRenderPipelineGlobalSettings data)
        {
#pragma warning disable 618 // Type or member is obsolete
            var defaultVolumeProfileSettings = GetOrCreateGraphicsSettings<URPDefaultVolumeProfileSettings>(data);
            defaultVolumeProfileSettings.volumeProfile = data.m_ObsoleteDefaultVolumeProfile;
            data.m_ObsoleteDefaultVolumeProfile = null; // Discard old reference after it is migrated
#pragma warning restore 618 // Type or member is obsolete
        }

        static void MigrateTerrainShaderSettings(UniversalRenderPipelineGlobalSettings data)
        {
            try
            {
                // Get existing UniversalRenderPipelineRuntimeShaders settings
                if (!GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>(out var runtimeShaders))
                {
                    return;
                }

                // Create/get UniversalRenderPipelineRuntimeTerrainShaders container
                var runtimeTerrainShaders = GetOrCreateGraphicsSettings<UniversalRenderPipelineRuntimeTerrainShaders>(data);

                // Migrate terrain shaders from runtimeShaders to terrainShaders
                runtimeTerrainShaders.terrainDetailLitShader = runtimeShaders.GetOriginalTerrainDetailLitShader();
                runtimeTerrainShaders.terrainDetailGrassBillboardShader = runtimeShaders.GetOriginalTerrainDetailGrassBillboardShader();
                runtimeTerrainShaders.terrainDetailGrassShader = runtimeShaders.GetOriginalTerrainDetailGrassShader();
                runtimeShaders.ClearOriginalTerrainDetailShaders();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"URP: Failed to migrate terrain detail shader settings: {ex.Message}. Terrain detail shaders will use default values.");
            }
        }

        static void MigrateScreenSpaceAmbientOcclusionToVolumeProfiles(UniversalRenderPipelineGlobalSettings data)
        {
            // SSAO is now driven by the volume stack, so the renderer feature settings are no longer read.
            // The default profile goes first because a quality profile can reference that same asset.
            var reportedAssets = new HashSet<UniversalRenderPipelineAsset>();
            MigrateScreenSpaceAmbientOcclusionToDefaultVolumeProfile(data, reportedAssets);
            MigrateScreenSpaceAmbientOcclusionToQualityVolumeProfiles(reportedAssets);
        }

        static void MigrateScreenSpaceAmbientOcclusionToQualityVolumeProfiles(HashSet<UniversalRenderPipelineAsset> reportedAssets)
        {
            var migratedProfiles = new Dictionary<VolumeProfile, UniversalRenderPipelineAsset>();
            foreach (var urpAsset in EnumerateQualityLevelPipelineAssets())
            {
                var profile = urpAsset.volumeProfile;
                if (profile == null || !TryGetActiveScreenSpaceAmbientOcclusionFeature(urpAsset, out var ssaoFeature))
                    continue;

                if (migratedProfiles.TryGetValue(profile, out var sourceAsset))
                {
                    Debug.LogWarning($"URP: The volume profile '{profile.name}' is shared by the pipeline assets '{sourceAsset.name}' and '{urpAsset.name}'. " +
                        $"Its Screen Space Ambient Occlusion override uses the settings of '{sourceAsset.name}'.");
                    continue;
                }

                // Authored by the user, or already migrated
                if (profile.Has<ScreenSpaceAmbientOcclusionVolumeOverride>())
                    continue;

                var ssaoOverride = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>(overrides: true);
                WarnIfRenderersHaveDifferentSettings(urpAsset, ssaoFeature, reportedAssets);
                CopyRendererFeatureSettingsToVolumeOverride(ssaoFeature, ssaoOverride);
                PersistVolumeOverride(profile, ssaoOverride, "Screen Space Ambient Occlusion");
                migratedProfiles.Add(profile, urpAsset);
            }
        }

        static void MigrateScreenSpaceAmbientOcclusionToDefaultVolumeProfile(UniversalRenderPipelineGlobalSettings data, HashSet<UniversalRenderPipelineAsset> reportedAssets)
        {
            // The default profile is shared by every quality level, so it can only hold the active pipeline's settings
            var profile = GetOrCreateGraphicsSettings<URPDefaultVolumeProfileSettings>(data).volumeProfile;
            if (profile == null)
                return;

            // Authored by the user, or already migrated
            if (profile.Has<ScreenSpaceAmbientOcclusionVolumeOverride>())
                return;

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            ScreenSpaceAmbientOcclusion ssaoFeature = null;
            bool active = urpAsset != null && TryGetActiveScreenSpaceAmbientOcclusionFeature(urpAsset, out ssaoFeature);

            var ssaoOverride = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>(overrides: active);

            if (active)
            {
                WarnIfRenderersHaveDifferentSettings(urpAsset, ssaoFeature, reportedAssets);
                CopyRendererFeatureSettingsToVolumeOverride(ssaoFeature, ssaoOverride);
            }

            PersistVolumeOverride(profile, ssaoOverride, "Screen Space Ambient Occlusion");
        }

        static IEnumerable<UniversalRenderPipelineAsset> EnumerateQualityLevelPipelineAssets()
        {
            var visited = new HashSet<UniversalRenderPipelineAsset>();
            int qualityLevelCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityLevelCount; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset urpAsset && visited.Add(urpAsset))
                    yield return urpAsset;
            }

            // Quality levels without their own asset render with the default one
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset && visited.Add(defaultAsset))
                yield return defaultAsset;
        }

        static bool TryGetActiveScreenSpaceAmbientOcclusionFeature(UniversalRenderPipelineAsset urpAsset, out ScreenSpaceAmbientOcclusion ssaoFeature)
        {
            if (urpAsset.TryGetRendererData(urpAsset.m_DefaultRendererIndex, out var defaultRendererData)
                && TryGetActiveScreenSpaceAmbientOcclusionFeature(defaultRendererData, out ssaoFeature))
                return true;

            // Cameras can render with any renderer of the asset, so fall back to the first one that has the feature
            foreach (var rendererData in urpAsset.rendererDataList)
            {
                if (TryGetActiveScreenSpaceAmbientOcclusionFeature(rendererData, out ssaoFeature))
                    return true;
            }

            ssaoFeature = null;
            return false;
        }

        static bool TryGetActiveScreenSpaceAmbientOcclusionFeature(ScriptableRendererData rendererData, out ScreenSpaceAmbientOcclusion ssaoFeature)
        {
            ssaoFeature = null;
            return rendererData != null
                && rendererData.TryGetRendererFeature<ScreenSpaceAmbientOcclusion>(out ssaoFeature)
                && ssaoFeature.isActive;
        }

        static void WarnIfRenderersHaveDifferentSettings(UniversalRenderPipelineAsset urpAsset, ScreenSpaceAmbientOcclusion migratedFeature, HashSet<UniversalRenderPipelineAsset> reportedAssets)
        {
            if (!reportedAssets.Add(urpAsset))
                return;

            string migratedRendererName = null;
            var skippedRendererNames = new List<string>();
            foreach (var rendererData in urpAsset.rendererDataList)
            {
                if (!TryGetActiveScreenSpaceAmbientOcclusionFeature(rendererData, out var ssaoFeature))
                    continue;

                if (ssaoFeature == migratedFeature)
                    migratedRendererName = rendererData.name;
                else if (!HaveSameMigratedSettings(ssaoFeature, migratedFeature) && !skippedRendererNames.Contains(rendererData.name))
                    skippedRendererNames.Add(rendererData.name);
            }

            if (skippedRendererNames.Count == 0)
                return;

            Debug.LogWarning($"URP: The pipeline asset '{urpAsset.name}' has renderers with different Screen Space Ambient Occlusion settings. " +
                $"Only the settings of '{migratedRendererName}' were migrated to the volume override. If you need the others, add them to a volume manually.\n" +
                $"Not migrated: '{string.Join("', '", skippedRendererNames)}'");
        }

        static bool HaveSameMigratedSettings(ScreenSpaceAmbientOcclusion a, ScreenSpaceAmbientOcclusion b)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var x = a.settings;
            var y = b.settings;
#pragma warning restore CS0618
            return (x.Intensity, x.Radius, x.Falloff, x.DirectLightingStrength, x.Downsample, x.AfterOpaque, x.AOMethod, x.Source, x.NormalSamples, x.Samples, x.BlurQuality)
                == (y.Intensity, y.Radius, y.Falloff, y.DirectLightingStrength, y.Downsample, y.AfterOpaque, y.AOMethod, y.Source, y.NormalSamples, y.Samples, y.BlurQuality);
        }

        static bool PersistVolumeOverride(VolumeProfile profile, VolumeComponent volumeOverride, string settingsName)
        {
            // A profile that only lives in memory cannot hold the override as a sub-asset
            if (!EditorUtility.IsPersistent(profile))
                return true;

            // A read-only profile cannot store the override, and the version bump would hide the loss
            if (!AssetDatabase.MakeEditable(AssetDatabase.GetAssetPath(profile)))
            {
                Debug.LogWarning($"URP: The volume profile '{AssetLinkUtils.AssetLink(profile)}' is not editable, so its {settingsName} settings were not migrated. " +
                    "Make the asset editable and add the override manually.");
                return false;
            }

            AssetDatabase.AddObjectToAsset(volumeOverride, profile);

            // Ensure only saves the global settings asset, so the version bump would outlive the override
            EditorUtility.SetDirty(volumeOverride);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return true;
        }

        static void CopyRendererFeatureSettingsToVolumeOverride(ScreenSpaceAmbientOcclusion feature, ScreenSpaceAmbientOcclusionVolumeOverride ssaoOverride)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var settings = feature.settings;
#pragma warning restore CS0618

            // Custom quality keeps the copied values instead of applying a preset
            ssaoOverride.mode = ScreenSpaceAmbientOcclusionMode.SSAO;
            ssaoOverride.quality = ScreenSpaceAmbientOcclusionQuality.Custom;

            ssaoOverride.intensity = settings.Intensity;
            ssaoOverride.radius = settings.Radius;
            ssaoOverride.falloffDistance = settings.Falloff;
            ssaoOverride.directLightingStrength = settings.DirectLightingStrength;
            ssaoOverride.downsample = settings.Downsample;
            ssaoOverride.afterOpaque = settings.AfterOpaque;

            // Note: sample count and blur quality run high to low on the feature, low to high on the volume
            ssaoOverride.method = settings.AOMethod switch
            {
                ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise => ScreenSpaceAmbientOcclusionNoiseMethod.BlueNoise,
                _ => ScreenSpaceAmbientOcclusionNoiseMethod.InterleavedGradient
            };

            ssaoOverride.depthSource = settings.Source switch
            {
                ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth => ScreenSpaceAmbientOcclusionDepthSource.Depth,
                _ => ScreenSpaceAmbientOcclusionDepthSource.DepthNormals
            };

            ssaoOverride.normalQuality = settings.NormalSamples switch
            {
                ScreenSpaceAmbientOcclusionSettings.NormalQuality.High => ScreenSpaceAmbientOcclusionNormalQuality.High,
                ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium => ScreenSpaceAmbientOcclusionNormalQuality.Medium,
                _ => ScreenSpaceAmbientOcclusionNormalQuality.Low
            };

            ssaoOverride.blurQuality = settings.BlurQuality switch
            {
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High => ScreenSpaceAmbientOcclusionBlurQuality.High,
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium => ScreenSpaceAmbientOcclusionBlurQuality.Medium,
                _ => ScreenSpaceAmbientOcclusionBlurQuality.Low
            };

            ssaoOverride.sampleCount = settings.Samples switch
            {
                ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High => ScreenSpaceAmbientOcclusionSampleCount.High,
                ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium => ScreenSpaceAmbientOcclusionSampleCount.Medium,
                _ => ScreenSpaceAmbientOcclusionSampleCount.Low
            };

            // GTAO and temporal filtering have no feature equivalent and keep the override defaults
        }

        static void MigrateLightFalloffMode(UniversalRenderPipelineGlobalSettings data)
        {
            // Quadratic is the value a user had to opt into, so it wins when assets disagree.
            var lightFalloffSettings = GetOrCreateGraphicsSettings<URPLightFalloffSettings>(data);

            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets"))
                    continue;

                var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
#pragma warning disable 618 // Type or member is obsolete
                if (urpAsset == null || urpAsset.m_ObsoleteLightFalloffMode != LightFalloffMode.Quadratic)
                    continue;
#pragma warning restore 618

                lightFalloffSettings.lightFalloffMode = LightFalloffMode.Quadratic;
                break;
            }
        }

        internal struct ShadowSettingsValues : IEquatable<ShadowSettingsValues>
        {
            internal float maxShadowDistance;
            internal int cascadeCount;
            internal float split0;
            internal float split1;
            internal float split2;
            internal float lastCascadeBorder;

            internal static ShadowSettingsValues FromCascadeSplitSets(float maxShadowDistance, int cascadeCount,
                float cascade2Split, Vector2 cascade3Split, Vector3 cascade4Split, float lastCascadeBorder)
            {
                var values = new ShadowSettingsValues
                {
                    maxShadowDistance = maxShadowDistance,
                    cascadeCount = cascadeCount,
                    split0 = cascade4Split.x,
                    split1 = cascade4Split.y,
                    split2 = cascade4Split.z,
                    lastCascadeBorder = lastCascadeBorder
                };

                if (cascadeCount == 2)
                {
                    values.split0 = cascade2Split;
                }
                else if (cascadeCount == 3)
                {
                    values.split0 = cascade3Split.x;
                    values.split1 = cascade3Split.y;
                }

                return values;
            }

            internal readonly bool IsDefault()
            {
                var defaults = CreateInstance<ShadowsVolumeOverride>();
                bool isDefault = Mathf.Approximately(maxShadowDistance, defaults.maxShadowDistance.value)
                    && cascadeCount == defaults.cascadeCount.value
                    && Mathf.Approximately(split0, defaults.cascadeSplit0.value)
                    && Mathf.Approximately(split1, defaults.cascadeSplit1.value)
                    && Mathf.Approximately(split2, defaults.cascadeSplit2.value)
                    && Mathf.Approximately(lastCascadeBorder, defaults.lastCascadeBorder.value);
                DestroyImmediate(defaults);
                return isDefault;
            }

            public readonly bool Equals(ShadowSettingsValues other)
            {
                return maxShadowDistance == other.maxShadowDistance
                    && cascadeCount == other.cascadeCount
                    && split0 == other.split0
                    && split1 == other.split1
                    && split2 == other.split2
                    && lastCascadeBorder == other.lastCascadeBorder;
            }

            public override readonly bool Equals(object obj) => obj is ShadowSettingsValues other && Equals(other);

            public override readonly int GetHashCode() => HashCode.Combine(maxShadowDistance, cascadeCount, split0, split1, split2, lastCascadeBorder);
        }

        static ShadowSettingsValues ReadObsoleteShadowSettings(UniversalRenderPipelineAsset urpAsset)
        {
#pragma warning disable 618 // Type or member is obsolete
            return ShadowSettingsValues.FromCascadeSplitSets(
                urpAsset.m_ObsoleteShadowDistance,
                urpAsset.m_ObsoleteShadowCascadeCount,
                urpAsset.m_ObsoleteCascade2Split,
                urpAsset.m_ObsoleteCascade3Split,
                urpAsset.m_ObsoleteCascade4Split,
                urpAsset.m_ObsoleteCascadeBorder);
#pragma warning restore 618
        }

        static void MigrateShadowSettingsToVolumeProfiles(UniversalRenderPipelineGlobalSettings data)
        {
            MigrateShadowSettingsToVolumeProfiles(data, FindPipelineAssets());
        }

        internal static void MigratePendingShadowSettingsToVolumeProfiles(List<UniversalRenderPipelineAsset> urpAssets)
        {
            if (GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>() is not UniversalRenderPipelineGlobalSettings data
                || data.m_AssetVersion < 17)
                return;

            bool needsMigration = false;
            foreach (var urpAsset in urpAssets)
                needsMigration |= urpAsset.m_ShadowSettingsMigrationPending;

            if (needsMigration)
                MigrateShadowSettingsToVolumeProfiles(data, FindPipelineAssets());
        }

        internal static bool HasUnmigratedShadowSettings(UniversalRenderPipelineAsset urpAsset, bool inReadOnlyPackage)
        {
            return urpAsset.m_ShadowSettingsMigrationPending
                && isInReadOnlyPackage(AssetDatabase.GetAssetPath(urpAsset)) == inReadOnlyPackage
                && !ReadObsoleteShadowSettings(urpAsset).IsDefault();
        }

        internal static Predicate<string> isInReadOnlyPackage = assetPath =>
            UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath) is { } package
            && package.source != UnityEditor.PackageManager.PackageSource.Embedded
            && package.source != UnityEditor.PackageManager.PackageSource.Local;

        internal static List<UniversalRenderPipelineAsset> FindPipelineAssets()
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            var urpAssets = new List<UniversalRenderPipelineAsset>(paths.Count);
            foreach (var path in paths)
            {
                var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (urpAsset != null)
                    urpAssets.Add(urpAsset);
            }
            return urpAssets;
        }

        static void MigrateShadowSettingsToVolumeProfiles(UniversalRenderPipelineGlobalSettings data, List<UniversalRenderPipelineAsset> urpAssets)
        {
            var defaultProfile = data.TryGet(typeof(URPDefaultVolumeProfileSettings), out var settings)
                && settings is URPDefaultVolumeProfileSettings defaultProfileSettings ? defaultProfileSettings.volumeProfile : null;

            var groups = new List<(VolumeProfile source, ShadowSettingsValues values, List<UniversalRenderPipelineAsset> members)>();
            var groupIndices = new Dictionary<(VolumeProfile source, ShadowSettingsValues values), int>();
            foreach (var urpAsset in urpAssets)
            {
                if (!urpAsset.m_ShadowSettingsMigrationPending)
                    continue;

                var values = ReadObsoleteShadowSettings(urpAsset);
                bool isDefault = values.IsDefault();

                string assetPath = AssetDatabase.GetAssetPath(urpAsset);
                if (isInReadOnlyPackage(assetPath))
                {
                    if (!isDefault)
                        Debug.LogWarning($"URP: The pipeline asset '{AssetLinkUtils.AssetLink(urpAsset)}' is in a read-only package, so its {nameof(ShadowsVolumeOverride)} settings were not migrated. " +
                            "Update the package, or copy the asset into the Assets folder and use the copy.");
                    continue;
                }

                var profile = urpAsset.volumeProfile;
                if (isDefault || (profile != null && profile.Has<ShadowsVolumeOverride>()))
                {
                    MarkShadowSettingsMigrated(urpAsset);
                    continue;
                }

                if (!IsPipelineAssetEditable<ShadowsVolumeOverride>(urpAsset))
                    continue;

                if (profile != null && groupIndices.TryGetValue((profile, values), out int groupIndex))
                {
                    groups[groupIndex].members.Add(urpAsset);
                    continue;
                }

                if (profile != null)
                    groupIndices.Add((profile, values), groups.Count);
                groups.Add((profile, values, new List<UniversalRenderPipelineAsset> { urpAsset }));
            }

            var lines = new List<string>();
            var sources = new List<VolumeProfile>();
            bool anyShared = false;
            foreach (var (source, values, members) in groups)
            {
                string name = members.Count == 1 ? $"{members[0].name}_VolumeProfile" : $"{source.name}_Shadows";
                var newProfile = CreateVolumeProfileCopy<ShadowsVolumeOverride>(source, VolumeProfilePathNextTo(members[0], name));
                if (!WriteShadowSettings(newProfile, values))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(newProfile));
                    continue;
                }

                foreach (var member in members)
                {
                    AssignVolumeProfile(member, newProfile);
                    MarkShadowSettingsMigrated(member);
                }

                if (source == null)
                    continue;

                lines.Add(DescribeVolumeProfileCopy(members, newProfile, source, defaultProfile) + ".");
                anyShared |= members.Count > 1;
                if (!sources.Contains(source))
                    sources.Add(source);
            }

            foreach (var source in sources)
            {
                var remainingUsers = urpAssets.FindAll(urpAsset => urpAsset.volumeProfile == source);
                if (remainingUsers.Count > 0)
                    lines.Add(DescribeRemainingUsers(source, remainingUsers) + ".");
            }

            if (lines.Count > 0)
                Debug.LogWarning("URP: Shadow distance and cascade settings moved to Shadows volume overrides in new volume profiles, " +
                    "so later changes to the previous volume profiles no longer apply to these pipeline assets." +
                    (anyShared ? " Pipeline assets with the same volume profile and shadow settings share one new profile." : string.Empty) + "\n" +
                    string.Join("\n", lines));
        }

        static void MarkShadowSettingsMigrated(UniversalRenderPipelineAsset urpAsset)
        {
            if (!AssetDatabase.MakeEditable(AssetDatabase.GetAssetPath(urpAsset)))
                return;

            urpAsset.m_ShadowSettingsMigrationPending = false;
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssetIfDirty(urpAsset);
        }

        internal static VolumeProfile CreateQualityVolumeProfile<T>(UniversalRenderPipelineAsset urpAsset) where T : VolumeComponent
        {
            if (!IsPipelineAssetEditable<T>(urpAsset))
                return null;

            var profile = CreateVolumeProfileCopy<T>(urpAsset.volumeProfile, VolumeProfilePathNextTo(urpAsset, $"{urpAsset.name}_VolumeProfile"));
            AssignVolumeProfile(urpAsset, profile);
            return profile;
        }

        static bool IsPipelineAssetEditable<T>(UniversalRenderPipelineAsset urpAsset) where T : VolumeComponent
        {
            if (AssetDatabase.MakeEditable(AssetDatabase.GetAssetPath(urpAsset)))
                return true;

            Debug.LogWarning($"URP: The pipeline asset '{AssetLinkUtils.AssetLink(urpAsset)}' is not editable, so its {typeof(T).Name} settings were not migrated. " +
                "Make the asset editable and add the override to its volume profile manually.");
            return false;
        }

        static string VolumeProfilePathNextTo(UniversalRenderPipelineAsset urpAsset, string name)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(urpAsset)).Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
        }

        static VolumeProfile CreateVolumeProfileCopy<T>(VolumeProfile source, string path) where T : VolumeComponent
        {
            var profile = CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            if (source != null)
            {
                foreach (var sourceComponent in source.components)
                {
                    if (sourceComponent == null || sourceComponent is T)
                        continue;

                    var component = Instantiate(sourceComponent);
                    component.name = sourceComponent.name;
                    component.hideFlags = sourceComponent.hideFlags;
                    profile.components.Add(component);
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        static void AssignVolumeProfile(UniversalRenderPipelineAsset urpAsset, VolumeProfile profile)
        {
            urpAsset.volumeProfile = profile;
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssetIfDirty(urpAsset);
        }

        internal static string DescribeVolumeProfileCopy(List<UniversalRenderPipelineAsset> urpAssets, VolumeProfile copy, VolumeProfile source, VolumeProfile defaultProfile)
        {
            return $"{AssetLinks(urpAssets)} now {(urpAssets.Count == 1 ? "uses" : "use")} '{AssetLinkUtils.AssetLink(copy)}', a copy of " +
                (source == defaultProfile ? $"the default volume profile '{AssetLinkUtils.AssetLink(source)}'" : $"'{AssetLinkUtils.AssetLink(source)}'");
        }

        internal static string DescribeRemainingUsers(VolumeProfile source, List<UniversalRenderPipelineAsset> urpAssets)
        {
            return $"'{AssetLinkUtils.AssetLink(source)}' is still used by {AssetLinks(urpAssets)}";
        }

        static string AssetLinks(List<UniversalRenderPipelineAsset> urpAssets)
        {
            var links = new List<string>(urpAssets.Count);
            foreach (var urpAsset in urpAssets)
                links.Add($"'{AssetLinkUtils.AssetLink(urpAsset)}'");
            return string.Join(", ", links);
        }

        internal static bool WriteShadowSettings(VolumeProfile profile, in ShadowSettingsValues values)
        {
            var shadowsOverride = profile.Add<ShadowsVolumeOverride>(overrides: true);
            CopyShadowSettingsToVolumeOverride(values, shadowsOverride);
            if (PersistVolumeOverride(profile, shadowsOverride, "Shadows"))
                return true;

            profile.Remove<ShadowsVolumeOverride>();
            CoreUtils.Destroy(shadowsOverride);
            return false;
        }

        static void CopyShadowSettingsToVolumeOverride(in ShadowSettingsValues values, ShadowsVolumeOverride shadowsOverride)
        {
            shadowsOverride.maxShadowDistance.value = values.maxShadowDistance;
            shadowsOverride.cascadeCount.value = values.cascadeCount;
            shadowsOverride.cascadeSplit0.value = values.split0;
            shadowsOverride.cascadeSplit1.value = values.split1;
            shadowsOverride.cascadeSplit2.value = values.split2;
            shadowsOverride.lastCascadeBorder.value = values.lastCascadeBorder;
        }

#endif // #if UNITY_EDITOR

        #endregion

        /// <summary>Default name when creating an URP Global Settings asset.</summary>
        public const string defaultAssetName = "UniversalRenderPipelineGlobalSettings";

#if UNITY_EDITOR
        internal static string defaultPath => $"Assets/{defaultAssetName}.asset";

        //Making sure there is at least one UniversalRenderPipelineGlobalSettings instance in the project
        internal static UniversalRenderPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            UniversalRenderPipelineGlobalSettings currentInstance = GraphicsSettings.
                GetSettingsForRenderPipeline<UniversalRenderPipeline>() as UniversalRenderPipelineGlobalSettings;

            if (RenderPipelineGlobalSettingsUtils.TryEnsure<UniversalRenderPipelineGlobalSettings, UniversalRenderPipeline>(ref currentInstance, defaultPath, canCreateNewAsset))
            {
                if (currentInstance != null && !currentInstance.IsAtLastVersion())
                {
                    UpgradeAsset(currentInstance.GetEntityId());
                    AssetDatabase.SaveAssetIfDirty(currentInstance);
                }

                return currentInstance;
            }

            return null;
        }

        public override void Initialize(RenderPipelineGlobalSettings source = null)
        {
#pragma warning disable 618 // Type or member is obsolete
            if (source is UniversalRenderPipelineGlobalSettings globalSettingsSource)
                Array.Copy(globalSettingsSource.m_RenderingLayerNames, m_RenderingLayerNames, globalSettingsSource.m_RenderingLayerNames.Length);
#pragma warning restore 618

            // Note: RenderPipelineGraphicsSettings are not populated yet when the global settings asset is being
            // initialized, so create the setting before using it
            var defaultVolumeProfileSettings = GetOrCreateGraphicsSettings<URPDefaultVolumeProfileSettings>(this);
            defaultVolumeProfileSettings.volumeProfile = GetOrCreateDefaultVolumeProfile(defaultVolumeProfileSettings.volumeProfile);
        }

#endif // #if UNITY_EDITOR

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            DecalProjector.UpdateAllDecalProperties();
        }

        internal static VolumeProfile GetOrCreateDefaultVolumeProfile(VolumeProfile defaultVolumeProfile)
        {
#if UNITY_EDITOR
            if (defaultVolumeProfile == null || defaultVolumeProfile.Equals(null))
            {
                const string k_DefaultVolumeProfileName = "DefaultVolumeProfile";
                const string k_DefaultVolumeProfilePath = "Assets/" + k_DefaultVolumeProfileName + ".asset";

                defaultVolumeProfile = CreateInstance<VolumeProfile>();
                Debug.Assert(defaultVolumeProfile);

                defaultVolumeProfile.name = k_DefaultVolumeProfileName;
                AssetDatabase.CreateAsset(defaultVolumeProfile, k_DefaultVolumeProfilePath);

                AssetDatabase.SaveAssetIfDirty(defaultVolumeProfile);
                AssetDatabase.Refresh();

                if (VolumeManager.instance.isInitialized && RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
                    VolumeManager.instance.SetGlobalDefaultProfile(defaultVolumeProfile);
            }
#endif
            return defaultVolumeProfile;
        }

        [SerializeField, FormerlySerializedAs("m_DefaultVolumeProfile")]
        [Obsolete("Kept For Migration. #from(2023.3)", false)]
        internal VolumeProfile m_ObsoleteDefaultVolumeProfile;

        [SerializeField]
        [Obsolete("Kept For Migration. #from(2023.3)", false)]
        internal string[] m_RenderingLayerNames = new string[] { "Default" };

        [SerializeField]
        uint m_ValidRenderingLayers;

        /// <summary>
        /// Names used for display of light layers with Layer's index as prefix.
        /// For example: "0: Light Layer Default"
        /// </summary>
        [Obsolete("This property is obsolete. Use RenderingLayerMask API and Tags & Layers project settings instead. #from(2022.2) #breakingFrom(2023.1)", true)]
        public string[] prefixedLightLayerNames => new string[0];


        #region Light Layer Names [3D]

        /// <summary>Name for light layer 0.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName0;
        /// <summary>Name for light layer 1.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName1;
        /// <summary>Name for light layer 2.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName2;
        /// <summary>Name for light layer 3.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName3;
        /// <summary>Name for light layer 4.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName4;
        /// <summary>Name for light layer 5.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName5;
        /// <summary>Name for light layer 6.</summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string lightLayerName6;
        /// <summary>Name for light layer 7.</summary>
        [Obsolete("This is obsolete, please use renderingLayerNames instead. #from(2022.2)")]
        public string lightLayerName7;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead. #from(2022.2)")]
        public string[] lightLayerNames => new string[0];
#pragma warning disable 618 // Type or member is obsolete
        internal void ResetRenderingLayerNames()
        {
            m_RenderingLayerNames = new string[] { "Default"};
        }
#pragma warning restore 618
        #endregion

#pragma warning disable 618
#pragma warning disable 612
        #region APV
        // This is temporarily here until we have a core place to put it shared between pipelines.
        [SerializeField]
        internal ProbeVolumeSceneData apvScenesData;

        internal ProbeVolumeSceneData GetOrCreateAPVSceneData()
        {
            if (apvScenesData == null)
                apvScenesData = new ProbeVolumeSceneData(this);

            apvScenesData.SetParentObject(this);
            return apvScenesData;
        }
#pragma warning restore 612
#pragma warning restore 618

        #endregion
    }
}
