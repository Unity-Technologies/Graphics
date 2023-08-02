using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IVersionable<HDRenderPipelineGlobalSettings.Version>, IMigratableAsset
    {
        // Keep in mind that if there is no HDRenderPipelineGlobalSettings,
        // it can be created from one HDRPAsset in GraphicSettings before version AddedHDRenderPipelineGlobalSettings.
        // When it occurs we force Version to be First again for all migration step to occures again.
        //
        // /!\ If you add data that are not from HDRPAsset and then add a migration pattern on them,
        // don't forget to add your migration step into skipedStepWhenCreatedFromHDRPAsset.
        //
        // /!\ Also for each new version, you must now upgrade asset in HDRP_Runtime, HDRP_Performance and SRP_SmokeTest test project.
        enum Version
        {
            First,
            UpdateMSAA,
            UpdateLensFlare,
            MovedSupportRuntimeDebugDisplayToGlobalSettings,
            DisableAutoRegistration,
            MoveDiffusionProfilesToVolume,
            GenericRenderingLayers,
            SupportRuntimeDebugDisplayToStripRuntimeDebugShaders, 
            EnableAmethystFeaturesByDefault, 
            ShaderStrippingSettings,
            RenderingPathFrameSettings,
        }

        static Version[] skipedStepWhenCreatedFromHDRPAsset = new Version[] { };

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

#if UNITY_EDITOR
        private static readonly MigrationDescription<Version, HDRenderPipelineGlobalSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UpdateMSAA, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                FrameSettings.MigrateMSAA(ref data.m_ObsoleteRenderingPathDefaultCameraFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_ObsoleteRenderingPathDefaultBakedOrCustomReflectionFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_ObsoleteRenderingPathDefaultRealtimeReflectionFrameSettings, ref unusedMaskForDefault);
#pragma warning restore 618
            }),

            MigrationStep.New(Version.UpdateLensFlare, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToLensFlare(ref data.m_ObsoleteRenderingPathDefaultCameraFrameSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.MovedSupportRuntimeDebugDisplayToGlobalSettings, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset activePipeline)
                {
                    data.m_SupportRuntimeDebugDisplay = activePipeline.currentPlatformRenderPipelineSettings.m_ObsoleteSupportRuntimeDebugDisplay;
                }
#pragma warning restore 618
            }),
            MigrationStep.New(Version.DisableAutoRegistration, (HDRenderPipelineGlobalSettings data) =>
            {
                // Field is on for new projects, but disable it for existing projects
                data.autoRegisterDiffusionProfiles = false;
            }),
            MigrationStep.New(Version.MoveDiffusionProfilesToVolume, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (data.m_ObsoleteDiffusionProfileSettingsList.Length == 0)
                    return;

                var volumeProfile = data.GetOrCreateDefaultVolumeProfile();

                #if UNITY_EDITOR
                // Profile from resources is read only in released packages, so we have to copy it to the assets folder
                if (data.IsVolumeProfileFromResources())
                {
                    data.volumeProfile = CopyVolumeProfileFromResourcesToAssets(volumeProfile);
                }

                UnityEditor.AssetDatabase.MakeEditable(UnityEditor.AssetDatabase.GetAssetPath(volumeProfile));
                #endif

                var overrides = data.GetOrCreateDiffusionProfileList();
                foreach (var profile in data.m_ObsoleteDiffusionProfileSettingsList)
                {
                    bool found = false;
                    foreach (var profile2 in overrides.diffusionProfiles.value)
                    {
                        if (profile2 == profile)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        data.AddDiffusionProfile(profile);
                }
#pragma warning restore 618
            }),
            MigrationStep.New(Version.GenericRenderingLayers, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                data.renderingLayerNames = new string[16]
                {
                    data.lightLayerName0,
                    data.lightLayerName1,
                    data.lightLayerName2,
                    data.lightLayerName3,
                    data.lightLayerName4,
                    data.lightLayerName5,
                    data.lightLayerName6,
                    data.lightLayerName7,
                    data.decalLayerName0,
                    data.decalLayerName1,
                    data.decalLayerName2,
                    data.decalLayerName3,
                    data.decalLayerName4,
                    data.decalLayerName5,
                    data.decalLayerName6,
                    data.decalLayerName7,
                };
                data.m_PrefixedRenderingLayerNames = null;

                data.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).SetEnabled(FrameSettingsField.RenderingLayerMaskBuffer, true);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.SupportRuntimeDebugDisplayToStripRuntimeDebugShaders, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                data.stripDebugVariants = !data.m_SupportRuntimeDebugDisplay; // Inversion logic
