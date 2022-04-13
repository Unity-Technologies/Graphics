using System;
using UnityEngine.Serialization;
using System.Linq;

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
            MovedSupportRuntimeDebugDisplayToGlobalSettings
        }

        static Version[] skipedStepWhenCreatedFromHDRPAsset = new Version[] { };

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

#if UNITY_EDITOR
        static readonly MigrationDescription<Version, HDRenderPipelineGlobalSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UpdateMSAA, (HDRenderPipelineGlobalSettings data) =>
            {
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultCameraFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings, ref unusedMaskForDefault);
            }),

            MigrationStep.New(Version.UpdateLensFlare, (HDRenderPipelineGlobalSettings data) =>
            {
                FrameSettings.MigrateToLensFlare(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),
            MigrationStep.New(Version.MovedSupportRuntimeDebugDisplayToGlobalSettings, (HDRenderPipelineGlobalSettings data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                var activePipeline = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                if (activePipeline != null)
                {
                    data.supportRuntimeDebugDisplay = activePipeline.currentPlatformRenderPipelineSettings.m_ObsoleteSupportRuntimeDebugDisplay;
                }
#pragma warning restore 618
            })
        );
        bool IMigratableAsset.Migrate()
            => k_Migration.Migrate(this);

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();

        internal static void MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset)
        {
            if (instance != null && !instance.Equals(null) && !(instance.m_Version == Version.First))
                return;

            //1. Create the instance or load current one
            HDRenderPipelineGlobalSettings assetToUpgrade = instance;

            if (assetToUpgrade == null || assetToUpgrade.Equals(null))
            {
                assetToUpgrade = Create($"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");
                UpdateGraphicsSettings(assetToUpgrade);
            }

            Debug.Assert(assetToUpgrade);

            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
#pragma warning disable 618 // Type or member is obsolete
            assetToUpgrade.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetToUpgrade.lookDevVolumeProfile = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetToUpgrade.m_RenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

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

            assetToUpgrade.shaderVariantLogLevel = oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetToUpgrade.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            // we need to make sure the old diffusion profile had time to upgrade before moving it away
            if (oldAsset.diffusionProfileSettings != null)
                oldAsset.diffusionProfileSettings.TryToUpgrade();

            int oldSize = oldAsset.m_ObsoleteDiffusionProfileSettingsList?.Length ?? 0;
            System.Array.Resize(ref assetToUpgrade.diffusionProfileSettingsList, oldSize);
            for (int i = 0; i < oldSize; ++i)
                assetToUpgrade.diffusionProfileSettingsList[i] = oldAsset.m_ObsoleteDiffusionProfileSettingsList[i];
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