#pragma warning restore 618
            }),
            MigrationStep.New(Version.EnableAmethystFeaturesByDefault, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateAmethystFeatures(ref data.m_ObsoleteRenderingPathDefaultCameraFrameSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.ShaderStrippingSettings, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                data.m_ShaderStrippingSetting.exportShaderVariants = data.m_ExportShaderVariants;
                data.m_ShaderStrippingSetting.shaderVariantLogLevel = data.m_ShaderVariantLogLevel;
                data.m_ShaderStrippingSetting.stripRuntimeDebugShaders= data.m_StripDebugVariants;
#pragma warning restore 618
            })
            ,
            MigrationStep.New(Version.RenderingPathFrameSettings, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                data.m_RenderingPath.GetDefaultFrameSettings(FrameSettingsRenderType.Camera)                  = data.m_ObsoleteRenderingPathDefaultCameraFrameSettings;
                data.m_RenderingPath.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection) = data.m_ObsoleteRenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                data.m_RenderingPath.GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection)      = data.m_ObsoleteRenderingPathDefaultRealtimeReflectionFrameSettings;
#pragma warning restore 618
            })
        );
        bool IMigratableAsset.Migrate()
            => k_Migration.Migrate(this);

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version >= MigrationDescription.LastVersion<Version>();

        internal static void MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset)
        {
            if (instance != null && !instance.Equals(null) && !(instance.m_Version == Version.First))
                return;

            //1. Create the instance or load current one
            HDRenderPipelineGlobalSettings assetToUpgrade = instance;

            if (assetToUpgrade == null || assetToUpgrade.Equals(null))
            {
                assetToUpgrade = RenderPipelineGlobalSettingsUtils.Create<HDRenderPipelineGlobalSettings>(defaultPath);
                EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(assetToUpgrade);
            }

            Debug.Assert(assetToUpgrade);

            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
#pragma warning disable 618 // Type or member is obsolete
            assetToUpgrade.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetToUpgrade.lookDevVolumeProfile = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetToUpgrade.m_ObsoleteRenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_ObsoleteRenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_ObsoleteRenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

            assetToUpgrade.m_RenderPipelineResources           = oldAsset.m_ObsoleteRenderPipelineResources;
            assetToUpgrade.m_RenderPipelineRayTracingResources = oldAsset.m_ObsoleteRenderPipelineRayTracingResources;

            assetToUpgrade.beforeTransparentCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses);
            assetToUpgrade.beforePostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses);
            assetToUpgrade.afterPostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses);
            assetToUpgrade.beforeTAACustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTAACustomPostProcesses);

            assetToUpgrade.lightLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName0;
            assetToUpgrade.lightLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName1;
            assetToUpgrade.lightLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName2;
            assetToUpgrade.lightLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName3;
            assetToUpgrade.lightLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName4;
            assetToUpgrade.lightLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName5;
            assetToUpgrade.lightLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName6;
            assetToUpgrade.lightLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName7;

            if (oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0 != null)
            {
                assetToUpgrade.decalLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0;
                assetToUpgrade.decalLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName1;
                assetToUpgrade.decalLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName2;
                assetToUpgrade.decalLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName3;
                assetToUpgrade.decalLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName4;
                assetToUpgrade.decalLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName5;
                assetToUpgrade.decalLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName6;
                assetToUpgrade.decalLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName7;
            }

            assetToUpgrade.shaderVariantLogLevel = (ShaderVariantLogLevel) oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetToUpgrade.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            // we need to make sure the old diffusion profile had time to upgrade before moving it away
            if (oldAsset.diffusionProfileSettings != null)
                oldAsset.diffusionProfileSettings.TryToUpgrade();

            int oldSize = oldAsset.m_ObsoleteDiffusionProfileSettingsList?.Length ?? 0;
            System.Array.Resize(ref assetToUpgrade.m_ObsoleteDiffusionProfileSettingsList, oldSize);
            for (int i = 0; i < oldSize; ++i)
                assetToUpgrade.m_ObsoleteDiffusionProfileSettingsList[i] = oldAsset.m_ObsoleteDiffusionProfileSettingsList[i];
#pragma warning restore 618

            //3. Set version to next & Launch remaining of migration
            // If we created it from HDRPAsset, we want to pass it from all migration step that are relevant as copied data are at version First
            assetToUpgrade.m_Version = Version.First; //Step only apply on version older
            for (Version i = Version.First; i <= MigrationDescription.LastVersion<Version>(); ++i)
            {
                if (skipedStepWhenCreatedFromHDRPAsset.Contains(i))
                    continue;

                k_Migration.ExecuteStep(assetToUpgrade, i);
            }
            // Above ExecuteStep will change version. Bring it back to last one.
            assetToUpgrade.m_Version = MigrationDescription.LastVersion<Version>();
            UnityEditor.EditorUtility.SetDirty(assetToUpgrade);
        }

#endif
    }
}
